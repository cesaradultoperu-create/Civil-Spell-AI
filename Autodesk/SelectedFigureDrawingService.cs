using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace CivilSpellAI.Autodesk
{
    internal sealed class SelectedFigureSnapshot
    {
        public ObjectId[] SourceIds;
        public ObjectId ReferenceId;
        public Point3d Origin;
        public double Width;
        public double Height;
        public string Description;
        public double AxisAngle;
        public bool ApproximatePreview;
        public FigureMeasureLayout Measures;
        public readonly List<Point2d[]> Outlines = new List<Point2d[]>();
    }

    internal sealed class SelectedFigureDrawingService
    {
        private const string RecordName = "CIVILSPELLAI_FIGURA_MEDIDAS";
        private const string DimensionsName = "CIVILSPELLAI_FIGURA_COTAS";
        private const string DimensionOwnerName = "CIVILSPELLAI_FIGURA_PROPIETARIO";

        public bool TryRead(IEnumerable<ObjectId> selectedIds, Transaction transaction,
            out SelectedFigureSnapshot snapshot, out string error)
        {
            snapshot = null;
            error = null;
            var ids = selectedIds.Where(id => !id.IsNull && id.IsValid && !id.IsErased).Distinct().ToArray();
            if (ids.Length == 0) { error = "No se seleccionaron objetos."; return false; }
            var references = new HashSet<ObjectId>();
            foreach (ObjectId id in ids)
            {
                ObjectId referenceId;
                if (TryResolve(id, transaction, out referenceId)) references.Add(referenceId);
            }
            if (references.Count > 1) { error = "Seleccione una sola figura editable."; return false; }
            ObjectId existingId = references.Count == 1 ? references.Single() : ObjectId.Null;
            if (!existingId.IsNull)
            {
                foreach (ObjectId id in ids)
                {
                    ObjectId resolved;
                    if (!TryResolve(id, transaction, out resolved) || resolved != existingId)
                    { error = "Seleccione solo la figura editable o sus cotas vinculadas."; return false; }
                }
                var reference = (BlockReference)transaction.GetObject(existingId, OpenMode.ForRead);
                ids = new[] { existingId }.Concat(ReadDimensionIds(reference, transaction)).ToArray();
            }
            var result = new SelectedFigureSnapshot { SourceIds = ids, ReferenceId = ObjectId.Null };
            Extents3d? geometryBounds = null;
            var counts = new SortedDictionary<string, int>();
            foreach (ObjectId id in ids)
            {
                Entity entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                if (entity == null) { error = "La selección contiene un objeto sin geometría editable."; return false; }
                string type = entity.GetRXClass().DxfName;
                counts[type] = counts.ContainsKey(type) ? counts[type] + 1 : 1;
                if (entity is Dimension || entity is DBText || entity is MText)
                    continue;
                try
                {
                    Extents3d bounds = entity.GeometricExtents;
                    AddBounds(ref geometryBounds, bounds);
                    CollectOutlines(entity, result.Outlines, 0, transaction);
                }
                catch (global::Autodesk.AutoCAD.Runtime.Exception)
                {
                    error = "No se pudo obtener la geometría de " + type + ". Seleccione los objetos visibles de la figura.";
                    return false;
                }
            }
            Extents3d? measured = geometryBounds;
            if (!measured.HasValue) { error = "Seleccione también la geometría de la figura; las cotas y textos solos no definen su tamaño."; return false; }
            result.Origin = measured.Value.MinPoint;
            result.Width = measured.Value.MaxPoint.X - result.Origin.X;
            result.Height = measured.Value.MaxPoint.Y - result.Origin.Y;
            if (result.Outlines.Count == 0)
            {
                result.ApproximatePreview = true;
                result.Outlines.Add(new[] { new Point2d(result.Origin.X, result.Origin.Y),
                    new Point2d(measured.Value.MaxPoint.X, result.Origin.Y),
                    new Point2d(measured.Value.MaxPoint.X, measured.Value.MaxPoint.Y),
                    new Point2d(result.Origin.X, measured.Value.MaxPoint.Y),
                    new Point2d(result.Origin.X, result.Origin.Y) });
            }
            if (result.Width <= 1e-10 && result.Height <= 1e-10)
            { error = "La selección no tiene ancho ni alto en la vista superior."; return false; }
            result.Description = ids.Length + " objetos: " + string.Join(", ", counts.Select(pair => pair.Value + " " + pair.Key));
            if (!existingId.IsNull)
            {
                var reference = (BlockReference)transaction.GetObject(existingId, OpenMode.ForRead);
                double baseWidth;
                double baseHeight;
                ReadBaseMeasures(reference, transaction, out baseWidth, out baseHeight);
                result.ReferenceId = existingId;
                // Editable sizes follow the block's local axes, even after ROTATE.
                result.Width = baseWidth * Math.Abs(reference.ScaleFactors.X);
                result.Height = baseHeight * Math.Abs(reference.ScaleFactors.Y);
                result.AxisAngle = reference.Rotation;
            }
            result.Measures = FigureMeasureLayout.Detect(result);
            snapshot = result;
            return true;
        }

        public bool IsFigureReference(ObjectId id, Transaction transaction)
        {
            BlockReference reference = transaction.GetObject(id, OpenMode.ForRead, false) as BlockReference;
            if (reference == null || reference.ExtensionDictionary.IsNull)
                return false;
            var dictionary = (DBDictionary)transaction.GetObject(reference.ExtensionDictionary, OpenMode.ForRead);
            return dictionary.Contains(RecordName);
        }

        private bool TryResolve(ObjectId id, Transaction transaction, out ObjectId referenceId)
        {
            referenceId = ObjectId.Null;
            if (IsFigureReference(id, transaction)) { referenceId = id; return true; }
            Dimension dimension = transaction.GetObject(id, OpenMode.ForRead, false) as Dimension;
            if (dimension == null || dimension.ExtensionDictionary.IsNull) return false;
            var dictionary = (DBDictionary)transaction.GetObject(dimension.ExtensionDictionary, OpenMode.ForRead);
            if (!dictionary.Contains(DimensionOwnerName)) return false;
            var record = (Xrecord)transaction.GetObject(dictionary.GetAt(DimensionOwnerName), OpenMode.ForRead);
            ObjectId owner = (ObjectId)record.Data.AsArray()[0].Value;
            if (owner.IsNull || !owner.IsValid || owner.IsErased || !IsFigureReference(owner, transaction)) return false;
            referenceId = owner;
            return true;
        }

        public ObjectId Apply(Database database, Transaction transaction, SelectedFigureSnapshot snapshot,
            double width, double height, bool createCopy, Point3d insertionPoint)
        {
            RequireSize(width, snapshot.Width, "ancho");
            RequireSize(height, snapshot.Height, "alto");
            if (createCopy && !snapshot.ReferenceId.IsNull)
            {
                var original = (BlockReference)transaction.GetObject(snapshot.ReferenceId, OpenMode.ForRead);
                ObjectId copyId = ObjectId.Null;
                using (var mapping = new IdMapping())
                {
                    database.DeepCloneObjects(new ObjectIdCollection(snapshot.SourceIds), original.OwnerId, mapping, false);
                    foreach (IdPair pair in mapping)
                        if (pair.Key == snapshot.ReferenceId) copyId = pair.Value;
                }
                var copy = (BlockReference)transaction.GetObject(copyId, OpenMode.ForWrite);
                Matrix3d before = copy.BlockTransform;
                copy.Position += insertionPoint - snapshot.Origin;
                Matrix3d move = copy.BlockTransform * before.Inverse();
                foreach (ObjectId id in ReadDimensionIds(copy, transaction))
                {
                    var dimension = (Dimension)transaction.GetObject(id, OpenMode.ForWrite);
                    TransformDimension(dimension, move);
                }
                var copySnapshot = new SelectedFigureSnapshot { ReferenceId = copyId,
                    Width = snapshot.Width, Height = snapshot.Height };
                return Apply(database, transaction, copySnapshot, width, height, false, insertionPoint);
            }
            if (!createCopy && !snapshot.ReferenceId.IsNull)
            {
                var existing = (BlockReference)transaction.GetObject(snapshot.ReferenceId, OpenMode.ForWrite);
                var layer = (LayerTableRecord)transaction.GetObject(existing.LayerId, OpenMode.ForRead);
                if (layer.IsLocked) throw new InvalidOperationException("La figura está en una capa bloqueada: " + layer.Name);
                Matrix3d oldTransform = existing.BlockTransform;
                double baseWidth;
                double baseHeight;
                ReadBaseMeasures(existing, transaction, out baseWidth, out baseHeight);
                existing.ScaleFactors = new Scale3d(
                    baseWidth > 1e-10 ? Math.Sign(existing.ScaleFactors.X) * width / baseWidth : existing.ScaleFactors.X,
                    baseHeight > 1e-10 ? Math.Sign(existing.ScaleFactors.Y) * height / baseHeight : existing.ScaleFactors.Y,
                    existing.ScaleFactors.Z);
                Matrix3d delta = existing.BlockTransform * oldTransform.Inverse();
                foreach (ObjectId id in ReadDimensionIds(existing, transaction))
                {
                    var dimension = (Dimension)transaction.GetObject(id, OpenMode.ForWrite);
                    var dimensionLayer = (LayerTableRecord)transaction.GetObject(dimension.LayerId, OpenMode.ForRead);
                    if (dimensionLayer.IsLocked) throw new InvalidOperationException("Una cota está en una capa bloqueada: " + dimensionLayer.Name);
                    TransformDimension(dimension, delta);
                }
                return existing.ObjectId;
            }

            ObjectId ownerId = ObjectId.Null;
            foreach (ObjectId id in snapshot.SourceIds)
            {
                var entity = (Entity)transaction.GetObject(id, OpenMode.ForRead);
                if (ownerId.IsNull) ownerId = entity.OwnerId;
                if (ownerId != entity.OwnerId)
                    throw new InvalidOperationException("Seleccione objetos del mismo espacio del dibujo.");
                if (!createCopy)
                {
                    var layer = (LayerTableRecord)transaction.GetObject(entity.LayerId, OpenMode.ForRead);
                    if (layer.IsLocked)
                        throw new InvalidOperationException("La figura incluye una capa bloqueada: " + layer.Name);
                }
            }
            var blocks = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForWrite);
            var definition = new BlockTableRecord { Name = "CIVILSPELLAI_FIGURA_" + Guid.NewGuid().ToString("N") };
            ObjectId definitionId = blocks.Add(definition);
            transaction.AddNewlyCreatedDBObject(definition, true);
            var geometryIds = snapshot.SourceIds.Where(id =>
                !(transaction.GetObject(id, OpenMode.ForRead) is Dimension)).ToArray();
            using (var mapping = new IdMapping())
                database.DeepCloneObjects(new ObjectIdCollection(geometryIds), definitionId, mapping, false);
            Matrix3d shift = Matrix3d.Displacement(Point3d.Origin - snapshot.Origin);
            foreach (ObjectId id in definition)
                ((Entity)transaction.GetObject(id, OpenMode.ForWrite)).TransformBy(shift);
            var owner = (BlockTableRecord)transaction.GetObject(ownerId, OpenMode.ForWrite);
            var reference = new BlockReference(createCopy ? insertionPoint : snapshot.Origin, definitionId)
            {
                ScaleFactors = new Scale3d(
                    snapshot.Width > 1e-10 ? width / snapshot.Width : 1,
                    snapshot.Height > 1e-10 ? height / snapshot.Height : 1, 1)
            };
            ObjectId referenceId = owner.AppendEntity(reference);
            transaction.AddNewlyCreatedDBObject(reference, true);
            reference.CreateExtensionDictionary();
            var dictionary = (DBDictionary)transaction.GetObject(reference.ExtensionDictionary, OpenMode.ForWrite);
            var record = new Xrecord { Data = new ResultBuffer(
                new TypedValue((int)DxfCode.Real, snapshot.Width),
                new TypedValue((int)DxfCode.Real, snapshot.Height)) };
            dictionary.SetAt(RecordName, record);
            transaction.AddNewlyCreatedDBObject(record, true);
            var dimensionIds = new List<ObjectId>();
            Matrix3d dimensionTransform = reference.BlockTransform * shift;
            var sourceDimensionIds = snapshot.SourceIds.Where(id => transaction.GetObject(id, OpenMode.ForRead) is Dimension).ToArray();
            var clonedDimensionIds = new List<ObjectId>();
            if (sourceDimensionIds.Length > 0)
            {
                using (var mapping = new IdMapping())
                {
                    database.DeepCloneObjects(new ObjectIdCollection(sourceDimensionIds), owner.ObjectId, mapping, false);
                    foreach (IdPair pair in mapping)
                        if (pair.IsPrimary && sourceDimensionIds.Contains(pair.Key)) clonedDimensionIds.Add(pair.Value);
                }
            }
            foreach (ObjectId dimensionId in clonedDimensionIds)
            {
                var dimension = (Dimension)transaction.GetObject(dimensionId, OpenMode.ForWrite);
                TransformDimension(dimension, dimensionTransform);
                if (dimension.ExtensionDictionary.IsNull) dimension.CreateExtensionDictionary();
                var dimensionDictionary = (DBDictionary)transaction.GetObject(dimension.ExtensionDictionary, OpenMode.ForWrite);
                Xrecord ownerRecord;
                if (dimensionDictionary.Contains(DimensionOwnerName))
                    ownerRecord = (Xrecord)transaction.GetObject(dimensionDictionary.GetAt(DimensionOwnerName), OpenMode.ForWrite);
                else
                {
                    ownerRecord = new Xrecord();
                    dimensionDictionary.SetAt(DimensionOwnerName, ownerRecord);
                    transaction.AddNewlyCreatedDBObject(ownerRecord, true);
                }
                ownerRecord.Data = new ResultBuffer(new TypedValue((int)DxfCode.SoftPointerId, referenceId));
                dimensionIds.Add(dimensionId);
            }
            if (dimensionIds.Count > 0)
            {
                var dimensionRecord = new Xrecord { Data = new ResultBuffer(dimensionIds.Select(id =>
                    new TypedValue((int)DxfCode.SoftPointerId, id)).ToArray()) };
                dictionary.SetAt(DimensionsName, dimensionRecord);
                transaction.AddNewlyCreatedDBObject(dimensionRecord, true);
            }
            if (!createCopy)
                foreach (ObjectId id in snapshot.SourceIds)
                    ((Entity)transaction.GetObject(id, OpenMode.ForWrite)).Erase();
            return referenceId;
        }

        internal static void TransformDimension(Dimension dimension, Matrix3d transform)
        {
            double literal;
            if (double.TryParse(dimension.DimensionText.Replace(',', '.'),
                    System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out literal) &&
                Math.Abs(literal - dimension.Measurement) <= Math.Max(1e-8, Math.Abs(literal) * 1e-4))
                dimension.DimensionText = "<>";
            RotatedDimension rotated = dimension as RotatedDimension;
            AlignedDimension aligned = dimension as AlignedDimension;
            if (rotated != null)
            {
                Vector3d axis = new Vector3d(Math.Cos(rotated.Rotation), Math.Sin(rotated.Rotation), 0).TransformBy(transform);
                rotated.XLine1Point = rotated.XLine1Point.TransformBy(transform);
                rotated.XLine2Point = rotated.XLine2Point.TransformBy(transform);
                rotated.DimLinePoint = rotated.DimLinePoint.TransformBy(transform);
                rotated.Rotation = Math.Atan2(axis.Y, axis.X);
            }
            else if (aligned != null)
            {
                aligned.XLine1Point = aligned.XLine1Point.TransformBy(transform);
                aligned.XLine2Point = aligned.XLine2Point.TransformBy(transform);
                aligned.DimLinePoint = aligned.DimLinePoint.TransformBy(transform);
            }
            else
            {
                CoordinateSystem3d axes = transform.CoordinateSystem3d;
                if (Math.Abs(axes.Xaxis.Length - axes.Yaxis.Length) > 1e-8)
                    throw new InvalidOperationException("La selección contiene cotas angulares o radiales. Use el mismo factor de escala para ancho y alto, o seleccione la geometría sin esas cotas.");
                dimension.TransformBy(transform);
            }
            dimension.RecomputeDimensionBlock(true);
        }

        internal static IEnumerable<ObjectId> ReadDimensionIds(BlockReference reference, Transaction transaction)
        {
            var result = new List<ObjectId>();
            if (reference.ExtensionDictionary.IsNull) return result;
            var dictionary = (DBDictionary)transaction.GetObject(reference.ExtensionDictionary, OpenMode.ForRead);
            if (!dictionary.Contains(DimensionsName)) return result;
            var record = (Xrecord)transaction.GetObject(dictionary.GetAt(DimensionsName), OpenMode.ForRead);
            using (ResultBuffer data = record.Data)
            {
                if (data == null) return result;
                foreach (TypedValue value in data)
                {
                    ObjectId id = (ObjectId)value.Value;
                    if (!id.IsNull && id.IsValid && !id.IsErased) result.Add(id);
                }
            }
            return result;
        }

        private static void ReadBaseMeasures(BlockReference reference, Transaction transaction,
            out double width, out double height)
        {
            var dictionary = (DBDictionary)transaction.GetObject(reference.ExtensionDictionary, OpenMode.ForRead);
            var record = (Xrecord)transaction.GetObject(dictionary.GetAt(RecordName), OpenMode.ForRead);
            TypedValue[] data = record.Data.AsArray();
            width = Convert.ToDouble(data[0].Value);
            height = Convert.ToDouble(data[1].Value);
        }

        internal static void WriteBaseMeasures(BlockReference reference, Transaction transaction, double width, double height)
        {
            var dictionary = (DBDictionary)transaction.GetObject(reference.ExtensionDictionary, OpenMode.ForRead);
            var record = (Xrecord)transaction.GetObject(dictionary.GetAt(RecordName), OpenMode.ForWrite);
            record.Data = new ResultBuffer(new TypedValue((int)DxfCode.Real, width), new TypedValue((int)DxfCode.Real, height));
        }

        private static void RequireSize(double value, double original, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) ||
                (original > 1e-10 ? value <= 0 : Math.Abs(value) > 1e-10))
                throw new ArgumentException("El " + name + " debe ser positivo; una dimensión nula debe permanecer en cero.");
        }

        private static void AddBounds(ref Extents3d? total, Extents3d next)
        {
            if (!total.HasValue) total = next;
            else
            {
                Extents3d value = total.Value;
                total = new Extents3d(
                    new Point3d(Math.Min(value.MinPoint.X, next.MinPoint.X), Math.Min(value.MinPoint.Y, next.MinPoint.Y), Math.Min(value.MinPoint.Z, next.MinPoint.Z)),
                    new Point3d(Math.Max(value.MaxPoint.X, next.MaxPoint.X), Math.Max(value.MaxPoint.Y, next.MaxPoint.Y), Math.Max(value.MaxPoint.Z, next.MaxPoint.Z)));
            }
        }

        private static void CollectOutlines(Entity entity, IList<Point2d[]> outlines, int depth, Transaction transaction)
        {
            if (depth >= 12 || outlines.Count >= 3000) return;
            Polyline polyline = entity as Polyline;
            if (polyline != null)
            {
                var points = new List<Point2d>();
                for (int index = 0; index < polyline.NumberOfVertices; index++)
                {
                    Point3d point = polyline.GetPoint3dAt(index);
                    points.Add(new Point2d(point.X, point.Y));
                    // Skip repeated vertices; the generic Curve API can fail on zero-length segments.
                    if (index + 1 < polyline.NumberOfVertices && Math.Abs(polyline.GetBulgeAt(index)) > 1e-9 &&
                        point.DistanceTo(polyline.GetPoint3dAt(index + 1)) > 1e-10)
                    {
                        for (int step = 1; step < 8; step++)
                        {
                            Point3d arcPoint = polyline.GetPointAtParameter(index + step / 8.0);
                            points.Add(new Point2d(arcPoint.X, arcPoint.Y));
                        }
                    }
                }
                if (polyline.Closed && points.Count > 0) points.Add(points[0]);
                if (points.Count > 0) outlines.Add(points.ToArray());
                return;
            }
            Curve curve = entity as Curve;
            if (curve != null && !(entity is Ray) && !(entity is Xline))
            {
                int count = entity is Line ? 1 : Math.Min(256,
                    entity is Polyline ? Math.Max(16, ((Polyline)entity).NumberOfVertices * 4) : 64);
                var points = new Point2d[count + 1];
                for (int index = 0; index <= count; index++)
                {
                    Point3d point = curve.GetPointAtParameter(
                        curve.StartParam + (curve.EndParam - curve.StartParam) * index / count);
                    points[index] = new Point2d(point.X, point.Y);
                }
                outlines.Add(points);
                return;
            }
            Hatch hatch = entity as Hatch;
            if (hatch != null)
            {
                if (hatch.Associative && !hatch.ObjectId.IsNull)
                {
                    foreach (ObjectId id in hatch.GetAssociatedObjectIds())
                    {
                        if (!id.IsNull && id.IsValid && !id.IsErased)
                            CollectOutlines((Entity)transaction.GetObject(id, OpenMode.ForRead), outlines, depth + 1, transaction);
                    }
                }
                // Keep hatch geometry intact; available source polylines supply the preview.
                return;
            }
            if (entity is Dimension || entity is DBText || entity is MText) return;
            BlockReference reference = entity as BlockReference;
            if (reference != null)
            {
                var definition = (BlockTableRecord)transaction.GetObject(reference.BlockTableRecord, OpenMode.ForRead);
                var local = new List<Point2d[]>();
                foreach (ObjectId id in definition)
                    CollectOutlines((Entity)transaction.GetObject(id, OpenMode.ForRead), local, depth + 1, transaction);
                foreach (Point2d[] line in local)
                    outlines.Add(line.Select(point => {
                        Point3d transformed = new Point3d(point.X, point.Y, 0).TransformBy(reference.BlockTransform);
                        return new Point2d(transformed.X, transformed.Y);
                    }).ToArray());
                return;
            }
            using (var objects = new DBObjectCollection())
            {
                try
                {
                    entity.Explode(objects);
                    foreach (DBObject item in objects)
                    {
                        Entity child = item as Entity;
                        if (child != null) CollectOutlines(child, outlines, depth + 1, transaction);
                    }
                }
                catch (global::Autodesk.AutoCAD.Runtime.Exception)
                {
                    Extents3d bounds = entity.GeometricExtents;
                    outlines.Add(new[] { new Point2d(bounds.MinPoint.X, bounds.MinPoint.Y),
                        new Point2d(bounds.MaxPoint.X, bounds.MinPoint.Y),
                        new Point2d(bounds.MaxPoint.X, bounds.MaxPoint.Y),
                        new Point2d(bounds.MinPoint.X, bounds.MaxPoint.Y),
                        new Point2d(bounds.MinPoint.X, bounds.MinPoint.Y) });
                }
                finally { foreach (DBObject item in objects) item.Dispose(); }
            }
        }
    }
}
