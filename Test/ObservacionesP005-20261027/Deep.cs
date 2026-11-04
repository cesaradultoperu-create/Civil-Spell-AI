using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using Entity=Autodesk.AutoCAD.DatabaseServices.Entity;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;
public class Deep {
 public const string Root=@"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI\Test\ObservacionesP005-20261027";
 public static double[] P(Point3d p){return new[]{p.X,p.Y,p.Z};}
 public static string Txt(Entity e){if(e is DBText t)return t.TextString;if(e is MText m)return m.Text;if(e is MLeader l&&l.ContentType==ContentType.MTextContent){using(var mt=l.MText)return mt.Text;}return "";}
 [CommandMethod("P005DEEP")]
 public void Run(){try{Dump(App.DocumentManager.MdiActiveDocument.Database,Path.Combine(Root,Path.GetFileNameWithoutExtension(App.DocumentManager.MdiActiveDocument.Name)+"-deep.json"));}catch(System.Exception ex){File.WriteAllText(Path.Combine(Root,"deep-error.txt"),ex.ToString());}}
 public static void Dump(Database db,string path){var rows=new List<object>();using(var tr=db.TransactionManager.StartTransaction()){
 var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead);
 foreach(ObjectId bid in bt){var b=(BlockTableRecord)tr.GetObject(bid,OpenMode.ForRead); if(b.IsFromExternalReference){rows.Add(new{space=b.Name,xref=b.PathName,unloaded=b.IsUnloaded});continue;}
 if(!b.IsLayout && b.Name.StartsWith("*"))continue; foreach(ObjectId id in b){File.WriteAllText(Path.Combine(Root,"deep-progress.txt"),b.Name+" "+id.Handle);var e=tr.GetObject(id,OpenMode.ForRead) as Entity;if(e==null)continue;var r=new Dictionary<string,object>{{"space",b.Name},{"h",e.Handle.ToString()},{"type",e.GetType().Name},{"layer",e.Layer},{"color",e.Color.ToString()},{"text",Txt(e)}};try{r["min"]=P(e.GeometricExtents.MinPoint);r["max"]=P(e.GeometricExtents.MaxPoint);}catch{}
 if(e is DBText dt){r["pos"]=P(dt.Position);r["height"]=dt.Height;}
 if(e is MText mt){r["pos"]=P(mt.Location);r["height"]=mt.TextHeight;r["contents"]=mt.Contents;}
 if(e is BlockReference br){r["block"]=((BlockTableRecord)tr.GetObject(br.BlockTableRecord,OpenMode.ForRead)).Name;r["pos"]=P(br.Position);r["scale"]=br.ScaleFactors.ToString();r["rotation"]=br.Rotation;r["attrs"]=br.AttributeCollection.Cast<ObjectId>().Select(a=>(AttributeReference)tr.GetObject(a,OpenMode.ForRead)).ToDictionary(a=>a.Tag,a=>a.TextString);}
 if(e is Line ln){r["start"]=P(ln.StartPoint);r["end"]=P(ln.EndPoint);}
 if(e is Polyline pl){r["vertices"]=Enumerable.Range(0,pl.NumberOfVertices).Select(i=>P(pl.GetPoint3dAt(i))).ToArray();r["closed"]=pl.Closed;}
 if(e is MLeader ml){r["leaders"]=ml.GetLeaderIndexes().Cast<int>().SelectMany(i=>ml.GetLeaderLineIndexes(i).Cast<int>()).Select(i=>new{first=P(ml.GetFirstVertex(i)),last=P(ml.GetLastVertex(i))}).ToArray();}
 if(e is Dimension dim){r["dimtext"]=dim.DimensionText;r["measurement"]=dim.Measurement;}
 if(e is Autodesk.Civil.DatabaseServices.Entity ce){try{r["name"]=ce.Name;}catch{}try{r["style"]=ce.StyleName;r["stylehandle"]=ce.StyleId.Handle.ToString();}catch{}}
 if(e is Profile pr){r["stations"]=new[]{pr.StartingStation,pr.EndingStation};r["profileType"]=pr.ProfileType.ToString();r["samples"]=Enumerable.Range(0,171).Select(i=>{double s=pr.StartingStation+(pr.EndingStation-pr.StartingStation)*i/170;try{return new[]{s,pr.ElevationAt(s)};}catch{return new[]{s,double.NaN};}}).ToArray();}
 if(e is Alignment al){r["length"]=al.Length;r["alignmentpoints"]=Enumerable.Range(0,171).Select(i=>{double s=al.StartingStation+al.Length*i/170;double x=0,y=0;try{al.PointLocation(s,0,ref x,ref y);}catch{}return new[]{s,x,y};}).ToArray();}
 if(e is ProfileView pv){r["location"]=P(pv.Location);r["range"]=new[]{pv.StationStart,pv.StationEnd,pv.ElevationMin,pv.ElevationMax};}
 if(e is RasterImage im){r["imageDef"]=im.ImageDefId.Handle.ToString();r["orientation"]=im.Orientation.ToString();}
 rows.Add(r);
 }}
 var imageId=RasterImageDef.GetImageDictionary(db);if(!imageId.IsNull)foreach(DBDictionaryEntry en in (DBDictionary)tr.GetObject(imageId,OpenMode.ForRead)){var im=tr.GetObject(en.Value,OpenMode.ForRead) as RasterImageDef;if(im!=null)rows.Add(new{imageName=en.Key,file=im.SourceFileName,h=im.Handle.ToString()});}
 var lt=(LayerTable)tr.GetObject(db.LayerTableId,OpenMode.ForRead);foreach(ObjectId id in lt){var l=(LayerTableRecord)tr.GetObject(id,OpenMode.ForRead);rows.Add(new{layerName=l.Name,h=l.Handle.ToString(),color=l.Color.ToString(),off=l.IsOff,frozen=l.IsFrozen,locked=l.IsLocked,plot=l.IsPlottable});}
 }
 File.WriteAllText(path,new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Serialize(rows));}
}




