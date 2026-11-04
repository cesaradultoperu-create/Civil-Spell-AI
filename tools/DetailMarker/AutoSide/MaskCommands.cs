using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace DetalleAutoLado
{
    public sealed class MaskCommands
    {
        public static void SetMask(MLeader m,Transaction tr,bool enabled)
        {
            if(!m.IsWriteEnabled)m.UpgradeOpen();SideSwitch.SetMaskEnabled(m,tr,enabled);SideSwitch.Update(m,tr);
            m.RecordGraphicsModified(true);
        }
        [CommandMethod("DETALLEADAPTABLE6","DETALLEMASCARA",CommandFlags.UsePickSet)]
        [CommandMethod("DETALLEADAPTABLE6","DETALLEMASCARA6",CommandFlags.UsePickSet)]
        public void Mask()
        {
            var doc=App.DocumentManager.MdiActiveDocument;var ed=doc.Editor;ObjectId id=ObjectId.Null;
            var selected=ed.SelectImplied();if(selected.Status==PromptStatus.OK&&selected.Value.Count==1)id=selected.Value.GetObjectIds()[0];
            if(id.IsNull)
            {
                var options=new PromptEntityOptions("\nSelecciona el detalle: ");options.SetRejectMessage("\nSelecciona una directriz de detalle.");options.AddAllowedClass(typeof(MLeader),true);
                var pick=ed.GetEntity(options);if(pick.Status!=PromptStatus.OK)return;id=pick.ObjectId;
            }
            using(var tr=doc.Database.TransactionManager.StartTransaction())
            {
                var m=tr.GetObject(id,OpenMode.ForRead) as MLeader;if(m==null||!SideSwitch.IsDetail(m,tr)){ed.WriteMessage("\nSelecciona un detalle adaptable.");return;}
                var options=new PromptKeywordOptions("\nBackground mask [Yes/No] <Yes>: "){AllowNone=true};options.Keywords.Add("Yes");options.Keywords.Add("No");options.Keywords.Default="Yes";
                var choice=ed.GetKeywords(options);if(choice.Status!=PromptStatus.OK&&choice.Status!=PromptStatus.None)return;
                bool enabled=choice.Status==PromptStatus.None||choice.StringResult=="Yes";SetMask(m,tr,enabled);tr.Commit();ed.WriteMessage("\nBackground mask = "+(enabled?"Yes":"No"));
            }
            ed.Regen();
        }
    }
}
