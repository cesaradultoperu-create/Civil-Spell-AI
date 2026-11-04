#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CivilSpellAI.Autodesk;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CivilSpellAI.Commands
{
    public sealed class SelectedFigureTestCommands
    {
        private static ObjectId[] undoSources;
        private static ObjectId undoReference;

        [CommandMethod("AISPELLTESTFIGUREMEASURES", CommandFlags.Modal)]
        public void TestInternalMeasures()
        {
            try { RunInternalMeasures(); }
            catch (System.Exception exception) { AcadApplication.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nFIGURE_MEASURES_FAILED " + exception); }
        }
        private void RunInternalMeasures()
        {
            TestPreviewNavigation();
            var document = AcadApplication.DocumentManager.MdiActiveDocument;
            using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blocks = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var owner = (BlockTableRecord)transaction.GetObject(blocks[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                ObjectId outer = Append(owner, transaction, Rectangle(2, 0, 6, 6));
                ObjectId inner = Append(owner, transaction, Rectangle(2.3, 0.3, 5.7, 5.7));
                var hatch = new Hatch();
                ObjectId hatchId = Append(owner, transaction, hatch);
                hatch.SetHatchPattern(HatchPatternType.PreDefined, "ANSI31");
                hatch.Associative = true;
                hatch.AppendLoop(HatchLoopTypes.Outermost, new ObjectIdCollection(new[] { outer }));
                hatch.AppendLoop(HatchLoopTypes.Default, new ObjectIdCollection(new[] { inner }));
                hatch.EvaluateHatch(true);
                document.Editor.WriteMessage("\nMEASURES_FIXTURE_HATCH_READY");
                var sources = new[] { outer, inner, hatchId,
                    Append(owner, transaction, new Line(new Point3d(0, 2.9, 0), new Point3d(2, 2.9, 0))),
                    Append(owner, transaction, new Line(new Point3d(0, 3.1, 0), new Point3d(2, 3.1, 0))),
                    Append(owner, transaction, new Line(new Point3d(6, 2.3, 0), new Point3d(10, 2.3, 0))),
                    Append(owner, transaction, new Line(new Point3d(6, 3.7, 0), new Point3d(10, 3.7, 0))),
                    Append(owner, transaction, new RotatedDimension(0, new Point3d(2.3, 5.7, 0), new Point3d(5.7, 5.7, 0),
                        new Point3d(4, 7, 0), "3.4", document.Database.Dimstyle)) };
                var service = new SelectedFigureDrawingService();
                SelectedFigureSnapshot snapshot; string error;
                if (!service.TryRead(sources, transaction, out snapshot, out error)) throw new InvalidOperationException(error);
                document.Editor.WriteMessage("\nMEASURES_FIXTURE_READ_READY count=" + snapshot.Measures.Measures.Count);
                FindMeasure(snapshot, false, 0.2);
                FindMeasure(snapshot, false, 1.4);
                FigureMeasure wall = snapshot.Measures.Measures.Single(m => !m.Horizontal &&
                    Math.Abs(snapshot.Measures.Y[m.First] - 5.7) < 1e-6 && Math.Abs(m.Original - 0.3) < 1e-6);
                FigureMeasure opening = FindMeasure(snapshot, true, 3.4);
                var edit = new FigureMeasureEdit(snapshot.Measures);
                edit.Set(wall, 0.5);
                edit.Set(opening, 4.0);
                double[] beforeInvalid = (double[])edit.Y.Clone();
                try { edit.Set(wall, 1e-12); throw new InvalidOperationException("Se aceptó una medida inválida."); }
                catch (ArgumentException) { }
                if (!beforeInvalid.SequenceEqual(edit.Y)) throw new InvalidOperationException("Una medida inválida alteró los bordes.");
                ObjectId copied = new FigureMeasureDrawingService().Apply(document.Database, transaction, snapshot, edit, 10.6, 6.2, true, new Point3d(30, 0, 0));
                document.Editor.WriteMessage("\nMEASURES_FIXTURE_COPY_READY");
                SelectedFigureSnapshot changed;
                if (!service.TryRead(new[] { copied }, transaction, out changed, out error)) throw new InvalidOperationException(error);
                FindMeasure(changed, true, 4.0);
                FindMeasure(changed, false, 0.5);
                FindMeasure(changed, true, 0.3);
                FindMeasure(changed, true, 2.0);
                if (sources.Any(id => id.IsErased) || Math.Abs(changed.Width - 10.6) > 1e-6 || Math.Abs(changed.Height - 6.2) > 1e-6)
                    throw new InvalidOperationException("La copia alteró el original o las medidas exteriores.");
                var reference = (BlockReference)transaction.GetObject(copied, OpenMode.ForRead);
                var definition = (BlockTableRecord)transaction.GetObject(reference.BlockTableRecord, OpenMode.ForRead);
                var updatedHatch = definition.Cast<ObjectId>().Where(id => !id.IsErased).Select(id => transaction.GetObject(id, OpenMode.ForRead)).OfType<Hatch>().Single();
                double[] areas = Enumerable.Range(0, updatedHatch.NumberOfLoops).Select(i => HatchLoopArea(updatedHatch.GetLoopAt(i))).OrderByDescending(a => a).ToArray();
                if (updatedHatch.NumberOfLoops != 2 || Math.Abs(areas[0] - areas[1] - (4.6 * 6.2 - 4 * 5.4)) > 1e-5)
                    throw new InvalidOperationException("El hatch no sigue el hueco interior y el muro.");
                var dim = (Dimension)transaction.GetObject(SelectedFigureDrawingService.ReadDimensionIds(reference, transaction).Single(), OpenMode.ForRead);
                if (Math.Abs(dim.Measurement - 4) > 1e-6 || dim.DimensionText != "<>")
                    throw new InvalidOperationException("La cota no sigue la medida interior.");
                // Edit a copy twice; a definition shared by the original must remain unchanged.
                var secondEdit = new FigureMeasureEdit(changed.Measures);
                secondEdit.Set(FindMeasure(changed, true, 4), 4.4);
                ObjectId second = new FigureMeasureDrawingService().Apply(document.Database, transaction, changed, secondEdit, 11, 6.2, true, new Point3d(60, 0, 0));
                SelectedFigureSnapshot secondSnapshot;
                if (!service.TryRead(new[] { second }, transaction, out secondSnapshot, out error)) throw new InvalidOperationException(error);
                FindMeasure(secondSnapshot, true, 4.4);
                if (!service.TryRead(new[] { copied }, transaction, out changed, out error)) throw new InvalidOperationException(error);
                FindMeasure(changed, true, 4);
                // A rotated figure keeps measures in its local drawing axes.
                var rotated = (BlockReference)transaction.GetObject(second, OpenMode.ForWrite);
                rotated.Rotation = Math.PI / 2;
                if (!service.TryRead(new[] { second }, transaction, out secondSnapshot, out error)) throw new InvalidOperationException(error);
                var rotatedEdit = new FigureMeasureEdit(secondSnapshot.Measures);
                rotatedEdit.Set(FindMeasure(secondSnapshot, true, 4.4), 4.8);
                new FigureMeasureDrawingService().Apply(document.Database, transaction, secondSnapshot, rotatedEdit, 11.4, 6.2, false, Point3d.Origin);
                if (!service.TryRead(new[] { second }, transaction, out secondSnapshot, out error)) throw new InvalidOperationException(error);
                FindMeasure(secondSnapshot, true, 4.8);
                // In-place conversion is still undone by the transaction if anything fails.
                new FigureMeasureDrawingService().Apply(document.Database, transaction, snapshot, edit, 10.6, 6.2, false, Point3d.Origin);
                if (sources.Any(id => !id.IsErased)) throw new InvalidOperationException("No se aplicó el cambio al original.");
            }
            document.Editor.WriteMessage("\nFIGURE_INTERNAL_MEASURES_OK");
        }

        private static void TestPreviewNavigation()
        {
            var viewport = new CivilSpellAI.UI.FigurePreviewViewport();
            var size = new System.Windows.Size(740, 560);
            var point = new System.Windows.Point(125, 210);
            var delta = new System.Windows.Vector(36, -24);
            viewport.Pan(delta);
            var anchor = viewport.Transform(point, size);
            viewport.ZoomAt(anchor, size, 2);
            if ((viewport.Transform(point, size) - anchor).Length > 1e-8)
                throw new InvalidOperationException("El zoom desplazó el punto bajo el cursor.");
            viewport.Pan(delta);
            if ((viewport.Transform(point, size) - anchor - delta).Length > 1e-8)
                throw new InvalidOperationException("El desplazamiento no conserva la escala.");
            viewport.ZoomAt(anchor, size, 1e6);
            if (viewport.Zoom != 16) throw new InvalidOperationException("El zoom excedió su límite superior.");
            viewport.ZoomAt(anchor, size, 1e-6);
            if (viewport.Zoom != 0.5) throw new InvalidOperationException("El zoom excedió su límite inferior.");
            viewport.Reset();
            if ((viewport.Transform(point, size) - point).Length > 1e-8 || viewport.Zoom != 1)
                throw new InvalidOperationException("Encuadrar no restauró la vista completa.");
            AcadApplication.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nFIGURE_PREVIEW_NAVIGATION_OK");
        }

        [CommandMethod("AISPELLTESTFIGUREMEASURESFILE", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void TestDrawingInternalMeasures()
        {
            try { RunDrawingInternalMeasures(); }
            catch (System.Exception exception) { AcadApplication.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nFIGURE_REAL_MEASURES_FAILED " + exception); }
        }
        private void RunDrawingInternalMeasures()
        {
            var document = AcadApplication.DocumentManager.MdiActiveDocument;
            var selection = document.Editor.SelectImplied();
            if (selection.Status != global::Autodesk.AutoCAD.EditorInput.PromptStatus.OK) throw new InvalidOperationException("Seleccione el detalle original.");
            using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
            {
                SelectedFigureSnapshot snapshot; string error;
                var service = new SelectedFigureDrawingService();
                if (!service.TryRead(selection.Value.GetObjectIds(), transaction, out snapshot, out error)) throw new InvalidOperationException(error);
                foreach (FigureMeasure measure in snapshot.Measures.Measures)
                    document.Editor.WriteMessage("\nMEASURE {0} {1} value={2:0.######} anchor={3:0.######}",
                        measure.Letter, measure.Horizontal ? "H" : "V", measure.Original, measure.Anchor);
                System.IO.File.WriteAllLines(System.IO.Path.ChangeExtension(document.Database.Filename, ".outlines.csv"), snapshot.Outlines.SelectMany((line, index) => line.Select(point => {
                    Point2d local = FigureMeasureLayout.ToLocal(snapshot, point);
                    return index + "," + local.X.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," + local.Y.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                })));
                FigureMeasure chosen = snapshot.Measures.Measures.Where(m => m.Horizontal && m.Original > 0.3 && m.Original < snapshot.Width * 0.6)
                    .OrderByDescending(m => m.Original).First();
                var edit = new FigureMeasureEdit(snapshot.Measures);
                edit.Set(chosen, chosen.Original + 0.2);
                ObjectId copy = new FigureMeasureDrawingService().Apply(document.Database, transaction, snapshot, edit,
                    snapshot.Width + 0.2, snapshot.Height, true, new Point3d(50, 0, 0));
                SelectedFigureSnapshot changed;
                if (!service.TryRead(new[] { copy }, transaction, out changed, out error)) throw new InvalidOperationException(error);
                FindMeasure(changed, true, chosen.Original + 0.2);
                if (snapshot.SourceIds.Any(id => id.IsErased)) throw new InvalidOperationException("Se alteró el original.");
                var reference = (BlockReference)transaction.GetObject(copy, OpenMode.ForRead);
                var definition = (BlockTableRecord)transaction.GetObject(reference.BlockTableRecord, OpenMode.ForRead);
                int hatchCount = definition.Cast<ObjectId>().Where(id => !id.IsErased).Count(id => transaction.GetObject(id, OpenMode.ForRead) is Hatch);
                document.Editor.WriteMessage("\nFIGURE_REAL_MEASURES_COPY letters={0} hatches={1} width={2:0.######} height={3:0.######}",
                    snapshot.Measures.Measures.Count, hatchCount, changed.Width, changed.Height);
                // Render the actual editor content without opening a modal window in Core Console.
                var window = new CivilSpellAI.UI.SelectedFigureWindow(snapshot, true);
                var content = (System.Windows.FrameworkElement)window.Content;
                content.Measure(new System.Windows.Size(1080, 660));
                content.Arrange(new System.Windows.Rect(0, 0, 1080, 660));
                content.UpdateLayout();
                var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(1080, 660, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                bitmap.Render(content);
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                string path = System.IO.Path.ChangeExtension(document.Database.Filename, ".measures.png");
                using (var stream = System.IO.File.Create(path)) encoder.Save(stream);
                var direct = new CivilSpellAI.UI.SelectedFigurePreview { Snapshot = snapshot,
                    MeasureEdit = new FigureMeasureEdit(snapshot.Measures), SelectedMeasure = FindMeasure(snapshot, true, 0.7) };
                direct.Measure(new System.Windows.Size(740, 560));
                direct.Arrange(new System.Windows.Rect(0, 0, 740, 560));
                var directBitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(740, 560, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                directBitmap.Render(direct);
                var directEncoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                directEncoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(directBitmap));
                using (var stream = System.IO.File.Create(System.IO.Path.ChangeExtension(path, ".detail.png"))) directEncoder.Save(stream);
                direct.ZoomAt(new System.Windows.Point(370, 280), 2);
                direct.Arrange(new System.Windows.Rect(0, 0, 740, 560));
                direct.UpdateLayout();
                var zoomBitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(740, 560, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                zoomBitmap.Render(direct);
                var zoomEncoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                zoomEncoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(zoomBitmap));
                using (var stream = System.IO.File.Create(System.IO.Path.ChangeExtension(path, ".zoom.png"))) zoomEncoder.Save(stream);
                direct.ResetView();
                window.Close();
                document.Editor.WriteMessage("\nFIGURE_MEASURES_PREVIEW " + path);
            }
            document.Editor.WriteMessage("\nFIGURE_REAL_INTERNAL_MEASURES_OK");
        }

        private static Polyline Rectangle(double left, double bottom, double right, double top)
        {
            var result = new Polyline { Closed = true };
            result.AddVertexAt(0, new Point2d(left, bottom), 0, 0, 0);
            result.AddVertexAt(1, new Point2d(right, bottom), 0, 0, 0);
            result.AddVertexAt(2, new Point2d(right, top), 0, 0, 0);
            result.AddVertexAt(3, new Point2d(left, top), 0, 0, 0);
            return result;
        }
        private static double HatchLoopArea(HatchLoop loop)
        {
            var points = loop.Polyline.Cast<BulgeVertex>().Select(v => v.Vertex).ToArray();
            double twiceArea = 0;
            for (int i = 0; i < points.Length; i++)
            {
                Point2d a = points[i], b = points[(i + 1) % points.Length];
                twiceArea += a.X * b.Y - b.X * a.Y;
            }
            return Math.Abs(twiceArea) / 2;
        }
        private static FigureMeasure FindMeasure(SelectedFigureSnapshot snapshot, bool horizontal, double value)
        {
            FigureMeasure result = snapshot.Measures.Measures.FirstOrDefault(m => m.Horizontal == horizontal && Math.Abs(m.Original - value) < 1e-5);
            if (result == null) throw new InvalidOperationException("No se detectó medida " + (horizontal ? "horizontal " : "vertical ") + value);
            return result;
        }

        [CommandMethod("AISPELLFIGUREUNDOSETUP", CommandFlags.Modal)]
        public void PrepareUndoFixture()
        {
            var document = AcadApplication.DocumentManager.MdiActiveDocument;
            using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blocks = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var owner = (BlockTableRecord)transaction.GetObject(blocks[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var boundary = new Polyline { Closed = true };
                boundary.AddVertexAt(0, new Point2d(100, 100), 0, 0, 0);
                boundary.AddVertexAt(1, new Point2d(104, 100), 0, 0, 0);
                boundary.AddVertexAt(2, new Point2d(104, 103), 0, 0, 0);
                boundary.AddVertexAt(3, new Point2d(100, 103), 0, 0, 0);
                undoSources = new[] { Append(owner, transaction, boundary),
                    Append(owner, transaction, new Line(new Point3d(100, 101, 0), new Point3d(104, 101, 0))) };
                transaction.Commit();
            }
            document.Editor.WriteMessage("\nFIGURE_UNDO_FIXTURE_READY");
        }

        [CommandMethod("AISPELLFIGUREUNDOCHANGE", CommandFlags.Modal)]
        public void ChangeUndoFixture()
        {
            var document = AcadApplication.DocumentManager.MdiActiveDocument;
            using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
            {
                SelectedFigureSnapshot snapshot;
                string error;
                var service = new SelectedFigureDrawingService();
                if (!service.TryRead(undoSources, transaction, out snapshot, out error)) throw new InvalidOperationException(error);
                var edit = new FigureMeasureEdit(snapshot.Measures);
                edit.Set(FindMeasure(snapshot, true, 4), 8);
                edit.Set(FindMeasure(snapshot, false, 3), 6);
                undoReference = new FigureMeasureDrawingService().Apply(document.Database, transaction, snapshot, edit, 8, 6, false, Point3d.Origin);
                if (undoSources.Any(id => !id.IsErased)) throw new InvalidOperationException("No se aplicó el cambio.");
                transaction.Commit();
            }
            document.Editor.WriteMessage("\nFIGURE_UNDO_CHANGE_READY");
        }

        [CommandMethod("AISPELLFIGUREUNDOCHECK", CommandFlags.Modal)]
        public void CheckUndoFixture()
        {
            var document = AcadApplication.DocumentManager.MdiActiveDocument;
            if (undoSources.Any(id => id.IsErased) || !undoReference.IsErased)
                throw new InvalidOperationException("U no restauró todos los objetos originales.");
            using (Transaction transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                SelectedFigureSnapshot snapshot;
                string error;
                if (!new SelectedFigureDrawingService().TryRead(undoSources, transaction, out snapshot, out error) ||
                    Math.Abs(snapshot.Width - 4) > 1e-8 || Math.Abs(snapshot.Height - 3) > 1e-8)
                    throw new InvalidOperationException("U no restauró las medidas originales.");
            }
            document.Editor.WriteMessage("\nSELECTED_FIGURE_SINGLE_UNDO_OK");
        }

        [CommandMethod("AISPELLPREPARESELECTEDFIGURECOPY", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void PrepareSelectedFigureCopy()
        {
            var document = AcadApplication.DocumentManager.MdiActiveDocument;
            var selection = document.Editor.SelectImplied();
            if (selection.Status != global::Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                throw new InvalidOperationException("Seleccione la figura original.");
            ObjectId[] ids = selection.Value.GetObjectIds();
            using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
            {
                var service = new SelectedFigureDrawingService();
                SelectedFigureSnapshot snapshot;
                string error;
                if (!service.TryRead(ids, transaction, out snapshot, out error)) throw new InvalidOperationException(error);
                ObjectId id = service.Apply(document.Database, transaction, snapshot,
                    snapshot.Width, snapshot.Height, true, new Point3d(50, 0, 0));
                var reference = (BlockReference)transaction.GetObject(id, OpenMode.ForRead);
                var owner = (BlockTableRecord)transaction.GetObject(reference.OwnerId, OpenMode.ForWrite);
                Append(owner, transaction, new DBText {
                    Position = new Point3d(50, snapshot.Height + 1, 0), Height = 0.3,
                    TextString = "FIGURA ORIGINAL - EDITARSECCION" });
                SelectedFigureSnapshot verified;
                if (!service.TryRead(new[] { id }, transaction, out verified, out error)) throw new InvalidOperationException(error);
                transaction.Commit();
                document.Editor.WriteMessage("\nSELECTED_FIGURE_PREPARED handle={0} width={1:0.######} height={2:0.######}",
                    id.Handle, snapshot.Width, snapshot.Height);
            }
        }

        [CommandMethod("AISPELLPREPAREMEASUREDCOPY", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void PrepareMeasuredCopy()
        {
            var document = AcadApplication.DocumentManager.MdiActiveDocument;
            var selection = document.Editor.SelectImplied();
            if (selection.Status != global::Autodesk.AutoCAD.EditorInput.PromptStatus.OK) throw new InvalidOperationException("Seleccione el detalle original.");
            using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
            {
                var service = new SelectedFigureDrawingService();
                SelectedFigureSnapshot snapshot; string error;
                if (!service.TryRead(selection.Value.GetObjectIds(), transaction, out snapshot, out error)) throw new InvalidOperationException(error);
                var edit = new FigureMeasureEdit(snapshot.Measures);
                edit.Set(FindMeasure(snapshot, true, 0.7), 0.9);
                ObjectId id = new FigureMeasureDrawingService().Apply(document.Database, transaction, snapshot, edit,
                    snapshot.Width + 0.2, snapshot.Height, true, new Point3d(56, 0, 0));
                var reference = (BlockReference)transaction.GetObject(id, OpenMode.ForRead);
                var owner = (BlockTableRecord)transaction.GetObject(reference.OwnerId, OpenMode.ForWrite);
                Append(owner, transaction, new DBText { Position = new Point3d(56, snapshot.Height + 1, 0), Height = 0.25,
                    TextString = "HUECO INTERIOR: 0.70 -> 0.90" });
                transaction.Commit();
                document.Editor.WriteMessage("\nMEASURED_FIGURE_PREPARED handle=" + id.Handle);
            }
        }

        [CommandMethod("AISPELLVERIFYMEASUREDCOPY", CommandFlags.Modal)]
        public void VerifyMeasuredCopy()
        {
            var document = AcadApplication.DocumentManager.MdiActiveDocument;
            using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blocks = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var model = (BlockTableRecord)transaction.GetObject(blocks[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                var service = new SelectedFigureDrawingService();
                int figures = 0;
                foreach (ObjectId id in model)
                {
                    if (!service.IsFigureReference(id, transaction)) continue;
                    SelectedFigureSnapshot snapshot; string error;
                    if (!service.TryRead(new[] { id }, transaction, out snapshot, out error)) throw new InvalidOperationException(error);
                    bool changed = snapshot.Origin.X > 55;
                    FindMeasure(snapshot, true, changed ? 0.9 : 0.7);
                    FindMeasure(snapshot, true, 0.15);
                    FindMeasure(snapshot, false, 0.15);
                    var reference = (BlockReference)transaction.GetObject(id, OpenMode.ForRead);
                    var definition = (BlockTableRecord)transaction.GetObject(reference.BlockTableRecord, OpenMode.ForRead);
                    var hatches = definition.Cast<ObjectId>().Where(child => !child.IsErased).Select(child => transaction.GetObject(child, OpenMode.ForRead)).OfType<Hatch>().ToArray();
                    if (hatches.Length != 9 || hatches.Any(h => h.NumberOfLoops == 0)) throw new InvalidOperationException("El hatch no sobrevivió al guardado.");
                    // Re-edit after SAVE/reopen to exercise native hatch data lifetime and persistent metadata.
                    var edit = new FigureMeasureEdit(snapshot.Measures);
                    edit.Set(FindMeasure(snapshot, true, changed ? 0.9 : 0.7), changed ? 1.0 : 0.8);
                    service.TryRead(new[] { new FigureMeasureDrawingService().Apply(document.Database, transaction, snapshot, edit,
                        snapshot.Width + 0.1, snapshot.Height, false, Point3d.Origin) }, transaction, out snapshot, out error);
                    if (snapshot == null) throw new InvalidOperationException(error);
                    FindMeasure(snapshot, true, changed ? 1.0 : 0.8);
                    figures++;
                }
                if (figures != 2) throw new InvalidOperationException("No se conservaron los dos ejemplos editables.");
            }
            document.Editor.WriteMessage("\nFIGURE_MEASURES_SAVE_REOPEN_OK");
        }

        [CommandMethod("AISPELLTESTSELECTEDFIGUREFILE", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void TestSelectedFigureFromDrawing()
        {
            var document = AcadApplication.DocumentManager.MdiActiveDocument;
            var selection = document.Editor.SelectImplied();
            if (selection.Status != global::Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                throw new InvalidOperationException("Seleccione la figura original antes de ejecutar la prueba.");
            ObjectId[] sourceIds = selection.Value.GetObjectIds();
            using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
            {
                SelectedFigureSnapshot snapshot;
                string error;
                var service = new SelectedFigureDrawingService();
                if (!service.TryRead(sourceIds, transaction, out snapshot, out error))
                    throw new InvalidOperationException(error);
                document.Editor.WriteMessage("\nSELECTED_FIGURE_FILE_READ count={0} width={1:0.######} height={2:0.######} outlines={3}",
                    snapshot.SourceIds.Length, snapshot.Width, snapshot.Height, snapshot.Outlines.Count);
                ObjectId id = service.Apply(document.Database, transaction, snapshot,
                    snapshot.Width, snapshot.Height, true,
                    new Point3d(snapshot.Origin.X + snapshot.Width + 10, snapshot.Origin.Y, snapshot.Origin.Z));
                SelectedFigureSnapshot copy;
                if (!service.TryRead(new[] { id }, transaction, out copy, out error) ||
                    Math.Abs(copy.Width - snapshot.Width) > 1e-7 || Math.Abs(copy.Height - snapshot.Height) > 1e-7 ||
                    copy.Outlines.Count < snapshot.Outlines.Count || snapshot.SourceIds.Any(source => source.IsErased))
                    throw new InvalidOperationException("La copia no conserva la figura original: " + error);
                string imagePath = System.IO.Path.ChangeExtension(document.Database.Filename, ".preview.png");
                var preview = new CivilSpellAI.UI.SelectedFigurePreview { Snapshot = snapshot };
                preview.Measure(new System.Windows.Size(720, 520));
                preview.Arrange(new System.Windows.Rect(0, 0, 720, 520));
                var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(720, 520, 96, 96,
                    System.Windows.Media.PixelFormats.Pbgra32);
                bitmap.Render(preview);
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                using (var stream = System.IO.File.Create(imagePath)) encoder.Save(stream);
                document.Editor.WriteMessage("\nSELECTED_FIGURE_FILE_PREVIEW path={0}", imagePath);
            }
            document.Editor.WriteMessage("\nSELECTED_FIGURE_FILE_COPY_OK");
        }

        [CommandMethod("AISPELLTESTSELECTEDFIGURE", CommandFlags.Modal)]
        public void TestSelectedFigure()
        {
            try { RunSelectedFigureTest(); }
            catch (System.Exception exception)
            {
                AcadApplication.DocumentManager.MdiActiveDocument.Editor.WriteMessage(
                    "\nSELECTED_FIGURE_TEST_FAILED " + exception.ToString());
            }
        }

        private void RunSelectedFigureTest()
        {
            var document = AcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blocks = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var owner = (BlockTableRecord)transaction.GetObject(blocks[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var ids = new List<ObjectId>();
                var boundary = new Polyline { Closed = true };
                boundary.AddVertexAt(0, new Point2d(10, 10), 0, 0, 0);
                boundary.AddVertexAt(1, new Point2d(14, 10), 0, 0, 0);
                boundary.AddVertexAt(2, new Point2d(14, 13), 0, 0, 0);
                boundary.AddVertexAt(3, new Point2d(10, 13), 0, 0, 0);
                ids.Add(Append(owner, transaction, boundary));
                ids.Add(Append(owner, transaction, new Line(new Point3d(10, 11, 0), new Point3d(14, 11, 0))));
                ids.Add(Append(owner, transaction, new Circle(new Point3d(12, 12, 0), Vector3d.ZAxis, 0.5)));
                var hatch = new Hatch();
                ids.Add(Append(owner, transaction, hatch));
                hatch.SetHatchPattern(HatchPatternType.PreDefined, "ANSI31");
                hatch.Associative = true;
                hatch.AppendLoop(HatchLoopTypes.External, new ObjectIdCollection(new[] { boundary.ObjectId }));
                hatch.EvaluateHatch(true);
                ids.Add(Append(owner, transaction, new RotatedDimension(0, new Point3d(10, 10, 0),
                    new Point3d(14, 10, 0), new Point3d(12, 8, 0), "", document.Database.Dimstyle)));
                ids.Add(Append(owner, transaction, new RotatedDimension(Math.PI / 2, new Point3d(10, 10, 0),
                    new Point3d(10, 13, 0), new Point3d(8, 11, 0), "", document.Database.Dimstyle)));
                foreach (ObjectId id in ids)
                {
                    Dimension dimension = transaction.GetObject(id, OpenMode.ForRead) as Dimension;
                    if (dimension != null) dimension.RecomputeDimensionBlock(true);
                }
                document.Editor.WriteMessage("\nFIGURE_TEST_FIXTURE_READY");
                var service = new SelectedFigureDrawingService();
                SelectedFigureSnapshot snapshot;
                string error;
                if (!service.TryRead(ids, transaction, out snapshot, out error) ||
                    Math.Abs(snapshot.Width - 4) > 1e-8 || Math.Abs(snapshot.Height - 3) > 1e-8 || snapshot.Outlines.Count < 4)
                    throw new InvalidOperationException("No se reconoció la figura rectangular real: " + error);
                document.Editor.WriteMessage("\nFIGURE_TEST_SNAPSHOT_READY");
                ObjectId copyId = service.Apply(document.Database, transaction, snapshot, 8, 3, true, new Point3d(30, 0, 0));
                document.Editor.WriteMessage("\nFIGURE_TEST_COPY_READY");
                if (ids.Any(id => id.IsErased) || Math.Abs(boundary.GeometricExtents.MinPoint.X - 10) > 1e-8 ||
                    Math.Abs(hatch.GeometricExtents.MinPoint.X - 10) > 1e-8)
                    throw new InvalidOperationException("Crear una copia alteró la fuente.");
                SelectedFigureSnapshot copy;
                if (!service.TryRead(new[] { copyId }, transaction, out copy, out error) || copy.Outlines.Count < snapshot.Outlines.Count ||
                    Math.Abs(copy.Width - 8) > 1e-8 || Math.Abs(copy.Height - 3) > 1e-8)
                    throw new InvalidOperationException("No se recuperaron las medidas de la copia: " + error);
                var reference = (BlockReference)transaction.GetObject(copyId, OpenMode.ForRead);
                var definition = (BlockTableRecord)transaction.GetObject(reference.BlockTableRecord, OpenMode.ForRead);
                int circles = 0;
                int hatches = 0;
                foreach (ObjectId id in definition)
                {
                    Entity entity = transaction.GetObject(id, OpenMode.ForRead) as Entity;
                    if (entity is Circle) circles++;
                    if (entity is Hatch)
                    {
                        var copiedHatch = (Hatch)entity;
                        if (!copiedHatch.Associative || copiedHatch.GetAssociatedObjectIds().Contains(boundary.ObjectId))
                            throw new InvalidOperationException("El hatch de la copia conserva una asociación con la fuente.");
                        hatches++;
                    }
                }
                if (circles != 1 || hatches != 1)
                    throw new InvalidOperationException("La copia perdió la geometría original.");
                var dimensions = SelectedFigureDrawingService.ReadDimensionIds(reference, transaction).ToArray();
                AssertDimensions(dimensions, transaction, 8, 3);
                document.Editor.WriteMessage("\nFIGURE_TEST_DIMENSIONS_READY");
                if (!service.TryRead(new[] { dimensions[0] }, transaction, out copy, out error) || copy.ReferenceId != copyId)
                    throw new InvalidOperationException("No se reconoció la figura desde una cota.");
                service.Apply(document.Database, transaction, copy, 6, 6, false, Point3d.Origin);
                AssertDimensions(dimensions, transaction, 6, 6);
                if (!service.TryRead(new[] { copyId }, transaction, out copy, out error))
                    throw new InvalidOperationException(error);
                ObjectId secondCopy = service.Apply(document.Database, transaction, copy, 9, 6, true, new Point3d(50, 0, 0));
                if (!service.TryRead(new[] { secondCopy }, transaction, out copy, out error) || copy.ReferenceId != secondCopy)
                    throw new InvalidOperationException("No se pudo copiar una figura ya editable.");
                AssertDimensions(SelectedFigureDrawingService.ReadDimensionIds(
                    (BlockReference)transaction.GetObject(secondCopy, OpenMode.ForRead), transaction).ToArray(), transaction, 9, 6);
                AssertDimensions(dimensions, transaction, 6, 6);
                SelectedFigureSnapshot lineSnapshot;
                if (!service.TryRead(new[] { ids[1] }, transaction, out lineSnapshot, out error))
                    throw new InvalidOperationException("No se reconoció la línea aislada.");
                ObjectId lineCopy = service.Apply(document.Database, transaction, lineSnapshot, 8, 0, true, new Point3d(70, 0, 0));
                if (!service.TryRead(new[] { lineCopy }, transaction, out lineSnapshot, out error) ||
                    Math.Abs(lineSnapshot.Width - 8) > 1e-8 ||
                    SelectedFigureDrawingService.ReadDimensionIds((BlockReference)transaction.GetObject(lineCopy, OpenMode.ForRead), transaction).Any())
                    throw new InvalidOperationException("La figura sin cotas no conserva sus medidas.");
                ObjectId replacement = service.Apply(document.Database, transaction, snapshot, 4, 3, false, Point3d.Origin);
                if (ids.Any(id => !id.IsErased) || !service.IsFigureReference(replacement, transaction))
                    throw new InvalidOperationException("La edición de la figura original no se aplicó al conjunto.");
                try
                {
                    service.Apply(document.Database, transaction, copy, 0, 6, false, Point3d.Origin);
                    throw new InvalidOperationException("Se aceptó un ancho nulo.");
                }
                catch (ArgumentException) { }
                // Nothing is committed: this test leaves the source DWG unchanged.
            }
            document.Editor.WriteMessage("\nSELECTED_FIGURE_GEOMETRY_DIMENSIONS_OK");
        }

        private static ObjectId Append(BlockTableRecord owner, Transaction transaction, Entity entity)
        {
            ObjectId id = owner.AppendEntity(entity);
            transaction.AddNewlyCreatedDBObject(entity, true);
            return id;
        }
        private static void AssertDimensions(ObjectId[] ids, Transaction transaction, double width, double height)
        {
            double[] actual = ids.Select(id => ((Dimension)transaction.GetObject(id, OpenMode.ForRead)).Measurement).OrderBy(value => value).ToArray();
            double[] expected = new[] { width, height }.OrderBy(value => value).ToArray();
            if (actual.Length != 2 || Math.Abs(actual[0] - expected[0]) > 1e-6 || Math.Abs(actual[1] - expected[1]) > 1e-6)
                throw new InvalidOperationException("Las cotas no siguen las medidas de la figura: " + string.Join(",", actual));
        }
    }
}
#endif
