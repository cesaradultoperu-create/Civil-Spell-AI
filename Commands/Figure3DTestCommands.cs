#if DEBUG
using System;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CivilSpellAI.Autodesk;
using CivilSpellAI.Geometry;
using CivilSpellAI.UI;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CivilSpellAI.Commands
{
    public sealed class Figure3DTestCommands
    {
        private static ObjectId modelId;
        private static ObjectId baselineDefinition;
        private static Figure3DParameters baseline;

        [CommandMethod("AISPELLTESTFIGURE3DLEFT", CommandFlags.Modal)]
        public void TestLeftReference()
        {
            var document = AcadApplication.DocumentManager.MdiActiveDocument;
            try
            {
                var filter = new SelectionFilter(new[] { new TypedValue(410, "Model") });
                var topSelection = document.Editor.SelectCrossingWindow(new Point3d(18.5, -2.7, 0), new Point3d(22.4, -1.0, 0), filter);
                var frontSelection = document.Editor.SelectCrossingWindow(new Point3d(18.5, -4.7, 0), new Point3d(22.4, -3.1, 0), filter);
                if (topSelection.Status != PromptStatus.OK || frontSelection.Status != PromptStatus.OK) throw new InvalidOperationException("Falta la pareja izquierda.");
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var reader = new SelectedFigureDrawingService();
                    SelectedFigureSnapshot top, front; Figure3DParameters p; string error;
                    if (!reader.TryRead(topSelection.Value.GetObjectIds(), transaction, out top, out error) ||
                        !reader.TryRead(frontSelection.Value.GetObjectIds(), transaction, out front, out error) ||
                        !new Figure3DReferenceAnalyzer().TryAnalyze(top, front, null, out p, out error)) throw new InvalidOperationException(error);
                    Close(p.Width, 1); Close(p.Depth, 1.3); Close(p.Height, 0.8); Close(p.BaseThickness, 0.15);
                    Close(p.Left.Width, 0.1); Close(p.Left.Height, 0.1); Close(p.Left.Bottom, 0.4);
                    var id = new Figure3DDrawingService().Create(document.Database, transaction, new Point3d(60, 6, 0), p);
                    CheckGeometry(id, transaction, p);
                    RenderWindow(p, top, front, ".figure3d-left.png");
                }
                document.Editor.WriteMessage("\nFIGURE3D_LEFT_REFERENCE_PREVIEW_OK");
            }
            catch (System.Exception e) { document.Editor.WriteMessage("\nFIGURE3D_FAILED " + e); }
        }

        [CommandMethod("AISPELLTESTFRONTSELECTION", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void TestFrontSelection()
        {
            try
            {
                var document = AcadApplication.DocumentManager.MdiActiveDocument;
                SelectedFigureSnapshot top, front; Figure3DParameters p; string error;
                if (!Figure3DCommands.ReadView(document, "TOP", true, out top) ||
                    !Figure3DCommands.ReadView(document, "FRONT", false, out front))
                    throw new InvalidOperationException("Faltan referencias.");
                if (top.SourceIds.Intersect(front.SourceIds).Any())
                    throw new InvalidOperationException("FRONT reutilizó objetos de TOP.");
                if (!new Figure3DReferenceAnalyzer().TryAnalyze(top, front, null, out p, out error))
                    throw new InvalidOperationException(error);
                Close(p.Height, 0.95);
                document.Editor.WriteMessage("\nFIGURE3D_FRONT_SELECTION_OK");
            }
            catch (System.Exception e) { AcadApplication.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nFIGURE3D_FAILED " + e); }
        }

        [CommandMethod("AISPELLTESTFIGURE3D", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void TestFigure3D()
        {
            try { Run(false); }
            catch (System.Exception e) { AcadApplication.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nFIGURE3D_FAILED " + e); }
        }
        [CommandMethod("AISPELLPREPAREFIGURE3D", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void PrepareFigure3D()
        {
            try { Run(true); }
            catch (System.Exception e) { AcadApplication.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nFIGURE3D_FAILED " + e); }
        }
        private static void Run(bool persist)
        {
            var document = AcadApplication.DocumentManager.MdiActiveDocument;
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK) throw new InvalidOperationException("Falta la selección TOP.");
            var frontSelection = document.Editor.SelectCrossingWindow(new Point3d(23.4, -4.7, 0), new Point3d(27.2, -3.1, 0),
                new SelectionFilter(new[] { new TypedValue(410, "Model") }));
            if (frontSelection.Status != PromptStatus.OK) throw new InvalidOperationException("Falta FRONT.");
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var reader = new SelectedFigureDrawingService();
                SelectedFigureSnapshot top, front; string error;
                if (!reader.TryRead(selection.Value.GetObjectIds(), transaction, out top, out error)) throw new InvalidOperationException(error);
                if (!reader.TryRead(frontSelection.Value.GetObjectIds(), transaction, out front, out error)) throw new InvalidOperationException(error);
                var analyzer = new Figure3DReferenceAnalyzer(); Figure3DParameters p;
                if (analyzer.TryAnalyze(top, top, null, out p, out error) || error == null || !error.Contains("FRONT incluye objetos"))
                    throw new InvalidOperationException("No se explicó la reutilización de TOP como FRONT.");
                if (!analyzer.TryAnalyze(top, front, new FigureMeasureEdit(top.Measures), out p, out error)) throw new InvalidOperationException(error);
                document.Editor.WriteMessage("\nFIGURE3D_INFERRED width={0:R} depth={1:R} height={2:R} base={3:R} left={4}/{5:R}/{6:R}/{7:R} right={8}/{9:R}/{10:R}/{11:R}",
                    p.Width, p.Depth, p.Height, p.BaseThickness, p.Left.Enabled, p.Left.Width, p.Left.Height, p.Left.Bottom,
                    p.Right.Enabled, p.Right.Width, p.Right.Height, p.Right.Bottom);
                Close(p.Width, 1); Close(p.Depth, 1.3); Close(p.Height, 0.95); Close(p.BaseThickness, 0.15);
                if (!p.Left.Enabled || !p.Right.Enabled) throw new InvalidOperationException("Ambos pasos deben estar activos con sus medidas iniciales.");
                Close(p.Left.Width, 0.1); Close(p.Left.Height, 0.1); Close(p.Left.Bottom, 0.55);
                Close(p.Right.Width, 0.1); Close(p.Right.Height, 0.1); Close(p.Right.Bottom, 0.45);
                if (analyzer.TryAnalyze(front, top, null, out baseline, out error)) throw new InvalidOperationException("Se aceptaron referencias invertidas.");
                var invalid = p.Copy(); invalid.BaseThickness = invalid.Height;
                try { invalid.Validate(); throw new InvalidOperationException("Se aceptó una base sin espacio interior."); } catch (ArgumentException) { }
                invalid = p.Copy(); invalid.Left.Bottom = 0;
                try { invalid.Validate(); throw new InvalidOperationException("Se aceptó un paso fuera del muro."); } catch (ArgumentException) { }
                document.Editor.WriteMessage("\nFIGURE3D_TOP_FRONT_REFERENCE_OK");
                var service = new Figure3DDrawingService();
                modelId = service.Create(document.Database, transaction, new Point3d(50, 6, 0), p);
                CheckGeometry(modelId, transaction, p);
                var blocks = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var owner = (BlockTableRecord)transaction.GetObject(blocks[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                ObjectId copyId = ObjectId.Null;
                using (var mapping = new IdMapping())
                {
                    document.Database.DeepCloneObjects(new ObjectIdCollection(new[] { modelId }), owner.ObjectId, mapping, false);
                    foreach (IdPair pair in mapping) if (pair.Key == modelId) copyId = pair.Value;
                }
                var copy = (BlockReference)transaction.GetObject(copyId, OpenMode.ForWrite);
                copy.Position = new Point3d(54, 6, 0); copy.Rotation = Math.PI / 2;
                var changed = p.Copy(); changed.Height += 0.2;
                changed.Right.Width = 0.20; changed.Right.Height = 0.12;
                changed.Right.Center += 0.05; changed.Right.Bottom += 0.05;
                service.Update(document.Database, transaction, copyId, changed);
                CheckGeometry(copyId, transaction, changed); CheckGeometry(modelId, transaction, p);
                Close(copy.Rotation, Math.PI / 2);
                if (top.SourceIds.Concat(front.SourceIds).Any(id => id.IsErased)) throw new InvalidOperationException("Se alteraron las referencias.");
                document.Editor.WriteMessage("\nFIGURE3D_SOLID_DIMENSIONS_COPY_OK");
                RenderWindow(p, top, front, ".figure3d.png");
                if (persist) transaction.Commit();
            }
            document.Editor.WriteMessage(persist ? "\nFIGURE3D_PREPARED" : "\nFIGURE3D_ROLLBACK_OK");
        }
        private static void RenderWindow(Figure3DParameters p, SelectedFigureSnapshot top, SelectedFigureSnapshot front, string suffix)
        {
                var window = new Figure3DWindow(p, top, front);
                var content = (System.Windows.FrameworkElement)window.Content;
                content.Measure(new System.Windows.Size(1060, 680)); content.Arrange(new System.Windows.Rect(0, 0, 1060, 680)); content.UpdateLayout();
                var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(1060, 680, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                bitmap.Render(content); var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                using (var stream = System.IO.File.Create(System.IO.Path.ChangeExtension(AcadApplication.DocumentManager.MdiActiveDocument.Database.Filename, suffix))) encoder.Save(stream);
                window.Close();
        }
        private static void CheckGeometry(ObjectId id, Transaction transaction, Figure3DParameters expected)
        {
            Figure3DParameters actual;
            if (!new Figure3DDrawingService().TryRead(id, transaction, out actual)) throw new InvalidOperationException("Faltan parámetros persistentes.");
            Close(actual.Width, expected.Width); Close(actual.Depth, expected.Depth); Close(actual.Height, expected.Height);
            CheckPassage(actual.Left, expected.Left); CheckPassage(actual.Right, expected.Right);
            var reference = (BlockReference)transaction.GetObject(id, OpenMode.ForRead);
            var definition = (BlockTableRecord)transaction.GetObject(reference.BlockTableRecord, OpenMode.ForRead);
            var solids = definition.Cast<ObjectId>().Select(child => transaction.GetObject(child, OpenMode.ForRead)).OfType<Solid3d>().ToArray();
            if (solids.Length != 1) throw new InvalidOperationException("El modelo debe contener un sólido nativo.");
            var solid = solids[0]; var bounds = solid.GeometricExtents;
            Close(bounds.MaxPoint.X - bounds.MinPoint.X, expected.Width);
            Close(bounds.MaxPoint.Y - bounds.MinPoint.Y, expected.Depth);
            Close(bounds.MaxPoint.Z - bounds.MinPoint.Z, expected.Height);
            double expectedVolume = expected.Width * expected.Depth * expected.Height - expected.InnerWidth * expected.InnerDepth * expected.ClearHeight;
            if (expected.Left.Enabled) expectedVolume -= expected.LeftWall * expected.Left.Width * expected.Left.Height;
            if (expected.Right.Enabled) expectedVolume -= expected.RightWall * expected.Right.Width * expected.Right.Height;
            Close(solid.MassProperties.Volume, expectedVolume);
            CheckPreview(expected, solid.MassProperties.Volume);
            var dimensions = definition.Cast<ObjectId>().Select(child => transaction.GetObject(child, OpenMode.ForRead)).OfType<Dimension>().ToArray();
            if (dimensions.Length != 11 + (expected.Left.Enabled ? 3 : 0) + (expected.Right.Enabled ? 3 : 0) || dimensions.Any(d => d.DimensionText != "<>")) throw new InvalidOperationException("Faltan cotas reales de planta/elevación.");
            var required = new[] { expected.Width, expected.Depth, expected.InnerWidth, expected.InnerDepth, expected.Height, expected.BaseThickness, expected.ClearHeight,
                expected.LeftWall, expected.RightWall, expected.FrontWall, expected.BackWall };
            if (expected.Left.Enabled) required = required.Concat(new[] { expected.Left.Width, expected.Left.Height, expected.Left.Bottom }).ToArray();
            if (expected.Right.Enabled) required = required.Concat(new[] { expected.Right.Width, expected.Right.Height, expected.Right.Bottom }).ToArray();
            foreach (double value in required)
                if (!dimensions.Any(d => Math.Abs(d.Measurement - value) < 1e-6)) throw new InvalidOperationException("Falta una cota 3D: " + value);
        }
        private static void CheckPreview(Figure3DParameters p, double solidVolume)
        {
            var mesh = Figure3DPreviewMesh.Build(p);
            double volume = 0, capArea = 0;
            for (int i = 0; i < mesh.TriangleIndices.Count; i += 3)
            {
                var a = mesh.Positions[mesh.TriangleIndices[i]];
                var b = mesh.Positions[mesh.TriangleIndices[i + 1]];
                var c = mesh.Positions[mesh.TriangleIndices[i + 2]];
                var normal = System.Windows.Media.Media3D.Vector3D.CrossProduct(b - a, c - a);
                double area = normal.Length / 2; normal.Normalize();
                foreach (int index in new[] { mesh.TriangleIndices[i], mesh.TriangleIndices[i + 1], mesh.TriangleIndices[i + 2] })
                    if ((mesh.Normals[index] - normal).Length > 1e-8) throw new InvalidOperationException("La vista previa suaviza una arista recta.");
                volume += System.Windows.Media.Media3D.Vector3D.DotProduct((System.Windows.Media.Media3D.Vector3D)a,
                    System.Windows.Media.Media3D.Vector3D.CrossProduct((System.Windows.Media.Media3D.Vector3D)b, (System.Windows.Media.Media3D.Vector3D)c)) / 6;
                if (Math.Abs(a.Z - p.Height) < 1e-8 && Math.Abs(b.Z - p.Height) < 1e-8 && Math.Abs(c.Z - p.Height) < 1e-8)
                    capArea += area;
            }
            Close(volume, solidVolume);
            Close(capArea, p.Width * p.Depth - p.InnerWidth * p.InnerDepth);
            Close(mesh.Bounds.SizeX, p.Width); Close(mesh.Bounds.SizeY, p.Depth); Close(mesh.Bounds.SizeZ, p.Height);
            AcadApplication.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nFIGURE3D_PREVIEW_SOLID_MATCH_OK");
        }
        [CommandMethod("AISPELLVERIFYFIGURE3D", CommandFlags.Modal)]
        public void VerifySavedFigure3D()
        {
            try
            {
                var document = AcadApplication.DocumentManager.MdiActiveDocument; int count = 0;
                using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blocks = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                    var owner = (BlockTableRecord)transaction.GetObject(blocks[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                    foreach (ObjectId id in owner) {
                        Figure3DParameters p;
                        if (new Figure3DDrawingService().TryRead(id, transaction, out p)) { CheckGeometry(id, transaction, p); count++; }
                    }
                }
                if (count != 2) throw new InvalidOperationException("No se conservaron los dos modelos de prueba.");
                document.Editor.WriteMessage("\nFIGURE3D_SAVE_REOPEN_OK");
            }
            catch (System.Exception e) { AcadApplication.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nFIGURE3D_FAILED " + e); }
        }
        [CommandMethod("AISPELLFIGURE3DUNDOSETUP", CommandFlags.Modal)]
        public void SetupUndo()
        {
            using (var transaction = AcadApplication.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartOpenCloseTransaction())
            {
                if (!new Figure3DDrawingService().TryRead(modelId, transaction, out baseline)) throw new InvalidOperationException("Falta modelo persistente.");
                baselineDefinition = ((BlockReference)transaction.GetObject(modelId, OpenMode.ForRead)).BlockTableRecord;
            }
        }
        [CommandMethod("AISPELLFIGURE3DUNDOCHANGE", CommandFlags.Modal)]
        public void ChangeForUndo()
        {
            var document = AcadApplication.DocumentManager.MdiActiveDocument;
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var changed = baseline.Copy(); changed.Height += 0.3;
                changed.Left.Width += 0.04; changed.Left.Height += 0.03;
                changed.Right.Width += 0.04; changed.Right.Height += 0.03;
                new Figure3DDrawingService().Update(document.Database, transaction, modelId, changed);
                CheckGeometry(modelId, transaction, changed); transaction.Commit();
            }
        }
        [CommandMethod("AISPELLFIGURE3DUNDOCHECK", CommandFlags.Modal)]
        public void VerifyUndo()
        {
            try
            {
                using (var transaction = AcadApplication.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    if (((BlockReference)transaction.GetObject(modelId, OpenMode.ForRead)).BlockTableRecord != baselineDefinition)
                        throw new InvalidOperationException("U no restauró la definición del bloque.");
                    CheckGeometry(modelId, transaction, baseline);
                }
                AcadApplication.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nFIGURE3D_SINGLE_UNDO_OK");
            }
            catch (System.Exception e) { AcadApplication.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nFIGURE3D_FAILED " + e); }
        }
        private static void Close(double actual, double expected)
        { if (Math.Abs(actual - expected) > 1e-6) throw new InvalidOperationException("Medida/volumen inesperado: " + actual + " frente a " + expected); }
        private static void CheckPassage(Figure3DPassage actual, Figure3DPassage expected)
        {
            if (actual.Enabled != expected.Enabled) throw new InvalidOperationException("No se conservó el estado del paso.");
            Close(actual.Width, expected.Width); Close(actual.Height, expected.Height);
            Close(actual.Center, expected.Center); Close(actual.Bottom, expected.Bottom);
        }
    }
}
#endif
