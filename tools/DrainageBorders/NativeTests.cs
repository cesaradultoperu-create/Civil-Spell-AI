using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using NetTopologySuite.Geometries;
using App = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Coordinate = NetTopologySuite.Geometries.Coordinate;

namespace DrenBordes
{
    public sealed class NativeTests
    {
        private static string interactiveBaseline;
        private static string interactiveNormalized;
        [CommandMethod("DRENTESTPREPARE", CommandFlags.Modal)]
        public void PrepareInteractive()
        {
            var db = App.DocumentManager.MdiActiveDocument.Database;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead))
                {
                    var e = (Entity)tr.GetObject(id, OpenMode.ForRead);
                    if (e.Layer == DrawingBorders.LayerName) { e.UpgradeOpen(); e.Erase(); }
                    else if (e is Polyline && e.Layer == "0_DLC 1 Drainage stg2")
                    { e.UpgradeOpen(); ((Polyline)e).ConstantWidth = 2; }
                }
                tr.Commit();
            }
            interactiveBaseline = SourceSignature(db);
            interactiveNormalized = Signature(db, false, "0_DLC 1 Drainage stg2");
        }
        [CommandMethod("DRENTESTCHECK", CommandFlags.Modal | CommandFlags.NoUndoMarker)]
        public void CheckInteractive() { CheckInteractiveCore(false); }
        [CommandMethod("DRENTESTUNDONE", CommandFlags.Modal | CommandFlags.NoUndoMarker)]
        public void CheckUndone() { CheckInteractiveCore(true); }
        private static void CheckInteractiveCore(bool undone)
        {
            var doc = App.DocumentManager.MdiActiveDocument;
            try
            {
                Require((undone ? interactiveBaseline : interactiveNormalized) == SourceSignature(doc.Database),
                    "El comando conserva las coordenadas y normaliza solo los anchos; Undo restaura los anchos anteriores.");
                int count = BorderSignature(doc.Database).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
                Require(count == (undone ? 0 : 74), "Cantidad de bordes incorrecta tras comando o Undo/Redo: " + count);
                if (!undone) CheckLayer(doc.Database);
                doc.Editor.WriteMessage(undone ? "\nDREN_INTERACTIVE_UNDO_OK\n" : "\nDREN_INTERACTIVE_OK\n");
            }
            catch (System.Exception ex) { doc.Editor.WriteMessage("\nDREN_INTERACTIVE_FAILED " + ex + "\n"); }
        }
        private static string Number(double value) { return value.ToString("R", CultureInfo.InvariantCulture); }
        private static void Require(bool ok, string description)
        { if (!ok) throw new InvalidOperationException(description); }
        private static Coordinate P(double x, double y) { return new Coordinate(x, y); }
        private static Coordinate[] Path(params double[] xy)
        { return Enumerable.Range(0, xy.Length / 2).Select(i => P(xy[2 * i], xy[2 * i + 1])).ToArray(); }

        [CommandMethod("DRENBORDESTEST", CommandFlags.Modal)]
        public void Run()
        {
            var ed = App.DocumentManager.MdiActiveDocument.Editor;
            try
            {
                GeometryTests();
                ed.WriteMessage("\nDREN_GEOMETRY_OK\n");
                string source = Environment.GetEnvironmentVariable("DRENBORDES_SOURCE_DIR");
                string target = Environment.GetEnvironmentVariable("DRENBORDES_OUTPUT_DIR");
                if (!string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(target))
                {
                    Directory.CreateDirectory(target);
                    foreach (int stage in new[] { 1, 2, 3 }) RealDrawing(source, target, stage);
                    ReferenceDrawing(source, target);
                }
                ed.WriteMessage("\nDREN_ALL_OK\n");
            }
            catch (System.Exception ex) { ed.WriteMessage("\nDREN_TEST_FAILED " + ex + "\n"); }
        }

        private static void GeometryTests()
        {
            var straight = NetworkBorders.Build(new[] { Path(0, 0, 10, 0) }, .5);
            Require(straight.Paths.Count == 2 && Math.Abs(straight.Footprint.Area - 10) < 1e-8, "Dos paralelas de una recta.");
            Require(straight.Paths.All(p => p.All(c => Math.Abs(Math.Abs(c.Y) - .5) < 1e-8)), "Offset exactamente 0.50.");
            Require(straight.Paths.Sum(p => new LineString(p).Length) == 20, "Extremos abiertos, sin tapas.");
            var tee = NetworkBorders.Build(new[] { Path(-10, 0, 10, 0), Path(0, 0, 0, -10) }, .5);
            Require(tee.Paths.Count == 3 && tee.Junctions == 1 && tee.Terminals == 3, "Encuentro en T continuo y sin tapas.");
            Require(tee.Paths.Any(p => p.Any(c => c.Equals2D(P(-.5, -.5)))) &&
                tee.Paths.Any(p => p.Any(c => c.Equals2D(P(.5, -.5)))), "T recortada en ambos lados del ramal.");
            var crossing = NetworkBorders.Build(new[] { Path(-10, 0, 10, 0), Path(0, -10, 0, 10) }, .5);
            Require(crossing.Paths.Count == 4 && crossing.Junctions == 1 && crossing.Terminals == 4, "Cruce de cuatro ramales.");
            Require(Math.Abs(crossing.Footprint.Area - 39) < 1e-8, "Cruce sin interiores duplicados.");
            var elbow = NetworkBorders.Build(new[] { Path(0, 10, 0, 0), Path(0, 0, 10, 0) }, .5);
            Require(elbow.Paths.Count == 2 && elbow.Paths.Any(p => p.Any(c => c.Equals2D(P(-.5, -.5)))), "Esquina exterior en inglete.");
            var angled = NetworkBorders.Build(new[] { Path(-10, -5, 10, 5), Path(0, 0, 1, -10) }, .5);
            Require(angled.Paths.Count == 3 && angled.Junctions == 1, "T oblicua.");
            Require(angled.Paths.All(p => !new LineString(p).Crosses(new LineString(Path(-10, -5, 10, 5)))), "El ramal no cruza el eje principal.");
            var star = NetworkBorders.Build(new[] { Path(0, 0, 10, 0), Path(0, 0, 0, 10), Path(0, 0, 10, 10) }, .5);
            Require(star.Paths.Any(p => p.Any(c => c.Equals2D(P(-.5, -.5)))), "Inglete exterior en nodo de tres rayos.");
            var duplicate = NetworkBorders.Build(new[] { Path(0, 0, 10, 0), Path(10, 0, 0, 0) }, .5);
            Require(duplicate.Paths.Count == 2 && duplicate.Footprint.EqualsTopologically(straight.Footprint), "Ejes invertidos y duplicados.");
            var square = NetworkBorders.Build(new[] { Path(0, 0, 10, 0, 10, 10, 0, 10, 0, 0) }, .5);
            Require(square.Paths.Count == 2 && square.Paths.All(p => p.First().Equals2D(p.Last())), "Anillo conserva interior y exterior cerrados.");
            var disconnected = NetworkBorders.Build(new[] { Path(0, 0, 10, 0), Path(0, 5, 10, 5) }, .5);
            Require(disconnected.Paths.Count == 4 && disconnected.Terminals == 4, "Redes independientes.");
            var shifted = NetworkBorders.Build(new[] { Path(370000, 6964000, 370010, 6964000) }, .5);
            Require(shifted.Paths.Count == 2 && shifted.Paths.All(p => p.All(c => Math.Abs(Math.Abs(c.Y - 6964000) - .5) < 1e-8)), "Precision en coordenadas UTM.");
            using (var arc = new Arc(new Point3d(0, 0, 0), 10, 0, Math.PI))
            {
                var sampled = DrawingBorders.Read(arc);
                Require(sampled.Points.Length > 64, "Muestreo de curvas.");
                var arcBorders = NetworkBorders.Build(new[] { sampled.Points }, .5);
                Require(arcBorders.Paths.Count == 2, "Offset de arco aproximado sin tapas.");
            }
            using (var vertical = new Line(new Point3d(0, 0, 0), new Point3d(1, 0, 1)))
            {
                bool rejected = false;
                try { DrawingBorders.Read(vertical); } catch (InvalidOperationException) { rejected = true; }
                Require(rejected, "No se aplanan ejes con cotas variables.");
            }
            using (var db = new Database(true, true))
            {
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var layers = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);
                    var layer = new LayerTableRecord { Name = "axes" };
                    layers.Add(layer); tr.AddNewlyCreatedDBObject(layer, true);
                    var model = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
                    foreach (double z in new[] { 0.0, 10.0 })
                    {
                        var poly = new Autodesk.AutoCAD.DatabaseServices.Polyline();
                        poly.AddVertexAt(0, new Point2d(0, 0), 0, 0, 0);
                        poly.AddVertexAt(1, new Point2d(10, 0), 0, 0, 0);
                        poly.LayerId = layer.ObjectId; poly.Elevation = z;
                        poly.ConstantWidth = 2;
                        poly.SetStartWidthAt(0, 1); poly.SetEndWidthAt(0, 3);
                        model.AppendEntity(poly); tr.AddNewlyCreatedDBObject(poly, true);
                    }
                    tr.Commit();
                }
                var planar = DrawingBorders.Generate(db, "axes", .5, false);
                Require(planar.Sources == 1 && planar.Skipped == 1 && planar.Borders == 2, "Filtro de ejes abiertos en Z=0.");
                var all = DrawingBorders.Generate(db, "axes", .5, true);
                Require(all.Sources == 2 && all.Groups == 2 && all.Borders == 4 && all.Replaced == 2, "Todas preserva cada elevacion sin conectarlas entre si.");
                CheckLayer(db);
            }
        }

        private static void RealDrawing(string source, string target, int stage)
        {
            var ed = App.DocumentManager.MdiActiveDocument.Editor;
            string input = System.IO.Path.Combine(source, "Drainage Stage " + stage + ".dxf");
            string stem = "Drainage Stage " + stage + " - Bordes 0.50";
            string layer = "0_DLC 1 Drainage stg" + stage;
            using (var db = new Database(false, true))
            {
                db.DxfIn(input, System.IO.Path.Combine(target, "import-stage-" + stage + ".log"));
                if (stage == 1)
                {
                    // Explicitly requested completion: replace the unfinished manual
                    // borders only in this new result database, never in the source file.
                    using (var tr = db.TransactionManager.StartTransaction())
                    {
                        foreach (ObjectId id in (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead))
                        {
                            var e = (Entity)tr.GetObject(id, OpenMode.ForRead);
                            if (e.Layer == DrawingBorders.LayerName) { e.UpgradeOpen(); e.Erase(); }
                        }
                        tr.Commit();
                    }
                }
                string original = Signature(db, false, layer);
                var first = DrawingBorders.Generate(db, layer, .5, false);
                Require(first.Sources == (stage == 1 ? 53 : stage == 2 ? 72 : 85) && first.Skipped == (stage == 1 ? 4 : 5), "Seleccion de ejes reales de Stage " + stage);
                Require(original == SourceSignature(db), "Ejes, rectangulos y polilineas elevadas intactos.");
                string generated = BorderSignature(db);
                var second = DrawingBorders.Generate(db, layer, .5, false);
                Require(first.Borders == second.Borders && second.Replaced == first.Borders && generated == BorderSignature(db), "Regeneracion sin duplicados.");
                Require(original == SourceSignature(db), "Origen intacto al regenerar.");
                CheckLayer(db);
                Export(db, System.IO.Path.Combine(target, "stage-" + stage + ".txt"));
                db.DxfOut(System.IO.Path.Combine(target, stem + ".dxf"), 16, DwgVersion.AC1027);
                db.SaveAs(System.IO.Path.Combine(target, stem + ".dwg"), DwgVersion.AC1027);
                File.WriteAllText(System.IO.Path.Combine(target, "stage-" + stage + "-report.txt"),
                    "Stage=" + stage + "\r\nSourceLayer=" + layer + "\r\nOffsetEachSide=0.50\r\n" +
                    "Sources=" + first.Sources + "\r\nSkipped=" + first.Skipped + "\r\nBorders=" + first.Borders +
                    "\r\nVertices=" + first.Vertices + "\r\nJunctions=" + first.Junctions + "\r\n" +
                    "Length=" + Number(first.Length) + "\r\nOriginalCoordinatesUnchanged=True\r\nAllPolylineWidthsZero=True\r\nIdempotent=True\r\n");
                using (var reopened = new Database(false, true))
                {
                    reopened.DxfIn(System.IO.Path.Combine(target, stem + ".dxf"), System.IO.Path.Combine(target, "reopen-stage-" + stage + ".log"));
                    Require(Near(original, SourceSignature(reopened)), "La exportacion DXF conserva toda la geometria original.");
                    Require(Near(generated, BorderSignature(reopened)), "Reapertura DXF con los mismos bordes.");
                    CheckLayer(reopened);
                }
                using (var reopened = new Database(false, true))
                {
                    reopened.ReadDwgFile(System.IO.Path.Combine(target, stem + ".dwg"), FileOpenMode.OpenForReadAndAllShare, false, null);
                    Require(original == SourceSignature(reopened) && generated == BorderSignature(reopened), "Reapertura DWG conserva ejes y bordes.");
                    CheckLayer(reopened);
                }
                ed.WriteMessage("\nDREN_STAGE_{0}_OK sources={1} skipped={2} borders={3} junctions={4}\n",
                    stage, first.Sources, first.Skipped, first.Borders, first.Junctions);
            }
        }
        private static void ReferenceDrawing(string source, string target)
        {
            string input = System.IO.Path.Combine(source, "Drainage Stage 1.dxf");
            if (!File.Exists(input)) return;
            using (var db = new Database(false, true))
            {
                db.DxfIn(input, System.IO.Path.Combine(target, "reference-stage-1.log"));
                Export(db, System.IO.Path.Combine(target, "stage-1-reference.txt"));
                string before = SourceSignature(db) + BorderSignature(db);
                bool rejected = false;
                try { DrawingBorders.Generate(db, "0_DLC 1 Drainage stg1", .5, false); }
                catch (InvalidOperationException ex) { rejected = ex.Message.Contains("manuales"); }
                Require(rejected && before == SourceSignature(db) + BorderSignature(db), "Los bordes manuales de Stage 1 se protegen.");
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var model = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
                    foreach (ObjectId id in model)
                    {
                        var entity = (Entity)tr.GetObject(id, OpenMode.ForRead);
                        if (entity.Layer == DrawingBorders.LayerName) { entity.UpgradeOpen(); entity.Erase(); }
                    }
                    tr.Commit();
                }
                var report = DrawingBorders.Generate(db, "0_DLC 1 Drainage stg1", .5, false);
                Require(report.Sources == 53 && report.Skipped == 4, "Stage 1 usa los mismos ejes que el avance manual.");
                Export(db, System.IO.Path.Combine(target, "stage-1-automatic.txt"));
                App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nDREN_STAGE_1_REFERENCE_OK sources={0} borders={1} junctions={2}\n", report.Sources, report.Borders, report.Junctions);
            }
        }
        private static void CheckLayer(Database db)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var layers = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                var layer = (LayerTableRecord)tr.GetObject(layers[DrawingBorders.LayerName], OpenMode.ForRead);
                Require(layer.Color.ColorIndex == 5 && !layer.IsFrozen && !layer.IsOff, "Capa azul visible.");
                foreach (ObjectId id in (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead))
                {
                    var entity = (Entity)tr.GetObject(id, OpenMode.ForRead);
                    if (entity.Layer == DrawingBorders.LayerName) Require(entity.ColorIndex == 256, "Todos los bordes usan ByLayer.");
                    var poly = entity as Polyline;
                    if (poly != null)
                    {
                        Require(poly.ConstantWidth == 0, "Ancho global cero: " + poly.Handle);
                        for (int i = 0; i < poly.NumberOfVertices; i++)
                            Require(poly.GetStartWidthAt(i) == 0 && poly.GetEndWidthAt(i) == 0,
                                "Anchos inicial y final cero: " + poly.Handle + "/" + i);
                    }
                }
            }
        }
        private static string SourceSignature(Database db) { return Signature(db, false); }
        private static string BorderSignature(Database db) { return Signature(db, true); }
        private static bool Near(string a, string b)
        {
            // DXF serializes decimal doubles; UTM coordinates can round by a few
            // nanounits. Database mutation checks above remain bit-for-bit exact.
            var left = a.Split(new[] { '\n', '|', ',' });
            var right = b.Split(new[] { '\n', '|', ',' });
            if (left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] == right[i]) continue;
                double x, y;
                if (!double.TryParse(left[i], NumberStyles.Float, CultureInfo.InvariantCulture, out x) ||
                    !double.TryParse(right[i], NumberStyles.Float, CultureInfo.InvariantCulture, out y) || Math.Abs(x - y) > 1e-8) return false;
            }
            return true;
        }
        private static string Signature(Database db, bool borders, string normalizedLayer = null)
        {
            var rows = new List<string>();
            using (var tr = db.TransactionManager.StartTransaction())
                foreach (ObjectId id in (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead))
                {
                    var entity = (Entity)tr.GetObject(id, OpenMode.ForRead);
                    if ((entity.Layer == DrawingBorders.LayerName) != borders) continue;
                    var poly = entity as Polyline;
                    var row = new StringBuilder();
                    row.Append(borders ? "" : entity.Handle.ToString()).Append('|').Append(entity.Layer).Append('|').Append(entity.ColorIndex);
                    Require(poly != null, "La fuente de estas pruebas debe contener polilineas.");
                    bool zeroWidth = entity.Layer == normalizedLayer;
                    row.Append('|').Append(poly.Closed).Append('|').Append(Number(poly.Elevation)).Append('|').Append(poly.Normal.ToString());
                    row.Append('|').Append(Number(poly.Thickness)).Append('|').Append(Number(zeroWidth ? 0 : poly.ConstantWidth));
                    for (int i = 0; i < poly.NumberOfVertices; i++)
                    {
                        var p = poly.GetPoint2dAt(i);
                        row.Append('|').Append(Number(p.X)).Append(',').Append(Number(p.Y)).Append(',').Append(Number(poly.GetBulgeAt(i)))
                            .Append(',').Append(Number(zeroWidth ? 0 : poly.GetStartWidthAt(i))).Append(',').Append(Number(zeroWidth ? 0 : poly.GetEndWidthAt(i)));
                    }
                    rows.Add(row.ToString());
                }
            rows.Sort(StringComparer.Ordinal);
            return string.Join("\n", rows);
        }
        private static void Export(Database db, string path)
        {
            using (var writer = new StreamWriter(path, false, Encoding.UTF8))
            using (var tr = db.TransactionManager.StartTransaction())
                foreach (ObjectId id in (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead))
                {
                    var poly = tr.GetObject(id, OpenMode.ForRead) as Polyline;
                    if (poly == null) continue;
                    writer.Write(poly.Layer == DrawingBorders.LayerName ? "B" : "S");
                    writer.Write("|" + poly.Handle + "|" + Number(poly.Elevation) + "|" + poly.Closed + "|");
                    for (int i = 0; i < poly.NumberOfVertices; i++)
                    {
                        var p = poly.GetPoint2dAt(i);
                        if (i > 0) writer.Write(';');
                        writer.Write(Number(p.X) + "," + Number(p.Y));
                    }
                    writer.WriteLine();
                }
        }
    }
}
