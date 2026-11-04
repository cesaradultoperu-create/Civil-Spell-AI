using System;
using System.IO;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcApp=Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace LocalizarCasillas
{
    public class Commands
    {
        static LocatorPanel panel;
        internal static Slot Pending;

        [CommandMethod("CASILLASDEMO")]
        public void Demo(){Open();panel.Search("03_R3_");}

        [CommandMethod("CASILLAS",CommandFlags.Modal)]
        public void Open()
        {
            var doc=AcApp.DocumentManager.MdiActiveDocument;
            if(panel==null || panel.IsDisposed || panel.Document!=doc)
            {
                if(panel!=null && !panel.IsDisposed)panel.Close();
                panel=new LocatorPanel(doc);Autodesk.AutoCAD.ApplicationServices.Application.ShowModelessDialog(panel);
            }
            else {panel.Show();panel.BringToFront();}
            panel.RefreshCatalog();
        }

        [CommandMethod("CASILLASBLOQUE",CommandFlags.Modal)]
        public void SelectBlock()
        {
            var doc=AcApp.DocumentManager.MdiActiveDocument;
            if(panel!=null && !panel.IsDisposed)panel.BeforeNativeEdit();
            try
            {
                var options=new PromptEntityOptions("\nSelecciona el bloque del rotulo o cuadro: ");
                options.SetRejectMessage("\nSelecciona una referencia de bloque.");options.AddAllowedClass(typeof(BlockReference),false);
                var result=doc.Editor.GetEntity(options);
                if(result.Status!=PromptStatus.OK)return;
                if(panel==null || panel.IsDisposed || panel.Document!=doc)Open();
                panel.Show();panel.Restrict(result.ObjectId);
            }
            finally {if(panel!=null && !panel.IsDisposed)panel.Show();}
        }

        [CommandMethod("CASILLASEDITAR",CommandFlags.Modal)]
        public void Edit()
        {
            var doc=AcApp.DocumentManager.MdiActiveDocument; var slot=Pending;Pending=null;
            if(slot==null || slot.Nested || slot.AttributeId.Database!=doc.Database)return;
            if(panel!=null && !panel.IsDisposed)panel.BeforeNativeEdit();
            bool queued=false;
            try
            {
                if(slot.BlockId.IsErased || slot.AttributeId.IsErased)return;
                doc.Editor.WriteMessage("\nCasilla localizada: "+slot.Tag+". Comprueba este TAG en el editor.\n");
                // The pick coordinate makes EATTEDIT open the attribute at this position.
                // The native editor preserves fields and supports its Insert/Edit Field workflow.
                string x=slot.Position.X.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
                string y=slot.Position.Y.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
                string z=slot.Position.Z.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
                string expression="(command \"_.EATTEDIT\" (list (handent \""+slot.BlockId.Handle+"\") (trans '("+x+" "+y+" "+z+") 0 1)))";
                // Return to the command prompt before running the native dialog, then restore the panel.
                Autodesk.AutoCAD.ApplicationServices.CommandEventHandler completed=null;
                completed=(sender,args)=>{
                    if(!String.Equals(args.GlobalCommandName,"EATTEDIT",StringComparison.OrdinalIgnoreCase))return;
                    doc.CommandEnded-=completed;doc.CommandCancelled-=completed;doc.CommandFailed-=completed;
                    EventHandler idle=null;
                    idle=(s,e)=>{AcApp.Idle-=idle;if(panel!=null && !panel.IsDisposed && panel.Document==doc)panel.AfterNativeEdit();};
                    AcApp.Idle+=idle;
                };
                doc.CommandEnded+=completed;doc.CommandCancelled+=completed;doc.CommandFailed+=completed;
                doc.SendStringToExecute(expression+"\n",true,false,false);queued=true;
            }
            catch(System.Exception ex){doc.Editor.WriteMessage("\nCASILLAS_EDITOR: "+ex.Message+"\n");}
            finally {if(!queued && panel!=null && !panel.IsDisposed)panel.AfterNativeEdit();}
        }
        [CommandMethod("CASILLASAUDITAR")]
        public void Audit()
        {
            var doc=AcApp.DocumentManager.MdiActiveDocument;
            try
            {
                var slots=Catalog.Read(doc.Database);
                Catalog.Export(slots,Path.Combine(Path.GetDirectoryName(doc.Name),"atributos.csv"));
                File.WriteAllLines(Path.Combine(Path.GetDirectoryName(doc.Name),"api.txt"),new[]{
                    String.Join(", ",Enum.GetNames(typeof(Autodesk.AutoCAD.DatabaseServices.FieldCodeFlags)))});
                doc.Editor.WriteMessage("\nCASILLAS_AUDIT_OK atributos="+slots.Count+"\n");
            }
            catch(System.Exception ex) {doc.Editor.WriteMessage("\nCASILLAS_FAILED "+ex+"\n");}
        }
    }
}
