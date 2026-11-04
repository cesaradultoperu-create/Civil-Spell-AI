using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CivilSpellAI.Autodesk;
using CivilSpellAI.Geometry;
using CivilSpellAI.UI;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CivilSpellAI.Commands
{
    public sealed class ParametricSectionCommands
    {
        [CommandMethod("CREARSECCION", CommandFlags.Modal)]
        public void CreateSection()
        {
            Document document = AcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null)
                return;

            ObjectId selectedReferenceId = ObjectId.Null;
            TransitionSectionParameters parameters = ShowParameterWindow(
                document,
                TransitionSectionParameters.Defaults,
                ref selectedReferenceId);
            if (parameters == null)
                return;

            PromptPointResult point = document.Editor.GetPoint(
                "\nIndique el centro del extremo izquierdo de la sección: ");
            if (point.Status != PromptStatus.OK)
                return;

            try
            {
                using (Transaction transaction =
                    document.Database.TransactionManager.StartTransaction())
                {
                    new ParametricSectionDrawingService().Create(
                        document.Database,
                        transaction,
                        point.Value,
                        parameters);
                    transaction.Commit();
                }

                document.Editor.WriteMessage(
                    "\nSección creada. Use EDITARSECCION para modificar sus medidas.");
            }
            catch (System.Exception exception)
            {
                document.Editor.WriteMessage(
                    "\nNo se pudo crear la sección: " + exception.Message);
            }
        }

        [CommandMethod("EDITARSECCION", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void EditSection()
        {
            Document document = AcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null)
                return;

            PromptSelectionResult selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK)
                selection = document.Editor.GetSelection(new PromptSelectionOptions {
                    MessageForAdding = "\nSeleccione la figura completa con una ventana, o un bloque/cota, y pulse Enter: " });
            if (selection.Status != PromptStatus.OK || selection.Value == null)
                return;
            ObjectId[] selectedIds = selection.Value.GetObjectIds();
            if (Figure3DCommands.TryEditSelection(document, selectedIds)) return;

            var service = new ParametricSectionDrawingService();
            TransitionSectionParameters original = null;
            ObjectId referenceId = ObjectId.Null;
            SelectedFigureSnapshot looseFigure = null;
            using (Transaction transaction =
                document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                bool hasParametricReference = false;
                foreach (ObjectId id in selectedIds)
                {
                    ObjectId resolved;
                    if (service.TryResolveSectionReference(id, transaction, out resolved))
                        hasParametricReference = true;
                }
                if (!hasParametricReference)
                {
                    string figureError;
                    if (!new SelectedFigureDrawingService().TryRead(selectedIds, transaction, out looseFigure, out figureError))
                    {
                        document.Editor.WriteMessage("\n" + figureError);
                        return;
                    }
                }
                else
                {
                    string error;
                    if (!new ParametricSectionObjectAnalyzer().TryAnalyze(
                            selectedIds, transaction,
                            out original, out referenceId, out error))
                    {
                        document.Editor.WriteMessage("\n" + error);
                        return;
                    }
                }
            }
            if (looseFigure != null)
            {
                ShowSelectedFigureWindow(document, looseFigure, false);
                return;
            }

            ObjectId selectedReferenceId = referenceId;
            TransitionSectionParameters updated = ShowParameterWindow(
                document, original, ref selectedReferenceId, true);
            if (updated == null)
                return;
            referenceId = selectedReferenceId;

            global::Autodesk.AutoCAD.Geometry.Point3d insertionPoint =
                global::Autodesk.AutoCAD.Geometry.Point3d.Origin;
            if (referenceId.IsNull)
            {
                document.Editor.WriteMessage(
                    "\nEl objeto no tiene parámetros guardados. Se creará una sección paramétrica editable a partir de sus medidas.");
                PromptPointResult point = document.Editor.GetPoint(
                    "\nIndique el centro del extremo izquierdo de la nueva sección: ");
                if (point.Status != PromptStatus.OK)
                    return;
                insertionPoint = point.Value;
            }

            try
            {
                using (Transaction transaction =
                    document.Database.TransactionManager.StartTransaction())
                {
                    if (referenceId.IsNull)
                        service.Create(document.Database, transaction, insertionPoint, updated);
                    else
                        service.Update(document.Database, transaction, referenceId, updated);
                    transaction.Commit();
                }

                document.Editor.WriteMessage(
                    referenceId.IsNull
                        ? "\nSección editable creada. El objeto de origen se conserva."
                        : "\nGeometría, hatch y cotas de la sección seleccionada actualizados.");
            }
            catch (System.Exception exception)
            {
                document.Editor.WriteMessage(
                    "\nNo se pudo actualizar la sección: " + exception.Message);
            }
        }

#if DEBUG
        [CommandMethod("AISPELLTESTSECTIONOBJECTS", CommandFlags.Modal)]
        public void TestSectionObjectSelection()
        {
            Document document = AcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null)
                return;

            // The transaction is deliberately rolled back: the fixture never changes the DWG.
            using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
            {
                var service = new ParametricSectionDrawingService();
                var analyzer = new ParametricSectionObjectAnalyzer();
                var expected = new TransitionSectionParameters(
                    2.1, 1.4, 0.8, 0.6, 1.8, 0.2, 0.12,
                    "GRAVEL", 0.31, 17.0, 3, TransitionSectionType.TypeA,
                    0.0, 0.0, 0.75, 63.0);
                ObjectId first = service.Create(document.Database, transaction,
                    global::Autodesk.AutoCAD.Geometry.Point3d.Origin, expected);
                ObjectId second = service.Create(document.Database, transaction,
                    new global::Autodesk.AutoCAD.Geometry.Point3d(0, 20, 0),
                    TransitionSectionParameters.TypeBDefaults);
                TransitionSectionParameters detected;
                ObjectId resolved;
                string error;
                if (!analyzer.TryAnalyze(new[] { first }, transaction,
                        out detected, out resolved, out error) || resolved != first ||
                    System.Math.Abs(detected.WallHeight - expected.WallHeight) > 1e-9 ||
                    System.Math.Abs(detected.WallAngleDegrees - expected.WallAngleDegrees) > 1e-9 ||
                    System.Math.Abs(detected.InitialStraightLength - expected.InitialStraightLength) > 1e-9 ||
                    detected.HatchColorIndex != expected.HatchColorIndex)
                    throw new System.InvalidOperationException("No se conservaron los parámetros del bloque: " + error);

                BlockReference reference = (BlockReference)transaction.GetObject(first, OpenMode.ForRead);
                BlockTableRecord owner = (BlockTableRecord)transaction.GetObject(reference.OwnerId, OpenMode.ForRead);
                ObjectId dimensionId = ObjectId.Null;
                foreach (ObjectId id in owner)
                {
                    ObjectId dimensionOwner;
                    if (id != first && service.TryResolveSectionReference(id, transaction, out dimensionOwner) &&
                        dimensionOwner == first)
                    {
                        dimensionId = id;
                        break;
                    }
                }
                if (dimensionId.IsNull || !analyzer.TryAnalyze(new[] { dimensionId },
                        transaction, out detected, out resolved, out error) || resolved != first ||
                    System.Math.Abs(detected.WallHeight - expected.WallHeight) > 1e-9)
                    throw new System.InvalidOperationException("No se reconoció la cota asociada: " + error);
                if (!analyzer.TryAnalyze(new[] { dimensionId, first, dimensionId },
                        transaction, out detected, out resolved, out error) || resolved != first)
                    throw new System.InvalidOperationException("La selección repetida de una sección se consideró ambigua.");
                if (!analyzer.TryAnalyze(new[] { second }, transaction, out detected, out resolved, out error) ||
                    resolved != second || detected.SectionType != TransitionSectionType.TypeB)
                    throw new System.InvalidOperationException("No se reconoció el segundo destino: " + error);
                if (analyzer.TryAnalyze(new[] { first, second }, transaction, out detected, out resolved, out error) ||
                    string.IsNullOrWhiteSpace(error))
                    throw new System.InvalidOperationException("Se aceptó una selección ambigua de dos secciones.");

                owner.UpgradeOpen();
                var conventional = new BlockReference(
                    new global::Autodesk.AutoCAD.Geometry.Point3d(10, 5, 0), reference.BlockTableRecord);
                ObjectId conventionalId = owner.AppendEntity(conventional);
                transaction.AddNewlyCreatedDBObject(conventional, true);
                if (!analyzer.TryAnalyze(new[] { conventionalId }, transaction,
                        out detected, out resolved, out error) || !resolved.IsNull ||
                    System.Math.Abs(detected.InitialStraightLength - expected.InitialStraightLength) > 1e-6 ||
                    System.Math.Abs(detected.InitialClearHeight - expected.InitialClearHeight) > 1e-6 ||
                    System.Math.Abs(detected.InnerThickness - expected.InnerThickness) > 1e-6 ||
                    System.Math.Abs(detected.OuterThickness - expected.OuterThickness) > 1e-6)
                    throw new System.InvalidOperationException("No se reconoció el bloque convencional: " + error);
                if (analyzer.TryAnalyze(new[] { first, conventionalId }, transaction,
                        out detected, out resolved, out error) || string.IsNullOrWhiteSpace(error))
                    throw new System.InvalidOperationException("Se aceptó una selección que mezcla dos objetos diferentes.");

                conventional.TransformBy(global::Autodesk.AutoCAD.Geometry.Matrix3d.Rotation(
                    System.Math.PI / 2.0, global::Autodesk.AutoCAD.Geometry.Vector3d.ZAxis,
                    conventional.Position));
                if (!analyzer.TryAnalyze(new[] { conventionalId }, transaction,
                        out detected, out resolved, out error) || !resolved.IsNull ||
                    System.Math.Abs(detected.InitialClearHeight - expected.InitialClearHeight) > 1e-6 ||
                    System.Math.Abs(detected.InitialStraightLength - expected.InitialStraightLength) > 1e-6)
                    throw new System.InvalidOperationException("No se reconoció el perfil vertical: " + error);

                var hatchIds = new System.Collections.Generic.List<ObjectId>();
                BlockTableRecord definition = (BlockTableRecord)transaction.GetObject(
                    reference.BlockTableRecord, OpenMode.ForRead);
                foreach (ObjectId id in definition)
                {
                    if (transaction.GetObject(id, OpenMode.ForRead) is Hatch)
                        hatchIds.Add(id);
                }
                if (!analyzer.TryAnalyze(hatchIds, transaction, out detected, out resolved, out error) ||
                    System.Math.Abs(detected.InnerThickness - expected.InnerThickness) > 1e-6 ||
                    System.Math.Abs(detected.OuterThickness - expected.OuterThickness) > 1e-6)
                    throw new System.InvalidOperationException("No se recuperaron los contornos de los hatch: " + error);

                var unsupported = new Line(global::Autodesk.AutoCAD.Geometry.Point3d.Origin,
                    new global::Autodesk.AutoCAD.Geometry.Point3d(1, 0, 0));
                ObjectId unsupportedId = owner.AppendEntity(unsupported);
                transaction.AddNewlyCreatedDBObject(unsupported, true);
                if (analyzer.TryAnalyze(new[] { unsupportedId }, transaction,
                        out detected, out resolved, out error) || string.IsNullOrWhiteSpace(error))
                    throw new System.InvalidOperationException("Se aceptó una línea aislada sin parámetros.");
            }
            document.Editor.WriteMessage("\nPARAMETRIC_SECTION_OBJECT_SELECTION_OK");
        }

        [CommandMethod("AISPELLTESTSECTIONFILE", CommandFlags.Modal)]
        public void TestSectionRecognitionInCurrentDrawing()
        {
            Document document = AcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null)
                return;

            using (Transaction transaction =
                document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                BlockTable blockTable = (BlockTable)transaction.GetObject(
                    document.Database.BlockTableId,
                    OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)transaction.GetObject(
                    blockTable[BlockTableRecord.ModelSpace],
                    OpenMode.ForRead);
                var polylines = new System.Collections.Generic.List<Polyline>();
                foreach (ObjectId entityId in modelSpace)
                {
                    Polyline polyline = transaction.GetObject(
                        entityId,
                        OpenMode.ForRead,
                        false) as Polyline;
                    if (polyline != null && polyline.Closed && polyline.NumberOfVertices >= 6)
                        polylines.Add(polyline);
                }

                var groups = GroupTouchingPolylines(polylines);
                int recognized = 0;
                int recognizedTypeB = 0;
                foreach (System.Collections.Generic.List<Polyline> group in groups)
                {
                    if (group.Count < 3)
                        continue;
                    TransitionSectionParameters detected;
                    string error;
                    if (!new ParametricSectionObjectAnalyzer().TryAnalyze(
                            group.ConvertAll(item => item.ObjectId),
                            transaction,
                            out detected,
                            out error))
                    {
                        continue;
                    }

                    recognized++;
                    if (detected.SectionType == TransitionSectionType.TypeB)
                        recognizedTypeB++;
                    document.Editor.WriteMessage(
                        "\nSECCION_RECONOCIDA tipo={0} tramos={1:0.###} alturaInicial={2:0.###} alturaFinal={3:0.###}",
                        detected.SectionType,
                        detected.DimensionedLength,
                        detected.InitialTotalHeight,
                        detected.SectionType == TransitionSectionType.TypeB
                            ? detected.TerminalTotalHeight
                            : detected.FinalTotalHeight);
                }

                if (recognized == 0)
                {
                    document.Editor.WriteMessage(
                        "No se reconoció en el DWG una sección de cinco estaciones.");
                    return;
                }
            }

            document.Editor.WriteMessage("\nPARAMETRIC_SECTION_FILE_SMOKE_OK");
        }

        private static System.Collections.Generic.List<System.Collections.Generic.List<Polyline>>
            GroupTouchingPolylines(System.Collections.Generic.IList<Polyline> polylines)
        {
            var groups = new System.Collections.Generic.List<System.Collections.Generic.List<Polyline>>();
            var visited = new bool[polylines.Count];
            for (int start = 0; start < polylines.Count; start++)
            {
                if (visited[start])
                    continue;
                var group = new System.Collections.Generic.List<Polyline>();
                var pending = new System.Collections.Generic.Queue<int>();
                pending.Enqueue(start);
                visited[start] = true;
                while (pending.Count > 0)
                {
                    int current = pending.Dequeue();
                    group.Add(polylines[current]);
                    for (int candidate = 0; candidate < polylines.Count; candidate++)
                    {
                        if (visited[candidate])
                            continue;
                        if (ExtentsOverlap(
                                polylines[current].GeometricExtents,
                                polylines[candidate].GeometricExtents))
                        {
                            visited[candidate] = true;
                            pending.Enqueue(candidate);
                        }
                    }
                }

                groups.Add(group);
            }

            return groups;
        }

        private static bool ExtentsOverlap(
            global::Autodesk.AutoCAD.DatabaseServices.Extents3d left,
            global::Autodesk.AutoCAD.DatabaseServices.Extents3d right)
        {
            double size = System.Math.Max(
                System.Math.Max(left.MaxPoint.X - left.MinPoint.X, left.MaxPoint.Y - left.MinPoint.Y),
                System.Math.Max(right.MaxPoint.X - right.MinPoint.X, right.MaxPoint.Y - right.MinPoint.Y));
            double tolerance = System.Math.Max(1e-8, size * 1e-5);
            return left.MinPoint.X <= right.MaxPoint.X + tolerance &&
                left.MaxPoint.X + tolerance >= right.MinPoint.X &&
                left.MinPoint.Y <= right.MaxPoint.Y + tolerance &&
                left.MaxPoint.Y + tolerance >= right.MinPoint.Y;
        }

        [CommandMethod("AISPELLTESTSECTION", CommandFlags.Modal)]
        public void TestSectionCreation()
        {
            Document document = AcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null)
                return;

            var service = new ParametricSectionDrawingService();
            ObjectId referenceId;
            ObjectId typeBReferenceId;
            using (Transaction transaction =
                document.Database.TransactionManager.StartTransaction())
            {
                referenceId = service.Create(
                    document.Database,
                    transaction,
                    global::Autodesk.AutoCAD.Geometry.Point3d.Origin,
                    TransitionSectionParameters.Defaults);
                typeBReferenceId = service.Create(
                    document.Database,
                    transaction,
                    new global::Autodesk.AutoCAD.Geometry.Point3d(0.0, 10.0, 0.0),
                    TransitionSectionParameters.TypeBDefaults);
                transaction.Commit();
            }

            using (Transaction transaction =
                document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                TransitionSectionParameters parameters;
                if (!service.TryReadParameters(referenceId, transaction, out parameters))
                {
                    throw new System.InvalidOperationException(
                        "No se recuperaron los parámetros de la sección de prueba.");
                }

                BlockReference reference = (BlockReference)transaction.GetObject(
                    referenceId,
                    OpenMode.ForRead);
                BlockTableRecord definition = (BlockTableRecord)transaction.GetObject(
                    reference.BlockTableRecord,
                    OpenMode.ForRead);
                int polylineCount = 0;
                int hatchCount = 0;
                int solidCount = 0;
                double solidVolume = 0.0;
                int positiveWallInclinations = 0;
                int negativeWallInclinations = 0;
                foreach (ObjectId entityId in definition)
                {
                    Entity entity = transaction.GetObject(
                        entityId,
                        OpenMode.ForRead) as Entity;
                    if (entity is Polyline)
                        polylineCount++;
                    else if (entity is Hatch)
                    {
                        Hatch hatch = (Hatch)entity;
                        if (!hatch.Associative)
                        {
                            throw new System.InvalidOperationException(
                                "El hatch de prueba no quedó asociativo.");
                        }

                        hatchCount++;
                    }
                    else if (entity is Solid3d)
                    {
                        Solid3d solid = (Solid3d)entity;
                        solidCount++;
                        solidVolume += solid.MassProperties.Volume;
                        int inclination = DetectWallInclination(
                            solid,
                            parameters.InnerThickness,
                            parameters.WallHeight);
                        if (inclination > 0)
                            positiveWallInclinations++;
                        else if (inclination < 0)
                            negativeWallInclinations++;
                    }
                }

                int dimensionCount = ParametricSectionDrawingService.CountAssociatedDimensions(
                    reference,
                    transaction);
                bool resolvedFromDimension = false;
                BlockTableRecord referenceOwner = (BlockTableRecord)transaction.GetObject(
                    reference.OwnerId,
                    OpenMode.ForRead);
                foreach (ObjectId ownerEntityId in referenceOwner)
                {
                    if (!(transaction.GetObject(
                            ownerEntityId,
                            OpenMode.ForRead,
                            false) is Dimension))
                    {
                        continue;
                    }

                    ObjectId resolvedId;
                    if (service.TryResolveSectionReference(
                            ownerEntityId,
                            transaction,
                            out resolvedId) &&
                        resolvedId == referenceId)
                    {
                        resolvedFromDimension = true;
                        break;
                    }
                }
                if (polylineCount != 5 || hatchCount != 5 || solidCount != 2 ||
                    System.Math.Abs(
                        solidVolume -
                        (2.0 * parameters.OuterThickness *
                         (parameters.InitialStraightLength + parameters.DimensionedLength) *
                         parameters.WallHeight)) > 1e-8 ||
                    positiveWallInclinations != 1 ||
                    negativeWallInclinations != 1 ||
                    !resolvedFromDimension ||
                    dimensionCount != 13 ||
                    System.Math.Abs(parameters.InitialTotalHeight - 0.80) > 1e-9 ||
                    System.Math.Abs(parameters.FinalTotalHeight - 1.70) > 1e-9)
                {
                    throw new System.InvalidOperationException(
                        "La sección de prueba no coincide con la geometría esperada.");
                }

                TransitionSectionParameters typeBParameters;
                if (!service.TryReadParameters(
                        typeBReferenceId,
                        transaction,
                        out typeBParameters) ||
                    typeBParameters.SectionType != TransitionSectionType.TypeB)
                {
                    throw new System.InvalidOperationException(
                        "No se recuperaron los parámetros de la sección tipo B.");
                }

                BlockReference typeBReference = (BlockReference)transaction.GetObject(
                    typeBReferenceId,
                    OpenMode.ForRead);
                BlockTableRecord typeBDefinition = (BlockTableRecord)transaction.GetObject(
                    typeBReference.BlockTableRecord,
                    OpenMode.ForRead);
                int typeBPolylineCount = 0;
                int typeBHatchCount = 0;
                int typeBSolidCount = 0;
                foreach (ObjectId entityId in typeBDefinition)
                {
                    Entity entity = transaction.GetObject(
                        entityId,
                        OpenMode.ForRead) as Entity;
                    if (entity is Polyline)
                        typeBPolylineCount++;
                    else if (entity is Hatch)
                        typeBHatchCount++;
                    else if (entity is Solid3d)
                        typeBSolidCount++;
                }

                int typeBDimensionCount =
                    ParametricSectionDrawingService.CountAssociatedDimensions(
                        typeBReference,
                        transaction);
                if (typeBPolylineCount != 5 || typeBHatchCount != 5 ||
                    typeBSolidCount != 2 ||
                    typeBDimensionCount != 12 ||
                    System.Math.Abs(typeBParameters.TerminalTotalHeight - 7.0) > 1e-9)
                {
                    throw new System.InvalidOperationException(
                        "La sección tipo B no coincide con la geometría esperada.");
                }

                var sourceEntityIds = new System.Collections.Generic.List<ObjectId>();
                foreach (ObjectId entityId in typeBDefinition)
                    sourceEntityIds.Add(entityId);
                TransitionSectionParameters detectedParameters;
                string detectionError;
                if (!new ParametricSectionObjectAnalyzer().TryAnalyze(
                        sourceEntityIds,
                        transaction,
                        out detectedParameters,
                        out detectionError) ||
                    detectedParameters.SectionType != TransitionSectionType.TypeB ||
                    System.Math.Abs(detectedParameters.DimensionedLength - 4.03) > 1e-6)
                {
                    throw new System.InvalidOperationException(
                        "El reconocimiento desde objeto falló: " + detectionError);
                }
            }

            document.Editor.WriteMessage("\nPARAMETRIC_SECTION_SMOKE_OK");
        }

        [CommandMethod("AISPELLPROBESOLIDS", CommandFlags.Modal)]
        public void ProbeSolidsInCurrentDrawing()
        {
            Document document = AcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null)
                return;

            int count = 0;
            using (Transaction transaction =
                document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                BlockTable blockTable = (BlockTable)transaction.GetObject(
                    document.Database.BlockTableId,
                    OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)transaction.GetObject(
                    blockTable[BlockTableRecord.ModelSpace],
                    OpenMode.ForRead);
                foreach (ObjectId entityId in modelSpace)
                {
                    Solid3d solid = transaction.GetObject(
                        entityId,
                        OpenMode.ForRead,
                        false) as Solid3d;
                    if (solid == null)
                        continue;

                    try
                    {
                        global::Autodesk.AutoCAD.DatabaseServices.Extents3d extents =
                            solid.GeometricExtents;
                        document.Editor.WriteMessage(
                            "\nSOLID3D handle={0} min=({1:0.###},{2:0.###},{3:0.###}) max=({4:0.###},{5:0.###},{6:0.###}) volume={7:0.#########}",
                            solid.Handle,
                            extents.MinPoint.X,
                            extents.MinPoint.Y,
                            extents.MinPoint.Z,
                            extents.MaxPoint.X,
                            extents.MaxPoint.Y,
                            extents.MaxPoint.Z,
                            solid.MassProperties.Volume);
                        count++;
                    }
                    catch (System.Exception exception)
                    {
                        document.Editor.WriteMessage(
                            "\nSOLID3D_PROBE_FAIL handle={0} error={1}",
                            solid.Handle,
                            exception.Message);
                    }
                }
            }

            document.Editor.WriteMessage("\nSOLID3D_PROBE_OK count={0}", count);
        }

        [CommandMethod("AISPELLPREPARESECTIONCOPY", CommandFlags.Modal)]
        public void PrepareSectionTestCopy()
        {
            Document document = AcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null)
                return;
            int count = 0;
            using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
            {
                BlockTable blocks = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                BlockTableRecord model = (BlockTableRecord)transaction.GetObject(
                    blocks[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var polylines = new System.Collections.Generic.List<Polyline>();
                foreach (ObjectId id in model)
                {
                    Polyline polyline = transaction.GetObject(id, OpenMode.ForRead, false) as Polyline;
                    if (polyline != null && polyline.Closed && polyline.NumberOfVertices >= 6)
                        polylines.Add(polyline);
                }
                var detectedSections = new System.Collections.Generic.List<TransitionSectionParameters>();
                foreach (System.Collections.Generic.List<Polyline> group in GroupTouchingPolylines(polylines))
                {
                    if (group.Count < 3)
                        continue;
                    TransitionSectionParameters parameters;
                    string error;
                    if (new ParametricSectionObjectAnalyzer().TryAnalyze(
                            group.ConvertAll(item => item.ObjectId), transaction, out parameters, out error))
                        detectedSections.Add(parameters);
                }
                if (detectedSections.Count == 0)
                    throw new System.InvalidOperationException("No se encontraron secciones compatibles para la copia de prueba.");

                double startX = double.MinValue;
                double startY = double.MinValue;
                foreach (Polyline polyline in polylines)
                {
                    startX = System.Math.Max(startX, polyline.GeometricExtents.MaxPoint.X);
                    startY = System.Math.Max(startY, polyline.GeometricExtents.MaxPoint.Y);
                }
                startX += 12.0;
                double size = 0.0;
                foreach (TransitionSectionParameters parameters in detectedSections)
                    size = System.Math.Max(size, System.Math.Max(
                        parameters.InitialStraightLength + parameters.DimensionedLength,
                        System.Math.Max(parameters.InitialTotalHeight,
                            parameters.SectionType == TransitionSectionType.TypeB
                                ? parameters.TerminalTotalHeight : parameters.FinalTotalHeight)));
                double columnWidth = size + 14.0;
                double rowHeight = size + 6.0;
                const string testLayer = "CIVILSPELLAI_PRUEBAS";
                LayerTable layers = (LayerTable)transaction.GetObject(document.Database.LayerTableId, OpenMode.ForRead);
                if (!layers.Has(testLayer))
                {
                    layers.UpgradeOpen();
                    var layer = new LayerTableRecord { Name = testLayer };
                    layers.Add(layer);
                    transaction.AddNewlyCreatedDBObject(layer, true);
                }
                AppendTestLabel(model, transaction, testLayer,
                    new global::Autodesk.AutoCAD.Geometry.Point3d(startX, startY + size + 4, 0),
                    "ZONA DE PRUEBAS - EDITARSECCION", 0.6);
                AppendTestLabel(model, transaction, testLayer,
                    new global::Autodesk.AutoCAD.Geometry.Point3d(startX, startY + size + 2.5, 0),
                    "Seleccione un contorno o una cota amarilla. El dibujo original se conserva a la izquierda.", 0.3);

                // Include a vertical instance with the same dimensions as the first recognized section.
                detectedSections.Add(detectedSections[0]);
                var service = new ParametricSectionDrawingService();
                for (int index = 0; index < detectedSections.Count; index++)
                {
                    TransitionSectionParameters parameters = detectedSections[index];
                    bool vertical = index == detectedSections.Count - 1;
                    var point = new global::Autodesk.AutoCAD.Geometry.Point3d(
                        startX + (index % 2) * columnWidth,
                        startY - (index / 2) * rowHeight, 0);
                    ObjectId id = service.Create(document.Database, transaction, point, parameters);
                    var reference = (BlockReference)transaction.GetObject(id, OpenMode.ForWrite);
                    reference.Layer = testLayer;
                    if (vertical)
                    {
                        var transform = global::Autodesk.AutoCAD.Geometry.Matrix3d.Rotation(
                            System.Math.PI / 2.0, global::Autodesk.AutoCAD.Geometry.Vector3d.ZAxis, point);
                        reference.TransformBy(transform);
                        foreach (ObjectId dimensionId in model)
                        {
                            if (!(transaction.GetObject(dimensionId, OpenMode.ForRead) is Dimension))
                                continue;
                            ObjectId resolved;
                            if (service.TryResolveSectionReference(dimensionId, transaction, out resolved) && resolved == id)
                                ((Dimension)transaction.GetObject(dimensionId, OpenMode.ForWrite)).TransformBy(transform);
                        }
                    }
                    AppendTestLabel(model, transaction, testLayer,
                        new global::Autodesk.AutoCAD.Geometry.Point3d(point.X - 1, point.Y + size + 0.8, 0),
                        "PRUEBA " + (index + 1) + " - " +
                        (parameters.SectionType == TransitionSectionType.TypeB ? "Tipo B" : "Tipo A") +
                        (vertical ? " - Vertical" : " - Horizontal"), 0.35);
                    TransitionSectionParameters verified;
                    if (!service.TryReadParameters(id, transaction, out verified))
                        throw new System.InvalidOperationException("La copia de prueba no conserva sus parametros.");
                    count++;
                }
                transaction.Commit();
            }
            document.Editor.WriteMessage("\nPARAMETRIC_TEST_COPY_PREPARED count={0}", count);
        }

        private static void AppendTestLabel(
            BlockTableRecord model, Transaction transaction, string layer,
            global::Autodesk.AutoCAD.Geometry.Point3d position, string text, double height)
        {
            var label = new DBText { Layer = layer, Position = position, TextString = text, Height = height };
            model.AppendEntity(label);
            transaction.AddNewlyCreatedDBObject(label, true);
        }

        [CommandMethod("AISPELLCREATEDRAWING3DTEST", CommandFlags.Modal)]
        public void CreateDrawing3DParametricTest()
        {
            Document document = AcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null)
                return;

            using (Transaction transaction =
                document.Database.TransactionManager.StartTransaction())
            {
                BlockTable blockTable = (BlockTable)transaction.GetObject(
                    document.Database.BlockTableId,
                    OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)transaction.GetObject(
                    blockTable[BlockTableRecord.ModelSpace],
                    OpenMode.ForRead);
                var polylines = new System.Collections.Generic.List<Polyline>();
                foreach (ObjectId entityId in modelSpace)
                {
                    Polyline polyline = transaction.GetObject(
                        entityId,
                        OpenMode.ForRead,
                        false) as Polyline;
                    if (polyline != null && polyline.Closed && polyline.NumberOfVertices >= 6)
                        polylines.Add(polyline);
                }

                foreach (System.Collections.Generic.List<Polyline> group in
                    GroupTouchingPolylines(polylines))
                {
                    if (group.Count < 3)
                        continue;

                    TransitionSectionParameters detected;
                    string error;
                    if (!new ParametricSectionObjectAnalyzer().TryAnalyze(
                            group.ConvertAll(item => item.ObjectId),
                            transaction,
                            out detected,
                            out error))
                    {
                        continue;
                    }

                    double minX = double.MaxValue;
                    double minY = double.MaxValue;
                    double maxY = double.MinValue;
                    foreach (Polyline polyline in group)
                    {
                        global::Autodesk.AutoCAD.DatabaseServices.Extents3d extents =
                            polyline.GeometricExtents;
                        minX = System.Math.Min(minX, extents.MinPoint.X);
                        minY = System.Math.Min(minY, extents.MinPoint.Y);
                        maxY = System.Math.Max(maxY, extents.MaxPoint.Y);
                    }

                    global::Autodesk.AutoCAD.Geometry.Point3d insertionPoint =
                        new global::Autodesk.AutoCAD.Geometry.Point3d(
                        minX,
                        minY - System.Math.Max(2.0, maxY - minY),
                        0.0);
                    ObjectId resultId = new ParametricSectionDrawingService().Create(
                        document.Database,
                        transaction,
                        insertionPoint,
                        detected);
                    transaction.Commit();
                    string resultPath = System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(document.Database.Filename),
                        "Drawing3D-parametric-result.dwg");
                    document.Database.SaveAs(
                        resultPath,
                        DwgVersion.Current);
                    document.Editor.WriteMessage(
                        "\nDRAWING3D_PARAMETRIC_TEST_OK handle={0} wallHeight={1:0.###} path={2}",
                        resultId.Handle,
                        detected.WallHeight,
                        resultPath);
                    return;
                }
            }

            document.Editor.WriteMessage("\nDRAWING3D_PARAMETRIC_TEST_FAIL");
        }

        [CommandMethod("AISPELLVALIDATEPARAMETRICBLOCK", CommandFlags.Modal)]
        public void ValidateParametricBlockInCurrentDrawing()
        {
            Document document = AcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null)
                return;

            int candidates = 0;
            int valid = 0;
            using (Transaction transaction =
                document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                BlockTable blockTable = (BlockTable)transaction.GetObject(
                    document.Database.BlockTableId,
                    OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)transaction.GetObject(
                    blockTable[BlockTableRecord.ModelSpace],
                    OpenMode.ForRead);
                var service = new ParametricSectionDrawingService();
                foreach (ObjectId entityId in modelSpace)
                {
                    BlockReference reference = transaction.GetObject(
                        entityId,
                        OpenMode.ForRead,
                        false) as BlockReference;
                    if (reference == null)
                        continue;

                    BlockTableRecord definition = transaction.GetObject(
                        reference.BlockTableRecord,
                        OpenMode.ForRead,
                        false) as BlockTableRecord;
                    if (definition == null || !definition.Name.StartsWith(
                            "CIVILSPELLAI_SECCION_",
                            System.StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    candidates++;
                    TransitionSectionParameters parameters;
                    if (service.TryReadParameters(entityId, transaction, out parameters))
                    {
                        valid++;
                        document.Editor.WriteMessage(
                            "\nPARAMETRIC_BLOCK_VALID handle={0} wallHeight={1:0.###} wallAngle={2:0.###}",
                            reference.Handle,
                            parameters.WallHeight,
                            parameters.WallAngleDegrees);
                    }
                    else
                    {
                        document.Editor.WriteMessage(
                            "\nPARAMETRIC_BLOCK_INVALID handle={0}",
                            reference.Handle);
                    }
                }
            }

            document.Editor.WriteMessage(
                "\nPARAMETRIC_BLOCK_VALIDATION candidates={0} valid={1}",
                candidates,
                valid);
        }

        [CommandMethod("AISPELLPROBESOLIDVERTICES", CommandFlags.Modal)]
        public void ProbeSolidVerticesInCurrentDrawing()
        {
            Document document = AcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null)
                return;

            using (Transaction transaction =
                document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                BlockTable blockTable = (BlockTable)transaction.GetObject(
                    document.Database.BlockTableId,
                    OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)transaction.GetObject(
                    blockTable[BlockTableRecord.ModelSpace],
                    OpenMode.ForRead);
                foreach (ObjectId entityId in modelSpace)
                {
                    Entity entity = transaction.GetObject(
                        entityId,
                        OpenMode.ForRead,
                        false) as Entity;
                    Solid3d directSolid = entity as Solid3d;
                    if (directSolid != null)
                    {
                        WriteSolidVertices(
                            document.Editor,
                            directSolid,
                            global::Autodesk.AutoCAD.Geometry.Matrix3d.Identity,
                            "MODEL");
                        continue;
                    }

                    BlockReference reference = entity as BlockReference;
                    if (reference == null)
                        continue;

                    BlockTableRecord definition = transaction.GetObject(
                        reference.BlockTableRecord,
                        OpenMode.ForRead,
                        false) as BlockTableRecord;
                    if (definition == null || !definition.Name.StartsWith(
                            "CIVILSPELLAI_SECCION_",
                            System.StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    foreach (ObjectId childId in definition)
                    {
                        Solid3d childSolid = transaction.GetObject(
                            childId,
                            OpenMode.ForRead,
                            false) as Solid3d;
                        if (childSolid != null)
                        {
                            WriteSolidVertices(
                                document.Editor,
                                childSolid,
                                reference.BlockTransform,
                                "PARAMETRIC");
                        }
                    }
                }
            }

            document.Editor.WriteMessage("\nSOLID_VERTEX_PROBE_OK");
        }

        private static void WriteSolidVertices(
            Editor editor,
            Solid3d solid,
            global::Autodesk.AutoCAD.Geometry.Matrix3d transform,
            string source)
        {
            using (var brep =
                new global::Autodesk.AutoCAD.BoundaryRepresentation.Brep(solid))
            {
                foreach (global::Autodesk.AutoCAD.BoundaryRepresentation.Vertex vertex
                    in brep.Vertices)
                {
                    global::Autodesk.AutoCAD.Geometry.Point3d point =
                        vertex.Point.TransformBy(transform);
                    editor.WriteMessage(
                        "\nSOLID_VERTEX source={0} handle={1} point=({2:0.######},{3:0.######},{4:0.######})",
                        source,
                        solid.Handle,
                        point.X,
                        point.Y,
                        point.Z);
                }
            }
        }

        private static int DetectWallInclination(
            Solid3d solid,
            double horizontalDisplacement,
            double wallHeight)
        {
            var points = new System.Collections.Generic.List<
                global::Autodesk.AutoCAD.Geometry.Point3d>();
            using (var brep =
                new global::Autodesk.AutoCAD.BoundaryRepresentation.Brep(solid))
            {
                foreach (global::Autodesk.AutoCAD.BoundaryRepresentation.Vertex vertex
                    in brep.Vertices)
                {
                    points.Add(vertex.Point);
                }
            }

            double tolerance = System.Math.Max(
                1e-8,
                System.Math.Max(horizontalDisplacement, wallHeight) * 1e-6);
            foreach (global::Autodesk.AutoCAD.Geometry.Point3d lower in points)
            {
                foreach (global::Autodesk.AutoCAD.Geometry.Point3d upper in points)
                {
                    if (System.Math.Abs(upper.X - lower.X) > tolerance ||
                        System.Math.Abs(
                            (upper.Z - lower.Z) - wallHeight) > tolerance)
                    {
                        continue;
                    }

                    double displacement = upper.Y - lower.Y;
                    if (System.Math.Abs(
                            displacement - horizontalDisplacement) <= tolerance)
                    {
                        return 1;
                    }

                    if (System.Math.Abs(
                            displacement + horizontalDisplacement) <= tolerance)
                    {
                        return -1;
                    }
                }
            }

            return 0;
        }
#endif

        private static TransitionSectionParameters ShowParameterWindow(
            Document document,
            TransitionSectionParameters initialValues,
            ref ObjectId selectedReferenceId,
            bool isEditing = false)
        {
            TransitionSectionParameters current = initialValues;
            while (true)
            {
                var window = new TransitionSectionWindow(current);
                window.Title = isEditing && !selectedReferenceId.IsNull
                    ? "Editar sección seleccionada"
                    : "Crear sección paramétrica";
                AcadApplication.ShowModalWindow(window);
                if (window.Parameters != null)
                    return window.Parameters;
                if (!window.CreateFromObjectRequested)
                    return null;
                if (window.DraftParameters != null)
                    current = window.DraftParameters;

                TransitionSectionParameters detected;
                ObjectId detectedReferenceId;
                SelectedFigureSnapshot selectedFigure;
                string error;
                if (!TryReadSectionFromObjects(
                        document, out detected, out detectedReferenceId, out selectedFigure, out error))
                {
                    if (!string.IsNullOrWhiteSpace(error))
                        document.Editor.WriteMessage("\n" + error);
                    continue;
                }
                if (selectedFigure != null)
                {
                    ShowSelectedFigureWindow(document, selectedFigure, !isEditing);
                    return null;
                }

                current = detected;
                selectedReferenceId = detectedReferenceId;
                document.Editor.WriteMessage(
                    detectedReferenceId.IsNull
                        ? "\nMedidas reconocidas desde la geometría. Se creará una nueva sección; revise los parámetros inferidos en la ventana."
                        : isEditing
                            ? "\nSección reconocida con sus parámetros guardados. EDITARSECCION editará esta sección al aceptar."
                            : "\nParámetros guardados recuperados. Se creará una nueva sección con estas medidas.");
            }
        }

        private static bool TryReadSectionFromObjects(
            Document document,
            out TransitionSectionParameters parameters,
            out ObjectId referenceId,
            out SelectedFigureSnapshot figure,
            out string error)
        {
            parameters = null;
            referenceId = ObjectId.Null;
            figure = null;
            error = null;
            var options = new PromptSelectionOptions
            {
                MessageForAdding =
                    "\nSeleccione la figura completa con una ventana, o un bloque/cota, y pulse Enter: "
            };
            PromptSelectionResult selection = document.Editor.GetSelection(options);
            if (selection.Status == PromptStatus.Cancel)
                return false;
            if (selection.Status != PromptStatus.OK || selection.Value == null)
            {
                error = "No se seleccionaron contornos válidos.";
                return false;
            }

            using (Transaction transaction =
                document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                ObjectId[] ids = selection.Value.GetObjectIds();
                var service = new ParametricSectionDrawingService();
                bool hasParametricReference = false;
                foreach (ObjectId id in ids)
                {
                    ObjectId resolved;
                    if (service.TryResolveSectionReference(id, transaction, out resolved))
                        hasParametricReference = true;
                }
                if (!hasParametricReference)
                    return new SelectedFigureDrawingService().TryRead(ids, transaction, out figure, out error);
                return new ParametricSectionObjectAnalyzer().TryAnalyze(
                    ids,
                    transaction,
                    out parameters,
                    out referenceId,
                    out error);
            }
        }

        private static void ShowSelectedFigureWindow(
            Document document, SelectedFigureSnapshot figure, bool createCopy)
        {
            while (true)
            {
                var window = new SelectedFigureWindow(figure, createCopy);
                AcadApplication.ShowModalWindow(window);
                if (window.Create3DRequested)
                {
                    Figure3DCommands.CreateFromTop(document, figure, window.MeasureEdit);
                    return;
                }
                if (window.SelectAgainRequested)
                {
                    PromptSelectionResult selection = document.Editor.GetSelection(new PromptSelectionOptions {
                        MessageForAdding = "\nSeleccione la figura completa y pulse Enter: " });
                    if (selection.Status != PromptStatus.OK || selection.Value == null)
                        continue;
                    using (Transaction transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
                    {
                        SelectedFigureSnapshot next;
                        string error;
                        if (new SelectedFigureDrawingService().TryRead(selection.Value.GetObjectIds(), transaction, out next, out error))
                            figure = next;
                        else document.Editor.WriteMessage("\n" + error);
                    }
                    continue;
                }
                if (!window.Accepted) return;
                var point = figure.Origin;
                if (window.CreateCopy)
                {
                    PromptPointResult insertion = document.Editor.GetPoint("\nIndique la esquina inferior izquierda de la copia editable: ");
                    if (insertion.Status != PromptStatus.OK) return;
                    point = insertion.Value;
                }
                try
                {
                    using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
                    {
                        new FigureMeasureDrawingService().Apply(document.Database, transaction, figure,
                            window.MeasureEdit, window.FigureWidth, window.FigureHeight, window.CreateCopy, point);
                        transaction.Commit();
                    }
                    document.Editor.Regen();
                    document.Editor.WriteMessage(window.CreateCopy
                        ? "\nCopia editable creada con la figura seleccionada. Use EDITARSECCION para ajustar sus medidas."
                        : "\nFigura seleccionada actualizada. Use U para deshacer.");
                }
                catch (System.Exception exception)
                {
                    document.Editor.WriteMessage("\nNo se pudo aplicar el cambio: " + exception.Message);
                }
                return;
            }
        }
    }
}
