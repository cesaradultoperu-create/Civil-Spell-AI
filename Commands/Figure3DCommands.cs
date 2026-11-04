using System;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
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
    public sealed class Figure3DCommands
    {
        [CommandMethod("CREARFIGURA3D", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void CreateFigure3D()
        {
            Document document = AcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            SelectedFigureSnapshot top;
            if (!ReadView(document, "TOP: seleccione la planta completa de la figura, incluidos sus contornos y hatches, y pulse Enter", true, out top)) return;
            CreateFromTop(document, top, new FigureMeasureEdit(top.Measures));
        }
        [CommandMethod("EDITARFIGURA3D", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void EditFigure3D()
        {
            Document document = AcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK)
                selection = document.Editor.GetSelection(new PromptSelectionOptions { MessageForAdding = "\nSeleccione una figura 3D editable y pulse Enter: " });
            if (selection.Status != PromptStatus.OK || selection.Value == null) return;
            if (!TryEditSelection(document, selection.Value.GetObjectIds()))
                document.Editor.WriteMessage("\nSeleccione un bloque 3D creado desde TOP y FRONT.");
        }
        internal static void CreateFromTop(Document document, SelectedFigureSnapshot top, FigureMeasureEdit edit)
        {
            var modeOptions = new PromptKeywordOptions("\nReconstrucción [Referencias/Componente/Caja] <Referencias>: ") { AllowNone = true };
            modeOptions.Keywords.Add("Referencias"); modeOptions.Keywords.Add("Componente"); modeOptions.Keywords.Add("Caja");
            modeOptions.Keywords.Default = "Referencias";
            var mode = document.Editor.GetKeywords(modeOptions);
            if (mode.Status == PromptStatus.Cancel) return;
            if (mode.Status != PromptStatus.OK && mode.Status != PromptStatus.None) return;
            if (mode.StringResult != "Caja")
            {
                if (edit != null && edit.Changed) { document.Editor.WriteMessage("\nConfirme primero las medidas de la referencia 2D y vuelva a seleccionar TOP y FRONT para reconstruirla."); return; }
                ReferenceAssembly3DCommands.CreateFromTop(document, top, mode.StringResult != "Componente"); return;
            }
            SelectedFigureSnapshot front;
            if (!ReadView(document, "FRONT: seleccione la elevación completa de la misma caja y pulse Enter", false, out front)) return;
            Figure3DParameters parameters; string error;
            if (!new Figure3DReferenceAnalyzer().TryAnalyze(top, front, edit, out parameters, out error))
            { document.Editor.WriteMessage("\n" + error); return; }
            var topPreview = new SelectedFigureSnapshot { Origin = top.Origin, AxisAngle = top.AxisAngle,
                Width = top.Width, Height = top.Height, Measures = top.Measures };
            topPreview.Outlines.AddRange(top.Outlines.Select(line => line.Select(p => {
                Point2d local = FigureMeasureLayout.ToLocal(top, p);
                return FigureMeasureLayout.ToWorld(top, edit.Map(local));
            }).ToArray()));
            // Reference previews show contours, without recomputing inferred letters after stretching.
            topPreview.Measures = null;
            var window = new Figure3DWindow(parameters, topPreview, front);
            AcadApplication.ShowModalWindow(window);
            if (window.Parameters == null || !IsActive(document)) return;
            var insertion = document.Editor.GetPoint("\nIndique la esquina inferior frontal del modelo 3D: ");
            if (insertion.Status != PromptStatus.OK || !IsActive(document)) return;
            try
            {
                using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
                {
                    if (top.SourceIds.Concat(front.SourceIds).Any(id => !id.IsValid || id.IsErased))
                        throw new InvalidOperationException("Una referencia TOP o FRONT ya no está disponible. Vuelva a seleccionarla.");
                    new Figure3DDrawingService().Create(document.Database, transaction,
                        insertion.Value.TransformBy(document.Editor.CurrentUserCoordinateSystem), window.Parameters);
                    transaction.Commit();
                }
                document.Editor.Regen();
                document.Editor.WriteMessage("\nModelo 3D creado con cotas de planta y elevación. Use EDITARSECCION o EDITARFIGURA3D para modificarlo; U deshace la creación.");
            }
            catch (System.Exception exception) { document.Editor.WriteMessage("\nNo se pudo crear la figura 3D: " + exception.Message); }
        }
        internal static bool TryEditSelection(Document document, ObjectId[] ids)
        {
            if (ReferenceAssembly3DCommands.TryEdit(document, ids)) return true;
            if (ids.Length != 1) return false;
            var service = new Figure3DDrawingService(); Figure3DParameters original;
            Matrix3d pose;
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                if (!service.TryRead(ids[0], transaction, out original)) return false;
                var reference = (BlockReference)transaction.GetObject(ids[0], OpenMode.ForRead);
                if (Math.Abs(reference.ScaleFactors.X - 1) > 1e-8 || Math.Abs(reference.ScaleFactors.Y - 1) > 1e-8 || Math.Abs(reference.ScaleFactors.Z - 1) > 1e-8)
                { document.Editor.WriteMessage("\nLa figura tiene una escala de bloque modificada. Restablezca X/Y/Z a 1 antes de editar sus medidas 3D."); return true; }
                pose = reference.BlockTransform;
            }
            var window = new Figure3DWindow(original, editing: true); AcadApplication.ShowModalWindow(window);
            if (window.Parameters == null || !IsActive(document)) return true;
            try
            {
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    Figure3DParameters current;
                    if (!service.TryRead(ids[0], transaction, out current) || !SameParameters(current, original) ||
                        !((BlockReference)transaction.GetObject(ids[0], OpenMode.ForRead)).BlockTransform.IsEqualTo(pose))
                        throw new InvalidOperationException("La figura cambió mientras se revisaban sus medidas. Vuelva a seleccionarla.");
                    service.Update(document.Database, transaction, ids[0], window.Parameters); transaction.Commit();
                }
                document.Editor.Regen(); document.Editor.WriteMessage("\nSólido y cotas 3D actualizados. Use U para deshacer.");
            }
            catch (System.Exception exception) { document.Editor.WriteMessage("\nNo se pudo editar la figura 3D: " + exception.Message); }
            return true;
        }
        private static bool SameParameters(Figure3DParameters a, Figure3DParameters b)
        {
            return a.Width == b.Width && a.Depth == b.Depth && a.Height == b.Height && a.BaseThickness == b.BaseThickness &&
                a.LeftWall == b.LeftWall && a.RightWall == b.RightWall && a.FrontWall == b.FrontWall && a.BackWall == b.BackWall &&
                SamePassage(a.Left, b.Left) && SamePassage(a.Right, b.Right);
        }
        private static bool SamePassage(Figure3DPassage a, Figure3DPassage b)
        { return a.Enabled == b.Enabled && a.Width == b.Width && a.Height == b.Height && a.Center == b.Center && a.Bottom == b.Bottom; }
        private static bool IsActive(Document document)
        { return AcadApplication.DocumentManager.MdiActiveDocument == document; }
        internal static bool ReadView(Document document, string prompt, bool implied, out SelectedFigureSnapshot snapshot)
        {
            snapshot = null;
            PromptSelectionResult selection = implied ? document.Editor.SelectImplied() : null;
            if (!implied || selection.Status != PromptStatus.OK)
                selection = document.Editor.GetSelection(new PromptSelectionOptions { MessageForAdding = "\n" + prompt + ": " });
            if (selection.Status != PromptStatus.OK || selection.Value == null) return false;
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                string error;
                var ids = selection.Value.GetObjectIds().Where(id => {
                    var entity = transaction.GetObject(id, OpenMode.ForRead) as Entity;
                    return !(entity is Solid3d) && !(entity is global::Autodesk.AutoCAD.DatabaseServices.Surface) && !(entity is SubDMesh);
                }).ToArray();
                if (ids.Length != selection.Value.Count) document.Editor.WriteMessage("\nSe usan las referencias 2D; los sólidos existentes quedan fuera de TOP/FRONT.");
                if (new SelectedFigureDrawingService().TryRead(ids, transaction, out snapshot, out error)) return true;
                document.Editor.WriteMessage("\n" + error); return false;
            }
        }
    }
}
