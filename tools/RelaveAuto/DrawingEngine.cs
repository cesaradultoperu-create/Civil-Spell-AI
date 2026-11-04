using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;

namespace RelaveAuto {
 public static class DrawingEngine {
  const string ImageLayer="RELAVEAUTO_FOTOS",ProfileLayer="RELAVEAUTO_PERFILES",HeightLayer="RELAVEAUTO_ALTURAS",RegApp="RELAVEAUTO";
  sealed class Box {public double X,Y,W,H;}
  sealed class Label {public double Number,X,Y;}
  public static List<Frame> ReadFrames(string file){
   using(var db=new Database(false,true)){
    db.ReadDwgFile(file,FileOpenMode.OpenForReadAndAllShare,true,null);db.CloseInput(true);
    if(db.Insunits!=UnitsValue.Meters)throw new InvalidOperationException("Esta version de prueba requiere un modelo configurado en metros.");
    var boxes=new List<Box>();var labels=new List<Label>();var markers=new List<XY>();
    using(var tr=db.TransactionManager.StartTransaction()){
     foreach(ObjectId id in (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForRead))Collect((Entity)tr.GetObject(id,OpenMode.ForRead),boxes,labels,markers);
     tr.Commit();
    }
    var frames=new List<Frame>();
    foreach(var label in labels.OrderBy(l=>l.Number)){
     var possible=boxes.Where(b=>label.Y>=b.Y+b.H-.05*b.H&&label.Y-(b.Y+b.H)<1.5*b.H&&Math.Abs(label.X-(b.X+b.W/2))<b.W*.75).OrderBy(b=>Math.Abs(label.X-(b.X+b.W/2))/b.W+Math.Abs(label.Y-b.Y-b.H)/b.H).ToArray();
     if(possible.Length==0)continue;var box=possible[0];double target=box.X+label.Number;
     var near=markers.Where(p=>Math.Abs(p.Y-box.Y)<1e-6&&Math.Abs(p.X-target)<Math.Max(1e-6,box.W*.002)).ToArray();
     if(label.Number>0&&near.Length==0&&Math.Abs(label.Number-box.W)>1e-6)continue;
     frames.Add(new Frame{Number=label.Number,Label=label.Number.ToString("0.00",CultureInfo.InvariantCulture),X=box.X,Y=box.Y,Width=box.W,Height=box.H,MarkerX=label.Number==0?box.X:(near.Length>0?near[0].X:box.X+box.W)});
    }
    if(frames.Count==0||!frames.Any(f=>f.Number==0))throw new InvalidOperationException("No se reconocen cuadros con numeros y sus lineas rojas. Use el modelo de cuadros.");
    if(frames.GroupBy(f=>f.Number).Any(g=>g.Count()>1))throw new InvalidOperationException("Hay numeros de cuadro repetidos. Use un modelo con un solo conjunto de cuadros.");
    return frames;
   }
  }
  static void Collect(Entity entity,List<Box> boxes,List<Label> labels,List<XY> markers){
   var mt=entity as MText;var text=entity as DBText;double number;
   string s=mt!=null?mt.Text:text!=null?text.TextString:null;
   if(s!=null&&double.TryParse(s.Trim().Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out number)&&number>=0){var e=entity.GeometricExtents;labels.Add(new Label{Number=number,X=(e.MinPoint.X+e.MaxPoint.X)/2,Y=e.MinPoint.Y});}
   var p=entity as Polyline;
   if(p!=null){
    if(p.Closed&&p.NumberOfVertices==4){
     bool orthogonal=true;for(int i=0;i<4;i++){var a=p.GetPoint2dAt(i);var b=p.GetPoint2dAt((i+1)%4);if(Math.Abs(a.X-b.X)>1e-7&&Math.Abs(a.Y-b.Y)>1e-7)orthogonal=false;}
     if(orthogonal){var e=p.GeometricExtents;double w=e.MaxPoint.X-e.MinPoint.X,h=e.MaxPoint.Y-e.MinPoint.Y;if(w>h&&h>1e-8)boxes.Add(new Box{X=e.MinPoint.X,Y=e.MinPoint.Y,W=w,H=h});}
    }
    if(p.NumberOfVertices==2){var a=p.GetPoint2dAt(0);var b=p.GetPoint2dAt(1);if(Math.Abs(a.X-b.X)<1e-7&&Math.Abs(a.Y-b.Y)>1e-7)markers.Add(new XY(a.X,Math.Min(a.Y,b.Y)));}
   }
   var block=entity as BlockReference;if(block!=null){using(var children=new DBObjectCollection()){block.Explode(children);foreach(DBObject child in children)using(child){if(child is Entity e)Collect(e,boxes,labels,markers);}}}
   var line=entity as Line;if(line!=null&&Math.Abs(line.StartPoint.X-line.EndPoint.X)<1e-7&&Math.Abs(line.StartPoint.Y-line.EndPoint.Y)>1e-7)markers.Add(new XY(line.StartPoint.X,Math.Min(line.StartPoint.Y,line.EndPoint.Y)));
  }
  public static void Generate(Job job,BatchResult result){
   var tick=Stopwatch.StartNew();Directory.CreateDirectory(job.OutputFolder);Directory.CreateDirectory(Path.Combine(job.OutputFolder,"fotos"));
   string output=Path.Combine(job.OutputFolder,"Relave_resultado.dwg");
   if(File.Exists(output))throw new InvalidOperationException("La carpeta ya tiene un resultado. Seleccione una carpeta nueva.");
   if(result.Photos.Any(p=>!p.RegistrationValid))throw new InvalidOperationException("Hay fotos sin encuadre comprobado. Recalibre antes de exportar.");
   if(result.Photos.Any(p=>!p.AlignmentValid))throw new InvalidOperationException("Hay lineas negras pendientes de centrar. Revise la alineacion antes de exportar.");
   if(result.Photos.Any(p=>p.Frame.Number>0&&(!ReviewActions.CanApprove(p)||!p.Approved)))throw new InvalidOperationException("Hay fotos pendientes de revisar. Corrija o valide todos sus contornos antes de exportar.");
   result.Heights=new List<HeightSample>();var lastPhoto=result.Photos.Where(p=>p.Frame.Number>0).OrderByDescending(p=>p.Frame.Number).FirstOrDefault();Polyline lastProfile=null;
   string temporary=Path.Combine(job.OutputFolder,".modelo-trabajo.dwg");File.Copy(job.ModelPath,temporary,false);
   var before=HostApplicationServices.WorkingDatabase;
   try{using(var db=new Database(false,true)){
    db.ReadDwgFile(temporary,FileOpenMode.OpenForReadAndAllShare,true,null);db.CloseInput(true);HostApplicationServices.WorkingDatabase=db;
    using(var tr=db.TransactionManager.StartTransaction()){
     var ms=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForWrite);
     foreach(ObjectId id in ms.Cast<ObjectId>().ToArray()){
      var e=(Entity)tr.GetObject(id,OpenMode.ForRead);bool erase=e.Layer==ImageLayer||e.Layer==ProfileLayer||e.Layer==HeightLayer||e.Layer=="RELAVE_PERFILES";
      if(e is RasterImage old){var def=(RasterImageDef)tr.GetObject(old.ImageDefId,OpenMode.ForRead);double n;if(ParseFile(Path.GetFileName(def.SourceFileName),out n)&&job.Frames.Any(f=>Math.Abs(f.Number-n)<1e-7))erase=true;}
      if(erase){e.UpgradeOpen();e.Erase();}
     }
     var lt=(LayerTable)tr.GetObject(db.LayerTableId,OpenMode.ForRead);
     var imageLayer=Layer(lt,tr,ImageLayer);var profileLayer=Layer(lt,tr,ProfileLayer);var heightLayer=Layer(lt,tr,HeightLayer,4);
     var apps=(RegAppTable)tr.GetObject(db.RegAppTableId,OpenMode.ForRead);if(!apps.Has(RegApp)){apps.UpgradeOpen();var app=new RegAppTableRecord{Name=RegApp};apps.Add(app);tr.AddNewlyCreatedDBObject(app,true);}
     if(RasterImageDef.GetImageDictionary(db).IsNull)RasterImageDef.CreateImageDictionary(db);
     var dict=(DBDictionary)tr.GetObject(RasterImageDef.GetImageDictionary(db),OpenMode.ForWrite);var imageIds=new ObjectIdCollection();RasterImage.EnableReactors(true);
     var c=job.Calibration;double dx=c.B.X-c.A.X,dy=c.B.Y-c.A.Y,length=Math.Sqrt(dx*dx+dy*dy);double height=(c.Top.X-c.A.X)*dy/length-(c.Top.Y-c.A.Y)*dx/length;
     int w=(int)Math.Round(Math.Min(1600,length)),h=(int)Math.Round(height*w/length);
     foreach(var photo in result.Photos){
      string source=Path.Combine(job.PhotoFolder,photo.File),copy=Path.Combine(job.OutputFolder,"fotos",photo.File);File.Copy(source,copy,true);
      var def=new RasterImageDef();var defId=dict.SetAt("RA_"+Guid.NewGuid().ToString("N"),def);tr.AddNewlyCreatedDBObject(def,true);def.SourceFileName=copy;def.Load();
      var image=new RasterImage();image.SetDatabaseDefaults(db);image.ImageDefId=defId;image.LayerId=imageLayer;image.ShowImage=true;
      var f=photo.Frame;double span=f.Number==0?f.Width:f.MarkerX-f.X;double distance=(photo.GridAnchorX-photo.GridOriginX)/(w-1.0)*length;
      double sx=photo.Scale*dx/length*distance,sy=-photo.Scale*dy/length*distance,n=sx*sx+sy*sy;
      double a=span*sx/n,b=-span*sy/n,tOrigin=photo.GridOriginX/(w-1.0);double ax=photo.Scale*(c.A.X+tOrigin*dx)+photo.Tx,ay=photo.PixelHeight-(photo.Scale*(c.A.Y+tOrigin*dy)+photo.Ty);
      image.Orientation=new CoordinateSystem3d(new Point3d(f.X-a*ax+b*ay,f.Y-b*ax-a*ay,0),new Vector3d(a*photo.PixelWidth,b*photo.PixelWidth,0),new Vector3d(-b*photo.PixelHeight,a*photo.PixelHeight,0));
      ms.AppendEntity(image);tr.AddNewlyCreatedDBObject(image,true);image.AssociateRasterDef(def);image.IsClipped=false;imageIds.Add(image.ObjectId);def.SourceFileName=@".\fotos\"+photo.File;
      if(f.Number==0)continue;
      double toeX=f.X+(photo.Toe-photo.GridOriginX)/(photo.GridAnchorX-photo.GridOriginX)*span;
      var points=new List<Point2d>{new Point2d(f.X,f.Y),new Point2d(toeX,f.Y)};
      double worldScale=span/distance;
      foreach(var p in photo.Surface.AsEnumerable().Reverse()){
       var pt=new Point2d(f.X+Math.Max(0,Math.Min(photo.Toe-photo.GridOriginX,p.X-photo.GridOriginX))/(w-1.0)*length*worldScale,f.Y+Math.Max(0,p.Y)/(h-1.0)*height*worldScale);
       if(pt.GetDistanceTo(points[points.Count-1])>1e-9)points.Add(pt);
      }
      if(points.Last().GetDistanceTo(points[0])<1e-9)points.RemoveAt(points.Count-1);
      var poly=new Polyline(points.Count);poly.SetDatabaseDefaults(db);poly.LayerId=profileLayer;for(int i=0;i<points.Count;i++)poly.AddVertexAt(i,points[i],0,0,0);poly.Closed=true;
      ms.AppendEntity(poly);tr.AddNewlyCreatedDBObject(poly,true);using(var data=new ResultBuffer(new TypedValue(1001,RegApp),new TypedValue(1000,photo.File),new TypedValue(1000,f.Label)))poly.XData=data;
      if(poly.Area<=0)throw new InvalidOperationException("Area nula en "+photo.File);photo.Area=poly.Area;photo.Handle=poly.Handle.ToString();
      if(ReferenceEquals(photo,lastPhoto))lastProfile=poly;
     }
     if(lastProfile!=null){
      var polygon=Enumerable.Range(0,lastProfile.NumberOfVertices).Select(i=>new XY(lastProfile.GetPoint2dAt(i).X,lastProfile.GetPoint2dAt(i).Y)).ToList();
      result.Heights=HeightSampler.Sample(polygon,lastPhoto.Frame.X,lastPhoto.Frame.Y,.10);
      foreach(var section in result.Heights){
       section.ProfileNumber=lastPhoto.Frame.Number;section.ProfileFile=lastPhoto.File;section.ProfileHandle=lastPhoto.Handle;
       if(section.Height==0)continue; // Preserve the zero in Excel without a degenerate DWG entity.
       var line=new Polyline(2);line.SetDatabaseDefaults(db);line.LayerId=heightLayer;
       line.AddVertexAt(0,new Point2d(section.X,section.BaseY),0,0,0);line.AddVertexAt(1,new Point2d(section.X,section.TopY),0,0,0);line.Closed=false;
       ms.AppendEntity(line);tr.AddNewlyCreatedDBObject(line,true);section.Handle=line.Handle.ToString();
       using(var data=new ResultBuffer(new TypedValue(1001,RegApp),new TypedValue(1000,"ALTURA"),new TypedValue(1000,section.ProfileFile),new TypedValue(1040,section.Distance),new TypedValue(1040,section.Height)))line.XData=data;
      }
     }
     ((DrawOrderTable)tr.GetObject(ms.DrawOrderTableId,OpenMode.ForWrite)).MoveToBottom(imageIds);tr.Commit();
    }
    db.SaveAs(output,DwgVersion.Current);
   }}finally{HostApplicationServices.WorkingDatabase=before;}
   VerifySaved(output,result);
   ExcelWriter.Save(Path.Combine(job.OutputFolder,"Areas_relave.xlsx"),result.Photos.Where(p=>p.Frame.Number>0).ToArray());
   ExcelWriter.SaveHeights(Path.Combine(job.OutputFolder,"Alturas_ultimo_perfil.xlsx"),result.Heights.ToArray());
   result.DrawingSeconds=tick.Elapsed.TotalSeconds;Json.Write(Path.Combine(job.OutputFolder,"resultado.json"),result);
   File.Delete(temporary);
  }
  static ObjectId Layer(LayerTable table,Transaction tr,string name,short color=7){if(table.Has(name))return table[name];if(!table.IsWriteEnabled)table.UpgradeOpen();var layer=new LayerTableRecord{Name=name,Color=Color.FromColorIndex(ColorMethod.ByAci,color)};var id=table.Add(layer);tr.AddNewlyCreatedDBObject(layer,true);return id;}
  public static bool ParseFile(string name,out double number){string stem=Path.GetFileNameWithoutExtension(name).Replace(',','.');int integer;if(int.TryParse(stem,out integer)){number=integer/100.0;return integer>=0;}return double.TryParse(stem,NumberStyles.Float,CultureInfo.InvariantCulture,out number)&&number>=0;}
  public static void VerifySaved(string file,BatchResult result){
   using(var db=new Database(false,true)){
    db.ReadDwgFile(file,FileOpenMode.OpenForReadAndAllShare,true,null);db.CloseInput(true);
    using(var tr=db.TransactionManager.StartTransaction()){
     var entities=((BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForRead)).Cast<ObjectId>().Select(id=>(Entity)tr.GetObject(id,OpenMode.ForRead)).ToArray();
     var images=entities.OfType<RasterImage>().Where(p=>p.Layer==ImageLayer).ToArray();var polys=entities.OfType<Polyline>().Where(p=>p.Layer==ProfileLayer).ToArray();
     if(images.Length!=result.Photos.Count||polys.Length!=result.Photos.Count(p=>p.Frame.Number>0))throw new InvalidOperationException("La verificacion del DWG no coincide con el lote.");
     foreach(var p in result.Photos.Where(p=>p.Frame.Number>0)){var poly=polys.Single(x=>x.Handle.ToString()==p.Handle);if(!poly.Closed||Math.Abs(poly.Area-p.Area)>1e-10||Math.Abs(poly.GeometricExtents.MaxPoint.X-(p.Frame.X+(p.Toe-p.GridOriginX)/(p.GridAnchorX-p.GridOriginX)*(p.Frame.MarkerX-p.Frame.X)))>1e-7)throw new InvalidOperationException("Perfil guardado incorrecto: "+p.File);}
     foreach(var image in images){var def=(RasterImageDef)tr.GetObject(image.ImageDefId,OpenMode.ForRead);if(Path.IsPathRooted(def.SourceFileName)||!File.Exists(Path.Combine(Path.GetDirectoryName(file),def.SourceFileName)))throw new InvalidOperationException("Referencia de foto no portable.");}
     var sections=entities.OfType<Polyline>().Where(p=>p.Layer==HeightLayer).ToArray();
     if(sections.Length!=result.Heights.Count(p=>p.Height>0))throw new InvalidOperationException("Las polilineas de altura no coinciden con la medicion.");
     foreach(var section in result.Heights){
      if(section.Height==0){if(!string.IsNullOrEmpty(section.Handle))throw new InvalidOperationException("Se creo un trazo de altura cero.");continue;}
      var line=sections.Single(p=>p.Handle.ToString()==section.Handle);
      if(line.Closed||line.NumberOfVertices!=2||Math.Abs(line.Length-section.Height)>1e-9||line.GetPoint2dAt(0).GetDistanceTo(new Point2d(section.X,section.BaseY))>1e-9||line.GetPoint2dAt(1).GetDistanceTo(new Point2d(section.X,section.TopY))>1e-9)throw new InvalidOperationException("Altura guardada incorrecta: "+section.Distance.ToString("F2",CultureInfo.InvariantCulture));
     }
     tr.Commit();
    }
   }
  }
 }
}
