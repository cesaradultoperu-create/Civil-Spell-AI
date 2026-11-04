using System;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CivilSpellAI.Geometry;

namespace CivilSpellAI.Autodesk
{
    internal sealed class Figure3DDrawingService
    {
        private const string RecordName = "CIVILSPELLAI_FIGURA_3D";

        public bool TryRead(ObjectId id, Transaction transaction, out Figure3DParameters parameters)
        {
            parameters = null;
            if (id.IsNull || !id.IsValid || id.IsErased) return false;
            var reference = transaction.GetObject(id, OpenMode.ForRead) as BlockReference;
            if (reference == null || reference.ExtensionDictionary.IsNull) return false;
            var dictionary = (DBDictionary)transaction.GetObject(reference.ExtensionDictionary, OpenMode.ForRead);
            if (!dictionary.Contains(RecordName)) return false;
            var record = (Xrecord)transaction.GetObject(dictionary.GetAt(RecordName), OpenMode.ForRead);
            if (record.Data == null) return false;
            var values = record.Data.AsArray();
            if (values.Length != 19) return false;
            try
            {
                if (Convert.ToInt16(values[0].Value) != 1) return false;
                parameters = new Figure3DParameters {
                    Width = Number(values, 1), Depth = Number(values, 2), Height = Number(values, 3),
                    LeftWall = Number(values, 4), RightWall = Number(values, 5), FrontWall = Number(values, 6),
                    BackWall = Number(values, 7), BaseThickness = Number(values, 8),
                    Left = ReadPassage(values, 9), Right = ReadPassage(values, 14) };
                parameters.Validate(); return true;
            }
            catch (ArgumentException) { parameters = null; return false; }
            catch (InvalidCastException) { parameters = null; return false; }
            catch (FormatException) { parameters = null; return false; }
        }
        public ObjectId Create(Database database, Transaction transaction, Point3d point, Figure3DParameters parameters)
        {
            parameters.Validate();
            ObjectId definitionId = CreateDefinition(database, transaction, parameters);
            var model = (BlockTableRecord)transaction.GetObject(database.CurrentSpaceId, OpenMode.ForWrite);
            var reference = new BlockReference(point, definitionId);
            model.AppendEntity(reference); transaction.AddNewlyCreatedDBObject(reference, true);
            WriteParameters(reference, transaction, parameters);
            return reference.ObjectId;
        }
        public void Update(Database database, Transaction transaction, ObjectId id, Figure3DParameters parameters)
        {
            parameters.Validate();
            Figure3DParameters original;
            if (!TryRead(id, transaction, out original)) throw new InvalidOperationException("La figura 3D ya no está disponible.");
            var reference = (BlockReference)transaction.GetObject(id, OpenMode.ForWrite);
            var layer = (LayerTableRecord)transaction.GetObject(reference.LayerId, OpenMode.ForRead);
            if (layer.IsLocked) throw new InvalidOperationException("La figura 3D está en una capa bloqueada.");
            reference.BlockTableRecord = CreateDefinition(database, transaction, parameters);
            WriteParameters(reference, transaction, parameters);
        }
        private static ObjectId CreateDefinition(Database database, Transaction transaction, Figure3DParameters p)
        {
            var blocks = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForWrite);
            var definition = new BlockTableRecord { Name = "CIVILSPELLAI_3D_" + Guid.NewGuid().ToString("N") };
            blocks.Add(definition); transaction.AddNewlyCreatedDBObject(definition, true);
            Solid3d body = Box(0, 0, 0, p.Width, p.Depth, p.Height);
            try
            {
                using (var cavity = Box(p.LeftWall, p.FrontWall, p.BaseThickness, p.InnerWidth, p.InnerDepth, p.ClearHeight + 0.01))
                    body.BooleanOperation(BooleanOperationType.BoolSubtract, cavity);
                CutPassage(body, p.Left, -0.01, p.LeftWall + 0.02);
                CutPassage(body, p.Right, p.Width - p.RightWall - 0.01, p.RightWall + 0.02);
                body.ColorIndex = 8;
                definition.AppendEntity(body); transaction.AddNewlyCreatedDBObject(body, true); body = null;
                AppendDimensions(definition, transaction, database.Dimstyle, p);
                return definition.ObjectId;
            }
            finally { if (body != null) body.Dispose(); }
        }
        private static void CutPassage(Solid3d body, Figure3DPassage passage, double x, double length)
        {
            if (!passage.Enabled) return;
            using (var cut = Box(x, passage.Center - passage.Width / 2, passage.Bottom, length, passage.Width, passage.Height))
                body.BooleanOperation(BooleanOperationType.BoolSubtract, cut);
        }
        private static Solid3d Box(double x, double y, double z, double width, double depth, double height)
        {
            using (var boundary = new Polyline())
            {
                boundary.AddVertexAt(0, new Point2d(x, y), 0, 0, 0);
                boundary.AddVertexAt(1, new Point2d(x + width, y), 0, 0, 0);
                boundary.AddVertexAt(2, new Point2d(x + width, y + depth), 0, 0, 0);
                boundary.AddVertexAt(3, new Point2d(x, y + depth), 0, 0, 0); boundary.Closed = true;
                using (var curves = new DBObjectCollection { boundary })
                using (var regions = Region.CreateFromCurves(curves))
                {
                    try
                    {
                        if (regions.Count != 1) throw new InvalidOperationException("No se pudo construir el perfil 3D.");
                        var solid = new Solid3d();
                        try
                        {
                            solid.CreateExtrudedSolid((Region)regions[0], new Vector3d(0, 0, height), new SweepOptions());
                            solid.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, z)));
                            return solid;
                        }
                        catch { solid.Dispose(); throw; }
                    }
                    finally { foreach (DBObject region in regions) region.Dispose(); }
                }
            }
        }
        private static void AppendDimensions(BlockTableRecord owner, Transaction transaction, ObjectId style, Figure3DParameters p)
        {
            double margin = Math.Max(p.Width, Math.Max(p.Depth, p.Height)) * 0.18;
            AddDimension(owner, transaction, style, 0, 0, p.Width, 0, p.Width / 2, -margin, false, 0);
            AddDimension(owner, transaction, style, p.Width, 0, p.Width, p.Depth, p.Width + margin, p.Depth / 2, false, 0);
            AddDimension(owner, transaction, style, p.LeftWall, p.FrontWall, p.Width - p.RightWall, p.FrontWall,
                p.Width / 2, p.Depth / 2, false, p.BaseThickness);
            AddDimension(owner, transaction, style, p.LeftWall, p.FrontWall, p.LeftWall, p.Depth - p.BackWall,
                p.LeftWall + p.InnerWidth / 3, p.Depth / 2, false, p.BaseThickness);
            AddDimension(owner, transaction, style, 0, p.Depth, p.LeftWall, p.Depth, p.LeftWall / 2, p.Depth + margin, false, 0);
            AddDimension(owner, transaction, style, p.Width - p.RightWall, p.Depth, p.Width, p.Depth,
                p.Width - p.RightWall / 2, p.Depth + margin, false, 0);
            AddDimension(owner, transaction, style, 0, 0, 0, p.FrontWall, -margin, p.FrontWall / 2, false, 0);
            AddDimension(owner, transaction, style, 0, p.Depth - p.BackWall, 0, p.Depth,
                -margin, p.Depth - p.BackWall / 2, false, 0);
            AddDimension(owner, transaction, style, 0, 0, 0, p.Height, -margin, p.Height / 2, true, -margin / 3);
            AddDimension(owner, transaction, style, p.Width, 0, p.Width, p.BaseThickness,
                p.Width + margin, p.BaseThickness / 2, true, -margin / 3);
            AddDimension(owner, transaction, style, p.Width, p.BaseThickness, p.Width, p.Height,
                p.Width + margin, (p.Height + p.BaseThickness) / 2, true, -margin / 3);
            if (p.Left.Enabled) PassageDimensions(owner, transaction, style, p.Left, 0, -margin, margin);
            if (p.Right.Enabled) PassageDimensions(owner, transaction, style, p.Right, p.Width, p.Width + margin, margin);
        }
        private static void PassageDimensions(BlockTableRecord owner, Transaction transaction, ObjectId style,
            Figure3DPassage passage, double x, double labelX, double margin)
        {
            AddDimension(owner, transaction, style, x, passage.Center - passage.Width / 2, x, passage.Center + passage.Width / 2,
                labelX, passage.Center, false, 0);
            AddDimension(owner, transaction, style, x, passage.Bottom, x, passage.Bottom + passage.Height,
                (x + labelX) / 2, passage.Bottom + passage.Height / 2, true, -margin / 3);
            AddDimension(owner, transaction, style, x, 0, x, passage.Bottom,
                labelX + (labelX > x ? margin : -margin), passage.Bottom / 2, true, -margin / 3);
        }
        private static void AddDimension(BlockTableRecord owner, Transaction transaction, ObjectId style,
            double x1, double y1, double x2, double y2, double labelX, double labelY, bool front, double offset)
        {
            var dimension = new AlignedDimension(new Point3d(x1, y1, 0), new Point3d(x2, y2, 0),
                new Point3d(labelX, labelY, 0), "<>", style) {
                Dimtxt = 0.035, Dimasz = 0.018, Dimgap = 0.012, Dimexo = 0.01, Dimexe = 0.02,
                Dimdec = 3, Dimzin = 0, Dimtmove = 1, ColorIndex = 2 };
            if (front)
                dimension.TransformBy(Matrix3d.Displacement(new Vector3d(0, offset, 0)) * Matrix3d.Rotation(Math.PI / 2, Vector3d.XAxis, Point3d.Origin));
            else dimension.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, offset)));
            owner.AppendEntity(dimension); transaction.AddNewlyCreatedDBObject(dimension, true);
        }
        private static double Number(TypedValue[] values, int index) { return Convert.ToDouble(values[index].Value); }
        private static Figure3DPassage ReadPassage(TypedValue[] values, int index)
        {
            return new Figure3DPassage { Enabled = Convert.ToInt16(values[index].Value) == 1,
                Width = Number(values, index + 1), Height = Number(values, index + 2),
                Center = Number(values, index + 3), Bottom = Number(values, index + 4) };
        }
        private static void WriteParameters(BlockReference reference, Transaction transaction, Figure3DParameters p)
        {
            if (reference.ExtensionDictionary.IsNull) reference.CreateExtensionDictionary();
            var dictionary = (DBDictionary)transaction.GetObject(reference.ExtensionDictionary, OpenMode.ForWrite);
            Xrecord record;
            if (dictionary.Contains(RecordName)) record = (Xrecord)transaction.GetObject(dictionary.GetAt(RecordName), OpenMode.ForWrite);
            else { record = new Xrecord(); dictionary.SetAt(RecordName, record); transaction.AddNewlyCreatedDBObject(record, true); }
            var values = new[] { p.Width, p.Depth, p.Height, p.LeftWall, p.RightWall, p.FrontWall, p.BackWall, p.BaseThickness,
                p.Left.Enabled ? 1 : 0, p.Left.Width, p.Left.Height, p.Left.Center, p.Left.Bottom,
                p.Right.Enabled ? 1 : 0, p.Right.Width, p.Right.Height, p.Right.Center, p.Right.Bottom };
            using (var buffer = new ResultBuffer(new[] { new TypedValue((int)DxfCode.Int16, (short)1) }
                .Concat(values.Select(value => new TypedValue((int)DxfCode.Real, value))).ToArray())) record.Data = buffer;
        }
    }
}
