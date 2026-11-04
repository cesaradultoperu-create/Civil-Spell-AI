using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using CivilSpellAI.Geometry;

namespace CivilSpellAI.Autodesk
{
    internal sealed class Figure3DReferenceAnalyzer
    {
        private sealed class Span { public double First, Last; }
        private sealed class FrontProfile
        {
            public double Left, InnerLeft, InnerRight, Right, Bottom, Floor;
        }

        public bool TryAnalyze(SelectedFigureSnapshot top, SelectedFigureSnapshot front, FigureMeasureEdit edit,
            out Figure3DParameters parameters, out string error)
        {
            parameters = null; error = null;
            if (top.SourceIds != null && front.SourceIds != null && top.SourceIds.Intersect(front.SourceIds).Any())
            { error = "FRONT incluye objetos que ya seleccionó en TOP. Seleccione solo la elevación de la misma caja, con sus contornos completos."; return false; }
            if (top.Measures == null || top.Measures.FrameBounds == null || top.ApproximatePreview || front.ApproximatePreview)
            { error = "TOP debe contener un marco rectangular exterior e interior reconocible."; return false; }
            var bounds = top.Measures.FrameBounds;
            double tolerance = Math.Max(top.Measures.Tolerance, front.Measures.Tolerance) * 4;
            var frontLines = front.Outlines.Select(line => line.Select(p => FigureMeasureLayout.ToLocal(front, p)).ToArray()).ToArray();
            var profile = frontLines.Select(line => ReadProfile(line, tolerance))
                .Where(item => item != null && Math.Abs((item.Right - item.Left) - (bounds[1] - bounds[0])) <= tolerance)
                .OrderByDescending(item => item.Floor - item.Bottom).FirstOrDefault();
            if (profile == null)
            { error = "FRONT debe contener el perfil en U de la misma caja y coincidir con el ancho exterior de TOP. Seleccione contornos completos, sin otras vistas."; return false; }
            double leftTop = HighestCap(frontLines, profile.Left, profile.InnerLeft, tolerance);
            double rightTop = HighestCap(frontLines, profile.InnerRight, profile.Right, tolerance);
            if (double.IsNaN(leftTop) || double.IsNaN(rightTop) || Math.Abs(leftTop - rightTop) > tolerance)
            { error = "No se reconoció una coronación común en FRONT. Este modelo requiere una caja rectangular con muros de la misma altura."; return false; }
            Func<Point2d, Point2d> map = p => edit == null ? p : edit.Map(p);
            Point2d outerLow = map(new Point2d(bounds[0], bounds[2])), outerHigh = map(new Point2d(bounds[1], bounds[3]));
            Point2d innerLow = map(new Point2d(bounds[4], bounds[6])), innerHigh = map(new Point2d(bounds[5], bounds[7]));
            var result = new Figure3DParameters {
                Width = outerHigh.X - outerLow.X, Depth = outerHigh.Y - outerLow.Y,
                Height = leftTop - profile.Bottom, BaseThickness = profile.Floor - profile.Bottom,
                LeftWall = innerLow.X - outerLow.X, RightWall = outerHigh.X - innerHigh.X,
                FrontWall = innerLow.Y - outerLow.Y, BackWall = outerHigh.Y - innerHigh.Y };
            var topLines = top.Outlines.Select(line => line.Select(p => FigureMeasureLayout.ToLocal(top, p)).ToArray()).ToArray();
            result.Left = ReadPassage(topLines, bounds[0], bounds[4], bounds[6], bounds[7], frontLines,
                profile.Left, profile.InnerLeft, profile, leftTop, outerLow.Y, map, tolerance);
            result.Right = ReadPassage(topLines, bounds[5], bounds[1], bounds[6], bounds[7], frontLines,
                profile.InnerRight, profile.Right, profile, rightTop, outerLow.Y, map, tolerance);
            try { result.Validate(); }
            catch (ArgumentException exception) { error = exception.Message; return false; }
            parameters = result;
            return true;
        }

        private static FrontProfile ReadProfile(Point2d[] points, double tolerance)
        {
            if (points.Length < 9 || (points[0] - points[points.Length - 1]).Length > tolerance) return null;
            var xs = Distinct(points.Select(p => p.X), tolerance);
            if (xs.Length != 4) return null;
            double bottom = points.Min(p => p.Y);
            double floor = double.NaN;
            bool outerBase = false;
            for (int i = 1; i < points.Length; i++)
            {
                Point2d a = points[i - 1], b = points[i];
                if (Math.Abs(a.X - b.X) > tolerance && Math.Abs(a.Y - b.Y) > tolerance) return null;
                if (Math.Abs(a.Y - b.Y) > tolerance) continue;
                double min = Math.Min(a.X, b.X), max = Math.Max(a.X, b.X);
                if (Math.Abs(min - xs[0]) < tolerance && Math.Abs(max - xs[3]) < tolerance && Math.Abs(a.Y - bottom) < tolerance)
                    outerBase = true;
                if (Math.Abs(min - xs[1]) < tolerance && Math.Abs(max - xs[2]) < tolerance && a.Y > bottom + tolerance)
                    floor = double.IsNaN(floor) ? a.Y : Math.Min(floor, a.Y);
            }
            if (!outerBase || double.IsNaN(floor) || points.Max(p => p.Y) <= floor + tolerance) return null;
            return new FrontProfile { Left = xs[0], InnerLeft = xs[1], InnerRight = xs[2], Right = xs[3], Bottom = bottom, Floor = floor };
        }
        private static double HighestCap(Point2d[][] lines, double left, double right, double tolerance)
        {
            var heights = new List<double>();
            foreach (var line in lines)
                for (int i = 1; i < line.Length; i++)
                {
                    Point2d a = line[i - 1], b = line[i];
                    if (Math.Abs(a.Y - b.Y) < tolerance && Math.Abs(Math.Min(a.X, b.X) - left) < tolerance &&
                        Math.Abs(Math.Max(a.X, b.X) - right) < tolerance) heights.Add((a.Y + b.Y) / 2);
                }
            return heights.Count == 0 ? double.NaN : heights.Max();
        }
        private static Figure3DPassage ReadPassage(Point2d[][] topLines, double topA, double topB, double topLow, double topHigh,
            Point2d[][] frontLines, double frontA, double frontB, FrontProfile profile, double high,
            double topOriginY, Func<Point2d, Point2d> map, double tolerance)
        {
            var planGaps = CommonGaps(topLines, topA, topB, topLow, topHigh, tolerance);
            var elevationGaps = CommonGaps(frontLines, frontA, frontB, profile.Floor, high, tolerance);
            // A plan/elevation can draw the inside face continuously through a low opening.
            // Retain its elevation from the outside face; an unspecified plan width starts
            // at the owner's agreed editable value of 0.10 drawing units.
            if (elevationGaps.Count == 0)
            {
                double exterior = Math.Abs(frontA - profile.Left) < tolerance ? frontA : frontB;
                elevationGaps = Gaps(frontLines, exterior, profile.Floor, high, tolerance)
                    .Where(gap => gap.First > profile.Floor + tolerance && gap.Last < high - tolerance).ToList();
            }
            if (elevationGaps.Count != 1) return new Figure3DPassage();
            if (planGaps.Count != 1) return new Figure3DPassage {
                Enabled = true, Width = Figure3DPassage.InitialWidth,
                Height = elevationGaps[0].Last - elevationGaps[0].First,
                Bottom = elevationGaps[0].First - profile.Bottom,
                Center = map(new Point2d(topA, (topLow + topHigh) / 2)).Y - topOriginY };
            double lowY = map(new Point2d(topA, planGaps[0].First)).Y;
            double highY = map(new Point2d(topA, planGaps[0].Last)).Y;
            return new Figure3DPassage { Enabled = true, Width = highY - lowY, Center = (highY + lowY) / 2 - topOriginY,
                Height = elevationGaps[0].Last - elevationGaps[0].First, Bottom = elevationGaps[0].First - profile.Bottom };
        }
        private static List<Span> CommonGaps(Point2d[][] lines, double a, double b, double low, double high, double tolerance)
        {
            var result = new List<Span>();
            foreach (Span first in Gaps(lines, a, low, high, tolerance))
                foreach (Span second in Gaps(lines, b, low, high, tolerance))
                {
                    double start = Math.Max(first.First, second.First), end = Math.Min(first.Last, second.Last);
                    if (end - start > tolerance && start > low + tolerance && end < high - tolerance)
                        result.Add(new Span { First = start, Last = end });
                }
            return result;
        }
        private static List<Span> Gaps(Point2d[][] lines, double x, double low, double high, double tolerance)
        {
            var spans = new List<Span>();
            foreach (var line in lines)
                for (int i = 1; i < line.Length; i++)
                {
                    Point2d a = line[i - 1], b = line[i];
                    if (Math.Abs(a.X - x) < tolerance && Math.Abs(b.X - x) < tolerance && Math.Abs(b.Y - a.Y) > tolerance)
                        spans.Add(new Span { First = Math.Min(a.Y, b.Y), Last = Math.Max(a.Y, b.Y) });
                }
            var gaps = new List<Span>(); double cursor = low;
            foreach (Span span in spans.OrderBy(item => item.First))
            {
                double first = Math.Max(low, span.First), last = Math.Min(high, span.Last);
                if (last <= cursor + tolerance) continue;
                if (first > cursor + tolerance) gaps.Add(new Span { First = cursor, Last = first });
                cursor = Math.Max(cursor, last);
            }
            if (cursor < high - tolerance) gaps.Add(new Span { First = cursor, Last = high });
            return gaps;
        }
        private static double[] Distinct(IEnumerable<double> values, double tolerance)
        {
            var result = new List<double>();
            foreach (double value in values.OrderBy(v => v))
                if (result.Count == 0 || value - result[result.Count - 1] > tolerance) result.Add(value);
            return result.ToArray();
        }
    }
}
