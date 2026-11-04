using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using LocalizarCasillas;
using AcApp=Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CasillasField
{
    public sealed class ModalMarks : IDisposable
    {
        readonly List<Entity> entities=new List<Entity>();
        public void Draw(Document doc,Slot destination,Slot[] uses)
        {
            Clear();
            foreach(var s in uses.Where(s=>destination==null || s.AttributeId!=destination.AttributeId))Add(s,5,"YA USA: ");
            if(destination!=null)Add(destination,1,"EDITANDO: ");
            doc.Editor.UpdateScreen();
        }
        void Add(Slot slot,short color,string caption)
        {
            foreach(var g in Marker.CreateGraphics(slot))
            {
                g.ColorIndex=color;var text=g as MText;if(text!=null)text.Contents=caption+slot.Tag;
                bool added=false;
                try {added=TransientManager.CurrentTransientManager.AddTransient(g,TransientDrawingMode.DirectTopmost,129,new Autodesk.AutoCAD.Geometry.IntegerCollection());}
                finally {if(added)entities.Add(g);else g.Dispose();}
            }
        }
        public void Clear()
        {
            foreach(var g in entities)
            {try{TransientManager.CurrentTransientManager.EraseTransient(g,new Autodesk.AutoCAD.Geometry.IntegerCollection());}catch(Autodesk.AutoCAD.Runtime.Exception){}finally{g.Dispose();}}
            entities.Clear();
        }
        public void Dispose()=>Clear();
    }

    public sealed class FieldGuide : IDisposable
    {
        readonly Document doc;
        readonly Timer timer=new Timer {Interval=220};
        readonly ModalMarks marks=new ModalMarks();
        readonly GuideForm form=new GuideForm();
        readonly Dictionary<ObjectId,Scene> scenes=new Dictionary<ObjectId,Scene>();
        List<Slot> slots;
        ObjectId root;
        string previous="",lastError="";
        DateTime lastPaint=DateTime.MinValue;
        bool busy,refresh,disposed;
        readonly Dictionary<ObjectId,Tuple<string,string>> pendingText=new Dictionary<ObjectId,Tuple<string,string>>();
        bool evaluate;
        IntPtr hiddenForField;
        internal bool RunningFor(Document document)=>!disposed && doc==document && timer.Enabled && !form.IsDisposed &&
            slots.Any(s=>s.BlockId==root && s.Layout==Convert.ToString(AcApp.GetSystemVariable("CTAB")));
        public FieldGuide(Document document,ObjectId block)
        {
            doc=document;root=block;Reload();
            timer.Tick+=(s,e)=>Tick();timer.Start();
            doc.CommandEnded+=CommandEnded;
            AcApp.DocumentManager.DocumentToBeDestroyed+=Destroying;
            form.FormClosed+=(s,e)=>timer.Stop();
            form.FormClosing+=(s,e)=>
            {
                if(e.CloseReason==CloseReason.UserClosing)
                {
                    // Keep observing native close/commit so queued local edits are not lost.
                    e.Cancel=true;hiddenForField=NativeDialogs.Read().Field;form.Hide();
                }
            };
            form.EditRequested+=Edit;
        }
        void Edit(Slot slot)
        {
            busy=true;
            try
            {
                bool linked=CellStatus.HasField(slot);var id=FieldIdentity.Parse(slot.Code);
                if(doc.IsReadOnly)throw new InvalidOperationException("El plano está abierto en modo de solo lectura. La ayuda permite consultar sus casillas.");
                string initial=linked?SheetSetStore.Read(doc.Name,id):slot.Value;
                if(pendingText.ContainsKey(slot.AttributeId))initial=pendingText[slot.AttributeId].Item2;
                var uses=linked?FieldIdentity.Uses(slots,id):new[]{slot};
                bool demo=linked && SheetSetStore.IsDemo(doc.Name,id);
                string impact=linked?"Usos en este DWG: "+uses.Length+" · "+String.Join(", ",uses.Select(s=>s.Tag+" ["+s.Layout+"]"))+".\n"+
                    (demo?"Sheet Set local de prueba; el proyecto original no se modifica.":"Se cambiará la propiedad compartida del proyecto. También afectará a otros planos vinculados al actualizar sus Fields; no se han contado aquí."):
                    "El texto se guardará al cerrar Field y el editor de atributos.\nSe comprobará que la casilla no haya cambiado antes de aplicarlo.";
                if(linked)impact+="\nGuardar modifica el DST; Cancelar Field o UNDO no deshacen ese valor.";
                Scene scene;scenes.TryGetValue(slot.BlockId,out scene);
                using(var editor=new CellValueEditor(slot,scene,initial,impact,linked))
                {
                    var native=NativeDialogs.Read();
                    if(editor.ShowDialog(new WindowOwner(native.Field!=IntPtr.Zero?native.Field:form.Handle))!=DialogResult.OK)return;
                    if(linked)
                    {
                        SheetSetStore.Write(doc.Name,id,initial,editor.Value);
                        foreach(var use in uses)use.Value=editor.Value;
                        evaluate=true;
                        NativeDialogs.RefreshProperty(native.Field);
                        Log("VALUE_SAVED property="+id.Property+" uses="+uses.Length);
                    }
                    else
                    {
                        string original=pendingText.ContainsKey(slot.AttributeId)?pendingText[slot.AttributeId].Item1:slot.Value;
                        pendingText[slot.AttributeId]=Tuple.Create(original,editor.Value);slot.Value=editor.Value;slot.PendingEdit=true;
                        Log("TEXT_PENDING tag="+slot.Tag);
                    }
                    form.SetCatalog(slots.Where(s=>s.BlockId==root),scenes);form.FocusSlot(slot);previous="";form.Invalidate(true);
                }
            }
            catch(System.Exception ex){MessageBox.Show(form,ex.Message,"No se guardó la casilla",MessageBoxButtons.OK,MessageBoxIcon.Information);Log("EDIT_ERROR "+ex);}
            finally{busy=false;}
        }
        sealed class WindowOwner : IWin32Window {public IntPtr Handle{get;}public WindowOwner(IntPtr h){Handle=h;}}
        void ApplyPending()
        {
            using(doc.LockDocument())
            {
                if(pendingText.Count>0)
                {
                    TextEdits.Apply(doc.Database,pendingText);
                    Log("TEXT_APPLIED count="+pendingText.Count);
                    pendingText.Clear();
                }
                if(evaluate){SheetSetStore.Evaluate(doc.Database);evaluate=false;}
            }
            doc.Editor.Regen();refresh=true;
        }
        void Reload()
        {
            using(doc.LockDocument())
            {
                slots=Catalog.Read(doc.Database);scenes.Clear();
                foreach(var group in slots.GroupBy(s=>s.BlockId))scenes[group.Key]=Scene.Read(doc.Database,group.Key,group);
            }
            form.SetCatalog(slots.Where(s=>s.BlockId==root),scenes);
            refresh=false;
        }
        void Tick()
        {
            if(busy)return;busy=true;
            try
            {
                if(AcApp.DocumentManager.MdiActiveDocument!=doc){form.Hide();marks.Clear();previous="";return;}
                var native=NativeDialogs.Read();
                if(native.Field==IntPtr.Zero)hiddenForField=IntPtr.Zero;
                if(native.Editor==IntPtr.Zero && native.Field==IntPtr.Zero)
                {
                    if((evaluate || pendingText.Count>0) && Convert.ToInt32(AcApp.GetSystemVariable("CMDACTIVE"))==0)
                    {
                        try{ApplyPending();}
                        catch(System.Exception ex)
                        {
                            pendingText.Clear();evaluate=false;refresh=true;
                            Log("APPLY_ERROR "+ex);
                            MessageBox.Show(ex.Message,"No se aplicó la edición pendiente",MessageBoxButtons.OK,MessageBoxIcon.Information);
                        }
                    }
                    if(form.Visible)form.Hide();
                    if(previous.Length>0){marks.Clear();previous="";doc.Editor.UpdateScreen();}
                    if(refresh && Convert.ToInt32(AcApp.GetSystemVariable("CMDACTIVE"))==0)Reload();
                    return;
                }
                string currentLayout=Convert.ToString(AcApp.GetSystemVariable("CTAB"));
                Slot target=ResolveTarget(slots,root,native.Tag,native.Block,currentLayout);
                var selected=FieldIdentity.Parse(native.Code);
                var uses=FieldIdentity.Uses(slots,selected);
                string state=native.Editor+"|"+native.Field+"|"+native.Tag+"|"+native.Code+"|"+native.SelectedValue;
                bool changed=state!=previous;
                if(changed)
                {
                    // Only adjust a view in its existing layout; the database and native field editor are untouched.
                    if(target!=null && target.Layout==Convert.ToString(AcApp.GetSystemVariable("CTAB")))
                    {
                        try{Marker.Zoom(doc.Editor,Catalog.MarkerBounds(target),target.Height*31);}catch(Autodesk.AutoCAD.Runtime.Exception){ }
                    }
                    form.UpdateInfo(target,native.SelectedValue,selected,uses,scenes);
                    Log("FIELD_STATE tag="+(target?.Tag??"?")+" selected="+(selected?.Property??"?")+" uses="+String.Join(",",uses.Select(s=>s.Tag)));
                    previous=state;
                }
                if(native.Field!=IntPtr.Zero && native.Field!=hiddenForField)form.Attach(native.Field,Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Handle);
                else if(form.Visible)form.Hide();
                if(changed || (DateTime.UtcNow-lastPaint).TotalSeconds>1.2)
                {
                    string layout=Convert.ToString(AcApp.GetSystemVariable("CTAB"));
                    marks.Draw(doc,target!=null && target.Layout==layout?target:null,uses.Where(s=>s.Layout==layout).ToArray());
                    lastPaint=DateTime.UtcNow;
                }
            }
            catch(System.Exception ex)
            {
                if(lastError!=ex.Message){lastError=ex.Message;Log("FIELD_ERROR "+ex);}
            }
            finally {busy=false;}
        }
        internal static Slot ResolveTarget(IEnumerable<Slot> slots,ObjectId selectedRoot,string tag,string block,string layout)
        {
            if(String.IsNullOrEmpty(tag)||String.IsNullOrEmpty(block))return null;
            // EATTEDIT exposes a TAG and block name, but no insertion ID. A repeated name
            // in this layout cannot be assigned to the remembered insertion with certainty.
            var candidates=slots.Where(s=>s.Tag==tag && s.Block==block && s.Layout==layout).ToArray();
            return candidates.Length==1 && candidates[0].BlockId==selectedRoot ? candidates[0] : null;
        }
        void Log(string text)
        {
            try {File.AppendAllText(ProjectAccess.LogFile(doc.Name),DateTime.Now.ToString("HH:mm:ss",CultureInfo.InvariantCulture)+" "+text+Environment.NewLine);}
            catch(IOException){ }catch(UnauthorizedAccessException){ }
        }
        void CommandEnded(object sender,CommandEventArgs e){if(e.GlobalCommandName=="EATTEDIT")refresh=true;}
        void Destroying(object sender,DocumentCollectionEventArgs e){if(e.Document==doc)Dispose();}
        public void Dispose()
        {
            if(disposed)return;disposed=true;
            timer.Stop();timer.Dispose();marks.Dispose();
            doc.CommandEnded-=CommandEnded;AcApp.DocumentManager.DocumentToBeDestroyed-=Destroying;
            if(!form.IsDisposed)form.Dispose();
        }
    }

    public class FieldCommands
    {
        static FieldGuide guide;
        internal static bool RunningFor(Document doc)=>guide!=null && guide.RunningFor(doc);
        internal static void ActivateAutomatic(Document doc)
        {
            var project=ProjectAccess.ForDrawing(doc.Name);
            if(project==null)Enable(doc,true);
            else
            {
                string layout=Convert.ToString(AcApp.GetSystemVariable("CTAB"));
                var roots=Catalog.Read(doc.Database).Where(s=>s.Layout==layout && s.Block==project.Block).Select(s=>s.BlockId).Distinct().ToArray();
                if(roots.Length!=1)return;
                if(guide!=null)guide.Dispose();guide=new FieldGuide(doc,roots[0]);
            }
            try{File.AppendAllText(ProjectAccess.LogFile(doc.Name),DateTime.Now.ToString("HH:mm:ss")+" AUTO_FIELD_ENABLED version="+typeof(FieldGuide).Assembly.GetName().Version+Environment.NewLine);}catch(IOException){}catch(UnauthorizedAccessException){}
        }
        static ObjectId Enable(Document doc,bool demo)
        {
            ObjectId block=ObjectId.Null;
            var slots=Catalog.Read(doc.Database);
            if(demo)block=slots.Where(s=>s.Tag=="R3_CH").Select(s=>s.BlockId).Distinct().Single();
            else
            {
                var implied=doc.Editor.SelectImplied();
                if(implied.Status==Autodesk.AutoCAD.EditorInput.PromptStatus.OK && implied.Value.Count==1)
                {
                    var candidate=implied.Value.GetObjectIds()[0];if(slots.Any(s=>s.BlockId==candidate))block=candidate;
                }
                if(block.IsNull)
                {
                    var options=new Autodesk.AutoCAD.EditorInput.PromptEntityOptions("\nSelecciona el bloque del rotulo para identificar sus celdas: ");
                    options.SetRejectMessage("\nSelecciona un bloque con atributos.");options.AddAllowedClass(typeof(BlockReference),false);
                    var result=doc.Editor.GetEntity(options);if(result.Status!=Autodesk.AutoCAD.EditorInput.PromptStatus.OK)return block;block=result.ObjectId;
                }
            }
            if(guide!=null)guide.Dispose();guide=new FieldGuide(doc,block);
            doc.Editor.WriteMessage("\nAyuda de Field activada. Abre un atributo de este bloque y su Field: rojo = celda en edicion, azul = usos existentes de Property.\n");
            return block;
        }
        [CommandMethod("CASILLASFIELD")]
        public void Start(){var doc=AcApp.DocumentManager.MdiActiveDocument;AutoStart.Resume(doc);Enable(doc,false);}
        [CommandMethod("CASILLASFIELDOFF")]
        public void Stop(){AutoStart.Pause(AcApp.DocumentManager.MdiActiveDocument);if(guide!=null){guide.Dispose();guide=null;}}
        [CommandMethod("CASILLASFIELDDEMO")]
        public void Demo()
        {
            var doc=AcApp.DocumentManager.MdiActiveDocument;Enable(doc,true);
            AutoStart.Resume(doc);
            OpenDemoAttribute(doc);
        }
        internal static void OpenDemoAttribute(Document doc)
        {
            var candidates=Catalog.Read(doc.Database).Where(s=>s.Tag=="R3_CH").ToArray();
            string layout=Convert.ToString(AcApp.GetSystemVariable("CTAB"));
            var current=candidates.Where(s=>s.Layout==layout).ToArray();
            var target=current.Length==1?current[0]:candidates.Single();var block=target.BlockId;
            if(LayoutManager.Current.CurrentLayout!=target.Layout)LayoutManager.Current.CurrentLayout=target.Layout;
            doc.Editor.SetImpliedSelection(new ObjectId[0]);Marker.Zoom(doc.Editor,Catalog.MarkerBounds(target),target.Height*31);
            string p=String.Join(" ",new[]{target.Position.X,target.Position.Y,target.Position.Z}.Select(n=>n.ToString("R",CultureInfo.InvariantCulture)));
            doc.SendStringToExecute("(command \"_.EATTEDIT\" (list (handent \""+block.Handle+"\") (trans '("+p+") 0 1)))\n",true,false,false);
        }
    }
}
