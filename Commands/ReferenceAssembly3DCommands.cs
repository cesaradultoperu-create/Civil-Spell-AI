using System;
using System.Collections.Generic;
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
    public sealed class ReferenceAssembly3DCommands
    {
        [CommandMethod("CREARCOMPONENTE3D",CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void CreateComponent()
        {
            var doc=AcadApplication.DocumentManager.MdiActiveDocument;if(doc==null)return;
            SelectedFigureSnapshot top;
            if(!Figure3DCommands.ReadView(doc,"TOP: seleccione perfiles cerrados del material de un componente",true,out top)) return;
            CreateFromTop(doc,top,false);
        }
        internal static void CreateFromTop(Document doc,SelectedFigureSnapshot top,bool recognizeDetail)
        {
            SelectedFigureSnapshot front;
            if(!Figure3DCommands.ReadView(doc,"FRONT: seleccione la sección del mismo componente o detalle, incluidos contornos y hatches",false,out front))return;
            try
            {
                ReferenceAssembly3D model;
                List<MaterialReferenceProfile> plan,elevation;
                bool pipe;
                using(var tr=doc.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    pipe=CorrugatedPipe3DAnalyzer.TryAnalyze(top,front,tr,out model);
                    plan=pipe?new List<MaterialReferenceProfile>():ReferenceProfileReader.Read(top.SourceIds,tr);
                    elevation=pipe?new List<MaterialReferenceProfile>():ReferenceProfileReader.Read(front.SourceIds,tr);
                }
                if(top.SourceIds.Intersect(front.SourceIds).Any()) throw new ArgumentException("TOP y FRONT deben ser selecciones distintas.");
                bool chamberDetail=recognizeDetail && plan.Any(p=>p.Pattern=="AR-CONC") && plan.Any(p=>p.Pattern=="GRAVEL") &&
                    elevation.Any(p=>p.Pattern=="AR-CONC") && elevation.Any(p=>p.Pattern=="GRAVEL");
                if(chamberDetail) model=new DrainageReference3DAnalyzer().Analyze(top,front,plan,elevation);
                else if(!pipe)
                {
                    doc.Editor.WriteMessage("\nSe relacionan perfiles de material del mismo componente. Para una estructura con varias piezas, seleccione cada pieza por separado.");
                    var tc=plan.Any(p=>p.Pattern!=null)?plan.Where(p=>p.Pattern!=null).ToArray():plan.ToArray();
                    var fc=elevation.Any(p=>p.Pattern!=null)?elevation.Where(p=>p.Pattern!=null).ToArray():elevation.ToArray();
                    if(tc.Select(p=>p.Pattern).Distinct().Count()>1 || fc.Select(p=>p.Pattern).Distinct().Count()>1)
                        throw new ArgumentException("La selección mezcla varios materiales. Use Componente y seleccione cada pieza con su perfil TOP y FRONT; no se unen terreno y estructura automáticamente.");
                    if(tc.Length==0 || fc.Length==0) throw new ArgumentException("Seleccione un perfil cerrado en cada vista.");
                    model=PairedProfile3DAnalyzer.Analyze(plan,elevation,tc.Min(p=>p.Contour.X.Min()),fc.Min(p=>p.Contour.X.Min()),
                        tc.Min(p=>p.Contour.Y.Min()),fc.Min(p=>p.Contour.Y.Min()));
                    model.TopReference=Handles(top);model.FrontReference=Handles(front);
                }
                var window=new ReferenceAssembly3DWindow(model,top,front);AcadApplication.ShowModalWindow(window);
                if(window.Model==null || AcadApplication.DocumentManager.MdiActiveDocument!=doc)return;
                var insertion=doc.Editor.GetPoint("\nPunto de inserción del conjunto 3D: ");
                if(insertion.Status!=PromptStatus.OK || AcadApplication.DocumentManager.MdiActiveDocument!=doc)return;
                using(var tr=doc.Database.TransactionManager.StartTransaction())
                {
                    Revalidate(top,tr);Revalidate(front,tr);
                    new ReferenceAssemblyDrawingService().Create(doc.Database,tr,insertion.Value.TransformBy(doc.Editor.CurrentUserCoordinateSystem),window.Model);
                    tr.Commit();
                }
                doc.Editor.Regen();doc.Editor.WriteMessage("\nModelo creado desde TOP y FRONT. EDITARFIGURA3D conserva los componentes; U deshace la creación.");
            }
            catch(System.Exception ex){doc.Editor.WriteMessage("\nNo se pudo reconstruir: "+ex.Message);}
        }
        private static string Handles(SelectedFigureSnapshot view){return string.Join(",",view.SourceIds.Select(id=>id.Handle.ToString()));}
        private static void Revalidate(SelectedFigureSnapshot original,Transaction tr)
        {
            if(original.SourceIds.Any(id=>!id.IsValid || id.IsErased)) throw new InvalidOperationException("Una referencia ya no está disponible. Selecciónela nuevamente.");
            SelectedFigureSnapshot current;string error;
            if(!new SelectedFigureDrawingService().TryRead(original.SourceIds,tr,out current,out error) || current.Outlines.Count!=original.Outlines.Count)
                throw new InvalidOperationException("Una referencia cambió durante la revisión.");
            for(int i=0;i<current.Outlines.Count;i++)
            {
                var a=current.Outlines[i];var b=original.Outlines[i];
                if(a.Length!=b.Length || a.Where((p,j)=>p.GetDistanceTo(b[j])>1e-8).Any())
                    throw new InvalidOperationException("Una referencia cambió durante la revisión.");
            }
        }
        internal static bool TryEdit(Document doc,ObjectId[] ids)
        {
            if(ids.Length!=1)return false;
            var service=new ReferenceAssemblyDrawingService();ReferenceAssembly3D original;Matrix3d pose;
            using(var tr=doc.Database.TransactionManager.StartOpenCloseTransaction())
            {
                if(!service.TryRead(ids[0],tr,out original))return false;
                var reference=(BlockReference)tr.GetObject(ids[0],OpenMode.ForRead);pose=reference.BlockTransform;
                if(Math.Abs(reference.ScaleFactors.X-1)>1e-8 || Math.Abs(reference.ScaleFactors.Y-1)>1e-8 || Math.Abs(reference.ScaleFactors.Z-1)>1e-8)
                {doc.Editor.WriteMessage("\nRestablezca la escala X/Y/Z del bloque a 1 antes de editarlo.");return true;}
            }
            var draft=original.Copy();
            try { using(var tr=doc.Database.TransactionManager.StartOpenCloseTransaction()) CorrugatedPipe3DAnalyzer.UpgradeCorrugation(draft,doc.Database,tr); }
            catch(System.Exception) { /* A detached model remains editable even without its original 2D references. */ }
            var window=new ReferenceAssembly3DWindow(draft,editing:true);AcadApplication.ShowModalWindow(window);
            if(window.Model==null || AcadApplication.DocumentManager.MdiActiveDocument!=doc)return true;
            try
            {
                using(var tr=doc.Database.TransactionManager.StartTransaction())
                {
                    ReferenceAssembly3D current;
                    if(!service.TryRead(ids[0],tr,out current) || current.Serialize()!=original.Serialize() ||
                        !((BlockReference)tr.GetObject(ids[0],OpenMode.ForRead)).BlockTransform.IsEqualTo(pose))
                        throw new InvalidOperationException("El modelo cambió durante la revisión.");
                    service.Update(doc.Database,tr,ids[0],window.Model);tr.Commit();
                }
                doc.Editor.Regen();
            }
            catch(System.Exception ex){doc.Editor.WriteMessage("\nNo se pudo editar: "+ex.Message);}
            return true;
        }
    }
}
