using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;

public sealed class FiguraSelectionDiagnostics
{
    [CommandMethod("FIGURADIAGNOSTICO")]
    public void Run()
    {
        var doc=App.DocumentManager.MdiActiveDocument;
        var folder=Path.GetDirectoryName(typeof(FiguraSelectionDiagnostics).Assembly.Location);
        var report=new Dictionary<string,object>();
        try
        {
            var assemblies=AppDomain.CurrentDomain.GetAssemblies().Where(a=>a.GetName().Name=="CivilSpellAI").ToArray();
            report["loaded"]=assemblies.Select(a=>new {path=a.Location,mvid=a.ManifestModule.ModuleVersionId.ToString(),sha256=Hash(a.Location)}).ToArray();
            report["drawing"]=doc.Name;
            var previous=doc.Editor.SelectPrevious();
            var previousIds=previous.Status==PromptStatus.OK?previous.Value.GetObjectIds():new ObjectId[0];
            report["previous"]=previousIds.Select(id=>id.Handle.ToString()).ToArray();
            var assembly=assemblies.Single();
            var snapshotReader=assembly.GetType("CivilSpellAI.Autodesk.SelectedFigureDrawingService");
            var profileReader=assembly.GetType("CivilSpellAI.Autodesk.ReferenceProfileReader");
            var analyzer=assembly.GetType("CivilSpellAI.Autodesk.DrainageReference3DAnalyzer");
            var cases=new List<object>();
            using(var tr=doc.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var space=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(doc.Database),OpenMode.ForRead);
                Func<double,double,double,double,ObjectId[]> window=(x1,y1,x2,y2)=>space.Cast<ObjectId>().Where(id=>{
                    var e=tr.GetObject(id,OpenMode.ForRead) as Entity;
                    if(!(e is Curve) && !(e is Hatch) && !(e is BlockReference))return false;
                    try {var b=e.GeometricExtents;return b.MinPoint.X>=x1 && b.MaxPoint.X<=x2 && b.MinPoint.Y>=y1 && b.MaxPoint.Y<=y2;}catch{return false;}
                }).ToArray();
                Action<string,ObjectId[],ObjectId[]> probe=(name,topIds,frontIds)=>{
                    var result=new Dictionary<string,object>{{"name",name},{"topHandles",topIds.Select(id=>id.Handle.ToString()).ToArray()},
                        {"frontHandles",frontIds.Select(id=>id.Handle.ToString()).ToArray()},{"topCount",topIds.Length},{"frontCount",frontIds.Length}};
                    try
                    {
                        object[] topArgs={topIds,tr,null,null},frontArgs={frontIds,tr,null,null};
                        if(!(bool)snapshotReader.GetMethod("TryRead").Invoke(Activator.CreateInstance(snapshotReader,true),topArgs))throw new System.Exception("TOP: "+topArgs[3]);
                        if(!(bool)snapshotReader.GetMethod("TryRead").Invoke(Activator.CreateInstance(snapshotReader,true),frontArgs))throw new System.Exception("FRONT: "+frontArgs[3]);
                        result["topSnapshot"]=Snapshot(topArgs[2]);result["frontSnapshot"]=Snapshot(frontArgs[2]);
                        var plan=profileReader.GetMethod("Read").Invoke(null,new object[]{topIds,tr});
                        var front=profileReader.GetMethod("Read").Invoke(null,new object[]{frontIds,tr});
                        var model=analyzer.GetMethod("Analyze").Invoke(Activator.CreateInstance(analyzer,true),new object[]{topArgs[2],frontArgs[2],plan,front});
                        result["model"]=model.GetType().GetMethod("Serialize").Invoke(model,null);
                        result["status"]="OK";
                    }
                    catch(System.Exception e){result["status"]="FAILED";result["error"]=(e.InnerException??e).ToString();}
                    cases.Add(result);
                    doc.Editor.WriteMessage("\nFIGURA_DIAG {0}: TOP={1} FRONT={2} {3}",name,topIds.Length,frontIds.Length,result["status"]);
                };
                var centralTop=window(23.3,-2.8,27.5,-1.0);var centralFront=window(23.3,-4.95,27.5,-3.0);
                var leftTop=window(19.3,-2.8,22.6,-1.0);var leftFront=window(19.3,-4.95,22.7,-3.0);
                probe("central",centralTop,centralFront);probe("izquierda",leftTop,leftFront);
                if(previousIds.Length>0)probe("central-con-FRONT-previo",centralTop,previousIds);
                report["cases"]=cases;
            }
            var snapshot=Path.Combine(folder,"Referencia-actual.dwg");
            if(File.Exists(snapshot))snapshot=Path.Combine(folder,"Referencia-"+Guid.NewGuid().ToString("N")+".dwg");
            using(var copy=doc.Database.Wblock())copy.SaveAs(snapshot,DwgVersion.Current);
            report["snapshot"]=snapshot;
        }
        catch(System.Exception e){report["error"]=e.ToString();}
        var path=Path.Combine(folder,"Diagnostico-"+Guid.NewGuid().ToString("N")+".json");
        File.WriteAllText(path,new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Serialize(report));
        doc.Editor.WriteMessage("\nFIGURA_DIAG_SAVED {0}",path);
    }
    private static string Hash(string path)
    {
        using(var input=File.OpenRead(path))using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(input)).Replace("-","");
    }
    private static object Snapshot(object view)
    {
        var type=view.GetType();var origin=(Point3d)type.GetField("Origin").GetValue(view);
        var measures=type.GetField("Measures").GetValue(view);
        return new {origin=new[]{origin.X,origin.Y,origin.Z},
            frame=measures==null?null:measures.GetType().GetField("FrameBounds").GetValue(measures),
            outlines=((IEnumerable<Point2d[]>)type.GetField("Outlines").GetValue(view)).Select(line=>line.Select(p=>new[]{p.X,p.Y}).ToArray()).ToArray()};
    }
}
