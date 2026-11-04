using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;

public class LabelClickProbe
{
    static Document watched;
    static System.Windows.Forms.Timer timeout;
    static string lastPoint="", lastPicked="", command="";
    static ObjectId[] picked=new ObjectId[0];
    static readonly string LogPath=Path.Combine(Path.GetDirectoryName(typeof(LabelClickProbe).Assembly.Location),"clicks.txt");
    static void Log(string line) {try{File.AppendAllText(LogPath,DateTime.Now.ToString("HH:mm:ss.fff")+" "+line+Environment.NewLine);}catch{}}
    static string Var(string name) {try{var value=App.GetSystemVariable(name);var arr=value as Array;return arr==null?Convert.ToString(value):String.Join(",",arr.Cast<object>());}catch{return "?";}}
    static string Read(object obj,string name) {try{var p=obj.GetType().GetProperty(name);return p==null?"?":Convert.ToString(p.GetValue(obj,null));}catch{return "?";}}
    [CommandMethod("CSLABELWATCH")]
    public void Start()
    {
        Stop();watched=App.DocumentManager.MdiActiveDocument;
        watched.Editor.PointMonitor+=Monitor;
        watched.Editor.PromptedForPoint+=Prompt;
        watched.Editor.PromptedForEntity+=Prompt;
        watched.Editor.PromptedForSelection+=Prompt;
        watched.CommandWillStart+=WillStart;
        watched.CommandEnded+=Ended;
        watched.CommandCancelled+=Ended;
        watched.Database.ObjectAppended+=Appended;
        timeout=new System.Windows.Forms.Timer{Interval=90000};timeout.Tick+=(s,e)=>Stop();timeout.Start();
        Log("START "+watched.Name+" CTAB="+Var("CTAB")+" CVPORT="+Var("CVPORT")+" TWIST="+Var("VIEWTWIST")+" OSMODE="+Var("OSMODE")+" PICKBOX="+Var("PICKBOX")+" QAFLAGS="+Var("QAFLAGS"));
        watched.Editor.WriteMessage("\nDiagnostico de clic activo durante 90 segundos. Prueba una linea con Add Labels.\n");
    }
    static void Monitor(object sender,PointMonitorEventArgs e)
    {
        try{lastPoint="RAW="+e.Context.RawPoint+" COMPUTED="+e.Context.ComputedPoint;var paths=e.Context.GetPickedEntities();picked=paths.SelectMany(p=>p.GetObjectIds()).Distinct().ToArray();lastPicked=String.Join(",",picked.Select(id=>id.Handle.ToString()));}catch{}
    }
    static void Prompt(object sender,EventArgs e)
    {
        try
        {
            object result=e.GetType().GetProperty("Result").GetValue(e,null);
            Log("INPUT "+e.GetType().Name+" STATUS="+Read(result,"Status")+" VALUE="+Read(result,"Value")+" PICK="+Read(result,"PickedPoint")+" CTAB="+Var("CTAB")+" CVPORT="+Var("CVPORT")+" "+lastPoint+" IDS="+lastPicked);
            using(var tr=watched.Database.TransactionManager.StartOpenCloseTransaction())
                foreach(var id in picked){try{var ent=tr.GetObject(id,OpenMode.ForRead) as Entity;Log("PICKED "+id.Handle+" "+id.ObjectClass.DxfName+" LAYER="+(ent==null?"?":ent.Layer));}catch{}}
        }catch(System.Exception ex){Log("INPUT_ERROR "+ex.Message);}
    }
    static void WillStart(object sender,CommandEventArgs e){command=e.GlobalCommandName;Log("COMMAND "+command+" CTAB="+Var("CTAB")+" CVPORT="+Var("CVPORT"));}
    static void Ended(object sender,CommandEventArgs e){Log("END "+e.GlobalCommandName);command="";}
    static void Appended(object sender,ObjectEventArgs e){if(command.IndexOf("LABEL",StringComparison.OrdinalIgnoreCase)>=0)Log("APPENDED "+e.DBObject.GetType().FullName+" "+e.DBObject.Handle);}
    [CommandMethod("CSLABELWATCHOFF")]
    public void Stop()
    {
        if(timeout!=null){timeout.Stop();timeout.Dispose();timeout=null;}
        if(watched==null)return;
        watched.Editor.PointMonitor-=Monitor;watched.Editor.PromptedForPoint-=Prompt;watched.Editor.PromptedForEntity-=Prompt;watched.Editor.PromptedForSelection-=Prompt;
        watched.CommandWillStart-=WillStart;watched.CommandEnded-=Ended;watched.CommandCancelled-=Ended;watched.Database.ObjectAppended-=Appended;
        Log("STOP");watched=null;
    }
}
