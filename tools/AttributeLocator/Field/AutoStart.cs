using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using AcApp=Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly:ExtensionApplication(typeof(CasillasField.AutoStart))]
namespace CasillasField
{
    public sealed class AutoStart : IExtensionApplication
    {
        static readonly HashSet<Document> paused=new HashSet<Document>();
        Timer timer;
        Controller controller;
        bool openRequested;
        bool busy;
        Document attemptedDocument;
        string attemptedLayout;
        DateTime nextAttempt;
        string lastError;
        public static bool CanEnable(string drawing)
        {
            if(ProjectAccess.ForDrawing(drawing)!=null)return true;
            if(!String.Equals(Path.GetFileName(drawing),"Ejemplo-Field-v3.dwg",StringComparison.OrdinalIgnoreCase))return false;
            string dst=SheetSetStore.DemoPath(drawing);
            if(!File.Exists(dst) || !File.Exists(dst+".demo"))return false;
            Guid guid;return Guid.TryParse(File.ReadAllText(dst+".demo").Trim().TrimStart('g','G'),out guid);
        }
        public static void Pause(Document doc)=>paused.Add(doc);
        public static void Resume(Document doc)=>paused.Remove(doc);
        public void Initialize()
        {
            timer=new Timer {Interval=500};timer.Tick+=(s,e)=>Tick();timer.Start();
            controller=new Controller(()=>openRequested=true);
            AcApp.DocumentManager.DocumentToBeDestroyed+=Closing;
        }
        void Tick()
        {
            if(busy)return;busy=true;
            try
            {
                var doc=AcApp.DocumentManager.MdiActiveDocument;
                if(doc==null || paused.Contains(doc) || !CanEnable(doc.Name))return;
                // Prepare the observer while idle, before a native attribute/Field dialog is opened.
                if(Convert.ToInt32(AcApp.GetSystemVariable("CMDACTIVE"))!=0)return;
                if(!FieldCommands.RunningFor(doc))
                {
                    string layout=Convert.ToString(AcApp.GetSystemVariable("CTAB"));
                    if(attemptedDocument==doc && attemptedLayout==layout && DateTime.UtcNow<nextAttempt)return;
                    attemptedDocument=doc;attemptedLayout=layout;nextAttempt=DateTime.UtcNow.AddSeconds(5);
                    FieldCommands.ActivateAutomatic(doc);
                }
                if(openRequested){openRequested=false;if(ProjectAccess.ForDrawing(doc.Name)==null)FieldCommands.OpenDemoAttribute(doc);}
                lastError=null;
            }
            catch(System.Exception ex)
            {
                if(lastError!=ex.Message)
                {
                    lastError=ex.Message;
                    var doc=AcApp.DocumentManager.MdiActiveDocument;
                    if(doc!=null)doc.Editor.WriteMessage("\nAyuda de casillas: no se pudo activar automaticamente. "+ex.Message+"\n");
                }
            }
            finally{busy=false;}
        }
        void Closing(object sender,DocumentCollectionEventArgs e)=>paused.Remove(e.Document);
        public void Terminate()
        {
            if(timer!=null){timer.Stop();timer.Dispose();timer=null;}
            if(controller!=null){controller.DestroyHandle();controller=null;}
            AcApp.DocumentManager.DocumentToBeDestroyed-=Closing;
        }
        sealed class Controller : NativeWindow
        {
            readonly Action open;
            public Controller(Action request)
            {
                open=request;CreateHandle(new CreateParams {Caption="CasillasFieldDemo.Controller.0.3.1"});
            }
            protected override void WndProc(ref Message message)
            {
                if(message.Msg==0x8049){open();message.Result=IntPtr.Zero;return;}
                base.WndProc(ref message);
            }
        }
    }
}
