using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using App = Autodesk.AutoCAD.ApplicationServices.Core.Application;
public class PipeInspect
{
    [CommandMethod("AUDITPIPEVIEW")]
    public void AuditPipeView()
    {
        var doc=App.DocumentManager.MdiActiveDocument;
        using(var tr=doc.Database.TransactionManager.StartOpenCloseTransaction())
        {
            var layers=(LayerTable)tr.GetObject(doc.Database.LayerTableId,OpenMode.ForRead);
            var layer=(LayerTableRecord)tr.GetObject(layers["REFERENCIAS_TOP_FRONT_2D"],OpenMode.ForRead);
            if(!layer.IsFrozen)throw new InvalidOperationException("La referencia 2D no está separada de la vista 3D.");
            foreach(var h in new[]{"7092","7093","7094","7095","7351","7352"})
            {
                var e=(Entity)tr.GetObject(doc.Database.GetObjectId(false,new Handle(Convert.ToInt64(h,16)),0),OpenMode.ForRead);
                if(e.Layer!=layer.Name)throw new InvalidOperationException("Falta una referencia original.");
            }
            var views=(ViewTable)tr.GetObject(doc.Database.ViewTableId,OpenMode.ForRead);
            foreach(var name in new[]{"MODELO_TUBERIA_3D","REFERENCIAS_TOP_FRONT"})
            {
                var v=(ViewTableRecord)tr.GetObject(views[name],OpenMode.ForRead);
                var style=(DBVisualStyle)tr.GetObject(v.VisualStyleId,OpenMode.ForRead);
                doc.Editor.WriteMessage("\nPIPE_SAVED_VIEW {0} style={1} layers={2}",name,style.Type,v.LayerState);
            }
            var table=(BlockTable)tr.GetObject(doc.Database.BlockTableId,OpenMode.ForRead);
            var space=(BlockTableRecord)tr.GetObject(table[BlockTableRecord.ModelSpace],OpenMode.ForRead);
            var block=space.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead) as BlockReference).Single(b=>b!=null && b.Name.StartsWith("CIVILSPELLAI_REFERENCIAS_3D_"));
            var def=(BlockTableRecord)tr.GetObject(block.BlockTableRecord,OpenMode.ForRead);
            var solids=def.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead) as Solid3d).ToArray();
            if(solids.Length!=5 || solids.Any(s=>s==null || s.MassProperties.Volume<=0))throw new InvalidOperationException("Se modificó el sólido.");
            doc.Editor.WriteMessage("\nPIPE_CLEAN_SAVED_PASS refs=6 solids=5 position={0}",block.Position);
        }
    }
    [CommandMethod("NORMALIZEPIPE2D")]
    public void NormalizePipe2D()
    {
        var doc=App.DocumentManager.MdiActiveDocument;
        using(var tr=doc.Database.TransactionManager.StartTransaction())
        {
            var table=(BlockTable)tr.GetObject(doc.Database.BlockTableId,OpenMode.ForRead);int count=0;
            foreach(ObjectId bid in table)
            {
                var block=(BlockTableRecord)tr.GetObject(bid,OpenMode.ForRead);
                if(!new[]{"DF_CORRUGADO_300_ORIGINAL","DF_TAPA_HDPE_300_V2","T","DF_CODO_MEJORADO_V2","TAPA DE HDPE"}.Contains(block.Name))continue;
                foreach(var id in block.Cast<ObjectId>().ToArray())
                {
                    var line=tr.GetObject(id,OpenMode.ForRead) as Line;
                    if(line==null || Math.Abs(line.StartPoint.Z-line.EndPoint.Z)>1e-9 || Math.Abs(line.Thickness)>1e-9)continue;
                    var next=new Polyline(2);next.SetPropertiesFrom(line);next.Elevation=line.StartPoint.Z;
                    next.AddVertexAt(0,new Point2d(line.StartPoint.X,line.StartPoint.Y),0,0,0);
                    next.AddVertexAt(1,new Point2d(line.EndPoint.X,line.EndPoint.Y),0,0,0);
                    block.UpgradeOpen();block.AppendEntity(next);tr.AddNewlyCreatedDBObject(next,true);
                    line.UpgradeOpen();line.Erase();count++;
                }
            }
            tr.Commit();doc.Editor.WriteMessage("\nPIPE_NORMALIZED_2D {0}",count);
        }
        doc.Editor.Regen();
    }
    [CommandMethod("LOCALPIPEVIEW")]
    public void LocalPipe()
    {
        var doc=App.DocumentManager.MdiActiveDocument;
        using(var tr=doc.Database.TransactionManager.StartTransaction())
        {
            var table=(BlockTable)tr.GetObject(doc.Database.BlockTableId,OpenMode.ForRead);
            var space=(BlockTableRecord)tr.GetObject(table[BlockTableRecord.ModelSpace],OpenMode.ForRead);
            var move=Matrix3d.Displacement(new Vector3d(-579168,-8297887,0));
            foreach(ObjectId id in space)((Entity)tr.GetObject(id,OpenMode.ForWrite)).TransformBy(move);
            tr.Commit();
        }
        using(var view=doc.Editor.GetCurrentView())
        {
            view.Target=new Point3d(3,0,1.5);view.CenterPoint=Point2d.Origin;
            view.ViewDirection=new Vector3d(1,-1,1);view.Width=9;view.Height=7;
            doc.Editor.SetCurrentView(view);
        }
        doc.Editor.Regen();doc.Editor.WriteMessage("\nPIPE_LOCAL_VIEW_PASS");
    }
    [CommandMethod("DUMPPIPESTYLE")]
    public void DumpStyle()
    {
        var doc=App.DocumentManager.MdiActiveDocument;
        foreach(var name in new[]{"VSEDGELEX","VSEDGEOVERHANG","VSEDGEJITTER","VSEDGES","VSSILHEDGES","ISOLINES","DISPSILH","VSFACESTYLE"})
            try{doc.Editor.WriteMessage("\nSTYLE_VAR {0}={1}",name,App.GetSystemVariable(name));}catch{}
        using(var tr=doc.Database.TransactionManager.StartOpenCloseTransaction())
        {
            var lt=(LayerTable)tr.GetObject(doc.Database.LayerTableId,OpenMode.ForRead);
            foreach(ObjectId id in lt)
            {
                var layer=(LayerTableRecord)tr.GetObject(id,OpenMode.ForRead);
                if(!layer.Name.StartsWith("DF_"))continue;
                var lineType=(LinetypeTableRecord)tr.GetObject(layer.LinetypeObjectId,OpenMode.ForRead);
                doc.Editor.WriteMessage("\nLINE_LAYER {0} TYPE={1}",layer.Name,lineType.Name);
            }
            var bt=(BlockTable)tr.GetObject(doc.Database.BlockTableId,OpenMode.ForRead);
            foreach(ObjectId bid in bt)
            {
                var b=(BlockTableRecord)tr.GetObject(bid,OpenMode.ForRead);
                if(b.Name!="DF_CORRUGADO_300_ORIGINAL")continue;
                foreach(ObjectId id in b)
                {
                    var e=(Entity)tr.GetObject(id,OpenMode.ForRead);
                    var line=e as Line;var polyline=e as Polyline;
                    if(line!=null)doc.Editor.WriteMessage("\nLINE_DETAIL {0} {1} {2} normal={3} thick={4:R} LT={5} scale={6:R}",line.Handle,line.StartPoint,line.EndPoint,line.Normal,line.Thickness,line.Linetype,line.LinetypeScale);
                    if(polyline!=null)doc.Editor.WriteMessage("\nPOLYLINE_DETAIL {0} normal={1} thick={2:R} LT={3} scale={4:R}",polyline.Handle,polyline.Normal,polyline.Thickness,polyline.Linetype,polyline.LinetypeScale);
                }
            }
            foreach(var type in typeof(DBVisualStyle).Assembly.GetTypes().Where(t=>t.IsEnum && t.Name.Contains("Edge") && (t.Name.Contains("Modif") || t.Name.Contains("Style"))))
                doc.Editor.WriteMessage("\nEDGE_ENUM {0}: {1}",type.Name,string.Join(",",Enum.GetNames(type).Select(n=>n+"="+Convert.ToInt32(Enum.Parse(type,n)))));
            var dict=(DBDictionary)tr.GetObject(doc.Database.VisualStyleDictionaryId,OpenMode.ForRead);
            foreach(DBDictionaryEntry entry in dict)
            {
                if(entry.Key!="Conceptual" && entry.Key!="Realistic" && entry.Key!="Shaded")continue;
                var style=(DBVisualStyle)tr.GetObject(entry.Value,OpenMode.ForRead);
                doc.Editor.WriteMessage("\nSTYLE_DEF {0} {1}",entry.Key,style.Type);
                foreach(Autodesk.AutoCAD.GraphicsInterface.VisualStyleProperty property in Enum.GetValues(typeof(Autodesk.AutoCAD.GraphicsInterface.VisualStyleProperty)))
                    if(property.ToString().StartsWith("Edge"))
                        try{var value=style.GetTrait(property);doc.Editor.WriteMessage("\nSTYLE_TRAIT {0} {1}={2} TYPE={3}",entry.Key,property,value,value==null?"null":value.GetType().Name);}catch{}
            }
            using(var view=doc.Editor.GetCurrentView())doc.Editor.WriteMessage("\nSTYLE_VIEW {0} direction={1} target={2}",view.VisualStyleId,view.ViewDirection,view.Target);
        }
    }
    [CommandMethod("VERIFYINSTALLEDPIPE")]
    public void VerifyInstalled()
    {
        var doc=App.DocumentManager.MdiActiveDocument;
        try
        {
            var assembly=System.Reflection.Assembly.LoadFrom(@"C:\Program Files\Autodesk\ApplicationPlugins\CivilSpellAI.bundle\Contents\Windows\2024\CivilSpellAI.dll");
            using(var tr=doc.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var serviceType=assembly.GetType("CivilSpellAI.Autodesk.SelectedFigureDrawingService");
                var service=Activator.CreateInstance(serviceType);
                var snapshots=new List<object>();
                foreach(var handles in new[]{new[]{"7092","7093","7094","7095"},new[]{"7351","7352"}})
                {
                    var ids=handles.Select(h=>doc.Database.GetObjectId(false,new Handle(Convert.ToInt64(h,16)),0)).ToArray();
                    var args=new object[]{ids,tr,null,null};
                    if(!(bool)serviceType.GetMethod("TryRead").Invoke(service,args))throw new InvalidOperationException((string)args[3]);
                    snapshots.Add(args[2]);
                }
                var analyzer=assembly.GetType("CivilSpellAI.Autodesk.CorrugatedPipe3DAnalyzer");
                var analysisArgs=new object[]{snapshots[0],snapshots[1],tr,null};
                if(!(bool)analyzer.GetMethod("TryAnalyze").Invoke(null,analysisArgs))throw new InvalidOperationException("La DLL instalada no detecta la tubería.");
                var drawingType=assembly.GetType("CivilSpellAI.Autodesk.ReferenceAssemblyDrawingService");
                var bodies=(System.Collections.IList)drawingType.GetMethod("Build").Invoke(Activator.CreateInstance(drawingType),new[]{analysisArgs[3]});
                try
                {
                    if(bodies.Count!=5)throw new InvalidOperationException("Se esperaban cinco sólidos.");
                    foreach(Solid3d body in bodies)if(body.MassProperties.Volume<=0)throw new InvalidOperationException("Sólido vacío.");
                    var routes=(System.Collections.IList)analysisArgs[3].GetType().GetField("PipeRoutes").GetValue(analysisArgs[3]);
                    var branch=routes[1];var type=branch.GetType();
                    var profile=type.GetField("CorrugationProfile").GetValue(branch);
                    if(profile==null)throw new InvalidOperationException("La DLL instalada todavía genera una envolvente lisa.");
                    var px=(double[])profile.GetType().GetField("X").GetValue(profile);var py=(double[])profile.GetType().GetField("Y").GetValue(profile);
                    int crest=Array.IndexOf(py,py.Max()),valley=Array.IndexOf(py,py.Min());
                    var bx=(double[])type.GetField("X").GetValue(branch);var by=(double[])type.GetField("Y").GetValue(branch);var bz=(double[])type.GetField("Z").GetValue(branch);
                    foreach(var sample in new[]{Tuple.Create((px[crest]+px[crest+1])/2,true),Tuple.Create(px[valley],false)})
                        using(var sphere=new Solid3d())
                        {
                            sphere.CreateSphere(.0008);sphere.TransformBy(Matrix3d.Displacement(new Vector3d(bx[0]+.145,by[0]+sample.Item1,bz[0])));
                            if(((Solid3d)bodies[1]).CheckInterference(sphere)!=sample.Item2)throw new InvalidOperationException("Falta relieve físico en el sólido instalado.");
                        }
                }
                finally {foreach(IDisposable body in bodies)body.Dispose();}
                var command=assembly.GetType("CivilSpellAI.Commands.Figure3DCommands").GetMethod("CreateFigure3D");
                if(!command.GetCustomAttributes(typeof(CommandMethodAttribute),false).Cast<CommandMethodAttribute>().Any(a=>a.GlobalName=="CREARFIGURA3D"))
                    throw new InvalidOperationException("No está registrado CREARFIGURA3D.");
            }
            doc.Editor.WriteMessage("\nPIPE300_INSTALLED_RELEASE_PASS");
        }
        catch(System.Exception ex){doc.Editor.WriteMessage("\nPIPE300_INSTALLED_RELEASE_FAIL {0}",ex);}
    }
    [CommandMethod("PROBEPIPESELECTION")]
    public void Probe()
    {
        var doc=App.DocumentManager.MdiActiveDocument;
        var asm=System.Reflection.Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(typeof(PipeInspect).Assembly.Location),"../../../plugin/CivilSpellAI.dll"));
        using(var tr=doc.Database.TransactionManager.StartTransaction())
        foreach(var handles in new[]{new[]{"7092","7093","7094","7095"},new[]{"7351","7352"}})
        {
            try {
                var ids=handles.Select(h=>doc.Database.GetObjectId(false,new Handle(Convert.ToInt64(h,16)),0)).ToArray();
                var type=asm.GetType("CivilSpellAI.Autodesk.SelectedFigureDrawingService");
                var args=new object[]{ids,tr,null,null};
                var ok=type.GetMethod("TryRead").Invoke(Activator.CreateInstance(type),args);
                doc.Editor.WriteMessage("\nPIPE_SELECTION {0}: {1} {2}",handles[0],ok,args[3]);
                var reader=asm.GetType("CivilSpellAI.Autodesk.ReferenceProfileReader");
                var profiles=(System.Collections.IList)reader.GetMethod("Read").Invoke(null,new object[]{ids,tr});
                doc.Editor.WriteMessage("\nPIPE_PROFILES {0}: {1}",handles[0],profiles.Count);
            } catch(System.Exception ex) {doc.Editor.WriteMessage("\nPIPE_SELECTION_FAIL {0}",ex);}
        }
    }
    static double[] P(Point3d p) { return new[]{p.X,p.Y,p.Z}; }
    [CommandMethod("DUMPPIPEDETAIL")]
    public void Dump()
    {
        var db=App.DocumentManager.MdiActiveDocument.Database;
        var rows=new List<object>();
        using(var tr=db.TransactionManager.StartTransaction())
        {
            var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead);
            foreach(ObjectId bid in bt)
            {
                var b=(BlockTableRecord)tr.GetObject(bid,OpenMode.ForRead);
                var ents=new List<object>();
                foreach(ObjectId id in b)
                {
                    var e=tr.GetObject(id,OpenMode.ForRead) as Entity; if(e==null)continue;
                    var d=new Dictionary<string,object>{{"handle",e.Handle.ToString()},{"type",e.GetRXClass().DxfName},{"layer",e.Layer},{"color",e.ColorIndex},{"linetype",e.Linetype}};
                    try {var ex=e.GeometricExtents;d["min"]=P(ex.MinPoint);d["max"]=P(ex.MaxPoint);}catch{}
                    var br=e as BlockReference;
                    if(br!=null){d["block"]=((BlockTableRecord)tr.GetObject(br.BlockTableRecord,OpenMode.ForRead)).Name;d["pos"]=P(br.Position);d["rotation"]=br.Rotation;d["scale"]=new[]{br.ScaleFactors.X,br.ScaleFactors.Y,br.ScaleFactors.Z};}
                    var pl=e as Polyline;
                    if(pl!=null){d["closed"]=pl.Closed;d["points"]=Enumerable.Range(0,pl.NumberOfVertices).Select(i=>P(pl.GetPoint3dAt(i))).ToArray();d["bulges"]=Enumerable.Range(0,pl.NumberOfVertices).Select(i=>pl.GetBulgeAt(i)).ToArray();}
                    var line=e as Line;if(line!=null)d["points"]=new[]{P(line.StartPoint),P(line.EndPoint)};
                    var arc=e as Arc;if(arc!=null){d["center"]=P(arc.Center);d["radius"]=arc.Radius;d["start"]=arc.StartAngle;d["end"]=arc.EndAngle;}
                    var c=e as Circle;if(c!=null){d["center"]=P(c.Center);d["radius"]=c.Radius;}
                    var text=e as DBText;if(text!=null)d["text"]=text.TextString;
                    var mt=e as MText;if(mt!=null)d["text"]=mt.Contents;
                    var hatch=e as Hatch;if(hatch!=null)d["pattern"]=hatch.PatternName;
                    if(e is Solid3d && (!b.Name.StartsWith("CIVILSPELLAI_REFERENCIAS_3D_") || b.GetBlockReferenceIds(true,false).Count>0))
                    {
                        var mesh=SubDMesh.GetObjectMesh(e,new MeshFaceterData{FaceterMeshType=2,FaceterDevSurface=.001,FaceterDevNormal=10});
                        d["vertices"]=mesh.VertexArray.Cast<Point3d>().Select(P).ToArray();
                        d["faces"]=mesh.FaceArray.Cast<int>().ToArray();
                    }
                    ents.Add(d);
                }
                if(ents.Count>0)rows.Add(new{block=b.Name,entities=ents});
            }
        }
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(db.Filename),"geometry.json"),new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Serialize(rows));
        App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nPIPE_DETAIL_OK");
    }
}
