using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using AcApp=Autodesk.AutoCAD.ApplicationServices.Application;

public class FigurasDistancias {
    static readonly string Root=Path.GetDirectoryName(typeof(FigurasDistancias).Assembly.Location);
    static readonly string[] Templates={"87EF","7360","8812","87F5","884C","884E"};
    static readonly string[][] Sources={
        new[]{"8FBF","733E","7298","8FBE","7297","7296"},
        new[]{"735D","735E","72AA","72AD","72A6","72A5"},
        new[]{"7366","7365","731E","7320","731A","7310"},
        new[]{"87EF","732F","8C5D","8FBE","8C61","721A"}
    };
    static readonly double[] Offsets={-12.5,12.5,-12.5,-37.5,12.5,37.5};
    static Document Work() {
        var d=AcApp.DocumentManager.MdiActiveDocument;
        if(!String.Equals(d.Name,Path.Combine(Root,"Distancias_figura_TRABAJO.dwg"),StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Command restricted to the task's local working drawing.");
        return d;
    }
    static ObjectId Id(Database db,string h){return db.GetObjectId(false,new Handle(Convert.ToInt64(h,16)),0);}
    static string Layer(int n){return "FIG_OPCION_"+n;}
    static int Option(string s) {
        if(!s.StartsWith("OPCION"))return 0;
        int n;return int.TryParse(s.Substring(6).Trim(),out n)?n:0;
    }
    static object Point(Point3d p){return new[]{p.X,p.Y,p.Z};}
    static void Write(string name,object data) {
        var j=new JavaScriptSerializer{MaxJsonLength=int.MaxValue};
        File.WriteAllText(Path.Combine(Root,name),j.Serialize(data));
    }
    [CommandMethod("DFINSPECT3",CommandFlags.Modal)]
    public void Inspect(){
        try {
            var d=Work();var db=d.Database;var list=new List<object>();
            using(var tr=db.TransactionManager.StartTransaction()) {
                var dict=(DBDictionary)tr.GetObject(db.LayoutDictionaryId,OpenMode.ForRead);
                foreach(DBDictionaryEntry de in dict) {
                    var l=(Layout)tr.GetObject(de.Value,OpenMode.ForRead);if(l.ModelType)continue;
                    var b=(BlockTableRecord)tr.GetObject(l.BlockTableRecordId,OpenMode.ForRead);
                    foreach(ObjectId id in b) {
                        var v=tr.GetObject(id,OpenMode.ForRead) as Viewport;if(v==null||Math.Abs(v.CustomScale-0.05)>0.00001)continue;
                        var frozen=new List<string>();
                        foreach(ObjectId f in v.GetFrozenLayers())frozen.Add(((LayerTableRecord)tr.GetObject(f,OpenMode.ForRead)).Name);
                        list.Add(new{layout=l.LayoutName,handle=v.Handle.ToString(),number=v.Number,on=v.On,scale=v.CustomScale,center=Point(v.CenterPoint),viewCenter=new[]{v.ViewCenter.X,v.ViewCenter.Y},target=Point(v.ViewTarget),direction=new[]{v.ViewDirection.X,v.ViewDirection.Y,v.ViewDirection.Z},twist=v.TwistAngle,frozen=frozen});
                    }
                }
            }
            Write("viewports-before.json",list);d.Editor.WriteMessage("\nDFINSPECT OK\n");
        }catch(System.Exception ex){Write("inspect-error.json",ex.ToString());}
    }
    [CommandMethod("DFPREPARAR3",CommandFlags.Modal)]
    public void Prepare(){
        try {
            var d=Work();var db=d.Database;var records=new List<object>();
            using(var tr=db.TransactionManager.StartTransaction()) {
                var lt=(LayerTable)tr.GetObject(db.LayerTableId,OpenMode.ForRead);
                for(int n=2;n<=5;n++)if(lt.Has(Layer(n)))throw new InvalidOperationException("Already prepared: "+Layer(n));
                lt.UpgradeOpen();
                var layerIds=new Dictionary<int,ObjectId>();
                for(int n=2;n<=5;n++) {
                    var lr=new LayerTableRecord{Name=Layer(n),Color=Color.FromColorIndex(ColorMethod.ByAci,7),IsOff=false,IsFrozen=false};
                    var lid=lt.Add(lr);tr.AddNewlyCreatedDBObject(lr,true);layerIds.Add(n,lid);
                }
                var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead);
                var model=(BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace],OpenMode.ForWrite);
                for(int n=2;n<=5;n++)for(int c=1;c<=6;c++) {
                    var source=(Polyline)tr.GetObject(Id(db,Sources[n-2][c-1]),OpenMode.ForRead);
                    var template=(Polyline)tr.GetObject(Id(db,Templates[c-1]),OpenMode.ForRead);
                    Polyline result=null;double offset=0;
                    if(n==3||n==4) {
                        using(var oriented=(Polyline)source.Clone()) {
                            // The yellow source of option 3 is stored destination-to-plant.
                            if(n==3&&c==2)oriented.ReverseCurve();
                            offset=Offsets[c-1];
                            var pieces=oriented.GetOffsetCurves(offset);
                            if(pieces.Count!=1||!(pieces[0] is Polyline)) {
                                foreach(DBObject obj in pieces)obj.Dispose();
                                throw new InvalidOperationException("Offset did not produce one polyline: "+source.Handle);
                            }
                            result=(Polyline)pieces[0];
                        }
                    } else result=(Polyline)source.Clone();
                    result.SetPropertiesFrom(template);
                    result.LayerId=layerIds[n];result.ConstantWidth=15;
                    model.AppendEntity(result);tr.AddNewlyCreatedDBObject(result,true);
                    records.Add(new{option=n,color=c,source=source.Handle.ToString(),template=template.Handle.ToString(),created=result.Handle.ToString(),sourceLength=source.Length,displayLength=result.Length,offset=offset,width=result.ConstantWidth,linetype=result.Linetype,ltscale=result.LinetypeScale});
                }
                // Original alternate layers stay hidden; the new layers are controlled per viewport.
                foreach(ObjectId lid in lt) {
                    var l=(LayerTableRecord)tr.GetObject(lid,OpenMode.ForRead);
                    if(l.Name.Contains("Planta de pro")&&!l.Name.Contains("op. 1)")) {
                        l.UpgradeOpen();l.IsOff=true;
                    }
                }
                var originals=new List<ObjectId>();
                foreach(ObjectId lid in lt) {
                    var l=(LayerTableRecord)tr.GetObject(lid,OpenMode.ForRead);
                    if(l.Name.Contains("Planta de pro"))originals.Add(lid);
                }
                var layouts=(DBDictionary)tr.GetObject(db.LayoutDictionaryId,OpenMode.ForRead);
                foreach(DBDictionaryEntry entry in layouts) {
                    var layout=(Layout)tr.GetObject(entry.Value,OpenMode.ForRead);
                    int n=Option(layout.LayoutName);if(n<1||n>5)continue;
                    var paper=(BlockTableRecord)tr.GetObject(layout.BlockTableRecordId,OpenMode.ForRead);
                    foreach(ObjectId eid in paper) {
                        var vp=tr.GetObject(eid,OpenMode.ForRead) as Viewport;if(vp==null||Math.Abs(vp.CustomScale-0.05)>0.00001)continue;
                        vp.UpgradeOpen();
                        var freeze=new ObjectIdCollection(layerIds.Where(kv=>kv.Key!=n).Select(kv=>kv.Value).ToArray());
                        if(n!=1)foreach(var lid in originals)freeze.Add(lid);
                        vp.FreezeLayersInViewport(freeze.GetEnumerator());
                        if(n!=1)vp.ThawLayersInViewport(new ObjectIdCollection(new[]{layerIds[n]}).GetEnumerator());
                        vp.Locked=true;
                    }
                }
                tr.Commit();
            }
            Write("preparation.json",records);
            d.Editor.Regen();d.Editor.WriteMessage("\nDFPREPARAR OK: 24 display polylines, 4 isolated layouts.\n");
        }catch(System.Exception ex){Write("prepare-error.json",ex.ToString());}
    }
    [CommandMethod("DFVALIDAR3",CommandFlags.Modal)]
    public void Validate(){
        try {
            var d=Work();var db=d.Database;var entities=new List<object>();var viewports=new List<object>();
            using(var tr=db.TransactionManager.StartTransaction()) {
                var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead);
                foreach(ObjectId id in (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace],OpenMode.ForRead)) {
                    var p=tr.GetObject(id,OpenMode.ForRead) as Polyline;if(p==null||!p.Layer.StartsWith("FIG_OPCION_"))continue;
                    var coordinates=new List<double>();
                    for(int i=0;i<p.NumberOfVertices;i++){var pt=p.GetPoint2dAt(i);coordinates.Add(pt.X);coordinates.Add(pt.Y);}
                    entities.Add(new{handle=p.Handle.ToString(),layer=p.Layer,color=p.ColorIndex,width=p.ConstantWidth,linetype=p.Linetype,length=p.Length,ltscale=p.LinetypeScale,coordinates=coordinates});
                }
                var dict=(DBDictionary)tr.GetObject(db.LayoutDictionaryId,OpenMode.ForRead);
                foreach(DBDictionaryEntry de in dict) {
                    var l=(Layout)tr.GetObject(de.Value,OpenMode.ForRead);int n=Option(l.LayoutName);if(n==0)continue;
                    foreach(ObjectId id in (BlockTableRecord)tr.GetObject(l.BlockTableRecordId,OpenMode.ForRead)) {
                        var v=tr.GetObject(id,OpenMode.ForRead) as Viewport;if(v==null||Math.Abs(v.CustomScale-0.05)>0.00001)continue;
                        var frozen=new List<string>();foreach(ObjectId f in v.GetFrozenLayers())frozen.Add(((LayerTableRecord)tr.GetObject(f,OpenMode.ForRead)).Name);
                        viewports.Add(new{layout=l.LayoutName,on=v.On,locked=v.Locked,scale=v.CustomScale,frozen=frozen});
                    }
                }
            }
            Write("validation.json",new{entities=entities,viewports=viewports});d.Editor.WriteMessage("\nDFVALIDAR OK\n");
        }catch(System.Exception ex){Write("validate-error.json",ex.ToString());}
    }
}
