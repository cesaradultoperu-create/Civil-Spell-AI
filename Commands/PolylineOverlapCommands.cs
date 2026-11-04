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
    // Source polylines are read-only; diagnostic marks are tagged and removable.
    public sealed partial class PolylineOverlapCommands
    {
#if CIVILSPELLAI_OVERLAP2023
        private const string ReviewCommandName = "REVISARTRAMOSDWG";
        private const string DiagnoseCommandName = "DETECTARTRAMOSDWG";
        private const string ClearCommandName = "QUITARMARCASTRAMOSDWG";
        private const string ShowCommandName = "VERMARCASTRAMOSDWG";
        private const string RestoreCommandName = "RESTAURARTRAMOSDWG";
        private const string RestoreOverlapsCommandName = "RESTAURARSOLAPESTRAMOSDWG";
#else
        private const string ReviewCommandName = "REVISARPOLILINEAS";
        private const string DiagnoseCommandName = "AISPELLDETECTARSUPERPUESTAS";
        private const string ClearCommandName = "AISPELLQUITARMARCAS";
        private const string ShowCommandName = "AISPELLVERMARCAS";
        private const string RestoreCommandName = "RESTAURARPOLILINEAS";
        private const string RestoreOverlapsCommandName = "RESTAURARSOLAPES";
#endif
        private const string MarkerApp = "CIVILSPELLAI_OVERLAP_V2";
        private const string OverlapLayer = "CIVILSPELLAI_SUPERPUESTAS";
        private const string ShortLayer = "CIVILSPELLAI_TRAMOS_NULOS";
        private const string InvalidLayer = "CIVILSPELLAI_GEOMETRIA_INVALIDA";
        private const int MaxHits = 10000;
        // Never extract display geometry from a Civil surface or a block.
        private static bool IsSourcePolyline(Entity entity, ObjectId modelId)
        {
            return entity != null && entity.GetType() == typeof(Polyline) &&
                entity.GetRXClass().DxfName == "LWPOLYLINE" &&
                entity.OwnerId == modelId && !IsMarker(entity);
        }
        internal sealed class Segment
        {
            public ObjectId Owner;
            public Point3d A, B;
            public int Index;
            public ArcPart Arc;
            public Point3d Min, Max;
            public void SetBounds()
            {
                var points = Arc == null ? new List<Point3d> { A, B } : Arc.BoundsPoints();
                Min = new Point3d(points.Min(p => p.X), points.Min(p => p.Y), points.Min(p => p.Z));
                Max = new Point3d(points.Max(p => p.X), points.Max(p => p.Y), points.Max(p => p.Z));
            }
            public double MinX { get { return Min.X; } }
            public double MaxX { get { return Max.X; } }
        }

        internal sealed class Hit
        {
            public ObjectId Owner, Other;
            public bool IsShort, IsInvalid;
            public int First, Second;
            public Point3d A, B;
            public ArcPart Arc;
            public double Length { get { return Arc == null ? A.DistanceTo(B) : Arc.Radius * Arc.Sweep; } }
        }

        internal static bool Overlap(Segment a, Segment b, double tolerance, out Hit hit)
        {
            hit = null;
            Vector3d delta = a.B - a.A;
            double length = delta.Length;
            if (length <= tolerance || b.A.DistanceTo(b.B) <= tolerance) return false;
            Vector3d unit = delta / length;
            Vector3d start = b.A - a.A, end = b.B - a.A;
            if (start.CrossProduct(unit).Length > tolerance ||
                end.CrossProduct(unit).Length > tolerance) return false;
            double t0 = start.DotProduct(unit), t1 = end.DotProduct(unit);
            double low = Math.Max(0, Math.Min(t0, t1));
            double high = Math.Min(length, Math.Max(t0, t1));
            if (high - low <= tolerance) return false;
            hit = new Hit { Owner = a.Owner, Other = b.Owner, First = a.Index, Second = b.Index,
                A = a.A + unit * low, B = a.A + unit * high };
            return true;
        }

        internal static List<Hit> FindOverlaps(List<Segment> segments, double tolerance, out bool limited)
        {
            limited = false;
            var hits = new List<Hit>();
            foreach (var segment in segments) segment.SetBounds();
            segments.Sort((a, b) => a.MinX.CompareTo(b.MinX));
            long comparisons = 0;
            for (int i = 0; i < segments.Count; i++)
            {
                var a = segments[i];
                for (int j = i + 1; j < segments.Count && segments[j].MinX <= a.MaxX + tolerance; j++)
                {
                    if (++comparisons > 20000000) { limited = true; return hits; }
                    var b = segments[j];
                    if (a.Max.Y + tolerance < b.Min.Y || b.Max.Y + tolerance < a.Min.Y ||
                        a.Max.Z + tolerance < b.Min.Z || b.Max.Z + tolerance < a.Min.Z) continue;
                    if (a.Arc != null && b.Arc != null) hits.AddRange(ArcOverlaps(a, b, tolerance));
                    else if (a.Arc == null && b.Arc == null)
                    {
                        Hit hit;
                        if (Overlap(a, b, tolerance, out hit)) hits.Add(hit);
                    }
                    if (hits.Count >= MaxHits) { limited = true; return hits; }
                }
            }
            return hits;
        }

        [CommandMethod(DiagnoseCommandName, CommandFlags.Modal)]
        public void Diagnose()
        {
            var doc = AcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var session = PromptReview(doc.Editor);
            if (session != null) Analyze(doc, session, true);
        }

        private sealed class ReviewSession
        {
            public ObjectId[] Ids;
            public string Scope;
            public double Tolerance, ShortTolerance, Radius;
            public List<Hit> Hits = new List<Hit>();
            public bool Incomplete;
        }

        private static ReviewSession PromptReview(Editor ed)
        {
            ed.WriteMessage("\nALCANCE: solo entidades LWPOLYLINE de Model en su posicion 3D. Superficies TIN, sus curvas visibles, LINE y bloques quedan excluidos.");
            var mode = new PromptKeywordOptions("\nRevisar polilineas: ")
                { AllowNone = true };
            mode.Keywords.Add("Seleccion"); mode.Keywords.Add("Modelo");
            mode.Keywords.Default = "Seleccion";
            var choice = ed.GetKeywords(mode);
            if (choice.Status == PromptStatus.Cancel) return null;
            var filter = new SelectionFilter(new[] { new TypedValue(0, "LWPOLYLINE"), new TypedValue(410, "Model") });
            var selection = choice.StringResult == "Modelo" ? ed.SelectAll(filter) :
                ed.GetSelection(new PromptSelectionOptions { MessageForAdding = "\nSeleccione polilineas de Model: " }, filter);
            if (selection.Status != PromptStatus.OK) return null;
            var toleranceOptions = new PromptDoubleOptions("\nTolerancia en unidades del dibujo <0.000001>: ")
                { DefaultValue = 0.000001, UseDefaultValue = true, AllowZero = false, AllowNegative = false };
            var toleranceResult = ed.GetDouble(toleranceOptions);
            if (toleranceResult.Status != PromptStatus.OK) return null;
            double tolerance = toleranceResult.Value;
            if (double.IsNaN(tolerance) || double.IsInfinity(tolerance)) return null;
            var radiusResult = ed.GetDouble(new PromptDoubleOptions("\nRadio minimo de los circulos (unidades del dibujo): ")
                { DefaultValue = 1.0, UseDefaultValue = true, AllowZero = false, AllowNegative = false });
            if (radiusResult.Status != PromptStatus.OK || double.IsInfinity(radiusResult.Value) || double.IsNaN(radiusResult.Value)) return null;
            double radius = radiusResult.Value;
            double shortTolerance = DefaultShortTolerance(AcadApplication.DocumentManager.MdiActiveDocument.Database, tolerance);
            ed.WriteMessage("\nSolapes: tolerancia {0:G}. Tramos diminutos: longitud maxima {1:G} unidades. Use Tramos para cambiar este limite.", tolerance, shortTolerance);
            return new ReviewSession { Ids = selection.Value.GetObjectIds(), Tolerance = tolerance, ShortTolerance = shortTolerance, Radius = radius,
                Scope = choice.StringResult == "Modelo" ? "Modelo" : SelectionScope(selection.Value.GetObjectIds()) };
        }

        // 1.5 mm for drawings with explicitly declared common insertion units.
        // Unspecified units keep the caller's threshold; never infer scale from coordinates.
        private static double DefaultShortTolerance(Database database, double tolerance)
        {
            double unitsPerMeter;
            switch ((int)database.Insunits)
            {
                case 1: unitsPerMeter = 1000.0 / 25.4; break;
                case 2: unitsPerMeter = 1000.0 / 304.8; break;
                case 4: unitsPerMeter = 1000; break;
                case 5: unitsPerMeter = 100; break;
                case 6: unitsPerMeter = 1; break;
                default: return tolerance;
            }
            return Math.Max(tolerance, 0.0015 * unitsPerMeter);
        }

        private static void Analyze(global::Autodesk.AutoCAD.ApplicationServices.Document doc, ReviewSession session, bool navigate)
        {
            var ed = doc.Editor;
            double tolerance = session.Tolerance, radius = session.Radius;
            double shortTolerance = Math.Max(tolerance, session.ShortTolerance);
            var shortHits = new List<Hit>();
            var invalidHits = new List<Hit>();
            var segments = new List<Segment>();
            int arcs = 0, invalidArcs = 0, shortSegments = 0, count = 0;
            bool limited = false;
            using (var transaction = doc.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var table = (BlockTable)transaction.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                ObjectId modelId = table[BlockTableRecord.ModelSpace];
                foreach (ObjectId id in session.Ids)
                {
                    if (!id.IsValid || id.IsErased) continue;
                    var entity = transaction.GetObject(id, OpenMode.ForRead) as Entity;
                    if (!IsSourcePolyline(entity, modelId)) continue;
                    var polyline = (Polyline)entity;
                    count++;
                    int n = polyline.Closed ? polyline.NumberOfVertices : polyline.NumberOfVertices - 1;
                    for (int i = 0; i < n; i++)
                    {
                        var segment = new Segment { Owner = id, A = polyline.GetPoint3dAt(i),
                            B = polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices), Index = i + 1 };
                        if (polyline.GetBulgeAt(i) != 0)
                        {
                            segment.Arc = ReadArc(segment.A, segment.B, polyline.Normal, polyline.GetBulgeAt(i));
                            if (segment.Arc == null)
                            {
                                invalidArcs++;
                                if (invalidHits.Count < MaxHits) invalidHits.Add(new Hit { Owner = id, Other = id,
                                    First = i + 1, Second = i + 1, A = segment.A, B = segment.B, IsInvalid = true });
                                if (invalidArcs <= 3) ed.WriteMessage("\nArco degenerado: polilinea {0}, segmento {1}, bulge={2:R}, cuerda={3:R}.",
                                    id.Handle, i + 1, polyline.GetBulgeAt(i), segment.A.DistanceTo(segment.B));
                                continue;
                            }
                            arcs++;
                        }
                        double length = segment.Arc == null ? segment.A.DistanceTo(segment.B) : segment.Arc.Radius * segment.Arc.Sweep;
                        if (length <= shortTolerance)
                        {
                            shortSegments++;
                            if (shortHits.Count < MaxHits) shortHits.Add(new Hit { Owner = id, Other = id,
                                First = i + 1, Second = i + 1, A = segment.A, B = segment.B, Arc = segment.Arc, IsShort = true });
                        }
                        // A short segment can still overlap another segment.
                        if (length <= tolerance) continue;
                        segments.Add(segment);
                    }
                }
            }
            ed.WriteMessage("\nComparando {0} segmentos (rectos y arcos) dentro de cada polilinea y entre polilineas...", segments.Count);
            var hits = FindOverlaps(segments, tolerance, out limited);
            ed.WriteMessage("\nDIAGNOSTICO: {0} polilineas revisadas; {1} afectadas; {2} pares de segmentos superpuestos.",
                count, hits.SelectMany(h => new[] { h.Owner, h.Other }).Distinct().Count(), hits.Count);
            ed.WriteMessage("\nPares internos: {0}; entre polilineas: {1}. Se compara la posicion 3D, incluida la elevacion.",
                hits.Count(h => h.Owner == h.Other), hits.Count(h => h.Owner != h.Other));
            ed.WriteMessage("\nArcos revisados: {0}; arcos degenerados omitidos: {1}; tramos diminutos <= {2:G}: {3} en {4} polilineas.",
                arcs, invalidArcs, shortTolerance, shortSegments, shortHits.Select(h => h.Owner).Distinct().Count());
            if (invalidArcs > 0) ed.WriteMessage("\nRESULTADO INCOMPLETO: existen arcos degenerados sin analizar.");
            if (limited) ed.WriteMessage("\nRESULTADO PARCIAL: limite de coincidencias o comparaciones. Revise una seleccion menor.");
            if (shortSegments > shortHits.Count) ed.WriteMessage("\nSolo se marcan los primeros 10000 tramos casi nulos.");
            var allHits = hits.Concat(shortHits).Concat(invalidHits).ToList();
            int removed = WriteMarkers(doc.Database, allHits, radius);
            var groups = GroupHits(allHits, radius);
            ed.WriteMessage("\nMarcas anteriores retiradas: {0} entidades. Zonas rojas S: {1}; amarillas N: {2}; magenta I: {3}. Etiquetas xN indican hallazgos agrupados.", removed, groups.Count(g => !g.Hits[0].IsShort && !g.Hits[0].IsInvalid), groups.Count(g => g.Hits[0].IsShort), groups.Count(g => g.Hits[0].IsInvalid));
            CompareAndRemember(doc.Database, ed, session.Scope + "|Tramos:" + shortTolerance.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                tolerance, hits.Count, shortSegments, invalidArcs, limited);
            ed.WriteMessage("\n" + ShowCommandName + ": Todas, Superpuestas, Nulos, Invalidas u Ocultar.");
            ed.WriteMessage("\nEl diagnostico no modifica polilineas. Marcas en capas no imprimibles. Use Limpiar en " + ReviewCommandName + " para limpieza controlada.");
            ed.Regen();
            session.Hits = allHits;
            session.Incomplete = limited || invalidArcs > 0;
            if (navigate && allHits.Count > 0) Navigate(ed, allHits, radius);
        }

        private static void Navigate(Editor ed, List<Hit> hits, double radius)
        {
            var groups = GroupHits(hits, radius);
            var labels = new Dictionary<Hit, string>();
            for (int g = 0; g < groups.Count; g++)
                foreach (var item in groups[g].Hits) labels[item] = (item.IsInvalid ? "I" : item.IsShort ? "N" : "S") + (g + 1);
            int current = 0;
            try
            {
                while (true)
                {
                    var hit = hits[current];
                    ed.Regen();
                    if (hit.Arc == null) ed.DrawVector(hit.A, hit.B, hit.IsShort ? 2 : 1, false);
                    else for (int k = 0; k < 32; k++) ed.DrawVector(hit.Arc.At(hit.Arc.Sweep * k / 32), hit.Arc.At(hit.Arc.Sweep * (k + 1) / 32), 1, false);
                    ed.WriteMessage("\nCaso {0}/{1} ({2}): polilineas {3}/{4}, segmentos {5}/{6}, longitud {7:0.########}.",
                        current + 1, hits.Count, hit.IsInvalid ? "I: geometria invalida (longitud mostrada: cuerda)" : hit.IsShort ? "N: casi nulo" : "S: superposicion",
                        hit.Owner.Handle, hit.Other.Handle, hit.First, hit.Second, hit.Length);
                    ed.WriteMessage("\nCirculo de referencia: {0}.", labels[hit]);
                    ed.WriteMessage("\nEl circulo senala una zona, no todos los objetos dentro de ella. Identificar muestra las entidades exactas.");
                    var options = new PromptKeywordOptions("\nRevisar caso: ") { AllowNone = true };
                    foreach (string keyword in new[] { "Acercar", "Siguiente", "Anterior", "Identificar", "Salir" }) options.Keywords.Add(keyword);
                    options.Keywords.Default = "Siguiente";
                    var answer = ed.GetKeywords(options);
                    if (answer.Status == PromptStatus.Cancel || answer.StringResult == "Salir") break;
                    if (answer.StringResult == "Identificar")
                    {
                        var doc = AcadApplication.DocumentManager.MdiActiveDocument;
                        using (var tx = doc.Database.TransactionManager.StartOpenCloseTransaction())
                        {
                            var table = (BlockTable)tx.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                            var ids = new[] { hit.Owner, hit.Other }.Distinct().ToArray();
                            bool valid = true;
                            foreach (var id in ids)
                            {
                                if (!id.IsValid || id.IsErased) { valid = false; continue; }
                                var entity = tx.GetObject(id, OpenMode.ForRead) as Entity;
                                if (!IsSourcePolyline(entity, table[BlockTableRecord.ModelSpace])) { valid = false; continue; }
                                var p = (Polyline)entity;
                                ed.WriteMessage("\nEntidad {0}: LWPOLYLINE; capa {1}; color {2}; elevacion {3:R}; cerrada {4}; vertices {5}.",
                                    id.Handle, p.Layer, p.Color, p.Elevation, p.Closed, p.NumberOfVertices);
                            }
                            if (valid)
                            {
                                ed.SetImpliedSelection(ids);
                                ed.WriteMessage("\nSeleccionadas solo las polilineas del caso. Ninguna superficie TIN forma parte del par.");
                            }
                            else ed.WriteMessage("\nCaso desactualizado: repita Analizar. No se seleccionaron entidades.");
                        }
                    }
                    else if (answer.StringResult == "Acercar")
                    {
                        var center = HitCenter(hit);
                        ed.Command("_.ZOOM", "_C", center.TransformBy(ed.CurrentUserCoordinateSystem.Inverse()),
                            Math.Max(hit.Length * 4, radius * 6));
                    }
                    else current = (current + (answer.StringResult == "Anterior" ? hits.Count - 1 : 1)) % hits.Count;
                }
            }
            finally { ed.Regen(); }
        }

        private static bool IsMarker(Entity entity)
        {
            using (var data = entity.GetXDataForApplication(MarkerApp))
                return data != null;
        }

        private static int EraseMarkers(Transaction transaction, BlockTableRecord model)
        {
            int count = 0;
            foreach (ObjectId id in model)
            {
                var entity = transaction.GetObject(id, OpenMode.ForRead) as Entity;
                if (entity == null || !IsMarker(entity)) continue;
                // Only tagged objects are removed, never everything on a named layer.
                transaction.GetObject(id, OpenMode.ForWrite, false, true);
                entity.Erase(); count++;
            }
            return count;
        }

        private static ObjectId GetMarkerLayer(Database database, Transaction transaction, string name, short color)
        {
            var table = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForRead);
            LayerTableRecord layer;
            if (table.Has(name)) layer = (LayerTableRecord)transaction.GetObject(table[name], OpenMode.ForWrite);
            else
            {
                table.UpgradeOpen(); layer = new LayerTableRecord { Name = name };
                table.Add(layer); transaction.AddNewlyCreatedDBObject(layer, true);
            }
            layer.Color = global::Autodesk.AutoCAD.Colors.Color.FromColorIndex(global::Autodesk.AutoCAD.Colors.ColorMethod.ByAci, color);
            layer.IsPlottable = false; layer.IsOff = false; layer.IsFrozen = false; layer.IsLocked = false;
            return layer.ObjectId;
        }

        internal static int WriteMarkers(Database database, List<Hit> hits, double minimumRadius)
        {
            using (var transaction = database.TransactionManager.StartTransaction())
            {
                var table = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
                var model = (BlockTableRecord)transaction.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                int removed = EraseMarkers(transaction, model);
                if (hits.Count != 0)
                {
                    var apps = (RegAppTable)transaction.GetObject(database.RegAppTableId, OpenMode.ForRead);
                    if (!apps.Has(MarkerApp))
                    {
                        apps.UpgradeOpen(); var app = new RegAppTableRecord { Name = MarkerApp };
                        apps.Add(app); transaction.AddNewlyCreatedDBObject(app, true);
                    }
                    ObjectId red = GetMarkerLayer(database, transaction, OverlapLayer, 1);
                    ObjectId yellow = GetMarkerLayer(database, transaction, ShortLayer, 2);
                    ObjectId magenta = GetMarkerLayer(database, transaction, InvalidLayer, 6);
                    var groups = GroupHits(hits, minimumRadius);
                    for (int i = 0; i < groups.Count; i++)
                    {
                        var group = groups[i];
                        var hit = group.Hits[0];
                        Point3d center = group.Center;
                        double radius = group.Radius;
                        string label = (hit.IsInvalid ? "I" : hit.IsShort ? "N" : "S") + (i + 1) + " x" + group.Hits.Count;
                        var circle = new Circle(center, Vector3d.ZAxis, radius);
                        var text = new DBText { Position = center + new Vector3d(radius, radius, 0),
                            Height = minimumRadius * 0.35, TextString = label };
                        foreach (Entity mark in new Entity[] { circle, text })
                        {
                            mark.SetDatabaseDefaults(database);
                            mark.LayerId = hit.IsInvalid ? magenta : hit.IsShort ? yellow : red;
                            mark.ColorIndex = hit.IsInvalid ? 6 : hit.IsShort ? 2 : 1;
                            model.AppendEntity(mark); transaction.AddNewlyCreatedDBObject(mark, true);
                            using (var data = new ResultBuffer(new TypedValue(1001, MarkerApp),
                                new TypedValue(1000, label), new TypedValue(1000, hit.Owner.Handle.ToString()),
                                new TypedValue(1000, hit.Other.Handle.ToString()),
                                new TypedValue(1071, hit.First), new TypedValue(1071, hit.Second))) mark.XData = data;
                        }
                    }
                }
                transaction.Commit(); return removed;
            }
        }

        [CommandMethod(ClearCommandName, CommandFlags.Modal)]
        public void ClearMarkers()
        {
            var doc = AcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            int removed = WriteMarkers(doc.Database, new List<Hit>(), 1);
            doc.Editor.Regen();
            doc.Editor.WriteMessage("\nSe retiraron {0} entidades de diagnostico. Polilineas intactas.", removed);
        }

#if DEBUG
        [CommandMethod("AISPELLTESTOVERLAP")]
        public void TestGeometry()
        {
            TestArcGeometry();
            var a = new Segment { A = new Point3d(0, 0, 0), B = new Point3d(10, 0, 0) };
            Hit hit;
            var cases = new[] {
                new Segment { A = new Point3d(8, 0, 0), B = new Point3d(2, 0, 0) },
                new Segment { A = new Point3d(10, 0, 0), B = new Point3d(12, 0, 0) },
                new Segment { A = new Point3d(5, -1, 0), B = new Point3d(5, 1, 0) },
                new Segment { A = new Point3d(2, 0, 1), B = new Point3d(8, 0, 1) },
                new Segment { A = new Point3d(2, 0, 0), B = new Point3d(2, 0, 0) } };
            for (int i = 0; i < cases.Length; i++)
                if (Overlap(a, cases[i], 0.000001, out hit) != (i == 0))
                    throw new InvalidOperationException("Overlap case " + i);
            if (!Overlap(a, cases[0], 0.000001, out hit) || Math.Abs(hit.A.DistanceTo(hit.B) - 6) > 1e-9)
                throw new InvalidOperationException("Overlap length");
            bool limited;
            var sweep = FindOverlaps(new List<Segment> { a, cases[0], cases[1], cases[2], cases[3] }, 0.000001, out limited);
            if (limited || sweep.Count != 1) throw new InvalidOperationException("Global sweep");
            using (var database = new Database(true, true))
            {
                ObjectId first, second, userCircle;
                using (var transaction = database.TransactionManager.StartTransaction())
                {
                    var table = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
                    var model = (BlockTableRecord)transaction.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                    var p = new Polyline(); p.AddVertexAt(0, new Point2d(0, 0), 0, 0, 0); p.AddVertexAt(1, new Point2d(10, 0), 0, 0, 0);
                    first = model.AppendEntity(p); transaction.AddNewlyCreatedDBObject(p, true);
                    var q = new Polyline(); q.AddVertexAt(0, new Point2d(8, 0), 0, 0, 0); q.AddVertexAt(1, new Point2d(2, 0), 0, 0, 0);
                    second = model.AppendEntity(q); transaction.AddNewlyCreatedDBObject(q, true);
                    if (!IsSourcePolyline(p, model.ObjectId) || !IsSourcePolyline(q, model.ObjectId))
                        throw new InvalidOperationException("Real polylines must be eligible");
                    var line = new Line(Point3d.Origin, new Point3d(10, 0, 0));
                    model.AppendEntity(line); transaction.AddNewlyCreatedDBObject(line, true);
                    var surface = new PlaneSurface();
                    model.AppendEntity(surface); transaction.AddNewlyCreatedDBObject(surface, true);
                    if (IsSourcePolyline(line, model.ObjectId) || IsSourcePolyline(surface, model.ObjectId))
                        throw new InvalidOperationException("Lines and surfaces must be excluded");
                    table.UpgradeOpen();
                    var block = new BlockTableRecord { Name = "OVERLAP_FILTER_TEST" };
                    table.Add(block); transaction.AddNewlyCreatedDBObject(block, true);
                    var nested = new Polyline();
                    nested.AddVertexAt(0, Point2d.Origin, 0, 0, 0);
                    nested.AddVertexAt(1, new Point2d(10, 0), 0, 0, 0);
                    block.AppendEntity(nested); transaction.AddNewlyCreatedDBObject(nested, true);
                    if (IsSourcePolyline(nested, model.ObjectId))
                        throw new InvalidOperationException("Block geometry must be excluded");
                    var circle = new Circle(Point3d.Origin, Vector3d.ZAxis, 20);
                    circle.LayerId = GetMarkerLayer(database, transaction, OverlapLayer, 1);
                    userCircle = model.AppendEntity(circle); transaction.AddNewlyCreatedDBObject(circle, true);
                    transaction.Commit();
                }
                a.Owner = first; cases[0].Owner = second;
                var findings = FindOverlaps(new List<Segment> { a, cases[0] }, 0.000001, out limited);
                if (findings.Count != 1 || findings[0].Owner == findings[0].Other) throw new InvalidOperationException("Cross entities");
                findings.Add(new Hit { Owner = first, Other = first, A = Point3d.Origin, B = Point3d.Origin, IsShort = true });
                if (WriteMarkers(database, findings, 1) != 0 || WriteMarkers(database, findings, 1) != 4)
                    throw new InvalidOperationException("Marker replacement");
                if (WriteMarkers(database, new List<Hit>(), 1) != 4 || WriteMarkers(database, new List<Hit>(), 1) != 0)
                    throw new InvalidOperationException("Marker cleanup");
                using (var transaction = database.TransactionManager.StartOpenCloseTransaction())
                {
                    var p = (Polyline)transaction.GetObject(first, OpenMode.ForRead);
                    var q = (Polyline)transaction.GetObject(second, OpenMode.ForRead);
                    if (p.NumberOfVertices != 2 || q.NumberOfVertices != 2 || Math.Abs(p.Length - 10) > 1e-9 || Math.Abs(q.Length - 6) > 1e-9 || userCircle.IsErased)
                        throw new InvalidOperationException("Source geometry changed");
                }
            }
            AcadApplication.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nOVERLAP_TEST_OK");
        }
#endif
    }
}
