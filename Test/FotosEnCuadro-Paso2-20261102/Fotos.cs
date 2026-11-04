using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;
public class FotosEnCuadro {
 public const string Root=@"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI\Test\FotosEnCuadro-Paso2-20261102";
 static double[] P(Point3d p){return new[]{p.X,p.Y,p.Z};}
 static double[] V(Vector3d v){return new[]{v.X,v.Y,v.Z};}
 public class Placement {
  public string file {get;set;} public string label {get;set;}
  public int width {get;set;} public int height {get;set;}
  public double screenshotScale {get;set;}
  public double step2ScaleFactor {get;set;}
  public double[] sourceLeft {get;set;} public double[] sourceRight {get;set;}
  public double[] destinationLeft {get;set;} public double[] destinationRight {get;set;}
 }
 static Placement[] ReadPlan(){return new JavaScriptSerializer().Deserialize<Placement[]>(File.ReadAllText(Path.Combine(Root,"plan.json")));}
 static CoordinateSystem3d Aligned(Placement p){
  double sx=p.sourceRight[0]-p.sourceLeft[0],sy=p.sourceRight[1]-p.sourceLeft[1];
  double dx=p.destinationRight[0]-p.destinationLeft[0],dy=p.destinationRight[1]-p.destinationLeft[1];
  double n=sx*sx+sy*sy;if(n<1)throw new InvalidOperationException("Guia degenerada");
  double a=(dx*sx+dy*sy)/n,b=(dy*sx-dx*sy)/n;
  var u=new Vector3d(a*p.width,b*p.width,0);var v=new Vector3d(-b*p.height,a*p.height,0);
  var origin=new Point3d(p.destinationLeft[0]-a*p.sourceLeft[0]+b*p.sourceLeft[1],p.destinationLeft[1]-b*p.sourceLeft[0]-a*p.sourceLeft[1],0);
  return new CoordinateSystem3d(origin,u,v);
 }
 [CommandMethod("FOTOSBUILD")]
 public void Build(){string stage="inicio";try{
  File.Delete(Path.Combine(Root,"build-error.txt"));
  var doc=App.DocumentManager.MdiActiveDocument;var db=doc.Database;
  if(!string.Equals(Path.GetFullPath(doc.Name),Path.Combine(Root,"Entrega","preparacion.dwg"),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Solo se puede modificar la copia de entrega.");
  Directory.SetCurrentDirectory(Path.Combine(Root,"Entrega"));var plan=ReadPlan();var images=new ObjectIdCollection();
  using(var tr=db.TransactionManager.StartTransaction()){
   var ms=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForWrite);
   stage="buscar imagen inicial";var existing=ms.Cast<ObjectId>().Where(id=>!id.IsErased&&tr.GetObject(id,OpenMode.ForRead) is RasterImage).ToArray();
   if(existing.Length!=1)throw new InvalidOperationException("Se esperaba una imagen inicial.");
   var layers=(LayerTable)tr.GetObject(db.LayerTableId,OpenMode.ForRead);ObjectId layer;
   if(layers.Has("FOTOS_ALINEADAS"))layer=layers["FOTOS_ALINEADAS"];
   else {layers.UpgradeOpen();var l=new LayerTableRecord{Name="FOTOS_ALINEADAS"};layer=layers.Add(l);tr.AddNewlyCreatedDBObject(l,true);}
   stage="diccionario";if(RasterImageDef.GetImageDictionary(db).IsNull)RasterImageDef.CreateImageDictionary(db);var dict=(DBDictionary)tr.GetObject(RasterImageDef.GetImageDictionary(db),OpenMode.ForWrite);
   RasterImage.EnableReactors(true);
   foreach(var p in plan){
    stage="crear "+p.file;var absolute=Path.Combine(Root,"Entrega","fotos",p.file);if(!File.Exists(absolute))throw new FileNotFoundException(absolute);
    RasterImage im;RasterImageDef def;ObjectId defId;
    if(p.file=="0.png"){
     im=(RasterImage)tr.GetObject(existing[0],OpenMode.ForWrite);defId=im.ImageDefId;def=(RasterImageDef)tr.GetObject(defId,OpenMode.ForWrite);
    }else{
     def=new RasterImageDef();defId=dict.SetAt("FOTO_"+Path.GetFileNameWithoutExtension(p.file),def);tr.AddNewlyCreatedDBObject(def,true);
     im=new RasterImage();im.SetDatabaseDefaults(db);im.ImageDefId=defId;ms.AppendEntity(im);tr.AddNewlyCreatedDBObject(im,true);
    }
    stage="cargar "+p.file;def.SourceFileName=absolute;def.Load();
    if(Math.Abs(def.Size.X-p.width)>.01||Math.Abs(def.Size.Y-p.height)>.01)throw new InvalidOperationException("Dimensiones distintas: "+p.file);
    stage="alinear "+p.file;im.Orientation=Aligned(p);im.LayerId=layer;im.ShowImage=true;im.IsClipped=false;im.AssociateRasterDef(def);
    def.SourceFileName=@".\fotos\"+p.file;
    images.Add(im.ObjectId);
   }
   stage="orden de dibujo";var order=(DrawOrderTable)tr.GetObject(ms.DrawOrderTableId,OpenMode.ForWrite);order.MoveToBottom(images);
   db.TileMode=true;tr.Commit();
  }
  using(var view=doc.Editor.GetCurrentView()){
   view.ViewDirection=Vector3d.ZAxis;view.Target=Point3d.Origin;view.ViewTwist=0;view.CenterPoint=new Point2d(50.2,6.3);view.Width=24;view.Height=16.5;doc.Editor.SetCurrentView(view);
  }
  stage="guardar";var output=Path.Combine(Root,"Entrega","modelo_fotos_paso2.dwg");db.SaveAs(output,DwgVersion.Current);
  using(var check=new Database(false,true)){check.ReadDwgFile(output,FileOpenMode.OpenForReadAndAllShare,true,null);check.CloseInput(true);Dump(check,Path.Combine(Root,"resultado.json"));Verify(check,plan);}
  doc.Editor.WriteMessage("\nFOTOS_BUILD_OK: "+images.Count+" fotos\n");
 }catch(System.Exception ex){File.WriteAllText(Path.Combine(Root,"build-error.txt"),stage+"\n"+ex.ToString());}}
 static void Verify(Database db,Placement[] plan){
  using(var tr=db.TransactionManager.StartTransaction()){
   var ms=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForRead);
   var all=ms.Cast<ObjectId>().ToArray();var images=all.Where(id=>tr.GetObject(id,OpenMode.ForRead) is RasterImage).ToArray();
   if(images.Length!=plan.Length)throw new InvalidOperationException("Numero de imagenes incorrecto");
   var seen=new HashSet<string>();double maxError=0;var reference=Aligned(plan.Single(p=>p.file=="0.png"));
   var rows=new List<string>{"foto\tcuadro\tgiro_grados\tancho\talto\terror_alineacion\truta_relativa\tdetras"};
   var order=(DrawOrderTable)tr.GetObject(ms.DrawOrderTableId,OpenMode.ForRead);
   foreach(var id in images){
    var im=(RasterImage)tr.GetObject(id,OpenMode.ForRead);var def=(RasterImageDef)tr.GetObject(im.ImageDefId,OpenMode.ForRead);var filename=Path.GetFileName(def.SourceFileName);
    if(!seen.Add(filename))throw new InvalidOperationException("Imagen repetida: "+filename);
    var p=plan.Single(x=>x.file==filename);var o=im.Orientation;
    foreach(var side in new[]{0,1}){
     var source=side==0?p.sourceLeft:p.sourceRight;var target=side==0?p.destinationLeft:p.destinationRight;
     var mapped=o.Origin+o.Xaxis*(source[0]/p.width)+o.Yaxis*(source[1]/p.height);maxError=Math.Max(maxError,mapped.DistanceTo(new Point3d(target[0],target[1],0)));
    }
    if(Path.IsPathRooted(def.SourceFileName)||!File.Exists(Path.Combine(Path.GetDirectoryName(db.Filename),def.SourceFileName)))throw new InvalidOperationException("Referencia no portatil: "+filename);
    def.UpgradeOpen();def.Load();if(!def.IsLoaded)throw new InvalidOperationException("AutoCAD no pudo cargar: "+filename);
    if(im.IsClipped)throw new InvalidOperationException("Recorte inesperado");
    if((o.Xaxis.GetNormal()-reference.Xaxis.GetNormal()).Length>1e-10||(o.Yaxis.GetNormal()-reference.Yaxis.GetNormal()).Length>1e-10)throw new InvalidOperationException("Giro diferente de 0.00: "+filename);
    if(Math.Abs(o.Xaxis.Length*p.screenshotScale-reference.Xaxis.Length*p.step2ScaleFactor)>1e-10||Math.Abs(o.Yaxis.Length*p.screenshotScale-reference.Yaxis.Length*p.step2ScaleFactor)>1e-10)throw new InvalidOperationException("Escala A--A1 incorrecta: "+filename);
    if(Math.Abs(o.Xaxis.Length/p.width-o.Yaxis.Length/p.height)>1e-10||Math.Abs(o.Xaxis.DotProduct(o.Yaxis))>1e-10)throw new InvalidOperationException("Escala no uniforme");
    foreach(var other in all.Except(images))if(!order.FirstEntityIsDrawnBeforeSecond(id,other))throw new InvalidOperationException("Imagen delante de geometria: "+filename);
    double degrees=(Math.Atan2(o.Xaxis.Y,o.Xaxis.X)*180/Math.PI+360)%360;
    rows.Add(filename+"\t"+p.label+"\t"+degrees.ToString("F12",System.Globalization.CultureInfo.InvariantCulture)+"\t"+o.Xaxis.Length.ToString("F12",System.Globalization.CultureInfo.InvariantCulture)+"\t"+o.Yaxis.Length.ToString("F12",System.Globalization.CultureInfo.InvariantCulture)+"\t"+maxError.ToString("G17",System.Globalization.CultureInfo.InvariantCulture)+"\t"+def.SourceFileName+"\tsi");
   }
   if(maxError>1e-8)throw new InvalidOperationException("Error de alineacion");
   File.WriteAllLines(Path.Combine(Root,"verificacion.tsv"),rows);
   File.WriteAllText(Path.Combine(Root,"verification-ok.txt"),images.Length+" images; same rotation as 0.00; A--A1 aligned to each vertical marker; screenshot zoom compensated; relative references exist; uniform scaling; images behind geometry; max endpoint error="+maxError.ToString("G17"));tr.Commit();
  }
 }
 [CommandMethod("FOTOSVERIFY")]
 public void ReopenVerify(){try{
  var doc=App.DocumentManager.MdiActiveDocument;Verify(doc.Database,ReadPlan());
  Dump(doc.Database,Path.Combine(Root,"reopened.json"));
  File.WriteAllText(Path.Combine(Root,"reopened-ok.txt"),"22 imagenes cargadas por AutoCAD en una nueva sesion aislada; rutas relativas correctas.");
  doc.Editor.WriteMessage("\nFOTOS_REOPEN_OK\n");
 }catch(System.Exception ex){File.WriteAllText(Path.Combine(Root,"reopen-error.txt"),ex.ToString());}}
 [CommandMethod("FOTOSINSPECT")]
 public void Inspect(){try{foreach(var name in new[]{"modelo","ejemplo"})using(var db=new Database(false,true)){db.ReadDwgFile(Path.Combine(Root,"Entrada",name+".dwg"),FileOpenMode.OpenForReadAndAllShare,true,null);db.CloseInput(true);Dump(db,Path.Combine(Root,name+".json"));}App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nFOTOS_INSPECT_OK\n");}catch(System.Exception ex){File.WriteAllText(Path.Combine(Root,"error.txt"),ex.ToString());}}
 public static void Dump(Database db,string path){var rows=new List<object>();using(var tr=db.TransactionManager.StartTransaction()){
 foreach(ObjectId id in (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForRead)){
 var e=tr.GetObject(id,OpenMode.ForRead) as Entity;if(e==null)continue;Collect(e,tr,rows,"");
 }tr.Commit();}File.WriteAllText(path,new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Serialize(rows));}
 static void Collect(Entity e,Transaction tr,List<object> rows,string parent){
 var d=new Dictionary<string,object>{{"type",e.GetType().Name},{"handle",e.Handle.ToString()},{"layer",e.Layer},{"parent",parent}};
 try{d["min"]=P(e.GeometricExtents.MinPoint);d["max"]=P(e.GeometricExtents.MaxPoint);}catch{}
 var t=e as DBText;if(t!=null){d["text"]=t.TextString;d["position"]=P(t.Position);d["height"]=t.Height;}
 var mt=e as MText;if(mt!=null){d["text"]=mt.Contents;d["position"]=P(mt.Location);}
 var pl=e as Polyline;if(pl!=null){d["closed"]=pl.Closed;d["points"]=Enumerable.Range(0,pl.NumberOfVertices).Select(i=>P(pl.GetPoint3dAt(i))).ToArray();}
 var l=e as Line;if(l!=null){d["points"]=new[]{P(l.StartPoint),P(l.EndPoint)};}
 var im=e as RasterImage;if(im!=null){var def=(RasterImageDef)tr.GetObject(im.ImageDefId,OpenMode.ForRead);d["source"]=def.SourceFileName;d["origin"]=P(im.Orientation.Origin);d["u"]=V(im.Orientation.Xaxis);d["v"]=V(im.Orientation.Yaxis);d["imageSize"]=new[]{def.Size.X,def.Size.Y};d["clip"]=im.GetClipBoundary().Cast<Point2d>().Select(p=>new[]{p.X,p.Y}).ToArray();d["clipped"]=im.IsClipped;}
 rows.Add(d);
 var br=e as BlockReference;if(br!=null){d["block"]=((BlockTableRecord)tr.GetObject(br.BlockTableRecord,OpenMode.ForRead)).Name;using(var coll=new DBObjectCollection()){br.Explode(coll);foreach(DBObject obj in coll){using(obj){var child=obj as Entity;if(child!=null)Collect(child,tr,rows,parent+"/"+e.Handle);}}}}
 }
}
