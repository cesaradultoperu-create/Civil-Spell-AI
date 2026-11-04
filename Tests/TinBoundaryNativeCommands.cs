using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CivilSpellAI.Autodesk;
using CivilSpellAI.Geometry;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CivilSpellAI.Tests
{
    public sealed class TinBoundaryNativeCommands
    {
        [CommandMethod("AISPELLTESTTINNINESESSION")]
        public void TestNineSession()
        {
            var doc = AcadApplication.DocumentManager.MdiActiveDocument;
            try
            {
                string path = Environment.GetEnvironmentVariable("CIVILSPELLAI_NINE_TSV");
                var surfaces = new SortedDictionary<int, TinFootprint>();
                foreach (var line in System.IO.File.ReadLines(path).Skip(1))
                {
                    var cells = line.Split('\t'); if (cells.Length != 5) continue;
                    int s = int.Parse(cells[0]), r = int.Parse(cells[1]);
                    TinFootprint footprint;
                    if (!surfaces.TryGetValue(s, out footprint)) { footprint = new TinFootprint { Name = "TIN exportado " + s }; surfaces.Add(s, footprint); }
                    while (footprint.Rings.Count <= r) footprint.Rings.Add(new List<TinBoundaryPoint>());
                    footprint.Rings[r].Add(new TinBoundaryPoint(double.Parse(cells[2], System.Globalization.CultureInfo.InvariantCulture),
                        double.Parse(cells[3], System.Globalization.CultureInfo.InvariantCulture), double.Parse(cells[4], System.Globalization.CultureInfo.InvariantCulture)));
                }
                Require(surfaces.Count == 9, "La prueba exige los nueve TIN exportados de la sesion real.");
                var timer = System.Diagnostics.Stopwatch.StartNew();
                var source = surfaces.Values.ToList();
                var loops = TinBoundaryGeometry.Build(source);
                var outer = TinBoundaryGeometry.Exterior(loops);
                var contours = new List<List<TinBoundaryPoint>> { TinBoundaryGeometry.Simplify(outer, .05, true) };
                foreach (var hole in loops.Where(l => l.Area < -1))
                {
                    var coordinates = hole.Points.Select(p => new NetTopologySuite.Geometries.Coordinate(p.X,p.Y)).ToList();
                    coordinates.Add(coordinates[0].Copy());
                    var point = new NetTopologySuite.Geometries.GeometryFactory().CreatePolygon(coordinates.ToArray()).InteriorPoint.Coordinate;
                    var selected = TinBoundaryGeometry.PickEmpty(source, loops, new TinBoundaryPoint(point.X,point.Y,0));
                    Require(selected == hole, "Pick Points eligio otro recinto en los nueve TIN.");
                    contours.Add(TinBoundaryGeometry.Simplify(selected, .05, true));
                }
                Require(contours.Count == 4, "Se esperaban exterior y tres recintos vacios significativos.");
                TinBoundaryDrawingService.Write(doc.Database,contours);
                timer.Stop();
                string folder=System.IO.Path.GetDirectoryName(typeof(TinBoundaryNativeCommands).Assembly.Location);
                for(int i=0;i<contours.Count;i++)
                    using(var writer=new System.IO.StreamWriter(System.IO.Path.Combine(folder,"nine-contour-"+i+".tsv")))
                    {
                        writer.WriteLine("x\ty\tz");
                        foreach(var p in contours[i]) writer.WriteLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0:R}\t{1:R}\t{2:R}",p.X,p.Y,p.Z));
                    }
                doc.Editor.WriteMessage("\nTIN_NINE_SESSION_OK exterior={0}, pickPoints={1}, seconds={2:F3}, area={3:R}\n",
                    contours[0].Count,contours.Count-1,timer.Elapsed.TotalSeconds,outer.Area);
            }
            catch (System.Exception ex) { doc.Editor.WriteMessage("\nTIN_NINE_SESSION_FAILED "+ex+"\n"); }
        }

        [CommandMethod("AISPELLVERIFYTINNINE", CommandFlags.Modal | CommandFlags.NoUndoMarker)]
        public void VerifyNine()
        {
            var doc=AcadApplication.DocumentManager.MdiActiveDocument;
            try
            {
                using(var tr=doc.Database.TransactionManager.StartTransaction())
                {
                    var model=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(doc.Database),OpenMode.ForRead);
                    var contours=model.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<Polyline3d>()
                        .Where(p=>p.Layer==TinBoundaryDrawingService.LayerName).ToList();
                    Require(contours.Count==4 && contours.All(p=>p.Closed && p.ColorIndex==6),"Los cuatro recintos no son polilineas 3D cerradas magenta.");
                    foreach(var poly in contours)
                    {
                        var points=poly.Cast<ObjectId>().Select(id=>((PolylineVertex3d)tr.GetObject(id,OpenMode.ForRead)).Position).ToList();
                        Require(points.Count>=3 && points.All(p=>p.Z>2400 && p.Z<2600),"Se perdieron las cotas de referencia en la prueba real.");
                    }
                }
                doc.Editor.WriteMessage("\nTIN_NINE_REOPEN_OK\n");
            }
            catch(System.Exception ex) { doc.Editor.WriteMessage("\nTIN_NINE_SESSION_FAILED "+ex+"\n"); }
        }

        [CommandMethod("AISPELLVERIFYTINREALRESULT", CommandFlags.Modal | CommandFlags.NoUndoMarker)]
        public void VerifyRealResult()
        {
            var doc = AcadApplication.DocumentManager.MdiActiveDocument;
            try
            {
                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    var model = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(doc.Database), OpenMode.ForRead);
                    var entities = model.Cast<ObjectId>().Select(id => tr.GetObject(id, OpenMode.ForRead)).OfType<Entity>().ToList();
                    var contours = entities.OfType<Polyline3d>().Where(p => p.Layer == TinBoundaryDrawingService.LayerName).ToList();
                    Require(contours.Count == 1, "Debe haber exactamente un borde de resultado.");
                    var poly = contours[0];
                    Require(poly.Closed && poly.ColorIndex == 6, "El borde real no esta cerrado y magenta.");
                    var points = poly.Cast<ObjectId>().Select(id => ((PolylineVertex3d)tr.GetObject(id, OpenMode.ForRead)).Position).ToList();
                    Require(points.Count >= 3 && points.All(p => !double.IsNaN(p.Z) && !double.IsInfinity(p.Z)), "Vertices o cotas no validos.");
                    int tins = entities.Count(e => TinBoundaryDrawingService.IsTin(e));
                    Require(tins == 8, "No se conservaron los ocho TIN del archivo de prueba.");
                    doc.Editor.WriteMessage("\nTIN_REAL_RESULT_OK vertices={0}, surfaces={1}, zMin={2:R}, zMax={3:R}, area={4:R}\n",
                        points.Count, tins, points.Min(p => p.Z), points.Max(p => p.Z),
                        TinBoundaryGeometry.SignedArea(points.Select(p => new TinBoundaryPoint(p.X, p.Y, p.Z)).ToList()));
                }
            }
            catch (System.Exception ex) { doc.Editor.WriteMessage("\nTIN_REAL_RESULT_FAILED " + ex + "\n"); }
        }

        [CommandMethod("AISPELLDIAGNOSETINBOUNDARY", CommandFlags.Modal | CommandFlags.NoUndoMarker)]
        public void DiagnoseDrawing()
        {
            var doc = AcadApplication.DocumentManager.MdiActiveDocument;
            string diagnosticDirectory = System.IO.Path.GetDirectoryName(typeof(TinBoundaryNativeCommands).Assembly.Location);
            try
            {
                Assembly.LoadFrom(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(
                    HostApplicationServices.Current.FindFile("acad.exe", doc.Database, FindFileHint.Default)), "C3D", "AeccDbMgd.dll"));
                var ids = TinBoundaryDrawingService.VisibleTinIds(doc.Database);
                var allIds = new List<ObjectId>();
                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    var model = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(doc.Database), OpenMode.ForRead);
                    foreach (ObjectId id in model)
                    {
                        var entity = tr.GetObject(id, OpenMode.ForRead) as Entity;
                        if (!TinBoundaryDrawingService.IsTin(entity)) continue;
                        var layer = (LayerTableRecord)tr.GetObject(entity.LayerId, OpenMode.ForRead);
                        doc.Editor.WriteMessage("\nTIN_DIAG_ALL {0}: {1}, visible={2}, off={3}, frozen={4}", allIds.Count,
                            entity.GetType().GetProperty("Name").GetValue(entity, null), entity.Visible, layer.IsOff, layer.IsFrozen);
                        allIds.Add(id);
                    }
                }
                if (Environment.GetEnvironmentVariable("CIVILSPELLAI_DIAG_ALL_TINS") == "1") ids = allIds.ToArray();
                doc.Editor.WriteMessage("\nTIN_DIAG_COUNT {0}", ids.Length);
                // Read the geometry stored in the snapshot without synchronizing
                // its references or changing its source surfaces.
                var footprints = TinBoundaryDrawingService.Read(doc.Database, ids, true);
                string path = Environment.GetEnvironmentVariable("CIVILSPELLAI_TIN_DIAGNOSTIC_PATH");
                if (string.IsNullOrEmpty(path)) path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(
                    typeof(TinBoundaryNativeCommands).Assembly.Location), "live-tin-boundary.tsv");
                using (var writer = new System.IO.StreamWriter(path, false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("surface\tring\tx\ty\tz");
                    for (int s = 0; s < footprints.Count; s++)
                    {
                        doc.Editor.WriteMessage("\nTIN_DIAG_SURFACE {0}: {1}, rings={2}, vertices={3}", s, footprints[s].Name,
                            footprints[s].Rings.Count, footprints[s].Rings.Sum(r => r.Count));
                        for (int r = 0; r < footprints[s].Rings.Count; r++)
                        foreach (var p in footprints[s].Rings[r]) writer.WriteLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                            "{0}\t{1}\t{2:R}\t{3:R}\t{4:R}", s, r, p.X, p.Y, p.Z));
                    }
                }
                doc.Editor.WriteMessage("\nTIN_DIAG_EXPORTED {0}\n", path);
                var loops = TinBoundaryGeometry.Build(footprints);
                doc.Editor.WriteMessage("\nTIN_DIAG_LOOPS {0}\n", loops.Count);
                foreach (var loop in loops) doc.Editor.WriteMessage("\nTIN_DIAG_LOOP area={0:R}, vertices={1}, Z={2}",
                    loop.Area, loop.Points.Count, loop.ElevationError ?? "OK");
                var contour = TinBoundaryGeometry.Simplify(TinBoundaryGeometry.Exterior(loops), .001, true);
                doc.Editor.WriteMessage("\nTIN_DIAG_EXTERIOR_OK vertices={0}\n", contour.Count);
            }
            catch (System.Exception ex)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(diagnosticDirectory, "diagnostic-error.txt"), ex.ToString());
                doc.Editor.WriteMessage("\nTIN_DIAG_FAILED " + ex + "\n");
            }
        }

        [CommandMethod("AISPELLTESTTINNATIVE")]
        public void WriteAndVerify()
        {
            var doc = AcadApplication.DocumentManager.MdiActiveDocument;
            try
            {
                var frame = Frame();
                var loops = TinBoundaryGeometry.Build(frame);
                var hole = TinBoundaryGeometry.PickEmpty(frame, loops, P(5, 5));
                var ids = TinBoundaryDrawingService.Write(doc.Database,
                    new[] { TinBoundaryGeometry.Simplify(hole, .001) });
                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    var poly = (Polyline3d)tr.GetObject(ids[0], OpenMode.ForRead);
                    Require(poly.Closed && poly.Layer == TinBoundaryDrawingService.LayerName && poly.ColorIndex == 6, "Tipo/capa/cierre/color magenta incorrectos.");
                    var vertices = poly.Cast<ObjectId>().Select(id => ((PolylineVertex3d)tr.GetObject(id, OpenMode.ForRead)).Position).ToList();
                    Require(vertices.Count == 4 && vertices.All(p => Math.Abs(p.Z - (100 + p.X + 2 * p.Y)) < 1e-8), "No conserva los vertices XYZ.");
                }
                doc.Editor.WriteMessage("\nTINBOUNDARY_NATIVE_WRITE_OK\n");
            }
            catch (System.Exception ex) { doc.Editor.WriteMessage("\nTINBOUNDARY_FAILED " + ex + "\n"); }
        }

        [CommandMethod("AISPELLVERIFYTINNATIVE", CommandFlags.Modal | CommandFlags.NoUndoMarker)]
        public void VerifyReopened()
        {
            var doc = AcadApplication.DocumentManager.MdiActiveDocument;
            try
            {
                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    var model = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(doc.Database), OpenMode.ForRead);
                    var polylines = model.Cast<ObjectId>().Select(id => tr.GetObject(id, OpenMode.ForRead)).OfType<Polyline3d>()
                        .Where(p => p.Layer == TinBoundaryDrawingService.LayerName).ToList();
                    Require(polylines.Count == 1, "Numero de contornos guardados incorrecto.");
                    foreach (var poly in polylines)
                    {
                        Require(poly.Closed && poly.ColorIndex == 6, "Contorno abierto o color magenta perdido al reabrir.");
                        foreach (ObjectId id in poly)
                        {
                            var p = ((PolylineVertex3d)tr.GetObject(id, OpenMode.ForRead)).Position;
                            Require(Math.Abs(p.Z - (100 + p.X + 2 * p.Y)) < 1e-8, "Se perdieron las cotas al reabrir.");
                        }
                    }
                }
                doc.Editor.WriteMessage("\nTINBOUNDARY_REOPEN_OK\n");
            }
            catch (System.Exception ex) { doc.Editor.WriteMessage("\nTINBOUNDARY_FAILED " + ex + "\n"); }
        }

        [CommandMethod("AISPELLVERIFYTINUNDONE", CommandFlags.Modal | CommandFlags.NoUndoMarker)]
        public void VerifyUndo()
        {
            var doc = AcadApplication.DocumentManager.MdiActiveDocument;
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var model = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(doc.Database), OpenMode.ForRead);
                bool remains = model.Cast<ObjectId>().Select(id => tr.GetObject(id, OpenMode.ForRead)).OfType<Polyline3d>()
                    .Any(p => p.Layer == TinBoundaryDrawingService.LayerName);
                doc.Editor.WriteMessage(remains ? "\nTINBOUNDARY_FAILED Undo dejo el contorno.\n" : "\nTINBOUNDARY_UNDO_OK\n");
            }
        }

        [CommandMethod("AISPELLTESTTINSURFACES")]
        public void RealSurfaces()
        {
            var doc = AcadApplication.DocumentManager.MdiActiveDocument;
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "AeccDbMgd") ??
                    Assembly.LoadFrom(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(HostApplicationServices.Current.FindFile("acad.exe", doc.Database, FindFileHint.Default)), "C3D", "AeccDbMgd.dll"));
                var type = assembly.GetType("Autodesk.Civil.DatabaseServices.TinSurface", true);
                var create = type.GetMethod("Create", new[] { typeof(Database), typeof(string) });
                var ids = new List<ObjectId>();
                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    foreach (var footprint in Frame())
                    {
                        var id = (ObjectId)create.Invoke(null, new object[] { doc.Database, "BORDETIN3D_TEST_" + ids.Count });
                        var surface = tr.GetObject(id, OpenMode.ForWrite);
                        var points = new Point3dCollection(footprint.Rings[0].Select(p => new Point3d(p.X, p.Y, p.Z)).ToArray());
                        type.GetMethod("AddVertices", new[] { typeof(Point3dCollection) }).Invoke(surface, new object[] { points });
                        type.GetMethod("Rebuild", Type.EmptyTypes).Invoke(surface, null);
                        ids.Add(id);
                    }
                    tr.Commit();
                }
                int before = Count(doc.Database);
                var read = TinBoundaryDrawingService.Read(doc.Database, ids.ToArray());
                Require(Count(doc.Database) == before, "ExtractBorder deja objetos temporales.");
                var hole = TinBoundaryGeometry.PickEmpty(read, TinBoundaryGeometry.Build(read), P(5, 5));
                Require(Math.Abs(hole.Area + 36) < 1e-6 && hole.Points.All(p => Math.Abs(p.Z - (100 + p.X + 2 * p.Y)) < 1e-6), "Lectura real TIN incorrecta.");
                doc.Editor.WriteMessage("\nTINBOUNDARY_REAL_SURFACES_OK\n");
            }
            catch (System.Exception ex) { doc.Editor.WriteMessage("\nTINBOUNDARY_REAL_SURFACES_UNAVAILABLE " + ex + "\n"); }
        }

        [CommandMethod("AISPELLVERIFYTINCOMMANDS", CommandFlags.Modal | CommandFlags.NoUndoMarker)]
        public void VerifyCommands()
        {
            var doc = AcadApplication.DocumentManager.MdiActiveDocument;
            try
            {
                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    var model = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(doc.Database), OpenMode.ForRead);
                    var entities = model.Cast<ObjectId>().Select(id => tr.GetObject(id, OpenMode.ForRead)).OfType<Entity>().ToList();
                    Require(entities.Count(e => TinBoundaryDrawingService.IsTin(e)) == 4, "Se altero el numero de TIN.");
                    var polylines = entities.OfType<Polyline3d>().Where(p => p.Layer == TinBoundaryDrawingService.LayerName).ToList();
                    Require(polylines.Count == 3, "PickPoints/Seleccion no crearon exactamente dos nuevos contornos.");
                    var areas = new List<double>();
                    foreach (var poly in polylines)
                    {
                        Require(poly.Closed && poly.ColorIndex == 6, "El comando creo un contorno abierto o sin color magenta.");
                        var points = poly.Cast<ObjectId>().Select(id => ((PolylineVertex3d)tr.GetObject(id, OpenMode.ForRead)).Position).ToList();
                        Require(points.Count == 4 && points.All(p => Math.Abs(p.Z - (100 + p.X + 2 * p.Y)) < 1e-6), "El comando perdio cotas o simplificacion.");
                        areas.Add(TinBoundaryGeometry.SignedArea(points.Select(p => new TinBoundaryPoint(p.X, p.Y, p.Z)).ToList()));
                    }
                    Require(areas.Count(a => Math.Abs(a + 36) < 1e-6) == 2 && areas.Count(a => Math.Abs(a - 100) < 1e-6) == 1,
                        "Pick Points o Seleccion devolvieron el recinto incorrecto.");
                    Require(entities.Count == 7, "Quedan objetos extraidos temporales en Model.");
                }
                doc.Editor.WriteMessage("\nTINBOUNDARY_INTERACTIVE_COMMANDS_OK\n");
            }
            catch (System.Exception ex) { doc.Editor.WriteMessage("\nTINBOUNDARY_FAILED " + ex + "\n"); }
        }

        [CommandMethod("AISPELLVERIFYTINCOMMANDUNDO", CommandFlags.Modal | CommandFlags.NoUndoMarker)]
        public void VerifyCommandUndo()
        {
            var doc = AcadApplication.DocumentManager.MdiActiveDocument;
            try
            {
                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    var model = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(doc.Database), OpenMode.ForRead);
                    var entities = model.Cast<ObjectId>().Select(id => tr.GetObject(id, OpenMode.ForRead)).OfType<Entity>().ToList();
                    Require(entities.Count == 6 && entities.Count(e => TinBoundaryDrawingService.IsTin(e)) == 4 &&
                        entities.OfType<Polyline3d>().Count() == 2, "Una U no retiro exactamente el ultimo borde sin alterar los TIN.");
                }
                doc.Editor.WriteMessage("\nTINBOUNDARY_COMMAND_UNDO_OK\n");
            }
            catch (System.Exception ex) { doc.Editor.WriteMessage("\nTINBOUNDARY_FAILED " + ex + "\n"); }
        }

        private static int Count(Database db)
        {
            using (var tr = db.TransactionManager.StartTransaction())
                return ((BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead)).Cast<ObjectId>().Count();
        }
        private static TinBoundaryPoint P(double x, double y) { return new TinBoundaryPoint(x, y, 100 + x + 2 * y); }
        private static List<TinFootprint> Frame()
        {
            var result = new List<TinFootprint>();
            foreach (var r in new[] { new[] { 0, 0, 10, 2 }, new[] { 0, 8, 10, 10 }, new[] { 0, 2, 2, 8 }, new[] { 8, 2, 10, 8 } })
            {
                var footprint = new TinFootprint { Name = "Prueba" };
                footprint.Rings.Add(new List<TinBoundaryPoint> { P(r[0], r[1]), P(r[2], r[1]), P(r[2], r[3]), P(r[0], r[3]) });
                result.Add(footprint);
            }
            return result;
        }
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
    }
}
