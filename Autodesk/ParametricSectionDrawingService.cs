using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CivilSpellAI.Geometry;

namespace CivilSpellAI.Autodesk
{
    public sealed class ParametricSectionDrawingService
    {
        private const string BlockNamePrefix = "CIVILSPELLAI_SECCION_";
        private const string ParameterRecordName = "CIVILSPELLAI_SECCION_PARAMETROS";
        private const string DimensionRecordName = "CIVILSPELLAI_SECCION_COTAS";
        private const string DimensionOwnerRecordName = "CIVILSPELLAI_SECCION_PROPIETARIO";
        private const string DimensionLayerName = "CIVILSPELLAI_COTAS";
        private const short SchemaVersion = 6;

        public ObjectId Create(
            Database database,
            Transaction transaction,
            Point3d insertionPoint,
            TransitionSectionParameters parameters)
        {
            if (database == null)
                throw new ArgumentNullException("database");
            if (transaction == null)
                throw new ArgumentNullException("transaction");
            if (parameters == null)
                throw new ArgumentNullException("parameters");

            ObjectId definitionId = CreateDefinition(
                database,
                transaction,
                parameters);
            BlockTable blockTable = (BlockTable)transaction.GetObject(
                database.BlockTableId,
                OpenMode.ForRead);
            BlockTableRecord modelSpace = (BlockTableRecord)transaction.GetObject(
                blockTable[BlockTableRecord.ModelSpace],
                OpenMode.ForWrite);
            var reference = new BlockReference(insertionPoint, definitionId);
            ObjectId referenceId = modelSpace.AppendEntity(reference);
            transaction.AddNewlyCreatedDBObject(reference, true);
            WriteParameters(reference, transaction, parameters);
            ReplaceDimensions(reference, modelSpace, transaction, parameters);
            return referenceId;
        }

        public bool TryReadParameters(
            ObjectId referenceId,
            Transaction transaction,
            out TransitionSectionParameters parameters)
        {
            parameters = null;
            if (referenceId.IsNull || transaction == null)
                return false;

            BlockReference reference = transaction.GetObject(
                referenceId,
                OpenMode.ForRead,
                false) as BlockReference;
            if (reference == null || reference.ExtensionDictionary.IsNull)
                return false;

            BlockTableRecord definition = transaction.GetObject(
                reference.BlockTableRecord,
                OpenMode.ForRead,
                false) as BlockTableRecord;
            if (definition == null ||
                !definition.Name.StartsWith(BlockNamePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            DBDictionary dictionary = transaction.GetObject(
                reference.ExtensionDictionary,
                OpenMode.ForRead,
                false) as DBDictionary;
            if (dictionary == null || !dictionary.Contains(ParameterRecordName))
                return false;

            Xrecord record = transaction.GetObject(
                dictionary.GetAt(ParameterRecordName),
                OpenMode.ForRead,
                false) as Xrecord;
            if (record == null || record.Data == null)
                return false;

            TypedValue[] values = record.Data.AsArray();
            if (values.Length < 8)
                return false;

            try
            {
                short version = Convert.ToInt16(values[0].Value);
                if (version == 1 && values.Length == 8)
                {
                    parameters = new TransitionSectionParameters(
                        Convert.ToDouble(values[1].Value),
                        Convert.ToDouble(values[2].Value),
                        Convert.ToDouble(values[3].Value),
                        Convert.ToDouble(values[4].Value),
                        Convert.ToDouble(values[5].Value),
                        Convert.ToDouble(values[6].Value),
                        Convert.ToDouble(values[7].Value));
                }
                else if (version == 2 && values.Length == 11)
                {
                    parameters = new TransitionSectionParameters(
                        Convert.ToDouble(values[1].Value),
                        Convert.ToDouble(values[2].Value),
                        Convert.ToDouble(values[3].Value),
                        Convert.ToDouble(values[4].Value),
                        Convert.ToDouble(values[5].Value),
                        Convert.ToDouble(values[6].Value),
                        Convert.ToDouble(values[7].Value),
                        Convert.ToString(values[8].Value),
                        Convert.ToDouble(values[9].Value),
                        Convert.ToDouble(values[10].Value));
                }
                else if (version == 3 && values.Length == 12)
                {
                    parameters = new TransitionSectionParameters(
                        Convert.ToDouble(values[1].Value),
                        Convert.ToDouble(values[2].Value),
                        Convert.ToDouble(values[3].Value),
                        Convert.ToDouble(values[4].Value),
                        Convert.ToDouble(values[5].Value),
                        Convert.ToDouble(values[6].Value),
                        Convert.ToDouble(values[7].Value),
                        Convert.ToString(values[8].Value),
                        Convert.ToDouble(values[9].Value),
                        Convert.ToDouble(values[10].Value),
                        Convert.ToInt32(values[11].Value));
                }
                else if (version == 4 && values.Length == 15)
                {
                    parameters = new TransitionSectionParameters(
                        Convert.ToDouble(values[1].Value),
                        Convert.ToDouble(values[2].Value),
                        Convert.ToDouble(values[3].Value),
                        Convert.ToDouble(values[4].Value),
                        Convert.ToDouble(values[5].Value),
                        Convert.ToDouble(values[6].Value),
                        Convert.ToDouble(values[7].Value),
                        Convert.ToString(values[8].Value),
                        Convert.ToDouble(values[9].Value),
                        Convert.ToDouble(values[10].Value),
                        Convert.ToInt32(values[11].Value),
                        (TransitionSectionType)Convert.ToInt16(values[12].Value),
                        Convert.ToDouble(values[13].Value),
                        Convert.ToDouble(values[14].Value));
                }
                else if (version == 5 && values.Length == 16)
                {
                    parameters = new TransitionSectionParameters(
                        Convert.ToDouble(values[1].Value),
                        Convert.ToDouble(values[2].Value),
                        Convert.ToDouble(values[3].Value),
                        Convert.ToDouble(values[4].Value),
                        Convert.ToDouble(values[5].Value),
                        Convert.ToDouble(values[6].Value),
                        Convert.ToDouble(values[7].Value),
                        Convert.ToString(values[8].Value),
                        Convert.ToDouble(values[9].Value),
                        Convert.ToDouble(values[10].Value),
                        Convert.ToInt32(values[11].Value),
                        (TransitionSectionType)Convert.ToInt16(values[12].Value),
                        Convert.ToDouble(values[13].Value),
                        Convert.ToDouble(values[14].Value),
                        Convert.ToDouble(values[15].Value));
                }
                else if (version == SchemaVersion && values.Length == 17)
                {
                    parameters = new TransitionSectionParameters(
                        Convert.ToDouble(values[1].Value),
                        Convert.ToDouble(values[2].Value),
                        Convert.ToDouble(values[3].Value),
                        Convert.ToDouble(values[4].Value),
                        Convert.ToDouble(values[5].Value),
                        Convert.ToDouble(values[6].Value),
                        Convert.ToDouble(values[7].Value),
                        Convert.ToString(values[8].Value),
                        Convert.ToDouble(values[9].Value),
                        Convert.ToDouble(values[10].Value),
                        Convert.ToInt32(values[11].Value),
                        (TransitionSectionType)Convert.ToInt16(values[12].Value),
                        Convert.ToDouble(values[13].Value),
                        Convert.ToDouble(values[14].Value),
                        Convert.ToDouble(values[15].Value),
                        Convert.ToDouble(values[16].Value));
                }
                else
                {
                    return false;
                }

                return true;
            }
            catch (System.Exception)
            {
                parameters = null;
                return false;
            }
        }

        public bool TryResolveSectionReference(
            ObjectId selectedId,
            Transaction transaction,
            out ObjectId referenceId)
        {
            referenceId = ObjectId.Null;
            if (selectedId.IsNull || transaction == null)
                return false;

            Entity selected = transaction.GetObject(
                selectedId,
                OpenMode.ForRead,
                false) as Entity;
            if (selected is BlockReference)
            {
                TransitionSectionParameters ignored;
                if (!TryReadParameters(selectedId, transaction, out ignored))
                    return false;

                referenceId = selectedId;
                return true;
            }

            Dimension dimension = selected as Dimension;
            ObjectId ownerId;
            if (dimension == null ||
                !TryReadDimensionOwner(dimension, transaction, out ownerId))
            {
                return false;
            }

            TransitionSectionParameters parameters;
            if (!TryReadParameters(ownerId, transaction, out parameters))
                return false;

            referenceId = ownerId;
            return true;
        }

        public ObjectId Update(
            Database database,
            Transaction transaction,
            ObjectId referenceId,
            TransitionSectionParameters parameters)
        {
            if (database == null)
                throw new ArgumentNullException("database");
            if (transaction == null)
                throw new ArgumentNullException("transaction");
            if (parameters == null)
                throw new ArgumentNullException("parameters");

            BlockReference reference = transaction.GetObject(
                referenceId,
                OpenMode.ForRead,
                false) as BlockReference;
            if (reference == null)
                throw new InvalidOperationException("La sección seleccionada ya no existe.");

            TransitionSectionParameters storedParameters;
            if (!TryReadParameters(referenceId, transaction, out storedParameters))
            {
                throw new InvalidOperationException(
                    "El bloque seleccionado no es una sección paramétrica creada por CivilSpellAI.");
            }

            BlockTableRecord definition = (BlockTableRecord)transaction.GetObject(
                reference.BlockTableRecord,
                OpenMode.ForRead);
            ObjectIdCollection references = definition.GetBlockReferenceIds(true, false);
            EraseAssociatedDimensions(reference, transaction);
            if (references.Count <= 1)
            {
                RebuildDefinition(definition, transaction, parameters);
                reference.UpgradeOpen();
                WriteParameters(reference, transaction, parameters);
                BlockTableRecord owner = (BlockTableRecord)transaction.GetObject(
                    reference.OwnerId,
                    OpenMode.ForWrite);
                ReplaceDimensions(reference, owner, transaction, parameters);
                return referenceId;
            }

            return ReplaceSharedReference(
                database,
                transaction,
                reference,
                parameters);
        }

        private static ObjectId CreateDefinition(
            Database database,
            Transaction transaction,
            TransitionSectionParameters parameters)
        {
            BlockTable blockTable = (BlockTable)transaction.GetObject(
                database.BlockTableId,
                OpenMode.ForWrite);
            var definition = new BlockTableRecord
            {
                Name = BlockNamePrefix + Guid.NewGuid().ToString("N"),
                Origin = Point3d.Origin
            };
            ObjectId definitionId = blockTable.Add(definition);
            transaction.AddNewlyCreatedDBObject(definition, true);
            AppendGeometry(definition, transaction, parameters);
            return definitionId;
        }

        private static void RebuildDefinition(
            BlockTableRecord definition,
            Transaction transaction,
            TransitionSectionParameters parameters)
        {
            definition.UpgradeOpen();
            var hatches = new List<ObjectId>();
            var otherEntities = new List<ObjectId>();
            foreach (ObjectId entityId in definition)
            {
                Entity entity = transaction.GetObject(
                    entityId,
                    OpenMode.ForRead,
                    false) as Entity;
                if (entity is Hatch)
                    hatches.Add(entityId);
                else
                    otherEntities.Add(entityId);
            }

            EraseEntities(hatches, transaction);
            EraseEntities(otherEntities, transaction);
            AppendGeometry(definition, transaction, parameters);
        }

        private static void EraseEntities(
            IEnumerable<ObjectId> entityIds,
            Transaction transaction)
        {
            foreach (ObjectId entityId in entityIds)
            {
                Entity entity = transaction.GetObject(
                    entityId,
                    OpenMode.ForWrite,
                    false) as Entity;
                if (entity != null)
                    entity.Erase();
            }
        }

        private static ObjectId ReplaceSharedReference(
            Database database,
            Transaction transaction,
            BlockReference original,
            TransitionSectionParameters parameters)
        {
            ObjectId definitionId = CreateDefinition(database, transaction, parameters);
            BlockTableRecord owner = (BlockTableRecord)transaction.GetObject(
                original.OwnerId,
                OpenMode.ForWrite);
            var replacement = new BlockReference(original.Position, definitionId);
            replacement.SetPropertiesFrom(original);
            replacement.Normal = original.Normal;
            replacement.Rotation = original.Rotation;
            replacement.ScaleFactors = original.ScaleFactors;
            ObjectId replacementId = owner.AppendEntity(replacement);
            transaction.AddNewlyCreatedDBObject(replacement, true);
            WriteParameters(replacement, transaction, parameters);
            ReplaceDimensions(replacement, owner, transaction, parameters);

            original.UpgradeOpen();
            original.Erase();
            return replacementId;
        }

        private static void AppendGeometry(
            BlockTableRecord definition,
            Transaction transaction,
            TransitionSectionParameters parameters)
        {
            var geometry = new TransitionSectionGeometry(parameters);
            foreach (IReadOnlyList<SectionPoint> polygon in geometry.Polygons)
            {
                Polyline boundary = CreateBoundary(polygon);
                ObjectId boundaryId = definition.AppendEntity(boundary);
                transaction.AddNewlyCreatedDBObject(boundary, true);

                var hatch = new Hatch
                {
                    Layer = "0",
                    Color = CreateEntityColor(parameters.HatchColorIndex)
                };
                definition.AppendEntity(hatch);
                transaction.AddNewlyCreatedDBObject(hatch, true);
                SetPattern(hatch, parameters.HatchPatternName);
                if (!string.Equals(
                        parameters.HatchPatternName,
                        "SOLID",
                        StringComparison.OrdinalIgnoreCase))
                {
                    hatch.PatternScale = parameters.HatchScale;
                    hatch.PatternAngle = parameters.HatchAngleDegrees * Math.PI / 180.0;
                }
                hatch.Associative = true;
                hatch.AppendLoop(
                    HatchLoopTypes.External,
                    new ObjectIdCollection(new[] { boundaryId }));
                hatch.EvaluateHatch(true);
            }

            AppendWallSolid(
                definition,
                transaction,
                TranslatePolygonY(
                    geometry.Polygons[3],
                    -parameters.InnerThickness),
                new Vector3d(
                    0.0,
                    parameters.InnerThickness,
                    parameters.WallHeight));
            AppendWallSolid(
                definition,
                transaction,
                TranslatePolygonY(
                    geometry.Polygons[4],
                    parameters.InnerThickness),
                new Vector3d(
                    0.0,
                    -parameters.InnerThickness,
                    parameters.WallHeight));

        }

        private static IReadOnlyList<SectionPoint> TranslatePolygonY(
            IReadOnlyList<SectionPoint> polygon,
            double displacement)
        {
            var translated = new List<SectionPoint>(polygon.Count);
            foreach (SectionPoint point in polygon)
            {
                translated.Add(new SectionPoint(
                    point.X,
                    point.Y + displacement));
            }

            return translated;
        }

        private static void AppendWallSolid(
            BlockTableRecord definition,
            Transaction transaction,
            IReadOnlyList<SectionPoint> polygon,
            Vector3d extrusion)
        {
            using (Polyline profile = CreateBoundary(polygon))
            {
                var curves = new DBObjectCollection { profile };
                DBObjectCollection regions = Region.CreateFromCurves(curves);
                try
                {
                    if (regions.Count != 1 || !(regions[0] is Region))
                    {
                        throw new InvalidOperationException(
                            "No se pudo crear el perfil cerrado del muro 3D.");
                    }

                    Region region = (Region)regions[0];
                    var solid = new Solid3d();
                    try
                    {
                        solid.SetDatabaseDefaults(definition.Database);
                        solid.CreateExtrudedSolid(
                            region,
                            extrusion,
                            new SweepOptions());
                        definition.AppendEntity(solid);
                        transaction.AddNewlyCreatedDBObject(solid, true);
                        solid = null;
                    }
                    finally
                    {
                        if (solid != null)
                            solid.Dispose();
                    }
                }
                finally
                {
                    foreach (DBObject region in regions)
                        region.Dispose();
                }
            }
        }

        private static List<ObjectId> AppendDimensions(
            BlockReference reference,
            BlockTableRecord owner,
            Transaction transaction,
            TransitionSectionParameters parameters)
        {
            EnsureDimensionLayer(owner.Database, transaction);
            if (parameters.SectionType == TransitionSectionType.TypeB)
            {
                return AppendTypeBDimensions(
                    reference,
                    owner,
                    transaction,
                    parameters);
            }

            return AppendTypeADimensions(reference, owner, transaction, parameters);
        }

        private static List<ObjectId> AppendTypeADimensions(
            BlockReference reference,
            BlockTableRecord owner,
            Transaction transaction,
            TransitionSectionParameters parameters)
        {
            double x0 = 0.0;
            double x1 = parameters.InitialStraightLength;
            double x2 = x1 + parameters.TransitionLength;
            double x3 = x2 + parameters.FinalStraightLength;
            double initialClearHalf = parameters.InitialClearHeight / 2.0;
            double finalClearHalf = parameters.FinalClearHeight / 2.0;
            double initialTotalHalf = parameters.InitialTotalHeight / 2.0;
            double finalTotalHalf = parameters.FinalTotalHeight / 2.0;
            double maximumHeight = Math.Max(
                parameters.InitialTotalHeight,
                parameters.FinalTotalHeight);
            double margin = Math.Max(0.20, maximumHeight * 0.20);
            double firstDimensionY = -finalTotalHalf - margin;
            double secondDimensionY = firstDimensionY - margin;
            double leftChainX = x0 - margin;
            double leftTotalX = x0 - (2.0 * margin);
            double rightChainX = x3 + margin;
            double rightTotalX = x3 + (2.0 * margin);
            double textHeight = Math.Max(0.06, maximumHeight / 20.0);
            ObjectId dimensionStyleId = owner.Database.Dimstyle;
            var dimensionIds = new List<ObjectId>();

            AppendDimension(reference, owner, transaction, dimensionIds, CreateHorizontalDimension(
                x1, -initialTotalHalf, x2, -finalTotalHalf,
                firstDimensionY, "<>", textHeight, dimensionStyleId));
            AppendDimension(reference, owner, transaction, dimensionIds, CreateHorizontalDimension(
                x2, -finalTotalHalf, x3, -finalTotalHalf,
                firstDimensionY, "<>", textHeight, dimensionStyleId));
            AppendDimension(reference, owner, transaction, dimensionIds, CreateHorizontalDimension(
                x1, -initialTotalHalf, x3, -finalTotalHalf,
                secondDimensionY, "<>", textHeight, dimensionStyleId));

            AppendDimension(reference, owner, transaction, dimensionIds, CreateVerticalDimension(
                x0,
                -initialClearHalf - parameters.InnerThickness,
                -initialClearHalf,
                leftChainX,
                "<>",
                textHeight,
                dimensionStyleId));
            AppendDimension(reference, owner, transaction, dimensionIds, CreateVerticalDimension(
                x0,
                -initialClearHalf,
                initialClearHalf,
                leftChainX,
                "<>",
                textHeight,
                dimensionStyleId));
            AppendDimension(reference, owner, transaction, dimensionIds, CreateVerticalDimension(
                x0,
                initialClearHalf,
                initialClearHalf + parameters.InnerThickness,
                leftChainX,
                "<>",
                textHeight,
                dimensionStyleId));
            AppendDimension(reference, owner, transaction, dimensionIds, CreateVerticalDimension(
                x0,
                -initialTotalHalf,
                initialTotalHalf,
                leftTotalX,
                "<>",
                textHeight,
                dimensionStyleId));

            AppendDimension(reference, owner, transaction, dimensionIds, CreateVerticalDimension(
                x3,
                -finalTotalHalf,
                -finalTotalHalf + parameters.OuterThickness,
                rightChainX,
                "<>",
                textHeight,
                dimensionStyleId));
            AppendDimension(reference, owner, transaction, dimensionIds, CreateVerticalDimension(
                x3,
                -finalClearHalf - parameters.InnerThickness,
                -finalClearHalf,
                rightChainX,
                "<>",
                textHeight,
                dimensionStyleId));
            AppendDimension(reference, owner, transaction, dimensionIds, CreateVerticalDimension(
                x3,
                -finalClearHalf,
                finalClearHalf,
                rightChainX,
                "<>",
                textHeight,
                dimensionStyleId));
            AppendDimension(reference, owner, transaction, dimensionIds, CreateVerticalDimension(
                x3,
                finalClearHalf,
                finalClearHalf + parameters.InnerThickness,
                rightChainX,
                "<>",
                textHeight,
                dimensionStyleId));
            AppendDimension(reference, owner, transaction, dimensionIds, CreateVerticalDimension(
                x3,
                finalClearHalf + parameters.InnerThickness,
                finalTotalHalf,
                rightChainX,
                "<>",
                textHeight,
                dimensionStyleId));
            AppendDimension(reference, owner, transaction, dimensionIds, CreateVerticalDimension(
                x3,
                -finalTotalHalf,
                finalTotalHalf,
                rightTotalX,
                "<>",
                textHeight,
                dimensionStyleId));

            return dimensionIds;
        }

        private static List<ObjectId> AppendTypeBDimensions(
            BlockReference reference,
            BlockTableRecord owner,
            Transaction transaction,
            TransitionSectionParameters parameters)
        {
            double x0 = 0.0;
            double x1 = parameters.InitialStraightLength;
            double x2 = x1 + parameters.TransitionLength;
            double x3 = x2 + parameters.FinalStraightLength;
            double x4 = x3 + parameters.SecondTransitionLength;
            double initialClearHalf = parameters.InitialClearHeight / 2.0;
            double intermediateClearHalf = parameters.FinalClearHeight / 2.0;
            double terminalClearHalf = parameters.TerminalClearHeight / 2.0;
            double initialTotalHalf = parameters.InitialTotalHeight / 2.0;
            double terminalTotalHalf = parameters.TerminalTotalHeight / 2.0;
            double maximumHeight = parameters.TerminalTotalHeight;
            double margin = Math.Max(0.25, maximumHeight * 0.07);
            double firstDimensionY = -terminalTotalHalf - margin;
            double secondDimensionY = firstDimensionY - margin;
            double leftChainX = x0 - margin;
            double leftTotalX = x0 - (2.0 * margin);
            double intermediateDimensionX = x3 + (margin * 0.45);
            double rightTotalX = x4 + margin;
            double textHeight = Math.Max(0.06, maximumHeight / 45.0);
            ObjectId dimensionStyleId = owner.Database.Dimstyle;
            var dimensionIds = new List<ObjectId>();

            AppendDimension(reference, owner, transaction, dimensionIds, CreateHorizontalDimension(
                x1, -initialTotalHalf, x2,
                -(intermediateClearHalf + parameters.InnerThickness + parameters.OuterThickness),
                firstDimensionY, "<>", textHeight, dimensionStyleId));
            AppendDimension(reference, owner, transaction, dimensionIds, CreateHorizontalDimension(
                x2,
                -(intermediateClearHalf + parameters.InnerThickness + parameters.OuterThickness),
                x3,
                -(intermediateClearHalf + parameters.InnerThickness + parameters.OuterThickness),
                firstDimensionY, "<>", textHeight, dimensionStyleId));
            AppendDimension(reference, owner, transaction, dimensionIds, CreateHorizontalDimension(
                x3,
                -(intermediateClearHalf + parameters.InnerThickness + parameters.OuterThickness),
                x4, -terminalTotalHalf,
                firstDimensionY, "<>", textHeight, dimensionStyleId));
            AppendDimension(reference, owner, transaction, dimensionIds, CreateHorizontalDimension(
                x1, -initialTotalHalf, x4, -terminalTotalHalf,
                secondDimensionY, "<>", textHeight, dimensionStyleId));

            AppendVerticalChain(
                reference, owner, transaction, dimensionIds, x0,
                initialClearHalf, initialTotalHalf, leftChainX,
                textHeight, dimensionStyleId, parameters);
            AppendDimension(reference, owner, transaction, dimensionIds, CreateVerticalDimension(
                x0, -initialTotalHalf, initialTotalHalf, leftTotalX,
                "<>", textHeight, dimensionStyleId));
            AppendDimension(reference, owner, transaction, dimensionIds, CreateVerticalDimension(
                x3, -intermediateClearHalf, intermediateClearHalf,
                intermediateDimensionX, "<>", textHeight, dimensionStyleId));
            AppendDimension(reference, owner, transaction, dimensionIds, CreateVerticalDimension(
                x4, -terminalTotalHalf, terminalTotalHalf, rightTotalX,
                "<>", textHeight, dimensionStyleId));

            return dimensionIds;
        }

        private static void AppendVerticalChain(
            BlockReference reference,
            BlockTableRecord owner,
            Transaction transaction,
            ICollection<ObjectId> dimensionIds,
            double geometryX,
            double clearHalf,
            double totalHalf,
            double dimensionX,
            double textHeight,
            ObjectId dimensionStyleId,
            TransitionSectionParameters parameters)
        {
            double inner = parameters.InnerThickness;
            AppendDimension(reference, owner, transaction, dimensionIds, CreateVerticalDimension(
                geometryX, -totalHalf, -clearHalf - inner, dimensionX,
                "<>", textHeight, dimensionStyleId));
            AppendDimension(reference, owner, transaction, dimensionIds, CreateVerticalDimension(
                geometryX, -clearHalf - inner, -clearHalf, dimensionX,
                "<>", textHeight, dimensionStyleId));
            AppendDimension(reference, owner, transaction, dimensionIds, CreateVerticalDimension(
                geometryX, -clearHalf, clearHalf, dimensionX,
                "<>", textHeight, dimensionStyleId));
            AppendDimension(reference, owner, transaction, dimensionIds, CreateVerticalDimension(
                geometryX, clearHalf, clearHalf + inner, dimensionX,
                "<>", textHeight, dimensionStyleId));
            AppendDimension(reference, owner, transaction, dimensionIds, CreateVerticalDimension(
                geometryX, clearHalf + inner, totalHalf, dimensionX,
                "<>", textHeight, dimensionStyleId));
        }

        private static RotatedDimension CreateHorizontalDimension(
            double startX,
            double startY,
            double endX,
            double endY,
            double dimensionY,
            string dimensionText,
            double textHeight,
            ObjectId dimensionStyleId)
        {
            return ConfigureDimension(
                new RotatedDimension(
                    0.0,
                    new Point3d(startX, startY, 0.0),
                    new Point3d(endX, endY, 0.0),
                    new Point3d((startX + endX) / 2.0, dimensionY, 0.0),
                    dimensionText,
                    dimensionStyleId),
                textHeight);
        }

        private static RotatedDimension CreateVerticalDimension(
            double geometryX,
            double startY,
            double endY,
            double dimensionX,
            string dimensionText,
            double textHeight,
            ObjectId dimensionStyleId)
        {
            return ConfigureDimension(
                new RotatedDimension(
                    Math.PI / 2.0,
                    new Point3d(geometryX, startY, 0.0),
                    new Point3d(geometryX, endY, 0.0),
                    new Point3d(dimensionX, (startY + endY) / 2.0, 0.0),
                    dimensionText,
                    dimensionStyleId),
                textHeight);
        }

        private static RotatedDimension ConfigureDimension(
            RotatedDimension dimension,
            double textHeight)
        {
            dimension.Layer = DimensionLayerName;
            dimension.Dimdec = 2;
            dimension.Dimzin = 0;
            dimension.Dimtxt = textHeight;
            dimension.Dimasz = textHeight * 0.55;
            dimension.Dimgap = textHeight * 0.35;
            dimension.Dimexo = textHeight * 0.25;
            dimension.Dimexe = textHeight * 0.45;
            dimension.Dimtmove = 1;
            return dimension;
        }

        private static void AppendDimension(
            BlockReference reference,
            BlockTableRecord owner,
            Transaction transaction,
            ICollection<ObjectId> dimensionIds,
            RotatedDimension dimension)
        {
            dimension.TransformBy(reference.BlockTransform);
            ObjectId dimensionId = owner.AppendEntity(dimension);
            transaction.AddNewlyCreatedDBObject(dimension, true);
            WriteDimensionOwner(dimension, reference.ObjectId, transaction);
            dimensionIds.Add(dimensionId);
        }

        private static void ReplaceDimensions(
            BlockReference reference,
            BlockTableRecord owner,
            Transaction transaction,
            TransitionSectionParameters parameters)
        {
            List<ObjectId> dimensionIds = AppendDimensions(
                reference,
                owner,
                transaction,
                parameters);
            WriteDimensionIds(reference, dimensionIds, transaction);
        }

        private static void EraseAssociatedDimensions(
            BlockReference reference,
            Transaction transaction)
        {
            foreach (ObjectId dimensionId in ReadDimensionIds(reference, transaction))
            {
                Dimension dimension = transaction.GetObject(
                    dimensionId,
                    OpenMode.ForRead,
                    false) as Dimension;
                if (dimension == null || !IsOwnedBy(dimension, reference.ObjectId, transaction))
                    continue;

                dimension.UpgradeOpen();
                dimension.Erase();
            }
        }

        internal static int CountAssociatedDimensions(
            BlockReference reference,
            Transaction transaction)
        {
            int count = 0;
            foreach (ObjectId dimensionId in ReadDimensionIds(reference, transaction))
            {
                Dimension dimension = transaction.GetObject(
                    dimensionId,
                    OpenMode.ForRead,
                    false) as Dimension;
                if (dimension != null && IsOwnedBy(dimension, reference.ObjectId, transaction))
                    count++;
            }

            return count;
        }

        private static IEnumerable<ObjectId> ReadDimensionIds(
            BlockReference reference,
            Transaction transaction)
        {
            var result = new List<ObjectId>();
            if (reference.ExtensionDictionary.IsNull)
                return result;

            DBDictionary dictionary = transaction.GetObject(
                reference.ExtensionDictionary,
                OpenMode.ForRead,
                false) as DBDictionary;
            if (dictionary == null || !dictionary.Contains(DimensionRecordName))
                return result;

            Xrecord record = transaction.GetObject(
                dictionary.GetAt(DimensionRecordName),
                OpenMode.ForRead,
                false) as Xrecord;
            if (record == null || record.Data == null)
                return result;

            foreach (TypedValue value in record.Data)
            {
                if (value.Value is ObjectId)
                    result.Add((ObjectId)value.Value);
            }

            return result;
        }

        private static void WriteDimensionIds(
            BlockReference reference,
            IEnumerable<ObjectId> dimensionIds,
            Transaction transaction)
        {
            if (reference.ExtensionDictionary.IsNull)
                reference.CreateExtensionDictionary();

            DBDictionary dictionary = (DBDictionary)transaction.GetObject(
                reference.ExtensionDictionary,
                OpenMode.ForWrite);
            Xrecord record;
            if (dictionary.Contains(DimensionRecordName))
            {
                record = (Xrecord)transaction.GetObject(
                    dictionary.GetAt(DimensionRecordName),
                    OpenMode.ForWrite);
            }
            else
            {
                record = new Xrecord();
                dictionary.SetAt(DimensionRecordName, record);
                transaction.AddNewlyCreatedDBObject(record, true);
            }

            var values = new List<TypedValue>();
            foreach (ObjectId dimensionId in dimensionIds)
                values.Add(new TypedValue((int)DxfCode.SoftPointerId, dimensionId));
            record.Data = new ResultBuffer(values.ToArray());
        }

        private static void WriteDimensionOwner(
            Dimension dimension,
            ObjectId referenceId,
            Transaction transaction)
        {
            dimension.CreateExtensionDictionary();
            DBDictionary dictionary = (DBDictionary)transaction.GetObject(
                dimension.ExtensionDictionary,
                OpenMode.ForWrite);
            var record = new Xrecord
            {
                Data = new ResultBuffer(
                    new TypedValue((int)DxfCode.SoftPointerId, referenceId))
            };
            dictionary.SetAt(DimensionOwnerRecordName, record);
            transaction.AddNewlyCreatedDBObject(record, true);
        }

        private static bool IsOwnedBy(
            Dimension dimension,
            ObjectId referenceId,
            Transaction transaction)
        {
            ObjectId ownerId;
            return TryReadDimensionOwner(dimension, transaction, out ownerId) &&
                ownerId == referenceId;
        }

        private static bool TryReadDimensionOwner(
            Dimension dimension,
            Transaction transaction,
            out ObjectId referenceId)
        {
            referenceId = ObjectId.Null;
            if (dimension.ExtensionDictionary.IsNull)
                return false;

            DBDictionary dictionary = transaction.GetObject(
                dimension.ExtensionDictionary,
                OpenMode.ForRead,
                false) as DBDictionary;
            if (dictionary == null || !dictionary.Contains(DimensionOwnerRecordName))
                return false;

            Xrecord record = transaction.GetObject(
                dictionary.GetAt(DimensionOwnerRecordName),
                OpenMode.ForRead,
                false) as Xrecord;
            if (record == null || record.Data == null)
                return false;

            TypedValue[] values = record.Data.AsArray();
            if (values.Length != 1 || !(values[0].Value is ObjectId))
                return false;

            referenceId = (ObjectId)values[0].Value;
            return !referenceId.IsNull;
        }

        private static void EnsureDimensionLayer(
            Database database,
            Transaction transaction)
        {
            LayerTable layers = (LayerTable)transaction.GetObject(
                database.LayerTableId,
                OpenMode.ForRead);
            if (layers.Has(DimensionLayerName))
                return;

            layers.UpgradeOpen();
            var layer = new LayerTableRecord
            {
                Name = DimensionLayerName,
                Color = Color.FromColorIndex(ColorMethod.ByAci, 2)
            };
            layers.Add(layer);
            transaction.AddNewlyCreatedDBObject(layer, true);
        }

        private static Polyline CreateBoundary(IReadOnlyList<SectionPoint> polygon)
        {
            var boundary = new Polyline(polygon.Count)
            {
                Closed = true,
                Layer = "0"
            };
            for (int index = 0; index < polygon.Count; index++)
            {
                SectionPoint point = polygon[index];
                boundary.AddVertexAt(index, new Point2d(point.X, point.Y), 0.0, 0.0, 0.0);
            }

            return boundary;
        }

        private static void SetPattern(Hatch hatch, string patternName)
        {
            try
            {
                hatch.SetHatchPattern(HatchPatternType.PreDefined, patternName);
            }
            catch (global::Autodesk.AutoCAD.Runtime.Exception exception)
            {
                throw new InvalidOperationException(
                    "El patrón de hatch '" + patternName +
                    "' no está disponible en esta instalación de AutoCAD.",
                    exception);
            }
        }

        private static Color CreateEntityColor(int colorIndex)
        {
            if (colorIndex == 0)
                return Color.FromColorIndex(ColorMethod.ByBlock, 0);
            if (colorIndex == 256)
                return Color.FromColorIndex(ColorMethod.ByLayer, 256);
            return Color.FromColorIndex(ColorMethod.ByAci, (short)colorIndex);
        }

        private static void WriteParameters(
            BlockReference reference,
            Transaction transaction,
            TransitionSectionParameters parameters)
        {
            if (reference.ExtensionDictionary.IsNull)
                reference.CreateExtensionDictionary();

            DBDictionary dictionary = (DBDictionary)transaction.GetObject(
                reference.ExtensionDictionary,
                OpenMode.ForWrite);
            Xrecord record;
            if (dictionary.Contains(ParameterRecordName))
            {
                record = (Xrecord)transaction.GetObject(
                    dictionary.GetAt(ParameterRecordName),
                    OpenMode.ForWrite);
            }
            else
            {
                record = new Xrecord();
                dictionary.SetAt(ParameterRecordName, record);
                transaction.AddNewlyCreatedDBObject(record, true);
            }

            record.Data = new ResultBuffer(
                new TypedValue((int)DxfCode.Int16, SchemaVersion),
                new TypedValue((int)DxfCode.Real, parameters.InitialStraightLength),
                new TypedValue((int)DxfCode.Real, parameters.TransitionLength),
                new TypedValue((int)DxfCode.Real, parameters.FinalStraightLength),
                new TypedValue((int)DxfCode.Real, parameters.InitialClearHeight),
                new TypedValue((int)DxfCode.Real, parameters.FinalClearHeight),
                new TypedValue((int)DxfCode.Real, parameters.InnerThickness),
                new TypedValue((int)DxfCode.Real, parameters.OuterThickness),
                new TypedValue((int)DxfCode.Text, parameters.HatchPatternName),
                new TypedValue((int)DxfCode.Real, parameters.HatchScale),
                new TypedValue((int)DxfCode.Real, parameters.HatchAngleDegrees),
                new TypedValue((int)DxfCode.Int16, parameters.HatchColorIndex),
                new TypedValue((int)DxfCode.Int16, (short)parameters.SectionType),
                new TypedValue((int)DxfCode.Real, parameters.SecondTransitionLength),
                new TypedValue((int)DxfCode.Real, parameters.TerminalTotalHeight),
                new TypedValue((int)DxfCode.Real, parameters.WallHeight),
                new TypedValue((int)DxfCode.Real, parameters.WallAngleDegrees));
        }
    }
}
