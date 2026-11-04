using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CivilSpellAI.Geometry;

namespace CivilSpellAI.Autodesk
{
    internal static class TinBoundaryDrawingService
    {
        public const string LayerName = "CIVILSPELLAI_BORDE_TIN_3D";
        public const short BoundaryColorIndex = 6; // Magenta, distinct from the green TIN borders.

        // Resolve the Civil API in the running host, rather than binding 2025/2026
        // to the installed 2024 AeccDbMgd assembly.
        internal static bool IsTin(Entity entity)
        {
            return entity != null && entity.GetType().FullName == "Autodesk.Civil.DatabaseServices.TinSurface";
        }

        internal static ObjectId[] VisibleTinIds(Database db)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var model = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
                var ids = new List<ObjectId>();
                foreach (ObjectId id in model)
                {
                    var entity = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (!IsTin(entity) || !entity.Visible) continue;
                    var layer = (LayerTableRecord)tr.GetObject(entity.LayerId, OpenMode.ForRead);
                    if (!layer.IsOff && !layer.IsFrozen) ids.Add(id);
                }
                return ids.ToArray();
            }
        }

        internal static List<TinFootprint> Read(Database db, ObjectId[] ids, bool allowCachedReferenceData = false,
            Action<string> cachedReferenceNotice = null)
        {
            var result = new List<TinFootprint>();
            // ExtractBorder creates database entities. Abort this transaction after
            // copying geometry, including on failures: no extracted objects persist.
            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (var id in ids.Distinct())
                {
                    var entity = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (!IsTin(entity)) throw new InvalidOperationException("Seleccione solamente superficies TIN del dibujo actual.");
                    var type = entity.GetType();
                    string name = (string)type.GetProperty("Name").GetValue(entity, null);
                    try
                    {
                        if (BoolProperty(entity, "IsReferenceObject") && !BoolProperty(entity, "IsReferenceValid"))
                            throw new InvalidOperationException("La referencia de datos no es valida.");
                        if (BoolProperty(entity, "IsReferenceStale"))
                        {
                            if (!allowCachedReferenceData) throw new InvalidOperationException("Actualice primero la referencia de datos desactualizada.");
                            if (cachedReferenceNotice != null) cachedReferenceNotice(name);
                        }
                        var extract = type.GetMethods().First(m => m.Name == "ExtractBorder" && m.GetParameters().Length == 1);
                        var settings = Enum.Parse(extract.GetParameters()[0].ParameterType, "Plan");
                        var elevation = type.GetMethod("FindElevationAtXY", new[] { typeof(double), typeof(double) });
                        if (elevation == null) throw new InvalidOperationException("No esta disponible la consulta de cotas del TIN.");
                        var borders = (ObjectIdCollection)extract.Invoke(entity, new[] { settings });
                        var footprint = new TinFootprint { Name = name };
                        foreach (ObjectId border in borders)
                        {
                            var points = ReadBorder(tr, border);
                            var ring = new List<TinBoundaryPoint>();
                            foreach (var point in points)
                            {
                                // Styles may flatten extracted polylines; always query
                                // the actual surface elevation at the source vertex.
                                double z = (double)elevation.Invoke(entity, new object[] { point.X, point.Y });
                                ring.Add(new TinBoundaryPoint(point.X, point.Y, z));
                            }
                            if (ring.Count > 1 && TinBoundaryGeometry.Distance(ring[0], ring[ring.Count - 1]) <= TinBoundaryGeometry.TopologyTolerance)
                                ring.RemoveAt(ring.Count - 1);
                            if (ring.Count < 3) throw new InvalidOperationException("El borde tiene menos de tres vertices.");
                            footprint.Rings.Add(ring);
                        }
                        if (footprint.Rings.Count == 0) throw new InvalidOperationException("La superficie no devuelve bordes; revise sus datos y su estilo de borde en planta.");
                        result.Add(footprint);
                    }
                    catch (Exception ex)
                    {
                        while (ex is TargetInvocationException && ex.InnerException != null) ex = ex.InnerException;
                        throw new InvalidOperationException("TIN '" + name + "': " + ex.Message, ex);
                    }
                }
                tr.Abort();
            }
            if (result.Count == 0) throw new InvalidOperationException("No se encontraron superficies TIN en el conjunto.");
            return result;
        }

        private static bool BoolProperty(object value, string name)
        {
            var property = value.GetType().GetProperty(name);
            return property != null && (bool)property.GetValue(value, null);
        }

        private static List<Point3d> ReadBorder(Transaction tr, ObjectId id)
        {
            var entity = tr.GetObject(id, OpenMode.ForRead);
            var points = new List<Point3d>();
            bool closed;
            var poly3d = entity as Polyline3d;
            var poly = entity as Polyline;
            if (poly3d != null)
            {
                closed = poly3d.Closed;
                foreach (ObjectId vertex in poly3d) points.Add(((PolylineVertex3d)tr.GetObject(vertex, OpenMode.ForRead)).Position);
            }
            else if (poly != null)
            {
                closed = poly.Closed;
                for (int i = 0; i < poly.NumberOfVertices; i++)
                {
                    if (Math.Abs(poly.GetBulgeAt(i)) > 1e-12) throw new InvalidOperationException("El borde extraido contiene arcos inesperados.");
                    points.Add(poly.GetPoint3dAt(i));
                }
            }
            else throw new InvalidOperationException("El borde extraido no es una polilinea; revise el estilo del TIN.");
            if (!closed && (points.Count < 3 || points[0].DistanceTo(points[points.Count - 1]) > TinBoundaryGeometry.TopologyTolerance))
                throw new InvalidOperationException("El borde extraido esta abierto.");
            return points;
        }

        internal static ObjectId[] Write(Database db, IList<List<TinBoundaryPoint>> contours)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var layers = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                ObjectId layerId;
                if (!layers.Has(LayerName))
                {
                    layers.UpgradeOpen();
                    using (var layer = new LayerTableRecord { Name = LayerName,
                        Color = global::Autodesk.AutoCAD.Colors.Color.FromColorIndex(global::Autodesk.AutoCAD.Colors.ColorMethod.ByAci, BoundaryColorIndex) })
                    { layerId = layers.Add(layer); tr.AddNewlyCreatedDBObject(layer, true); }
                }
                else layerId = layers[LayerName];
                var targetLayer = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForRead);
                if (targetLayer.IsLocked || targetLayer.IsOff || targetLayer.IsFrozen)
                    throw new InvalidOperationException("Desbloquee y encienda la capa " + LayerName + " antes de generar el borde.");
                var model = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
                var ids = new List<ObjectId>();
                foreach (var contour in contours)
                {
                    var points = new Point3dCollection(contour.Select(p => new Point3d(p.X, p.Y, p.Z)).ToArray());
                    using (var poly = new Polyline3d(Poly3dType.SimplePoly, points, true))
                    {
                        poly.SetDatabaseDefaults(db); poly.LayerId = layerId;
                        poly.ColorIndex = BoundaryColorIndex;
                        ids.Add(model.AppendEntity(poly)); tr.AddNewlyCreatedDBObject(poly, true);
                    }
                }
                tr.Commit();
                return ids.ToArray();
            }
        }
    }
}
