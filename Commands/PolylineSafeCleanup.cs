using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace CivilSpellAI.Commands
{
    public sealed partial class PolylineOverlapCommands
    {
        private sealed class VertexBackup
        {
            public ObjectId Id;
            public List<VertexData> Before, After;
            public bool Closed;
            public double Elevation;
            public Vector3d Normal;
        }
        private sealed class CleanupBackup
        {
            public readonly List<VertexBackup> Items = new List<VertexBackup>();
        }
#if CIVILSPELLAI_OVERLAP2023
        private const string BackupKey = "CivilSpellAI.TramosCleanupBackup";
#else
        private const string BackupKey = "CivilSpellAI.PolylineCleanupBackup";
#endif
        private sealed class VertexData
        {
            public Point2d Point;
            public double Bulge, StartWidth, EndWidth;
            public int OriginalIndex;
        }

        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        private static bool Plain(VertexData vertex)
        { return vertex.Bulge == 0 && vertex.StartWidth == 0 && vertex.EndWidth == 0; }

        private static List<VertexData> ReadVertices(Polyline polyline)
        {
            var vertices = new List<VertexData>();
            for (int i = 0; i < polyline.NumberOfVertices; i++) vertices.Add(new VertexData {
                Point = polyline.GetPoint2dAt(i), Bulge = polyline.GetBulgeAt(i),
                StartWidth = polyline.GetStartWidthAt(i), EndWidth = polyline.GetEndWidthAt(i), OriginalIndex = i });
            return vertices;
        }

        // Collapse only straight tiny edges. A nonfinite bulge is repairable only
        // on EXACTLY coincident endpoints; we never guess the shape of a real arc.
        private static int CollapseTinyEdges(List<VertexData> vertices, bool closed, double tolerance)
        {
            int removed = 0, minimum = closed ? 3 : 2;
            for (int i = 0; i < vertices.Count - 1 && vertices.Count > minimum;)
            {
                var a = vertices[i]; var b = vertices[i + 1];
                double distance = a.Point.GetDistanceTo(b.Point);
                bool exact = distance == 0;
                bool candidate = distance <= tolerance && (a.Bulge == 0 || (!Finite(a.Bulge) && exact));
                bool endpoint = !closed && i + 1 == vertices.Count - 1;
                if (!candidate || (!exact && (!Plain(a) || !Plain(b) || endpoint))) { i++; continue; }
                a.Bulge = b.Bulge; a.StartWidth = b.StartWidth; a.EndWidth = b.EndWidth;
                vertices.RemoveAt(i + 1); removed++;
                // i stays fixed: prevents cumulative drift across a chain of tiny edges.
            }
            while (closed && vertices.Count > minimum)
            {
                var last = vertices[vertices.Count - 1]; var first = vertices[0];
                double distance = last.Point.GetDistanceTo(first.Point);
                bool exact = distance == 0;
                if (distance > tolerance || !(last.Bulge == 0 || (!Finite(last.Bulge) && exact)) ||
                    (!exact && (!Plain(last) || !Plain(first) || !Plain(vertices[vertices.Count - 2])))) break;
                vertices.RemoveAt(vertices.Count - 1); removed++;
            }
            return removed;
        }

        // Check every original point against the final segment, not successive
        // local deletions: errors must not accumulate along a nearly straight run.
        // Remove redundant forward vertices as well as contained backtracking.
        private static int SimplifyStraightRuns(List<VertexData> vertices, double tolerance, bool closed = false)
        {
            if (closed)
            {
                // Start at a genuine corner (or protected edge), then append the
                // first vertex so that the closing segment is checked as well.
                int anchor = -1;
                for (int i = 0; i < vertices.Count; i++)
                {
                    var previous = vertices[(i + vertices.Count - 1) % vertices.Count];
                    var current = vertices[i];
                    var next = vertices[(i + 1) % vertices.Count];
                    var chord = next.Point - previous.Point;
                    var offset = current.Point - previous.Point;
                    if (!Plain(previous) || !Plain(current) || chord.Length <= tolerance)
                    { anchor = i; break; }
                    var direction = chord / chord.Length;
                    double projection = offset.DotProduct(direction);
                    if (projection < 0 || projection > chord.Length ||
                        Math.Abs(offset.X * direction.Y - offset.Y * direction.X) > tolerance)
                    { anchor = i; break; }
                }
                if (anchor < 0) return 0; // Do not reconstruct a degenerate ring.
                var ring = Enumerable.Range(0, vertices.Count)
                    .Select(i => vertices[(anchor + i) % vertices.Count]).ToList();
                ring.Add(ring[0]);
                int count = SimplifyStraightRuns(ring, tolerance);
                if (count == 0 || ring.Count < 4) return 0;
                ring.RemoveAt(ring.Count - 1);
                // Keep the original start whenever it survived simplification.
                int originalStart = ring.IndexOf(vertices[0]);
                if (originalStart > 0)
                    ring = ring.Skip(originalStart).Concat(ring.Take(originalStart)).ToList();
                vertices.Clear(); vertices.AddRange(ring);
                return count;
            }
            int removed = 0;
            for (int start = 0; start + 2 < vertices.Count; start++)
            {
                if (!Plain(vertices[start])) continue;
                Vector2d delta = vertices[start + 1].Point - vertices[start].Point;
                if (delta.Length <= tolerance) continue;
                Vector2d unit = delta / delta.Length;
                int end = start + 1;
                while (end + 1 < vertices.Count && Plain(vertices[end]))
                {
                    Vector2d offset = vertices[end + 1].Point - vertices[start].Point;
                    if (Math.Abs(offset.X * unit.Y - offset.Y * unit.X) > tolerance) break;
                    end++;
                }
                if (end < start + 2) continue;
                Vector2d total = vertices[end].Point - vertices[start].Point;
                double length = total.Length;
                if (length <= tolerance) { start = end - 1; continue; }
                Vector2d direction = total / length;
                bool safe = true;
                for (int j = start + 1; j < end; j++)
                {
                    Vector2d offset = vertices[j].Point - vertices[start].Point;
                    double projection = offset.DotProduct(direction);
                    if (projection < 0 || projection > length || Math.Abs(offset.X * direction.Y - offset.Y * direction.X) > tolerance)
                    { safe = false; break; }
                }
                if (!safe) { start = end - 1; continue; }
                int n = end - start - 1;
                vertices.RemoveRange(start + 1, n); removed += n;
            }
            return removed;
        }

        private sealed class CleanupResult
        {
            public int Changed, Removed, Skipped;
            public double MaxDisplacement;
        }

        // Check all ORIGINAL points in each replaced run against its final chord.
        // This also catches accumulated displacement from tiny-edge collapse plus simplification.
        private static bool ValidateCleanupDisplacement(List<VertexData> before, List<VertexData> after,
            bool closed, double limit, out double maximum)
        {
            maximum = 0;
            int n = before.Count, edges = closed ? after.Count : after.Count - 1;
            for (int i = 0; i < edges; i++)
            {
                var a = after[i]; var b = after[(i + 1) % after.Count];
                int steps = (b.OriginalIndex - a.OriginalIndex + n) % n;
                if (steps == 0 || (!closed && b.OriginalIndex <= a.OriginalIndex)) return false;
                if (steps == 1) continue;
                if (!Plain(a))
                {
                    // Exactly duplicated starts can precede an intact arc/width.
                    for (int j = 1; j < steps; j++)
                        if (before[(a.OriginalIndex + j) % n].Point != a.Point) return false;
                    continue;
                }
                var chord = b.Point - a.Point;
                double squared = chord.DotProduct(chord);
                for (int j = 1; j < steps; j++)
                {
                    var point = before[(a.OriginalIndex + j) % n].Point;
                    var delta = point - a.Point;
                    double t = squared == 0 ? 0 : Math.Max(0, Math.Min(1, delta.DotProduct(chord) / squared));
                    double deviation = point.GetDistanceTo(a.Point + chord * t);
                    if (!Finite(deviation) || deviation > limit + 1e-9) return false;
                    maximum = Math.Max(maximum, deviation);
                }
            }
            return true;
        }

        private static CleanupResult SafeCleanup(Database database, ObjectId[] ids, double tolerance, double? shortTolerance = null)
        {
            var result = new CleanupResult();
            double tinyLimit = Math.Max(tolerance, shortTolerance ?? tolerance);
            var backup = new CleanupBackup();
            using (var transaction = database.TransactionManager.StartTransaction())
            {
                var table = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
                foreach (ObjectId id in ids)
                {
                    if (!id.IsValid || id.IsErased) continue;
                    var entity = transaction.GetObject(id, OpenMode.ForRead) as Entity;
                    if (!IsSourcePolyline(entity, table[BlockTableRecord.ModelSpace])) continue;
                    var source = (Polyline)entity;
                    var layer = (LayerTableRecord)transaction.GetObject(source.LayerId, OpenMode.ForRead);
                    // Associative/reactor-linked geometry needs a dedicated workflow.
                    if (layer.IsLocked || source.GetPersistentReactorIds().Count > 0 || source.ConstantWidth != 0)
                    { result.Skipped++; continue; }
                    var vertices = ReadVertices(source);
                    var before = ReadVertices(source);
                    int removed = CollapseTinyEdges(vertices, source.Closed, tinyLimit);
                    // A closed contour must keep at least three vertices.
                    var simplified = vertices.Select(v => new VertexData { Point = v.Point, Bulge = v.Bulge,
                        StartWidth = v.StartWidth, EndWidth = v.EndWidth, OriginalIndex = v.OriginalIndex }).ToList();
                    int simplifiedCount = SimplifyStraightRuns(simplified, tolerance, source.Closed);
                    if (simplified.Count >= (source.Closed ? 3 : 2)) { vertices = simplified; removed += simplifiedCount; }
                    if (removed == 0) continue;
                    if (vertices.Any(v => !Finite(v.Point.X) || !Finite(v.Point.Y) || !Finite(v.Bulge) ||
                        !Finite(v.StartWidth) || !Finite(v.EndWidth))) { result.Skipped++; continue; }
                    double displacement;
                    if (!ValidateCleanupDisplacement(before, vertices, source.Closed, tinyLimit, out displacement))
                    { result.Skipped++; continue; }
                    using (var candidate = (Polyline)source.Clone())
                    {
                        ApplyVertices(candidate, vertices);
                        if (!Finite(candidate.Length)) { result.Skipped++; continue; }
                        if (!source.Closed && (candidate.StartPoint.DistanceTo(source.StartPoint) > 0 ||
                            candidate.EndPoint.DistanceTo(source.EndPoint) > 0)) { result.Skipped++; continue; }
                        source.UpgradeOpen();
                        ApplyVertices(source, vertices);
                        if (!Finite(source.Length)) throw new InvalidOperationException("Geometria no finita: se cancela toda la limpieza.");
                        result.Changed++; result.Removed += removed;
                        result.MaxDisplacement = Math.Max(result.MaxDisplacement, displacement);
                        backup.Items.Add(new VertexBackup { Id = id, Before = before, After = ReadVertices(source),
                            Closed = source.Closed, Elevation = source.Elevation, Normal = source.Normal });
                    }
                }
                transaction.Commit();
            }
            if (backup.Items.Count > 0)
                global::Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.UserData[BackupKey] = backup;
            return result;
        }

        private static void ApplyVertices(Polyline polyline, List<VertexData> vertices)
        {
            while (polyline.NumberOfVertices < vertices.Count)
            {
                var vertex = vertices[polyline.NumberOfVertices];
                polyline.AddVertexAt(polyline.NumberOfVertices, vertex.Point, vertex.Bulge, vertex.StartWidth, vertex.EndWidth);
            }
            while (polyline.NumberOfVertices > vertices.Count) polyline.RemoveVertexAt(polyline.NumberOfVertices - 1);
            for (int i = 0; i < vertices.Count; i++)
            {
                polyline.SetPointAt(i, vertices[i].Point);
                polyline.SetBulgeAt(i, vertices[i].Bulge);
                polyline.SetStartWidthAt(i, vertices[i].StartWidth);
                polyline.SetEndWidthAt(i, vertices[i].EndWidth);
            }
        }

        private static bool SameVertices(List<VertexData> a, List<VertexData> b)
        {
            return a.Count == b.Count && a.Zip(b, (x, y) => x.Point == y.Point && x.Bulge.Equals(y.Bulge) &&
                x.StartWidth.Equals(y.StartWidth) && x.EndWidth.Equals(y.EndWidth)).All(same => same);
        }

        [CommandMethod(RestoreCommandName, CommandFlags.Modal)]
        public void RestoreCleanup()
        {
            var doc = global::Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var backup = doc.UserData[BackupKey] as CleanupBackup;
            if (backup == null)
            { doc.Editor.WriteMessage("\nNo hay limpieza controlada para restaurar en esta apertura del dibujo."); return; }
            using (var transaction = doc.Database.TransactionManager.StartTransaction())
            {
                foreach (var item in backup.Items)
                {
                    if (!item.Id.IsValid || item.Id.IsErased) { doc.Editor.WriteMessage("\nNo se restaura: falta una entidad de la copia previa."); return; }
                    var source = (Polyline)transaction.GetObject(item.Id, OpenMode.ForRead);
                    var layer = (LayerTableRecord)transaction.GetObject(source.LayerId, OpenMode.ForRead);
                    if (layer.IsLocked || source.Closed != item.Closed || source.Elevation != item.Elevation || source.Normal != item.Normal ||
                        !SameVertices(ReadVertices(source), item.After))
                    { doc.Editor.WriteMessage("\nNo se restaura: una polilinea cambio despues de la limpieza o esta bloqueada. No se modifico ninguna."); return; }
                }
                foreach (var item in backup.Items)
                    ApplyVertices((Polyline)transaction.GetObject(item.Id, OpenMode.ForWrite), item.Before);
                transaction.Commit();
            }
            doc.UserData.Remove(BackupKey);
            ClearMarkers();
            doc.Editor.WriteMessage("\nRestauradas {0} polilineas al estado anterior a la ultima limpieza controlada. Repita el diagnostico.", backup.Items.Count);
        }

#if DEBUG
        private const string TestSnapshotKey = "CivilSpellAI.TestPolylineSnapshot";
        [CommandMethod("AISPELLTESTCAPTURE")]
        public void TestCaptureVertices()
        {
            var db = global::Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
            var snapshot = new CleanupBackup();
            using (var tx = db.TransactionManager.StartTransaction())
            {
                var table = (BlockTable)tx.GetObject(db.BlockTableId, OpenMode.ForRead);
                var model = (BlockTableRecord)tx.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in model)
                {
                    var p = tx.GetObject(id, OpenMode.ForRead) as Polyline;
                    if (p != null) snapshot.Items.Add(new VertexBackup { Id = id, Before = ReadVertices(p), Closed = p.Closed,
                        Elevation = p.Elevation, Normal = p.Normal });
                }
            }
            global::Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.UserData[TestSnapshotKey] = snapshot;
        }
        [CommandMethod("AISPELLTESTRESTORED")]
        public void TestRestoredVertices()
        {
            var doc = global::Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            var snapshot = doc.UserData[TestSnapshotKey] as CleanupBackup;
            if (snapshot == null) throw new InvalidOperationException("No snapshot");
            using (var tx = doc.Database.TransactionManager.StartTransaction())
                foreach (var item in snapshot.Items)
                {
                    var p = (Polyline)tx.GetObject(item.Id, OpenMode.ForRead);
                    if (p.Closed != item.Closed || p.Elevation != item.Elevation || p.Normal != item.Normal ||
                        !SameVertices(ReadVertices(p), item.Before)) throw new InvalidOperationException("Restore mismatch " + item.Id.Handle);
                }
            doc.Editor.WriteMessage("\nSAFE_RESTORE_EXACT_OK");
        }
        [CommandMethod("AISPELLTESTSAFECLEANUP")]
        public void TestSafeCleanup()
        {
            var vertices = new List<VertexData> { new VertexData { Point = new Point2d(0, 0) },
                new VertexData { Point = new Point2d(8, 0) }, new VertexData { Point = new Point2d(4, 0) },
                new VertexData { Point = new Point2d(2, 0) }, new VertexData { Point = new Point2d(10, 0) } };
            if (SimplifyStraightRuns(vertices, 1e-6) != 3 || vertices.Count != 2) throw new InvalidOperationException("Backtrack test");
            vertices.Insert(1, new VertexData { Point = new Point2d(20, 0) });
            if (SimplifyStraightRuns(vertices, 1e-6) != 0) throw new InvalidOperationException("Excursion test");
            vertices = new List<VertexData> { new VertexData { Point = new Point2d(0, 0), Bulge = double.NaN },
                new VertexData { Point = new Point2d(0, 0), Bulge = 1, StartWidth = 2, EndWidth = 3 },
                new VertexData { Point = new Point2d(10, 0) } };
            if (CollapseTinyEdges(vertices, false, 1e-6) != 1 || vertices[0].Bulge != 1 || vertices[0].StartWidth != 2)
                throw new InvalidOperationException("Duplicate preserves next arc");
            vertices[0].Bulge = double.NaN;
            vertices.Insert(1, new VertexData { Point = new Point2d(2, 0) });
            if (CollapseTinyEdges(vertices, false, 1e-6) != 0) throw new InvalidOperationException("Invalid nonzero edge");
            vertices = new List<VertexData> { new VertexData { Point = new Point2d(0, 0) },
                new VertexData { Point = new Point2d(2, 0) }, new VertexData { Point = new Point2d(10, 0) } };
            if (SimplifyStraightRuns(vertices, 1e-6) != 1 || vertices.Count != 2)
                throw new InvalidOperationException("Forward collinear vertex must be removed");
            // First vertex is redundant; the last edge also has a redundant vertex.
            vertices = new List<VertexData> { new VertexData { Point = new Point2d(2, 0) },
                new VertexData { Point = new Point2d(4, 0) }, new VertexData { Point = new Point2d(4, 4) },
                new VertexData { Point = new Point2d(0, 4) }, new VertexData { Point = new Point2d(0, 0) },
                new VertexData { Point = new Point2d(1, 0) } };
            if (SimplifyStraightRuns(vertices, 1e-6, true) != 2 || vertices.Count != 4 ||
                vertices.Any(v => v.Point == new Point2d(1, 0) || v.Point == new Point2d(2, 0)))
                throw new InvalidOperationException("Closed seam collinear vertices");
            if (SimplifyStraightRuns(vertices, 1e-6, true) != 0)
                throw new InvalidOperationException("Closed cleanup must be idempotent");
            // A visible bend is not evidence of overlap and must remain.
            vertices = new List<VertexData> { new VertexData { Point = new Point2d(0, 0) },
                new VertexData { Point = new Point2d(1, 0.001) }, new VertexData { Point = new Point2d(2, 0) } };
            if (SimplifyStraightRuns(vertices, 1e-6) != 0) throw new InvalidOperationException("Real kink protection");
            vertices[1].Point = new Point2d(1, 0); vertices[1].Bulge = 0.1;
            if (SimplifyStraightRuns(vertices, 1e-6) != 0) throw new InvalidOperationException("Arc must remain");
            vertices[1].Bulge = 0; vertices[0].StartWidth = 1;
            if (SimplifyStraightRuns(vertices, 1e-6) != 0) throw new InvalidOperationException("Width must remain");
            // Local coordinates from the reported closed contour: preserve the
            // genuine corner and remove only the redundant point on its closing edge.
            var corner = new Point2d(0.3439445789, 0.064285007);
            var redundant = new Point2d(0.3425802226, 0.064030002);
            vertices = new List<VertexData> { new VertexData { Point = Point2d.Origin },
                new VertexData { Point = new Point2d(0.4576170225, 0.023040694) },
                new VertexData { Point = corner }, new VertexData { Point = redundant } };
            if (SimplifyStraightRuns(vertices, 1e-6, true) != 1 || vertices.Count != 3 ||
                !vertices.Any(v => v.Point == corner) || vertices.Any(v => v.Point == redundant))
                throw new InvalidOperationException("Reported closing edge must retain the real corner");
            // Regression: the reported tiny kink is NOT an exact duplicate.
            vertices = new List<VertexData> {
                new VertexData { Point = new Point2d(0, 0), OriginalIndex = 0 },
                new VertexData { Point = new Point2d(1, 0), OriginalIndex = 1 },
                new VertexData { Point = new Point2d(1.0000385588, 0.000153865), OriginalIndex = 2 },
                new VertexData { Point = new Point2d(2, 0), OriginalIndex = 3 } };
            var tinyBefore = vertices.Select(v => new VertexData { Point = v.Point, OriginalIndex = v.OriginalIndex }).ToList();
            if (CollapseTinyEdges(vertices, false, 1e-6) != 0) throw new InvalidOperationException("Strict overlap threshold must keep tiny nonduplicate edge");
            if (CollapseTinyEdges(vertices, false, 0.0015) != 1 || vertices.Count != 3)
                throw new InvalidOperationException("Separate 1.5 mm threshold must remove reported tiny edge");
            double deviation;
            if (!ValidateCleanupDisplacement(tinyBefore, vertices, false, 0.0015, out deviation) || deviation > 0.0015)
                throw new InvalidOperationException("Reported tiny edge displacement bound");
            if (CollapseTinyEdges(vertices, false, 0.0015) != 0) throw new InvalidOperationException("Tiny cleanup idempotence");
            vertices = new List<VertexData> {
                new VertexData { Point = Point2d.Origin, OriginalIndex = 0 },
                new VertexData { Point = new Point2d(1, 0.0000004), OriginalIndex = 1 },
                new VertexData { Point = new Point2d(1, 0.0015003), OriginalIndex = 2 },
                new VertexData { Point = new Point2d(2, 0), OriginalIndex = 3 } };
            tinyBefore = vertices.Select(v => new VertexData { Point = v.Point, OriginalIndex = v.OriginalIndex }).ToList();
            CollapseTinyEdges(vertices, false, 0.0015);
            SimplifyStraightRuns(vertices, 1e-6);
            if (ValidateCleanupDisplacement(tinyBefore, vertices, false, 0.0015, out deviation))
                throw new InvalidOperationException("Combined cleanup must reject cumulative displacement outside limit");
            vertices = new List<VertexData> { new VertexData { Point = Point2d.Origin },
                new VertexData { Point = new Point2d(1, 0) }, new VertexData { Point = new Point2d(1.0013, 0) } };
            if (CollapseTinyEdges(vertices, false, 0.0015) != 0)
                throw new InvalidOperationException("Open endpoint must remain even when final edge is tiny");
            global::Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nSAFE_CLEANUP_TEST_OK");
        }
#endif
    }
}
