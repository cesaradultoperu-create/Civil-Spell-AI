using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace CivilSpellAI.Autodesk
{
    internal sealed class FigureMeasureDrawingService
    {
        public ObjectId Apply(Database database, Transaction transaction, SelectedFigureSnapshot snapshot,
            FigureMeasureEdit edit, double width, double height, bool createCopy, Point3d insertion)
        {
            var service = new SelectedFigureDrawingService();
            if (edit == null || !edit.Changed)
                return service.Apply(database, transaction, snapshot, width, height, createCopy, insertion);

            // First clone/group the source through the existing transaction-safe path.
            ObjectId id = service.Apply(database, transaction, snapshot, snapshot.Width, snapshot.Height, createCopy, insertion);
            var reference = (BlockReference)transaction.GetObject(id, OpenMode.ForWrite);
            Matrix3d pose = reference.BlockTransform;
            Matrix3d inverse = pose.Inverse();
            Vector3d placement = createCopy ? insertion - snapshot.Origin : new Vector3d(0, 0, 0);
            Func<Point3d, Point3d> warp = point =>
                (edit.MapWorld(snapshot, point.TransformBy(pose) - placement) + placement).TransformBy(inverse);

            // An independent definition prevents changes to other references of the same block.
            var blocks = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForWrite);
            var definition = new BlockTableRecord { Name = "CIVILSPELLAI_FIGURA_" + Guid.NewGuid().ToString("N") };
            ObjectId definitionId = blocks.Add(definition);
            transaction.AddNewlyCreatedDBObject(definition, true);
            var oldDefinition = (BlockTableRecord)transaction.GetObject(reference.BlockTableRecord, OpenMode.ForRead);
            using (var mapping = new IdMapping())
                database.DeepCloneObjects(new ObjectIdCollection(oldDefinition.Cast<ObjectId>().ToArray()), definitionId, mapping, false);

            foreach (ObjectId childId in definition.Cast<ObjectId>().ToArray())
                Deform(database, transaction, definition, (Entity)transaction.GetObject(childId, OpenMode.ForWrite), warp, 0);
            reference.BlockTableRecord = definitionId;
            foreach (ObjectId dimensionId in SelectedFigureDrawingService.ReadDimensionIds(reference, transaction))
            {
                var dimension = (Dimension)transaction.GetObject(dimensionId, OpenMode.ForWrite);
                var layer = (LayerTableRecord)transaction.GetObject(dimension.LayerId, OpenMode.ForRead);
                if (layer.IsLocked) throw new InvalidOperationException("Una cota está en una capa bloqueada: " + layer.Name);
                DeformDimension(dimension, p => edit.MapWorld(snapshot, p - placement) + placement);
            }
            // Measure native geometry in the new definition; annotation extents are excluded.
            var extents = definition.Cast<ObjectId>().Where(child => !child.IsErased)
                .Select(child => transaction.GetObject(child, OpenMode.ForRead) as Entity)
                .Where(child => child != null && !(child is Dimension) && !(child is DBText) && !(child is MText))
                .Select(child => child.GeometricExtents).ToArray();
            var localBounds = new Extents3d(
                new Point3d(extents.Min(e => e.MinPoint.X), extents.Min(e => e.MinPoint.Y), extents.Min(e => e.MinPoint.Z)),
                new Point3d(extents.Max(e => e.MaxPoint.X), extents.Max(e => e.MaxPoint.Y), extents.Max(e => e.MaxPoint.Z)));
            double localWidth = localBounds.MaxPoint.X - localBounds.MinPoint.X;
            double localHeight = localBounds.MaxPoint.Y - localBounds.MinPoint.Y;
            SelectedFigureDrawingService.WriteBaseMeasures(reference, transaction, localWidth, localHeight);
            return id;
        }

        private static void Deform(Database database, Transaction transaction, BlockTableRecord owner,
            Entity entity, Func<Point3d, Point3d> warp, int depth)
        {
            if (depth > 12) throw new InvalidOperationException("El bloque tiene demasiados niveles anidados.");
            Hatch hatch = entity as Hatch;
            if (hatch != null) { DeformHatch(hatch, warp); return; }
            BlockReference block = entity as BlockReference;
            if (block != null)
            {
                if (block.AttributeCollection.Count > 0)
                    throw new InvalidOperationException("Este bloque contiene atributos. Seleccione sus contornos para editar las medidas interiores.");
                var blocks = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForWrite);
                var definition = new BlockTableRecord { Name = "CIVILSPELLAI_DETALLE_" + Guid.NewGuid().ToString("N") };
                ObjectId definitionId = blocks.Add(definition);
                transaction.AddNewlyCreatedDBObject(definition, true);
                var source = (BlockTableRecord)transaction.GetObject(block.BlockTableRecord, OpenMode.ForRead);
                if (source.IsFromExternalReference) throw new InvalidOperationException("La referencia externa debe copiarse al dibujo antes de editar sus medidas interiores.");
                using (var mapping = new IdMapping())
                    database.DeepCloneObjects(new ObjectIdCollection(source.Cast<ObjectId>().ToArray()), definitionId, mapping, false);
                Matrix3d matrix = block.BlockTransform, inverse = matrix.Inverse();
                foreach (ObjectId id in definition.Cast<ObjectId>().ToArray())
                    Deform(database, transaction, definition, (Entity)transaction.GetObject(id, OpenMode.ForWrite),
                        p => warp(p.TransformBy(matrix)).TransformBy(inverse), depth + 1);
                block.BlockTableRecord = definitionId;
                return;
            }
            Dimension dimension = entity as Dimension;
            if (dimension != null) { DeformDimension(dimension, warp); return; }
            DBText text = entity as DBText;
            if (text != null) { text.TransformBy(Matrix3d.Displacement(warp(text.Position) - text.Position)); return; }
            MText mtext = entity as MText;
            if (mtext != null) { mtext.TransformBy(Matrix3d.Displacement(warp(mtext.Location) - mtext.Location)); return; }
            Line line = entity as Line;
            if (line != null) { line.StartPoint = warp(line.StartPoint); line.EndPoint = warp(line.EndPoint); return; }
            Polyline polyline = entity as Polyline;
            if (polyline != null)
            {
                if ((polyline.Normal - Vector3d.ZAxis).Length > 1e-8)
                    throw new InvalidOperationException("Las medidas interiores requieren contornos en el plano XY.");
                var points = SamplePolyline(polyline);
                Vector3d delta;
                if (RigidTranslation(points, warp, out delta)) { polyline.TransformBy(Matrix3d.Displacement(delta)); return; }
                bool hasArcs = Enumerable.Range(0, polyline.NumberOfVertices).Any(i => Math.Abs(polyline.GetBulgeAt(i)) > 1e-10);
                if (!hasArcs)
                {
                    for (int i = 0; i < polyline.NumberOfVertices; i++)
                    {
                        Point3d mapped = warp(polyline.GetPoint3dAt(i));
                        polyline.SetPointAt(i, new Point2d(mapped.X, mapped.Y));
                    }
                    return;
                }
                ReplaceCurve(transaction, owner, polyline, points.Select(warp).ToArray(), polyline.Closed);
                return;
            }
            Curve curve = entity as Curve;
            if (curve != null && !(curve is Ray) && !(curve is Xline))
            {
                var points = Enumerable.Range(0, 257).Select(i => curve.GetPointAtParameter(
                    curve.StartParam + (curve.EndParam - curve.StartParam) * i / 256.0)).ToArray();
                Vector3d delta;
                if (RigidTranslation(points, warp, out delta)) curve.TransformBy(Matrix3d.Displacement(delta));
                else ReplaceCurve(transaction, owner, curve, points.Select(warp).ToArray(), curve.Closed);
                return;
            }
            throw new InvalidOperationException("No se pueden ajustar medidas interiores de " + entity.GetRXClass().DxfName + ". Seleccione sus contornos editables.");
        }

        private static bool RigidTranslation(Point3d[] points, Func<Point3d, Point3d> warp, out Vector3d delta)
        {
            delta = warp(points[0]) - points[0];
            Vector3d translation = delta;
            return points.All(p => (warp(p) - p - translation).Length < 1e-8);
        }

        private static Point3d[] SamplePolyline(Polyline polyline)
        {
            var points = new List<Point3d>();
            int count = polyline.Closed ? polyline.NumberOfVertices : polyline.NumberOfVertices - 1;
            for (int i = 0; i < count; i++)
            {
                points.Add(polyline.GetPoint3dAt(i));
                int next = (i + 1) % polyline.NumberOfVertices;
                if (Math.Abs(polyline.GetBulgeAt(i)) > 1e-10 && polyline.GetPoint3dAt(i).DistanceTo(polyline.GetPoint3dAt(next)) > 1e-10)
                    for (int step = 1; step < 64; step++) points.Add(polyline.GetPointAtParameter(i + step / 64.0));
            }
            if (!polyline.Closed) points.Add(polyline.GetPoint3dAt(polyline.NumberOfVertices - 1));
            return points.ToArray();
        }

        private static void ReplaceCurve(Transaction transaction, BlockTableRecord owner, Entity original, Point3d[] points, bool closed)
        {
            var replacement = new Polyline { Closed = closed, Elevation = points[0].Z };
            replacement.SetPropertiesFrom(original);
            var sourcePolyline = original as Polyline;
            if (sourcePolyline != null) replacement.ConstantWidth = sourcePolyline.ConstantWidth;
            for (int i = 0; i < points.Length; i++)
                replacement.AddVertexAt(i, new Point2d(points[i].X, points[i].Y), 0, 0, 0);
            owner.AppendEntity(replacement);
            transaction.AddNewlyCreatedDBObject(replacement, true);
            original.Erase();
        }

        private static void DeformHatch(Hatch hatch, Func<Point3d, Point3d> warp)
        {
            if ((hatch.Normal - Vector3d.ZAxis).Length > 1e-8)
                throw new InvalidOperationException("El hatch debe estar en el plano XY para ajustar medidas interiores.");
            // Copy all borrowed boundary data before modifying the hatch. Do not dispose its curves.
            var loops = new List<Point2d[]>();
            var types = new List<HatchLoopTypes>();
            for (int i = 0; i < hatch.NumberOfLoops; i++)
            {
                HatchLoop loop = hatch.GetLoopAt(i);
                var points = new List<Point2d>();
                if (loop.IsPolyline)
                {
                    using (var boundary = new Polyline { Closed = true, Elevation = hatch.Elevation })
                    {
                        int index = 0;
                        foreach (BulgeVertex vertex in loop.Polyline)
                            boundary.AddVertexAt(index++, vertex.Vertex, vertex.Bulge, 0, 0);
                        points.AddRange(SamplePolyline(boundary).Select(p => new Point2d(p.X, p.Y)));
                    }
                }
                else
                {
                    foreach (Curve2d curve in loop.Curves)
                    {
                        Interval interval = curve.GetInterval();
                        int steps = curve is LineSegment2d ? 1 : 128;
                        for (int step = 0; step < steps; step++)
                            points.Add(curve.EvaluatePoint(interval.LowerBound + (interval.UpperBound - interval.LowerBound) * step / steps));
                    }
                }
                if (points.Count < 3) throw new InvalidOperationException("No se pudo leer un contorno cerrado del hatch.");
                loops.Add(points.Select(p => { Point3d mapped = warp(new Point3d(p.X, p.Y, hatch.Elevation)); return new Point2d(mapped.X, mapped.Y); }).ToArray());
                types.Add(hatch.LoopTypeAt(i) & (HatchLoopTypes.External | HatchLoopTypes.Outermost));
            }
            hatch.Associative = false;
            for (int i = hatch.NumberOfLoops - 1; i >= 0; i--) hatch.RemoveLoopAt(i);
            for (int i = 0; i < loops.Count; i++)
                hatch.AppendLoop(types[i] | HatchLoopTypes.Polyline, new Point2dCollection(loops[i]), new DoubleCollection(new double[loops[i].Length]));
            hatch.EvaluateHatch(true);
        }

        private static void DeformDimension(Dimension dimension, Func<Point3d, Point3d> warp)
        {
            double previous = dimension.Measurement;
            var rotated = dimension as RotatedDimension;
            var aligned = dimension as AlignedDimension;
            if (rotated != null)
            {
                rotated.XLine1Point = warp(rotated.XLine1Point);
                rotated.XLine2Point = warp(rotated.XLine2Point);
                rotated.DimLinePoint = warp(rotated.DimLinePoint);
            }
            else if (aligned != null)
            {
                aligned.XLine1Point = warp(aligned.XLine1Point);
                aligned.XLine2Point = warp(aligned.XLine2Point);
                aligned.DimLinePoint = warp(aligned.DimLinePoint);
            }
            else throw new InvalidOperationException("Seleccione la geometría sin cotas angulares o radiales para ajustar medidas interiores.");
            double literal;
            if (double.TryParse(dimension.DimensionText.Replace(',', '.'), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out literal) && Math.Abs(literal - previous) < Math.Max(1e-8, Math.Abs(previous) * 1e-4)) dimension.DimensionText = "<>";
            dimension.RecomputeDimensionBlock(true);
        }
    }
}
