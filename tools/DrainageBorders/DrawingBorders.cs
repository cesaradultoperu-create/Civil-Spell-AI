using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Coordinate = NetTopologySuite.Geometries.Coordinate;

namespace DrenBordes
{
    internal sealed class DrawingResult
    {
        internal int Sources, Skipped, Borders, Vertices, Replaced, Junctions, Groups;
        internal double Length;
    }
    internal static class DrawingBorders
    {
        internal const string LayerName = "0_DCL_Bordes";
        internal const string AppName = "DRENBORDES_V1";
        internal const double CurveTolerance = .001;
        internal sealed class Axis
        {
            internal Coordinate[] Points;
            internal double Z;
        }
        internal static Dictionary<string, int> Layers(Database db)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            using (var tr = db.TransactionManager.StartTransaction())
                foreach (ObjectId id in (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead))
                {
                    var entity = (Entity)tr.GetObject(id, OpenMode.ForRead);
                    if (entity is Curve && !string.Equals(entity.Layer, LayerName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!result.ContainsKey(entity.Layer)) result.Add(entity.Layer, 0);
                        result[entity.Layer]++;
                    }
                }
            return result;
        }
        internal static DrawingResult Generate(Database db, string layer, double offset, bool all)
        {
            if (string.Equals(layer, LayerName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("La capa de ejes debe ser diferente de 0_DCL_Bordes.");
            var report = new DrawingResult();
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var model = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
                var old = new List<ObjectId>();
                var axes = new List<Axis>();
                var widthSources = new List<ObjectId>();
                foreach (ObjectId id in model)
                {
                    var entity = (Entity)tr.GetObject(id, OpenMode.ForRead);
                    if (string.Equals(entity.Layer, LayerName, StringComparison.OrdinalIgnoreCase))
                    {
                        using (var data = entity.GetXDataForApplication(AppName))
                        {
                            if (data == null) throw new InvalidOperationException(
                                "0_DCL_Bordes contiene bordes manuales. Pruebe en una copia sin esos bordes; se conservaron todos los objetos.");
                            var values = data.AsArray();
                            if (values.Length >= 2 && string.Equals(values[1].Value as string, layer, StringComparison.OrdinalIgnoreCase)) old.Add(id);
                        }
                    }
                    if (!string.Equals(entity.Layer, layer, StringComparison.OrdinalIgnoreCase)) continue;
                    if (entity is Polyline || entity is Polyline2d) widthSources.Add(id);
                    var curve = entity as Curve;
                    if (curve == null) { report.Skipped++; continue; }
                    if (!all && (curve.Closed || Math.Abs(curve.StartPoint.Z) > NetworkBorders.Grid)) { report.Skipped++; continue; }
                    try { axes.Add(Read(curve)); report.Sources++; }
                    catch (Exception ex) { throw new InvalidOperationException("Objeto " + entity.Handle + ": " + ex.Message, ex); }
                }
                if (axes.Count == 0) throw new InvalidOperationException("No se encontraron ejes compatibles en " + layer + ".");
                // Compute every group before upgrading any database object for writing.
                var computed = axes.GroupBy(a => Math.Round(a.Z / NetworkBorders.Grid)).Select(group => new
                {
                    Z = group.First().Z,
                    Result = NetworkBorders.Build(group.Select(a => a.Points).ToList(), offset)
                }).ToList();
                report.Groups = computed.Count;
                var layers = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                LayerTableRecord borderLayer;
                if (layers.Has(LayerName))
                    borderLayer = (LayerTableRecord)tr.GetObject(layers[LayerName], OpenMode.ForWrite);
                else
                {
                    layers.UpgradeOpen();
                    borderLayer = new LayerTableRecord { Name = LayerName };
                    layers.Add(borderLayer); tr.AddNewlyCreatedDBObject(borderLayer, true);
                }
                borderLayer.Color = Color.FromColorIndex(ColorMethod.ByAci, 5);
                borderLayer.IsOff = false; borderLayer.IsFrozen = false; borderLayer.IsLocked = false;
                var apps = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
                if (!apps.Has(AppName))
                {
                    apps.UpgradeOpen();
                    var app = new RegAppTableRecord { Name = AppName };
                    apps.Add(app); tr.AddNewlyCreatedDBObject(app, true);
                }
                model.UpgradeOpen();
                // Stage 1 uses zero-width centerlines and references. Do not carry
                // the 1/2-unit display widths stored in the Stage 2/3 source files.
                foreach (var id in widthSources) ClearWidth((Entity)tr.GetObject(id, OpenMode.ForWrite), tr);
                foreach (var group in computed)
                {
                    report.Junctions += group.Result.Junctions;
                    foreach (var path in group.Result.Paths)
                    {
                        bool closed = path[0].Equals2D(path[path.Length - 1]);
                        int count = path.Length - (closed ? 1 : 0);
                        if (count < (closed ? 3 : 2)) throw new InvalidOperationException("Contorno degenerado.");
                        var poly = new Polyline(count);
                        poly.SetDatabaseDefaults(db);
                        for (int i = 0; i < count; i++) poly.AddVertexAt(i, new Point2d(path[i].X, path[i].Y), 0, 0, 0);
                        poly.ConstantWidth = 0;
                        poly.Closed = closed; poly.Elevation = group.Z;
                        poly.LayerId = borderLayer.ObjectId; poly.ColorIndex = 256;
                        poly.LinetypeId = db.ContinuousLinetype; poly.LineWeight = LineWeight.ByLayer;
                        model.AppendEntity(poly); tr.AddNewlyCreatedDBObject(poly, true);
                        using (var data = new ResultBuffer(new TypedValue(1001, AppName), new TypedValue(1000, layer),
                            new TypedValue(1040, offset), new TypedValue(1000, all ? "Todas" : "Ejes"))) poly.XData = data;
                        report.Borders++; report.Vertices += count; report.Length += poly.Length;
                    }
                }
                foreach (var id in old) ((Entity)tr.GetObject(id, OpenMode.ForWrite)).Erase();
                report.Replaced = old.Count;
                tr.Commit();
            }
            return report;
        }
        private static void ClearWidth(Entity entity, Transaction tr)
        {
            var poly = entity as Polyline;
            if (poly != null)
            {
                poly.ConstantWidth = 0;
                for (int i = 0; i < poly.NumberOfVertices; i++)
                { poly.SetStartWidthAt(i, 0); poly.SetEndWidthAt(i, 0); }
                return;
            }
            var legacy = entity as Polyline2d;
            if (legacy == null) return;
            legacy.DefaultStartWidth = 0; legacy.DefaultEndWidth = 0;
            foreach (ObjectId id in legacy)
            {
                var vertex = (Vertex2d)tr.GetObject(id, OpenMode.ForWrite);
                vertex.StartWidth = 0; vertex.EndWidth = 0;
            }
        }
        internal static Axis Read(Curve curve)
        {
            if (!(curve is Polyline || curve is Line || curve is Polyline2d || curve is Polyline3d || curve is Arc))
                throw new InvalidOperationException("Se admiten LINE, ARC y polilineas planas; convierta esta curva antes de ejecutar.");
            var points = new List<Point3d> { curve.StartPoint };
            var poly = curve as Polyline;
            var poly2 = curve as Polyline2d;
            if (poly2 != null && poly2.PolyType != Poly2dType.SimplePoly)
                throw new InvalidOperationException("Convierta la polilinea ajustada a una polilinea simple.");
            var poly3 = curve as Polyline3d;
            if (poly3 != null && poly3.PolyType != Poly3dType.SimplePoly)
                throw new InvalidOperationException("Convierta la polilinea 3D ajustada a una polilinea simple.");
            double start = curve.StartParam, end = curve.EndParam;
            int segments = curve is Line || curve is Arc ? 1 : (int)Math.Round(end - start);
            if (segments < 1) throw new InvalidOperationException("Eje sin segmentos.");
            for (int i = 0; i < segments; i++)
            {
                double a = start + (end - start) * i / segments;
                double b = start + (end - start) * (i + 1) / segments;
                Sample(curve, a, b, curve.GetPointAtParameter(a), curve.GetPointAtParameter(b), points, 0);
            }
            double z = points[0].Z;
            if (points.Any(p => Math.Abs(p.Z - z) > NetworkBorders.Grid))
                throw new InvalidOperationException("El eje tiene cotas variables. Esta herramienta trabaja con redes horizontales.");
            var xy = new List<Coordinate>();
            foreach (var p in points)
                if (xy.Count == 0 || xy[xy.Count - 1].Distance(new Coordinate(p.X, p.Y)) >= NetworkBorders.Grid)
                    xy.Add(new Coordinate(p.X, p.Y));
            if (xy.Count < 2) throw new InvalidOperationException("Eje de longitud nula.");
            if (xy.Count > 100000) throw new InvalidOperationException("Demasiados vertices en un eje.");
            return new Axis { Points = xy.ToArray(), Z = z };
        }
        private static void Sample(Curve curve, double a, double b, Point3d p, Point3d q, List<Point3d> output, int depth)
        {
            var mid = curve.GetPointAtParameter((a + b) / 2);
            var chord = q - p;
            double t = chord.LengthSqrd == 0 ? 0 : Math.Max(0, Math.Min(1, (mid - p).DotProduct(chord) / chord.LengthSqrd));
            double error = mid.DistanceTo(p + chord * t);
            if (error <= CurveTolerance) { output.Add(q); return; }
            if (depth >= 24 || output.Count >= 100000) throw new InvalidOperationException("Curva demasiado compleja.");
            Sample(curve, a, (a + b) / 2, p, mid, output, depth + 1);
            Sample(curve, (a + b) / 2, b, mid, q, output, depth + 1);
        }
    }
}
