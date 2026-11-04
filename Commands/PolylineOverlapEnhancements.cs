using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CivilSpellAI.Commands
{
    public sealed partial class PolylineOverlapCommands
    {
        internal sealed class ArcPart
        {
            public Point3d Center;
            public Vector3d X, Normal;
            public double Radius, Sweep;
            public Vector3d Y { get { return Normal.CrossProduct(X); } }
            public Point3d At(double angle) { return Center + Radius * (X * Math.Cos(angle) + Y * Math.Sin(angle)); }
            public List<Point3d> BoundsPoints()
            {
                var points = new List<Point3d> { At(0), At(Sweep) };
                double[] x = { X.X, X.Y, X.Z }, y = { Y.X, Y.Y, Y.Z };
                for (int i = 0; i < 3; i++)
                    for (int k = 0; k < 2; k++)
                    {
                        double angle = Normalize(Math.Atan2(y[i], x[i]) + k * Math.PI);
                        if (angle <= Sweep) points.Add(At(angle));
                    }
                return points;
            }
        }

        private static double Normalize(double value)
        {
            double period = 2 * Math.PI;
            return ((value % period) + period) % period;
        }

        internal static ArcPart ReadArc(Point3d a, Point3d b, Vector3d normal, double bulge)
        {
            Vector3d chord = b - a;
            if (chord.Length == 0 || bulge == 0) return null;
            Point3d center = a + chord * 0.5 + normal.CrossProduct(chord) * ((1 / bulge - bulge) / 4);
            double radius = a.DistanceTo(center);
            if (radius == 0 || double.IsInfinity(radius) || double.IsNaN(radius)) return null;
            return new ArcPart { Center = center, Radius = radius, X = (a - center) / radius,
                Normal = bulge > 0 ? normal : -normal, Sweep = 4 * Math.Atan(Math.Abs(bulge)) };
        }

        private static List<double[]> Intervals(double start, double sweep)
        {
            start = Normalize(start);
            double end = start + sweep, period = 2 * Math.PI;
            return end <= period ? new List<double[]> { new[] { start, end } } :
                new List<double[]> { new[] { start, period }, new[] { 0.0, end - period } };
        }

        internal static List<Hit> ArcOverlaps(Segment a, Segment b, double tolerance)
        {
            var result = new List<Hit>();
            ArcPart p = a.Arc, q = b.Arc;
            if (p.Center.DistanceTo(q.Center) + Math.Abs(p.Radius - q.Radius) > tolerance ||
                p.Normal.CrossProduct(q.Normal).Length * Math.Max(p.Radius, q.Radius) > tolerance) return result;
            Vector3d startVector = q.At(0) - p.Center;
            double start = Math.Atan2(startVector.DotProduct(p.Y), startVector.DotProduct(p.X));
            if (p.Normal.DotProduct(q.Normal) < 0) start -= q.Sweep;
            foreach (double[] interval in Intervals(start, q.Sweep))
            {
                double low = Math.Max(0, interval[0]), high = Math.Min(p.Sweep, interval[1]);
                if ((high - low) * p.Radius <= tolerance) continue;
                var arc = new ArcPart { Center = p.Center, Radius = p.Radius, Normal = p.Normal,
                    X = (p.At(low) - p.Center).GetNormal(), Sweep = high - low };
                result.Add(new Hit { Owner = a.Owner, Other = b.Owner, First = a.Index, Second = b.Index,
                    A = arc.At(0), B = arc.At(arc.Sweep), Arc = arc });
            }
            return result;
        }

        internal sealed class MarkerGroup
        {
            public readonly List<Hit> Hits = new List<Hit>();
            public Point3d Center;
            public double Radius;
        }

        private static List<Point3d> HitPoints(Hit hit)
        {
            return hit.Arc == null ? new List<Point3d> { hit.A, hit.B } : hit.Arc.BoundsPoints();
        }

        private static Point3d HitCenter(Hit hit)
        {
            return hit.Arc == null ? hit.A + (hit.B - hit.A) * 0.5 : hit.Arc.At(hit.Arc.Sweep / 2);
        }

        internal static List<MarkerGroup> GroupHits(List<Hit> hits, double radius)
        {
            var groups = new List<MarkerGroup>();
            foreach (Hit hit in hits)
            {
                Point3d anchor = HitCenter(hit);
                var group = groups.FirstOrDefault(g => g.Hits[0].IsShort == hit.IsShort && g.Hits[0].IsInvalid == hit.IsInvalid && g.Center.DistanceTo(anchor) <= radius);
                if (group == null) { group = new MarkerGroup { Center = anchor }; groups.Add(group); }
                group.Hits.Add(hit);
            }
            foreach (var group in groups)
            {
                var points = group.Hits.SelectMany(HitPoints).ToList();
                var min = new Point3d(points.Min(p => p.X), points.Min(p => p.Y), points.Min(p => p.Z));
                var max = new Point3d(points.Max(p => p.X), points.Max(p => p.Y), points.Max(p => p.Z));
                group.Center = min + (max - min) * 0.5;
                group.Radius = Math.Max(radius, min.DistanceTo(max) * 0.55);
            }
            return groups;
        }

        private const string HistoryKey = "CIVILSPELLAI_OVERLAP_HISTORY_V3";
        private static string SelectionScope(ObjectId[] ids)
        {
            string handles = string.Join(",", ids.Select(id => id.Handle.ToString()).OrderBy(s => s, StringComparer.Ordinal));
            using (var hash = System.Security.Cryptography.SHA256.Create())
                return "Seleccion:" + BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(handles)));
        }
        private static void CompareAndRemember(Database database, Editor editor, string scope, double tolerance,
            int overlaps, int shortCount, int invalidCount, bool incomplete)
        {
            using (var transaction = database.TransactionManager.StartTransaction())
            {
                var dictionary = (DBDictionary)transaction.GetObject(database.NamedObjectsDictionaryId, OpenMode.ForRead);
                Xrecord record;
                if (dictionary.Contains(HistoryKey)) record = (Xrecord)transaction.GetObject(dictionary.GetAt(HistoryKey), OpenMode.ForWrite);
                else
                {
                    dictionary.UpgradeOpen(); record = new Xrecord();
                    dictionary.SetAt(HistoryKey, record); transaction.AddNewlyCreatedDBObject(record, true);
                }
                using (var previous = record.Data)
                {
                    var values = previous == null ? null : previous.AsArray();
                    if (values != null && values.Length == 6 && (string)values[0].Value == scope &&
                        (double)values[1].Value == tolerance && (int)values[4].Value == 0 && !incomplete)
                    {
                        int oldOverlap = (int)values[2].Value, oldShort = (int)values[3].Value;
                        editor.WriteMessage("\nANTES -> AHORA: coincidencias {0} -> {1}; casi nulos {2} -> {3}.", oldOverlap, overlaps, oldShort, shortCount);
                        editor.WriteMessage("\nGeometria invalida: {0} -> {1}. Comparacion de conteos, no certifica correcciones individuales.", values[5].Value, invalidCount);
                        if ((int)values[5].Value == 0 && invalidCount == 0)
                            editor.WriteMessage("\nReduccion neta: coincidencias {0}; casi nulos {1}. No atribuye cambios a OVERKILL.", oldOverlap - overlaps, oldShort - shortCount);
                        else editor.WriteMessage("\nComparacion con geometria invalida: la revision de superposiciones es incompleta.");
                    }
                    else editor.WriteMessage("\nNueva referencia antes/despues: no hay revision completa comparable con el mismo alcance y tolerancia.");
                }
                using (var data = new ResultBuffer(new TypedValue(1, scope), new TypedValue(40, tolerance),
                    new TypedValue(90, overlaps), new TypedValue(90, shortCount), new TypedValue(90, incomplete ? 1 : 0), new TypedValue(90, invalidCount))) record.Data = data;
                transaction.Commit();
            }
        }

        [CommandMethod(ShowCommandName, CommandFlags.Modal)]
        public void ShowMarks()
        {
            var doc = AcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var options = new PromptKeywordOptions("\nMostrar marcas: ") { AllowNone = true };
            foreach (string item in new[] { "Todas", "Superpuestas", "Nulos", "Invalidas", "Ocultar" }) options.Keywords.Add(item);
            options.Keywords.Default = "Todas";
            var answer = doc.Editor.GetKeywords(options);
            if (answer.Status == PromptStatus.Cancel) return;
            SetMarkerVisibility(doc.Database, answer.StringResult);
            doc.Editor.Regen();
        }

        private static void SetMarkerVisibility(Database database, string visibility)
        {
            using (var transaction = database.TransactionManager.StartTransaction())
            {
                var table = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForRead);
                foreach (string name in new[] { OverlapLayer, ShortLayer, InvalidLayer })
                {
                    if (!table.Has(name)) continue;
                    var layer = (LayerTableRecord)transaction.GetObject(table[name], OpenMode.ForWrite);
                    string selected = visibility == "Superpuestas" ? OverlapLayer : visibility == "Nulos" ? ShortLayer : InvalidLayer;
                    layer.IsOff = visibility != "Todas" && (visibility == "Ocultar" || name != selected);
                }
                transaction.Commit();
            }
        }

#if DEBUG
        private static Segment TestArc(double start, double sweep, bool reverse = false, double elevation = 0)
        {
            var arc = new ArcPart { Center = new Point3d(0, 0, elevation), Radius = 10,
                X = new Vector3d(Math.Cos(start), Math.Sin(start), 0),
                Normal = reverse ? -Vector3d.ZAxis : Vector3d.ZAxis, Sweep = sweep };
            return new Segment { A = arc.At(0), B = arc.At(sweep), Arc = arc };
        }

        private static void TestArcGeometry()
        {
            double pi = Math.PI, tolerance = 0.000001;
            var a = TestArc(0, pi);
            var b = TestArc(pi / 2, pi);
            var hits = ArcOverlaps(a, b, tolerance);
            if (hits.Count != 1 || Math.Abs(hits[0].Length - 5 * pi) > tolerance) throw new InvalidOperationException("Partial arcs");
            hits = ArcOverlaps(a, TestArc(pi, pi, true), tolerance);
            if (hits.Count != 1 || Math.Abs(hits[0].Length - 10 * pi) > tolerance) throw new InvalidOperationException("Reverse arc");
            if (ArcOverlaps(a, TestArc(pi, pi), tolerance).Count != 0) throw new InvalidOperationException("Touching arcs");
            if (ArcOverlaps(a, TestArc(0, pi, false, 1), tolerance).Count != 0) throw new InvalidOperationException("Arc elevation");
            hits = ArcOverlaps(TestArc(0, 1.8 * pi), TestArc(pi, 1.8 * pi), tolerance);
            if (hits.Count != 2) throw new InvalidOperationException("Wrapped arc intervals");
            var tilted = TestArc(0, pi);
            tilted.Arc.Normal = Vector3d.YAxis;
            if (ArcOverlaps(a, tilted, tolerance).Count != 0) throw new InvalidOperationException("Arc planes");
            var parsed = ReadArc(new Point3d(10, 0, 0), new Point3d(-10, 0, 0), Vector3d.ZAxis, 1);
            if (parsed == null || parsed.Center.DistanceTo(Point3d.Origin) > tolerance || parsed.At(pi / 2).DistanceTo(new Point3d(0, 10, 0)) > tolerance)
                throw new InvalidOperationException("Bulge conversion");
            parsed = ReadArc(new Point3d(-10, 0, 0), new Point3d(10, 0, 0), Vector3d.ZAxis, -1);
            if (parsed.At(pi / 2).DistanceTo(new Point3d(0, 10, 0)) > tolerance) throw new InvalidOperationException("Negative bulge");
            bool limited;
            if (FindOverlaps(new List<Segment> { a, b }, tolerance, out limited).Count != 1 || limited)
                throw new InvalidOperationException("Arc sweep bounds");
            var close = new List<Hit> {
                new Hit { A = Point3d.Origin, B = Point3d.Origin },
                new Hit { A = new Point3d(0.5, 0, 0), B = new Point3d(0.5, 0, 0) },
                new Hit { A = Point3d.Origin, B = Point3d.Origin, IsShort = true } };
            var groups = GroupHits(close, 1);
            if (groups.Count != 2 || groups[0].Hits.Count != 2) throw new InvalidOperationException("Grouping types");
        }
#endif
    }
}
