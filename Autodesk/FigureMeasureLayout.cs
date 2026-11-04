using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;

namespace CivilSpellAI.Autodesk
{
    internal sealed class FigureMeasure
    {
        public string Letter;
        public bool Horizontal;
        public int First;
        public int Last;
        public double Anchor;
        public double Original;
        public string Name;
        public string Description { get { return Name ?? (Horizontal ? "Distancia horizontal" : "Distancia vertical"); } }
    }

    // Coordinate spans are inferred from actual straight contour edges, not hatch pattern strokes.
    // A change stretches only the selected span; intervals outside it keep their lengths.
    internal sealed class FigureMeasureLayout
    {
        public double[] X;
        public double[] Y;
        public readonly List<FigureMeasure> Measures = new List<FigureMeasure>();
        public double Tolerance;
        // Outer left/right/bottom/top, then inner left/right/bottom/top, in local axes.
        public double[] FrameBounds;

        private sealed class Edge
        {
            public bool Horizontal;
            public double Level;
            public double Min;
            public double Max;
        }

        public static FigureMeasureLayout Detect(SelectedFigureSnapshot snapshot)
        {
            var layout = new FigureMeasureLayout { Tolerance = Math.Max(1e-7, Math.Max(snapshot.Width, snapshot.Height) * 1e-6) };
            double tolerance = layout.Tolerance;
            var edges = new List<Edge>();
            foreach (Point2d[] outline in snapshot.Outlines)
            {
                var local = outline.Select(p => ToLocal(snapshot, p)).ToArray();
                for (int index = 1; index < local.Length; index++)
                {
                    Point2d a = local[index - 1], b = local[index];
                    double dx = Math.Abs(a.X - b.X), dy = Math.Abs(a.Y - b.Y);
                    if (dy <= tolerance && dx > tolerance * 10)
                        edges.Add(new Edge { Horizontal = true, Level = (a.Y + b.Y) / 2, Min = Math.Min(a.X, b.X), Max = Math.Max(a.X, b.X) });
                    else if (dx <= tolerance && dy > tolerance * 10)
                        edges.Add(new Edge { Horizontal = false, Level = (a.X + b.X) / 2, Min = Math.Min(a.Y, b.Y), Max = Math.Max(a.Y, b.Y) });
                }
            }
            layout.X = Merge(edges.SelectMany(e => e.Horizontal ? new[] { e.Min, e.Max } : new[] { e.Level }), tolerance);
            layout.Y = Merge(edges.SelectMany(e => e.Horizontal ? new[] { e.Level } : new[] { e.Min, e.Max }), tolerance);
            foreach (Edge edge in edges)
                layout.Add(edge.Horizontal, edge.Min, edge.Max, edge.Level);
            foreach (bool horizontal in new[] { true, false })
            {
                var supports = edges.Where(e => e.Horizontal != horizontal).OrderBy(e => e.Level).ToArray();
                for (int i = 0; i < supports.Length; i++)
                {
                    Edge a = supports[i];
                    double nearest = double.PositiveInfinity;
                    for (int j = i + 1; j < supports.Length; j++)
                    {
                        Edge b = supports[j];
                        double gap = b.Level - a.Level;
                        double low = Math.Max(a.Min, b.Min), high = Math.Min(a.Max, b.Max);
                        if (gap <= tolerance || high - low <= tolerance * 10) continue;
                        bool matchingEnds = Math.Abs(a.Min - b.Min) < tolerance * 3 && Math.Abs(a.Max - b.Max) < tolerance * 3;
                        if (gap <= nearest + tolerance || matchingEnds)
                        {
                            layout.Add(horizontal, a.Level, b.Level, (low + high) / 2);
                            nearest = Math.Min(nearest, gap);
                        }
                    }
                }
            }
            layout.Measures.Sort((a, b) => {
                int axis = b.Horizontal.CompareTo(a.Horizontal);
                if (axis != 0) return axis;
                int start = a.First.CompareTo(b.First);
                return start != 0 ? start : a.Last.CompareTo(b.Last);
            });
            layout.PrioritizeFrame(snapshot, edges);
            for (int index = 0; index < layout.Measures.Count; index++)
                layout.Measures[index].Letter = Letter(index);
            return layout;
        }

        private sealed class FrameRectangle
        {
            public double Left, Right, Bottom, Top;
            public double Area { get { return (Right - Left) * (Top - Bottom); } }
        }
        private void PrioritizeFrame(SelectedFigureSnapshot snapshot, List<Edge> edges)
        {
            var rectangles = new List<FrameRectangle>();
            foreach (Point2d[] outline in snapshot.Outlines)
            {
                var points = outline.Select(p => ToLocal(snapshot, p)).ToArray();
                if (points.Length < 5 || (points[0] - points[points.Length - 1]).Length > Tolerance) continue;
                var rectangle = new FrameRectangle { Left = points.Min(p => p.X), Right = points.Max(p => p.X), Bottom = points.Min(p => p.Y), Top = points.Max(p => p.Y) };
                if (rectangle.Right - rectangle.Left <= Tolerance || rectangle.Top - rectangle.Bottom <= Tolerance) continue;
                if (!points.All(p => (Math.Abs(p.X - rectangle.Left) <= Tolerance || Math.Abs(p.X - rectangle.Right) <= Tolerance) &&
                    (Math.Abs(p.Y - rectangle.Bottom) <= Tolerance || Math.Abs(p.Y - rectangle.Top) <= Tolerance))) continue;
                rectangles.Add(rectangle);
            }
            // Older details often assemble each rectangle from several open polylines.
            // Join collinear spans and recognize the same closed frame from its four sides.
            var merged = new List<Edge>();
            foreach (Edge edge in edges.OrderBy(e => e.Horizontal).ThenBy(e => e.Level).ThenBy(e => e.Min))
            {
                Edge matching = merged.LastOrDefault(e => e.Horizontal == edge.Horizontal && Math.Abs(e.Level - edge.Level) < Tolerance * 3 &&
                    edge.Min <= e.Max + Tolerance && edge.Max >= e.Min - Tolerance);
                if (matching == null) merged.Add(new Edge { Horizontal = edge.Horizontal, Level = edge.Level, Min = edge.Min, Max = edge.Max });
                else { matching.Min = Math.Min(matching.Min, edge.Min); matching.Max = Math.Max(matching.Max, edge.Max); }
            }
            // Pipe openings can interrupt the frame sides. Accept short gaps when most
            // of the straight side is present, while keeping widely separated pieces apart.
            foreach (bool axis in new[] { true, false })
            {
                foreach (double level in Merge(merged.Where(e => e.Horizontal == axis).Select(e => e.Level), Tolerance * 3))
                {
                    var spans = merged.Where(e => e.Horizontal == axis && Math.Abs(e.Level - level) < Tolerance * 3).ToArray();
                    double min = spans.Min(e => e.Min), max = spans.Max(e => e.Max);
                    if (spans.Sum(e => e.Max - e.Min) >= (max - min) * 0.75)
                        merged.Add(new Edge { Horizontal = axis, Level = level, Min = min, Max = max });
                }
            }
            var vertical = merged.Where(e => !e.Horizontal).OrderBy(e => e.Level).Take(300).ToArray();
            var horizontal = merged.Where(e => e.Horizontal).ToArray();
            for (int i = 0; i < vertical.Length && rectangles.Count < 3000; i++)
                for (int j = i + 1; j < vertical.Length && rectangles.Count < 3000; j++)
                {
                    Edge leftEdge = vertical[i], rightEdge = vertical[j];
                    if (rightEdge.Level - leftEdge.Level <= Tolerance) continue;
                    double bottomLimit = Math.Max(leftEdge.Min, rightEdge.Min), topLimit = Math.Min(leftEdge.Max, rightEdge.Max);
                    var levels = Merge(horizontal.Where(e => e.Min <= leftEdge.Level + Tolerance && e.Max >= rightEdge.Level - Tolerance &&
                        e.Level >= bottomLimit - Tolerance && e.Level <= topLimit + Tolerance).Select(e => e.Level), Tolerance);
                    for (int a = 0; a < levels.Length && rectangles.Count < 3000; a++)
                        for (int b = a + 1; b < levels.Length && rectangles.Count < 3000; b++)
                            rectangles.Add(new FrameRectangle { Left = leftEdge.Level, Right = rightEdge.Level, Bottom = levels[a], Top = levels[b] });
                }
            FrameRectangle outer = null, inner = null;
            foreach (FrameRectangle candidate in rectangles.OrderByDescending(r => r.Area))
            {
                FrameRectangle contained = rectangles.Where(r => r.Left > candidate.Left + Tolerance && r.Right < candidate.Right - Tolerance &&
                    r.Bottom > candidate.Bottom + Tolerance && r.Top < candidate.Top - Tolerance).OrderByDescending(r => r.Area).FirstOrDefault();
                if (contained != null) { outer = candidate; inner = contained; break; }
            }
            if (outer == null) return;
            FrameBounds = new[] { outer.Left, outer.Right, outer.Bottom, outer.Top,
                inner.Left, inner.Right, inner.Bottom, inner.Top };
            var ordered = new List<FigureMeasure>();
            var leftLevels = edges.Where(e => e.Horizontal && e.Min < outer.Left - Tolerance && e.Max <= inner.Left + (inner.Left - outer.Left) + Tolerance &&
                e.Level >= inner.Bottom - Tolerance && e.Level <= inner.Top + Tolerance).Select(e => e.Level).ToArray();
            var rightLevels = edges.Where(e => e.Horizontal && e.Min >= inner.Right - Tolerance && e.Max > outer.Right + Tolerance &&
                e.Level >= inner.Bottom - Tolerance && e.Level <= inner.Top + Tolerance).Select(e => e.Level).ToArray();
            FigureMeasure left = Measures.Where(m => !m.Horizontal && leftLevels.Any(v => Math.Abs(v - Y[m.First]) <= Tolerance) &&
                leftLevels.Any(v => Math.Abs(v - Y[m.Last]) <= Tolerance)).OrderBy(m => m.Original).ThenByDescending(m => Y[m.Last]).FirstOrDefault();
            FigureMeasure top = Find(false, inner.Top, outer.Top);
            FigureMeasure opening = Find(true, inner.Left, inner.Right);
            FigureMeasure right = Measures.Where(m => !m.Horizontal && rightLevels.Any(v => Math.Abs(v - Y[m.First]) <= Tolerance) &&
                rightLevels.Any(v => Math.Abs(v - Y[m.Last]) <= Tolerance)).OrderByDescending(m => m.Original).FirstOrDefault();
            Prioritize(ordered, left, "Separación del conducto izquierdo");
            Prioritize(ordered, top, "Espesor superior del marco");
            Prioritize(ordered, opening, "Ancho interior del marco");
            Prioritize(ordered, right, "Alto del conducto derecho");
            ordered.AddRange(Measures.Where(m => !ordered.Contains(m)).ToArray());
            Measures.Clear(); Measures.AddRange(ordered);
        }
        private FigureMeasure Find(bool horizontal, double first, double last)
        {
            double[] values = horizontal ? X : Y;
            return Measures.FirstOrDefault(m => m.Horizontal == horizontal && Math.Abs(values[m.First] - first) < Tolerance * 3 && Math.Abs(values[m.Last] - last) < Tolerance * 3);
        }
        private static void Prioritize(List<FigureMeasure> list, FigureMeasure measure, string name)
        {
            if (measure == null || list.Contains(measure)) return;
            measure.Name = name; list.Add(measure);
        }

        private void Add(bool horizontal, double first, double last, double anchor)
        {
            double[] stations = horizontal ? X : Y;
            int a = Nearest(stations, first), b = Nearest(stations, last);
            if (a < 0 || b <= a || stations[b] - stations[a] <= Tolerance * 10) return;
            if (Measures.Any(m => m.Horizontal == horizontal && m.First == a && m.Last == b)) return;
            Measures.Add(new FigureMeasure { Horizontal = horizontal, First = a, Last = b, Anchor = anchor, Original = stations[b] - stations[a] });
        }

        internal static Point2d ToLocal(SelectedFigureSnapshot snapshot, Point2d point)
        {
            double x = point.X - snapshot.Origin.X, y = point.Y - snapshot.Origin.Y;
            double cos = Math.Cos(snapshot.AxisAngle), sin = Math.Sin(snapshot.AxisAngle);
            return new Point2d(x * cos + y * sin, -x * sin + y * cos);
        }
        internal static Point2d ToWorld(SelectedFigureSnapshot snapshot, Point2d point)
        {
            double cos = Math.Cos(snapshot.AxisAngle), sin = Math.Sin(snapshot.AxisAngle);
            return new Point2d(snapshot.Origin.X + point.X * cos - point.Y * sin,
                snapshot.Origin.Y + point.X * sin + point.Y * cos);
        }
        private static double[] Merge(IEnumerable<double> values, double tolerance)
        {
            var result = new List<double>();
            foreach (double value in values.OrderBy(v => v))
                if (result.Count == 0 || value - result[result.Count - 1] > tolerance) result.Add(value);
            return result.ToArray();
        }
        private int Nearest(double[] stations, double value)
        {
            for (int index = 0; index < stations.Length; index++)
                if (Math.Abs(stations[index] - value) < Tolerance * 3) return index;
            return -1;
        }
        private static string Letter(int index)
        {
            string result = "";
            do { result = (char)('A' + index % 26) + result; index = index / 26 - 1; } while (index >= 0);
            return result;
        }
    }

    internal sealed class FigureMeasureEdit
    {
        public readonly FigureMeasureLayout Layout;
        public readonly double[] X;
        public readonly double[] Y;
        public FigureMeasureEdit(FigureMeasureLayout layout)
        {
            Layout = layout;
            X = (double[])layout.X.Clone();
            Y = (double[])layout.Y.Clone();
        }
        public bool Changed { get { return X.Where((v, i) => Math.Abs(v - Layout.X[i]) > Layout.Tolerance).Any() ||
            Y.Where((v, i) => Math.Abs(v - Layout.Y[i]) > Layout.Tolerance).Any(); } }
        public double Value(FigureMeasure measure)
        {
            double[] values = measure.Horizontal ? X : Y;
            return values[measure.Last] - values[measure.First];
        }
        public void Set(FigureMeasure measure, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= Layout.Tolerance * 2)
                throw new ArgumentException("La medida debe ser positiva.");
            double[] target = measure.Horizontal ? X : Y;
            double[] values = (double[])target.Clone();
            double previous = Value(measure), origin = values[measure.First];
            for (int index = measure.First + 1; index < values.Length; index++)
                values[index] = index <= measure.Last ? origin + (values[index] - origin) * value / previous : values[index] + value - previous;
            for (int index = 1; index < values.Length; index++)
                if (values[index] - values[index - 1] <= Layout.Tolerance)
                    throw new ArgumentException("La medida es demasiado pequeña: superpone bordes interiores.");
            Array.Copy(values, target, values.Length);
        }
        public Point2d Map(Point2d local) { return new Point2d(MapAxis(local.X, Layout.X, X), MapAxis(local.Y, Layout.Y, Y)); }
        public Point3d MapWorld(SelectedFigureSnapshot snapshot, Point3d world)
        {
            Point2d point = FigureMeasureLayout.ToWorld(snapshot, Map(FigureMeasureLayout.ToLocal(snapshot, new Point2d(world.X, world.Y))));
            return new Point3d(point.X, point.Y, world.Z);
        }
        private static double MapAxis(double value, double[] source, double[] destination)
        {
            if (source.Length == 0) return value;
            if (value <= source[0]) return value + destination[0] - source[0];
            for (int index = 1; index < source.Length; index++)
                if (value <= source[index]) return destination[index - 1] + (value - source[index - 1]) *
                    (destination[index] - destination[index - 1]) / (source[index] - source[index - 1]);
            return value + destination[destination.Length - 1] - source[source.Length - 1];
        }
    }
}
