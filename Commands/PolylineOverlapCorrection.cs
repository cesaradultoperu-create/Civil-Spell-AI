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
#if CIVILSPELLAI_OVERLAP2023
        private const string CorrectionBackupKey = "CivilSpellAI.TramosOverlapCorrectionBackup";
#else
        private const string CorrectionBackupKey = "CivilSpellAI.OverlapCorrectionBackup";
#endif
        private sealed class CorrectionBackup
        {
            public readonly List<ObjectId> Originals = new List<ObjectId>();
            public readonly List<VertexBackup> Fragments = new List<VertexBackup>();
            public readonly Dictionary<ObjectId, string> Properties = new Dictionary<ObjectId, string>();
        }

        private static string FragmentProperties(Polyline p)
        {
            return string.Join("|", new object[] { p.LayerId, p.Color, p.LinetypeId,
                p.LinetypeScale, p.LineWeight, p.Transparency, p.Visible, p.Thickness,
                p.Plinegen, p.MaterialId, p.PlotStyleNameId });
        }
        private sealed class CorrectionPlan
        {
            public Polyline Source;
            public List<List<Point3d>> Runs;
            public readonly HashSet<ObjectId> Keepers = new HashSet<ObjectId>();
            public double Removed;
        }

        // This first correction supports only plain straight, open source paths.
        // Closed contours are always keepers; they are never cut or erased.
        private static bool CanCorrect(Polyline p, Transaction tx, ObjectId model)
        {
            if (!IsSourcePolyline(p, model) || p.NumberOfVertices < 2 || p.Thickness != 0 ||
                p.ConstantWidth != 0 || !p.ExtensionDictionary.IsNull ||
                p.GetPersistentReactorIds().Count != 0 ||
                ((LayerTableRecord)tx.GetObject(p.LayerId, OpenMode.ForRead)).IsLocked) return false;
            using (var data = p.XData) if (data != null) return false;
            return ReadVertices(p).All(v => Plain(v) && Finite(v.Point.X) && Finite(v.Point.Y)) &&
                Finite(p.Elevation);
        }

        private static List<Segment> StraightSegments(Polyline p)
        {
            var result = new List<Segment>();
            int n = p.Closed ? p.NumberOfVertices : p.NumberOfVertices - 1;
            for (int i = 0; i < n; i++) result.Add(new Segment { Owner = p.ObjectId,
                A = p.GetPoint3dAt(i), B = p.GetPoint3dAt((i + 1) % p.NumberOfVertices), Index = i + 1 });
            return result;
        }

        // Subtract only certified collinear intervals. Preserve all uncovered
        // pieces, including deviations between otherwise matching vertices.
        private static CorrectionPlan PlanCorrection(Polyline source, List<Segment> keepers, double tolerance)
        {
            var plan = new CorrectionPlan { Source = source, Runs = new List<List<Point3d>>() };
            List<Point3d> run = null;
            long comparisons = 0;
            foreach (var edge in StraightSegments(source))
            {
                double length = edge.A.DistanceTo(edge.B);
                if (!Finite(length) || length <= tolerance) return null; // clean tiny edges first
                var direction = (edge.B - edge.A) / length;
                var cuts = new List<Tuple<double, double>>();
                foreach (var keeper in keepers)
                {
                    if (++comparisons > 2000000) return null;
                    Hit hit;
                    if (!Overlap(edge, keeper, tolerance, out hit)) continue;
                    double lo = Math.Max(0, (hit.A - edge.A).DotProduct(direction));
                    double hi = Math.Min(length, (hit.B - edge.A).DotProduct(direction));
                    if (hi <= lo) continue;
                    cuts.Add(Tuple.Create(lo, hi));
                    plan.Keepers.Add(keeper.Owner);
                }
                cuts.Sort((a, b) => a.Item1.CompareTo(b.Item1));
                double cursor = 0;
                foreach (var cut in cuts)
                {
                    if (cut.Item1 > cursor)
                        AddRetainedRun(plan.Runs, ref run, edge, direction, length, cursor, cut.Item1);
                    if (cut.Item2 > cursor)
                    {
                        plan.Removed += cut.Item2 - Math.Max(cursor, cut.Item1);
                        cursor = cut.Item2;
                        run = null;
                    }
                }
                if (cursor < length) AddRetainedRun(plan.Runs, ref run, edge, direction, length, cursor, length);
            }
            return plan;
        }

        private static void AddRetainedRun(List<List<Point3d>> runs, ref List<Point3d> run,
            Segment edge, Vector3d direction, double length, double lo, double hi)
        {
            Point3d a = lo == 0 ? edge.A : edge.A + direction * lo;
            Point3d b = hi == length ? edge.B : edge.A + direction * hi;
            if (a == b) return;
            if (run == null || run[run.Count - 1] != a)
            { run = new List<Point3d> { a }; runs.Add(run); }
            run.Add(b);
        }

        private static void ApplyCorrection(Database db, Transaction tx, CorrectionPlan plan, CorrectionBackup backup)
        {
            var model = (BlockTableRecord)tx.GetObject(plan.Source.OwnerId, OpenMode.ForWrite);
            foreach (var points in plan.Runs)
            {
                var fragment = (Polyline)plan.Source.Clone();
                try
                {
                    var vertices = points.Select(p => p.TransformBy(Matrix3d.WorldToPlane(fragment.Normal)))
                        .Select(p => new VertexData { Point = new Point2d(p.X, p.Y) }).ToList();
                    ApplyVertices(fragment, vertices);
                    if (!Finite(fragment.Length) || fragment.Length <= 0)
                        throw new InvalidOperationException("Fragmento invalido; se cancela la transaccion.");
                    var id = model.AppendEntity(fragment);
                    tx.AddNewlyCreatedDBObject(fragment, true);
                    backup.Fragments.Add(new VertexBackup { Id = id, After = ReadVertices(fragment),
                        Closed = fragment.Closed, Normal = fragment.Normal, Elevation = fragment.Elevation });
                    backup.Properties.Add(id, FragmentProperties(fragment));
                }
                catch { if (fragment.ObjectId.IsNull) fragment.Dispose(); throw; }
            }
            plan.Source.UpgradeOpen();
            plan.Source.Erase();
            backup.Originals.Add(plan.Source.ObjectId);
        }

        private static void CorrectOverlaps(global::Autodesk.AutoCAD.ApplicationServices.Document doc, ReviewSession session)
        {
            var ed = doc.Editor;
            Analyze(doc, session, false);
            if (session.Incomplete)
            { ed.WriteMessage("\nCorreccion bloqueada: analisis incompleto o geometria invalida."); return; }
            if (doc.UserData[CorrectionBackupKey] != null)
            { ed.WriteMessage("\nYa existe una correccion en esta apertura. Use Restaurarsolapes antes de otra correccion."); return; }
            var pairs = session.Hits.Where(h => !h.IsShort && !h.IsInvalid && h.Owner != h.Other).ToList();
            if (pairs.Count == 0) { ed.WriteMessage("\nNo hay solapes entre polilineas en este conjunto."); return; }
            var backup = new CorrectionBackup();
            using (var tx = doc.Database.TransactionManager.StartTransaction())
            {
                var table = (BlockTable)tx.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ids = new HashSet<ObjectId>(session.Ids);
                var affected = pairs.SelectMany(h => new[] { h.Owner, h.Other }).Distinct()
                    .Where(id => ids.Contains(id) && id.IsValid && !id.IsErased)
                    .Select(id => tx.GetObject(id, OpenMode.ForRead) as Polyline)
                    .Where(p => p != null && CanCorrect(p, tx, table[BlockTableRecord.ModelSpace])).ToList();
                var keepers = affected.Where(p => p.Closed).SelectMany(StraightSegments).ToList();
                var plans = new List<CorrectionPlan>();
                foreach (var source in affected.Where(p => !p.Closed).OrderBy(p => p.Handle.Value))
                {
                    var plan = PlanCorrection(source, keepers, session.Tolerance);
                    if (plan != null && plan.Removed > session.Tolerance) plans.Add(plan);
                    // Only unchanged objects become keepers, avoiding erase chains.
                    else keepers.AddRange(StraightSegments(source));
                }
                if (plans.Count == 0)
                { ed.WriteMessage("\nNo hay correcciones compatibles: se conservan contornos cerrados, arcos, anchos, objetos vinculados y capas bloqueadas. Limpie primero los tramos casi nulos."); return; }
                foreach (var plan in plans)
                    ed.WriteMessage("\nPolilinea {0} (capa {1}, color {2}): quitar {3:G10} unidades coincidentes; conservar {4} fragmentos. Referencias intactas: {5}.",
                        plan.Source.Handle, plan.Source.Layer, plan.Source.Color, plan.Removed, plan.Runs.Count,
                        string.Join(", ", plan.Keepers.Select(id => id.Handle.ToString())));
                ed.WriteMessage("\nSe conservan los contornos cerrados. Las abiertas afectadas se sustituyen por sus partes no superpuestas; cambian sus identificadores. No se guarda el DWG automaticamente.");
                var confirm = new PromptKeywordOptions("\nAplicar estas correcciones: ") { AllowNone = true };
                confirm.Keywords.Add("Si"); confirm.Keywords.Add("No"); confirm.Keywords.Default = "No";
                var answer = ed.GetKeywords(confirm);
                if (answer.Status != PromptStatus.OK || answer.StringResult != "Si")
                { ed.WriteMessage("\nCorreccion cancelada. Polilineas intactas."); return; }
                foreach (var plan in plans) ApplyCorrection(doc.Database, tx, plan, backup);
                tx.Commit();
            }
            doc.UserData[CorrectionBackupKey] = backup;
            session.Ids = session.Ids.Concat(backup.Fragments.Select(f => f.Id)).Distinct().ToArray();
            if (session.Scope != "Modelo") session.Scope = SelectionScope(session.Ids.Where(id => id.IsValid && !id.IsErased).ToArray());
            ed.WriteMessage("\nCORRECCION: {0} originales sustituidas; {1} fragmentos conservados. Restaurarsolapes recupera los originales mientras el dibujo siga abierto.", backup.Originals.Count, backup.Fragments.Count);
            Analyze(doc, session, false);
            ed.WriteMessage(session.Incomplete || session.Hits.Any(h => !h.IsShort && !h.IsInvalid)
                ? "\nQuedan hallazgos pendientes. Revise el diagnostico."
                : "\nSin superposiciones detectadas en las LWPOLYLINE del conjunto a esta tolerancia.");
        }

        [CommandMethod(RestoreOverlapsCommandName, CommandFlags.Modal)]
        public void RestoreOverlaps()
        {
            var doc = AcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var backup = doc.UserData[CorrectionBackupKey] as CorrectionBackup;
            if (backup == null) { doc.Editor.WriteMessage("\nNo hay correccion de solapes para restaurar."); return; }
            if (backup.Originals.All(id => id.IsValid && !id.IsErased) &&
                backup.Fragments.All(f => !f.Id.IsValid || f.Id.IsErased))
            {
                doc.UserData.Remove(CorrectionBackupKey);
                doc.Editor.WriteMessage("\nCorreccion ya deshecha. Respaldo en memoria liberado.");
                return;
            }
            using (var tx = doc.Database.TransactionManager.StartTransaction())
            {
                foreach (var id in backup.Originals)
                {
                    if (!id.IsValid || !id.IsErased) { doc.Editor.WriteMessage("\nRespaldo desactualizado (posible U). No se restaura."); return; }
                    var p = (Polyline)tx.GetObject(id, OpenMode.ForRead, true);
                    if (((LayerTableRecord)tx.GetObject(p.LayerId, OpenMode.ForRead)).IsLocked)
                    { doc.Editor.WriteMessage("\nNo se restaura: capa bloqueada."); return; }
                }
                foreach (var item in backup.Fragments)
                {
                    if (!item.Id.IsValid || item.Id.IsErased) { doc.Editor.WriteMessage("\nNo se restaura: falta un fragmento."); return; }
                    var p = (Polyline)tx.GetObject(item.Id, OpenMode.ForRead);
                    if (((LayerTableRecord)tx.GetObject(p.LayerId, OpenMode.ForRead)).IsLocked ||
                        p.Closed != item.Closed || p.Normal != item.Normal || p.Elevation != item.Elevation ||
                        !SameVertices(ReadVertices(p), item.After) || p.GetPersistentReactorIds().Count != 0 ||
                        !p.ExtensionDictionary.IsNull || FragmentProperties(p) != backup.Properties[item.Id])
                    { doc.Editor.WriteMessage("\nNo se restaura: un fragmento fue editado o protegido."); return; }
                    using (var data = p.XData)
                        if (data != null) { doc.Editor.WriteMessage("\nNo se restaura: fragmento con datos agregados."); return; }
                }
                foreach (var item in backup.Fragments) tx.GetObject(item.Id, OpenMode.ForWrite).Erase();
                foreach (var id in backup.Originals) tx.GetObject(id, OpenMode.ForWrite, true).Erase(false);
                tx.Commit();
            }
            doc.UserData.Remove(CorrectionBackupKey);
            ClearMarkers();
            doc.Editor.WriteMessage("\nSOLAPES_RESTAURADOS: {0} originales recuperadas. Repita Analizar.", backup.Originals.Count);
        }

#if DEBUG
        [CommandMethod("AISPELLTESTCORRECTION")]
        public void TestOverlapCorrection()
        {
            using (var p = new Polyline())
            {
                p.AddVertexAt(0, Point2d.Origin, 0, 0, 0);
                p.AddVertexAt(1, new Point2d(10, 0), 0, 0, 0);
                var keeper = new Segment { A = new Point3d(2, 0, 0), B = new Point3d(8, 0, 0) };
                var plan = PlanCorrection(p, new List<Segment> { keeper }, 1e-6);
                if (plan.Runs.Count != 2 || Math.Abs(plan.Removed - 6) > 1e-10 ||
                    plan.Runs[0][0] != Point3d.Origin || plan.Runs[0][1] != keeper.A ||
                    plan.Runs[1][0] != keeper.B || plan.Runs[1][1] != new Point3d(10, 0, 0))
                    throw new InvalidOperationException("Partial coverage must preserve both tails");
                plan = PlanCorrection(p, new List<Segment> { keeper, keeper }, 1e-6);
                if (plan.Runs.Count != 2 || Math.Abs(plan.Removed - 6) > 1e-10)
                    throw new InvalidOperationException("Duplicate coverage must not double subtract");
                keeper.A = new Point3d(0, 0, 1); keeper.B = new Point3d(10, 0, 1);
                plan = PlanCorrection(p, new List<Segment> { keeper }, 1e-6);
                if (plan.Removed != 0 || plan.Runs.Count != 1)
                    throw new InvalidOperationException("Different elevations must not be corrected");
                keeper.A = new Point3d(5, -1, 0); keeper.B = new Point3d(5, 1, 0);
                if (PlanCorrection(p, new List<Segment> { keeper }, 1e-6).Removed != 0)
                    throw new InvalidOperationException("Crossings must remain intact");
                keeper.A = new Point3d(10, 0, 0); keeper.B = Point3d.Origin;
                plan = PlanCorrection(p, new List<Segment> { keeper }, 1e-6);
                if (plan.Runs.Count != 0 || Math.Abs(plan.Removed - 10) > 1e-10)
                    throw new InvalidOperationException("Reverse full duplicate");
                var gapKeepers = new List<Segment> {
                    new Segment { A = Point3d.Origin, B = new Point3d(4, 0, 0) },
                    new Segment { A = new Point3d(6, 0, 0), B = new Point3d(10, 0, 0) } };
                plan = PlanCorrection(p, gapKeepers, 1e-6);
                if (plan.Runs.Count != 1 || plan.Runs[0][0] != new Point3d(4, 0, 0) ||
                    plan.Runs[0][1] != new Point3d(6, 0, 0))
                    throw new InvalidOperationException("Uncovered gap must remain");
            }
            using (var db = new Database(true, true))
            using (var tx = db.TransactionManager.StartTransaction())
            {
                var table = (BlockTable)tx.GetObject(db.BlockTableId, OpenMode.ForRead);
                var model = (BlockTableRecord)tx.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var p = new Polyline(); p.AddVertexAt(0, Point2d.Origin, 0, 0, 0);
                p.AddVertexAt(1, new Point2d(10, 0), 0, 0, 0);
                model.AppendEntity(p); tx.AddNewlyCreatedDBObject(p, true);
                if (!CanCorrect(p, tx, model.ObjectId)) throw new InvalidOperationException("Eligible source");
                p.SetBulgeAt(0, 1);
                if (CanCorrect(p, tx, model.ObjectId)) throw new InvalidOperationException("Arc protection");
                p.SetBulgeAt(0, 0); p.ConstantWidth = 1;
                if (CanCorrect(p, tx, model.ObjectId)) throw new InvalidOperationException("Width protection");
                p.ConstantWidth = 0;
                var layer = (LayerTableRecord)tx.GetObject(p.LayerId, OpenMode.ForWrite);
                layer.IsLocked = true;
                if (CanCorrect(p, tx, model.ObjectId)) throw new InvalidOperationException("Locked layer protection");
                layer.IsLocked = false;
                p.CreateExtensionDictionary();
                if (CanCorrect(p, tx, model.ObjectId)) throw new InvalidOperationException("Custom data protection");
            }
            AcadApplication.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nCORRECTION_GEOMETRY_TEST_OK");
        }
#endif
    }
}
