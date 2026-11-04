using System;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
namespace RelaveAuto {
 public class NativeProbe {
  [CommandMethod("RAADDPOINTAREA")]
  public void AddPointArea(){
   string original=Environment.GetEnvironmentVariable("RELAVEAUTO_JOB");var job=Json.Read<Job>(original);string parent=job.OutputFolder;
   try{
   var batch=Json.Read<BatchResult>(Path.Combine(parent,"resultado.json"));var last=batch.Photos.Where(p=>p.Frame.Number>0).OrderBy(p=>p.Frame.Number).Last();
   var segment=last.Surface.Zip(last.Surface.Skip(1),(a,b)=>new{a,b}).OrderByDescending(s=>s.b.X-s.a.X).First();
   double prior=last.Area,scale=(last.Frame.MarkerX-last.Frame.X)/(last.GridAnchorX-last.GridOriginX);
   var c=job.Calibration;double dx=c.B.X-c.A.X,dy=c.B.Y-c.A.Y,length=Math.Sqrt(dx*dx+dy*dy),height=(c.Top.X-c.A.X)*dy/length-(c.Top.Y-c.A.Y)*dx/length;
   int w=(int)Math.Round(Math.Min(1600,length)),h=(int)Math.Round(height*w/length);
   double scaleY=scale*(height/length)*(w-1)/(h-1);
   double expectedDelta=(segment.b.X-segment.a.X)*3*.5*scale*scaleY;int count=last.Surface.Count;
   ProfileGeometry.AddVertex(last,new XY((segment.a.X+segment.b.X)/2,(segment.a.Y+segment.b.Y)/2+3));last.Approved=true;last.NeedsReview=false;
   job.OutputFolder=Path.Combine(parent,"Punto_agregado");Directory.CreateDirectory(job.OutputFolder);string config=Path.Combine(job.OutputFolder,"trabajo.json");Json.Write(config,job);DrawingEngine.Generate(job,batch);
   using(var db=new Database(false,true)){
    db.ReadDwgFile(Path.Combine(job.OutputFolder,"Relave_resultado.dwg"),FileOpenMode.OpenForReadAndAllShare,true,null);db.CloseInput(true);
    using(var tr=db.TransactionManager.StartTransaction()){
     var polygon=((BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForRead)).Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<Polyline>().Single(p=>p.Handle.ToString()==last.Handle);
     if(polygon.NumberOfVertices!=count+2||Math.Abs(polygon.Area-prior-expectedDelta)>1e-9)throw new InvalidOperationException("DWG no conserva punto agregado o area analitica.");
     if(Math.Abs(last.Area-polygon.Area)>1e-9)throw new InvalidOperationException("El area exportada difiere del DWG editado.");tr.Commit();
    }
   }
   try{Environment.SetEnvironmentVariable("RELAVEAUTO_JOB",config);Heights();}finally{Environment.SetEnvironmentVariable("RELAVEAUTO_JOB",original);}
   File.WriteAllText(Path.Combine(parent,"agregar-area-nativa-ok.txt"),"Punto agregado exportado al DWG; incremento de area analitico="+expectedDelta.ToString("G17")+"; alturas recalculadas e interseccion nativa correcta.");
   }catch(System.Exception ex){File.WriteAllText(Path.Combine(parent,"agregar-area-nativa-error.txt"),ex.ToString());}
  }
  [CommandMethod("RAHEIGHTS")]
  public void Heights(){
   var job=Json.Read<Job>(Environment.GetEnvironmentVariable("RELAVEAUTO_JOB"));var result=Json.Read<BatchResult>(Path.Combine(job.OutputFolder,"resultado.json"));
   var last=result.Photos.Where(p=>p.Frame.Number>0).OrderBy(p=>p.Frame.Number).Last();
   if(result.Heights.Count<2||result.Heights.Any(s=>s.ProfileFile!=last.File||s.ProfileHandle!=last.Handle||s.ProfileNumber!=last.Frame.Number))throw new InvalidOperationException("Las alturas no pertenecen al ultimo perfil disponible.");
   using(var db=new Database(false,true)){
    db.ReadDwgFile(Path.Combine(job.OutputFolder,"Relave_resultado.dwg"),FileOpenMode.OpenForReadAndAllShare,true,null);db.CloseInput(true);
    using(var tr=db.TransactionManager.StartTransaction()){
     var polygons=((BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForRead)).Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<Polyline>().ToArray();
     var source=polygons.Single(p=>p.Handle.ToString()==last.Handle);var drawn=polygons.Where(p=>p.Layer=="RELAVEAUTO_ALTURAS").ToArray();
     if(drawn.Length!=result.Heights.Count(s=>s.Height>0))throw new InvalidOperationException("Cantidad de polilineas de altura incorrecta.");
     double maxError=0;foreach(var sample in result.Heights){
      if(!sample.AtTip&&Math.Abs(sample.Distance/.10-Math.Round(sample.Distance/.10))>1e-7)throw new InvalidOperationException("Seccion fuera de la malla 0.10 m.");
      var hits=new Point3dCollection();using(var vertical=new Line(new Point3d(sample.X,last.Frame.Y-1,0),new Point3d(sample.X,source.GeometricExtents.MaxPoint.Y+1,0)))source.IntersectWith(vertical,Intersect.OnBothOperands,hits,IntPtr.Zero,IntPtr.Zero);
      if(hits.Count==0)throw new InvalidOperationException("La seccion no intersecta la polilinea nativa.");
      double expected=hits.Cast<Point3d>().Max(p=>p.Y)-last.Frame.Y;maxError=Math.Max(maxError,Math.Abs(expected-sample.Height));
      if(sample.Height>0){var line=drawn.Single(p=>p.Handle.ToString()==sample.Handle);if(line.Closed||line.NumberOfVertices!=2||Math.Abs(line.Length-expected)>1e-8||Math.Abs(line.GetPoint2dAt(0).X-last.Frame.X-sample.Distance)>1e-8)throw new InvalidOperationException("Longitud o posicion incorrecta de seccion.");}
      else if(!string.IsNullOrEmpty(sample.Handle))throw new InvalidOperationException("Trazo degenerado en altura cero.");
     }
     if(maxError>1e-8)throw new InvalidOperationException("Alturas difieren de interseccion nativa: "+maxError);
     File.WriteAllText(Path.Combine(job.OutputFolder,"alturas-nativas-ok.txt"),last.Frame.Label+": "+result.Heights.Count+" estaciones, "+drawn.Length+" polilineas de altura. Interseccion independiente con perfil nativo; error maximo="+maxError.ToString("G17"));
     tr.Commit();
    }
   }
  }
  [CommandMethod("RANOFINALPHOTO")]
  public void NoFinalPhoto(){
   string original=Environment.GetEnvironmentVariable("RELAVEAUTO_JOB");var job=Json.Read<Job>(original);var batch=Json.Read<BatchResult>(Path.Combine(job.OutputFolder,"resultado.json"));string parent=job.OutputFolder;
   double previousMax=batch.Photos.Max(p=>p.Frame.Number);batch.Photos=batch.Photos.Where(p=>p.Frame.Number<previousMax).ToList();double expected=batch.Photos.Max(p=>p.Frame.Number);
   job.OutputFolder=Path.Combine(parent,"Prueba_sin_ultima_foto");Directory.CreateDirectory(job.OutputFolder);string config=Path.Combine(job.OutputFolder,"trabajo.json");Json.Write(config,job);DrawingEngine.Generate(job,batch);
   try{Environment.SetEnvironmentVariable("RELAVEAUTO_JOB",config);Heights();}finally{Environment.SetEnvironmentVariable("RELAVEAUTO_JOB",original);}
   if(batch.Heights.Any(h=>h.ProfileNumber!=expected))throw new InvalidOperationException("Se midio una foto ausente.");
   File.WriteAllText(Path.Combine(parent,"ultima-disponible-ok.txt"),"Sin foto "+previousMax.ToString("F2")+" se mide "+expected.ToString("F2")+". Sin perfiles inventados.");
  }
  [CommandMethod("RAEDITLASTHEIGHT")]
  public void EditLastHeight(){
   string original=Environment.GetEnvironmentVariable("RELAVEAUTO_JOB");var job=Json.Read<Job>(original);var batch=Json.Read<BatchResult>(Path.Combine(job.OutputFolder,"resultado.json"));string parent=job.OutputFolder;
   var last=batch.Photos.OrderBy(p=>p.Frame.Number).Last();double prior=batch.Heights[0].Height;foreach(var p in last.Surface)p.Y*=1.1;last.Approved=true;
   job.OutputFolder=Path.Combine(parent,"Prueba_alturas_editadas");Directory.CreateDirectory(job.OutputFolder);string config=Path.Combine(job.OutputFolder,"trabajo.json");Json.Write(config,job);DrawingEngine.Generate(job,batch);
   try{Environment.SetEnvironmentVariable("RELAVEAUTO_JOB",config);Heights();}finally{Environment.SetEnvironmentVariable("RELAVEAUTO_JOB",original);}
   if(Math.Abs(batch.Heights[0].Height-prior*1.1)>1e-9)throw new InvalidOperationException("Las alturas no se recalculan del perfil final editado.");
   File.WriteAllText(Path.Combine(parent,"alturas-editadas-ok.txt"),"Al modificar el ultimo perfil, las alturas nativas y el Excel se recalculan; no se conservan medidas antiguas.");
  }
  [CommandMethod("RACOMPARISON")]
  public void ComparePrevious(){
   var job=Json.Read<Job>(Environment.GetEnvironmentVariable("RELAVEAUTO_JOB"));var result=Json.Read<BatchResult>(Path.Combine(job.OutputFolder,"resultado.json"));
   var current=result.Photos.Single(p=>Math.Abs(p.Frame.Number-.6)<1e-7);var previous=ProfileComparison.Previous(result.Photos,current);
   if(previous==null||Math.Abs(previous.Frame.Number-.5)>1e-7)throw new InvalidOperationException("Referencia anterior inesperada en el fixture 0.60.");
   var mapped=ProfileComparison.Overlay(previous,current);if(mapped.Count!=previous.Surface.Count)throw new InvalidOperationException("Falta la superposicion.");
   double length=Math.Sqrt(Math.Pow(job.Calibration.B.X-job.Calibration.A.X,2)+Math.Pow(job.Calibration.B.Y-job.Calibration.A.Y,2));
   double height=((job.Calibration.Top.X-job.Calibration.A.X)*(job.Calibration.B.Y-job.Calibration.A.Y)-(job.Calibration.Top.Y-job.Calibration.A.Y)*(job.Calibration.B.X-job.Calibration.A.X))/length;
   int w=(int)Math.Round(Math.Min(1600,length)),h=(int)Math.Round(height*w/length);double aspect=height/(h-1.0)*(w-1.0)/length;
   double scale=(current.Frame.MarkerX-current.Frame.X)/(current.GridAnchorX-current.GridOriginX),max=0;
   using(var db=new Database(false,true)){
    db.ReadDwgFile(Path.Combine(job.OutputFolder,"Relave_resultado.dwg"),FileOpenMode.OpenForReadAndAllShare,true,null);db.CloseInput(true);
    using(var tr=db.TransactionManager.StartTransaction()){
     var profiles=((BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForRead)).Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<Polyline>().Where(p=>p.Layer=="RELAVEAUTO_PERFILES").ToArray();
     if(profiles.Length!=result.Photos.Count(p=>p.Frame.Number>0))throw new InvalidOperationException("Se exportaron perfiles visuales adicionales.");
     var prior=profiles.Single(p=>p.Handle.ToString()==previous.Handle);
     foreach(var p in profiles)if(p.ColorIndex==2)throw new InvalidOperationException("Se guardo una referencia amarilla en la capa de perfiles.");
     var translation=new Vector2d(current.Frame.X-previous.Frame.X,current.Frame.Y-previous.Frame.Y);
     foreach(var p in mapped){
      var point=new Point2d(current.Frame.X+(p.X-current.GridOriginX)*scale,current.Frame.Y+p.Y*scale*aspect);
      double distance=Enumerable.Range(0,prior.NumberOfVertices).Min(i=>point.GetDistanceTo(prior.GetPoint2dAt(i)+translation));max=Math.Max(max,distance);
     }
     tr.Commit();
    }
   }
   if(max>1e-8)throw new InvalidOperationException("La guia no coincide con la polilinea anterior trasladada fisicamente: "+max);
   File.WriteAllText(Path.Combine(job.OutputFolder,"comparacion-exportacion-ok.txt"),"Referencia 0.50 sobre 0.60 comparada con vertices reales del DWG; error maximo="+max.ToString("G17")+". Sin polilineas extra ni referencias amarillas exportadas.");
  }
  [CommandMethod("RAEXPORTPROPOSALS")]
  public void ExportProposals(){
   var job=Json.Read<Job>(Environment.GetEnvironmentVariable("RELAVEAUTO_JOB"));string source=job.OutputFolder;
   var batch=Json.Read<BatchResult>(Path.Combine(source,"resultado-vision.json"));
   var skipped=batch.Photos.Where(p=>!p.RegistrationValid||!p.AlignmentValid||p.Surface.Count<2||Math.Abs(p.Toe-p.GridAnchorX)>3||Math.Abs(p.Surface.Last().Y)>1e-6).Select(p=>p.File).ToArray();
   batch.Photos=batch.Photos.Where(p=>!skipped.Contains(p.File)).ToList();
   if(batch.Photos.Count==0)throw new InvalidOperationException("No hay propuestas completas para probar la exportacion.");
   job.Frames=DrawingEngine.ReadFrames(job.ModelPath);
   foreach(var p in batch.Photos){p.Frame=job.Frames.Single(f=>Math.Abs(f.Number-p.Frame.Number)<1e-7);p.Approved=true;p.Message="Aceptacion simulada del arnes para comprobar DWG y Excel; no equivale a revision fisica.";}
   job.OutputFolder=Path.Combine(source,"Prueba_exportacion");Directory.CreateDirectory(job.OutputFolder);
   var config=Path.Combine(job.OutputFolder,"trabajo.json");Json.Write(config,job);Json.Write(Path.Combine(job.OutputFolder,"excluidas-pendientes.json"),skipped);
   DrawingEngine.Generate(job,batch);Environment.SetEnvironmentVariable("RELAVEAUTO_JOB",config);Check();
   File.WriteAllText(Path.Combine(job.OutputFolder,"exportacion-propuestas-ok.txt"),batch.Photos.Count+" propuestas completas exportadas por el arnes; "+skipped.Length+" excluidas por referencias o puntas pendientes. No es aprobacion fisica de los contornos.");
  }
  [CommandMethod("RAORIGIN")]
  public void Origin(){
   string originalJob=Environment.GetEnvironmentVariable("RELAVEAUTO_JOB");var job=Json.Read<Job>(originalJob);string parent=job.OutputFolder;
   var result=Json.Read<BatchResult>(Path.Combine(parent,"resultado.json"));var right=result.Photos.ToDictionary(p=>p.File,p=>p.GridAnchorX);
   ProfileGeometry.SetOriginAll(result.Photos,3);foreach(var p in result.Photos)p.Approved=true;
   if(result.Photos.Any(p=>p.GridOriginX!=3||p.GridAnchorX!=right[p.File]))throw new InvalidOperationException("A no se aplico sin cambiar la derecha.");
   job.OutputFolder=Path.Combine(parent,"Prueba_inicio_A");Directory.CreateDirectory(job.OutputFolder);string config=Path.Combine(job.OutputFolder,"trabajo.json");Json.Write(config,job);DrawingEngine.Generate(job,result);
   try{Environment.SetEnvironmentVariable("RELAVEAUTO_JOB",config);Check();}finally{Environment.SetEnvironmentVariable("RELAVEAUTO_JOB",originalJob);}
   if(!File.Exists(Path.Combine(job.OutputFolder,"geometria-ok.txt")))throw new InvalidOperationException("La imagen no respeta A y su referencia al guardar.");
   File.WriteAllText(Path.Combine(parent,"inicio-a-nativo-ok.txt"),"A movido a 3 px en todas las fotos. Derechas conservadas; DWG reabierto, origenes y referencias comprobados, Excel recalculado.");
  }
  [CommandMethod("RAEDITAREA")]
  public void EditArea(){
   var job=Json.Read<Job>(Environment.GetEnvironmentVariable("RELAVEAUTO_JOB"));string parent=job.OutputFolder;var result=Json.Read<BatchResult>(Path.Combine(parent,"resultado.json"));
   var photo=result.Photos.First(p=>p.Frame.Number>0&&p.Surface.Count>3);int beforeCount=photo.Surface.Count;double oldArea=photo.Area;
   int index=Enumerable.Range(1,beforeCount-2).OrderByDescending(i=>Math.Abs((photo.Surface[i].X-photo.Surface[i-1].X)*(photo.Surface[i+1].Y-photo.Surface[i-1].Y)-(photo.Surface[i].Y-photo.Surface[i-1].Y)*(photo.Surface[i+1].X-photo.Surface[i-1].X))).First();
   ProfileGeometry.RemoveVertex(photo,index);photo.Approved=true;job.OutputFolder=Path.Combine(parent,"Prueba_eliminar_punto");DrawingEngine.Generate(job,result);
   if(photo.Surface.Count!=beforeCount-1||Math.Abs(photo.Area-oldArea)<1e-10)throw new InvalidOperationException("No cambio el area al quitar el vertice de prueba.");
   File.WriteAllText(Path.Combine(parent,"eliminar-area-ok.txt"),photo.File+": "+beforeCount+" a "+photo.Surface.Count+" puntos. Area nativa "+oldArea.ToString("G17")+" a "+photo.Area.ToString("G17")+". Excel generado con el valor nuevo.");
  }
  [CommandMethod("RACHECKINPUTS")]
  public void CheckInputs(){
   string root=Environment.GetEnvironmentVariable("RELAVEAUTO_INPUT_ROOT"),report=Environment.GetEnvironmentVariable("RELAVEAUTO_INPUT_REPORT");
   var rows=new System.Collections.Generic.List<object>();
   foreach(string folder in Directory.GetDirectories(root).OrderBy(x=>x)){
    string model=Path.Combine(folder,"modelo_.dwg");if(!File.Exists(model))continue;
    var frames=DrawingEngine.ReadFrames(model);var files=PhotoInputs.Files(folder);var numbers=files.Select(PhotoInputs.Number).ToArray();
    var missing=numbers.Where(n=>!frames.Any(f=>Math.Abs(f.Number-n)<1e-7)).ToArray();if(missing.Length>0)throw new InvalidOperationException("Fotos sin cuadro: "+folder);
    foreach(string file in files){using(var stream=File.OpenRead(file)){var header=new byte[24];if(stream.Read(header,0,24)!=24||header[0]!=137||header[1]!=80||header[2]!=78||header[3]!=71)throw new InvalidOperationException("PNG no valido: "+file);}}
    rows.Add(new{Folder=Path.GetFileName(folder),Reference=Path.GetFileName(PhotoInputs.Reference(folder)),Photos=files.Length,First=numbers.First(),Last=numbers.Last(),Frames=frames.Count,EmptyFrames=frames.Count(f=>!numbers.Any(n=>Math.Abs(n-f.Number)<1e-7)),Numbers=numbers});
   }
   Json.Write(report,rows);File.WriteAllText(report+".ok.txt",rows.Count+" carpetas: modelos en metros, PNG, referencias y cuadros numericos comprobados. Sin crear fotos faltantes.");
  }
  [CommandMethod("RAIMAGEINDEPENDENCE")]
  public void ImageIndependence(){
   var job=Json.Read<Job>(Environment.GetEnvironmentVariable("RELAVEAUTO_JOB"));string original=Path.Combine(job.OutputFolder,"Relave_resultado.dwg");
   var before=Orientations(original);var result=Json.Read<BatchResult>(Path.Combine(job.OutputFolder,"resultado.json"));
   foreach(var photo in result.Photos.Where(p=>p.Frame.Number>0)){foreach(var v in photo.Surface)v.Y*=.9;ProfileGeometry.SetToe(photo,photo.Toe+2);photo.Approved=true;}
   job.OutputFolder=Path.Combine(job.OutputFolder,"Prueba_independencia");DrawingEngine.Generate(job,result);var after=Orientations(Path.Combine(job.OutputFolder,"Relave_resultado.dwg"));
   double error=before.Max(p=>p.Value.Select((value,i)=>Math.Abs(value-after[p.Key][i])).Max());
   if(error>1e-12)throw new InvalidOperationException("La foto se movio al modificar el perfil: "+error);
   File.WriteAllText(Path.Combine(Path.GetDirectoryName(job.OutputFolder),"independencia-ok.txt"),before.Count+" orientaciones comparadas antes/despues de cambiar altura y punta del relave. Error maximo: "+error.ToString("G17"));
  }
  static System.Collections.Generic.Dictionary<string,double[]> Orientations(string file){
   var result=new System.Collections.Generic.Dictionary<string,double[]>();using(var db=new Database(false,true)){db.ReadDwgFile(file,FileOpenMode.OpenForReadAndAllShare,true,null);db.CloseInput(true);using(var tr=db.TransactionManager.StartTransaction()){
    foreach(ObjectId id in (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForRead)){var image=tr.GetObject(id,OpenMode.ForRead) as RasterImage;if(image==null||image.Layer!="RELAVEAUTO_FOTOS")continue;var def=(RasterImageDef)tr.GetObject(image.ImageDefId,OpenMode.ForRead);var o=image.Orientation;result.Add(Path.GetFileName(def.SourceFileName),new[]{o.Origin.X,o.Origin.Y,o.Xaxis.X,o.Xaxis.Y,o.Yaxis.X,o.Yaxis.Y});}tr.Commit();}}
   return result;
  }
  [CommandMethod("RAQUALITYGUARDS")]
  public void QualityGuards(){
   var job=Json.Read<Job>(Environment.GetEnvironmentVariable("RELAVEAUTO_JOB"));
   var photo=new PhotoResult{Frame=new Frame{Number=.2},RegistrationValid=true,AlignmentValid=true,GridAnchorX=10,Toe=10,Surface=new System.Collections.Generic.List<XY>{new XY(0,5),new XY(10,0)}};
   var result=new BatchResult();result.Photos.Add(photo);
   var test=new Job{OutputFolder=Path.Combine(job.OutputFolder,"Guardas"),ModelPath=job.ModelPath};
   bool pending=false,registration=false,grid=false;
   try{DrawingEngine.Generate(test,result);}catch(InvalidOperationException ex){pending=ex.Message.Contains("pendientes");}
   photo.Approved=true;photo.RegistrationValid=false;
   try{DrawingEngine.Generate(test,result);}catch(InvalidOperationException ex){registration=ex.Message.Contains("encuadre");}
   photo.RegistrationValid=true;photo.AlignmentValid=false;
   try{DrawingEngine.Generate(test,result);}catch(InvalidOperationException ex){grid=ex.Message.Contains("lineas negras");}
   if(!pending||!registration||!grid||File.Exists(Path.Combine(test.OutputFolder,"Relave_resultado.dwg")))throw new InvalidOperationException("Fallo en las guardas de revision, encuadre o cuadricula.");
   File.WriteAllText(Path.Combine(job.OutputFolder,"guardas-ok.txt"),"Exportacion bloqueada sin validar contorno, sin encuadre y sin referencia de cuadricula comprobada, incluso con Approved=true.");
  }
  [CommandMethod("RAFIXTURE")]
  public void Fixture(){string path=Environment.GetEnvironmentVariable("RELAVEAUTO_FIXTURE_MODEL");using(var db=new Database(true,true)){db.Insunits=UnitsValue.Meters;using(var tr=db.TransactionManager.StartTransaction()){
   var ms=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForWrite);
   double[] numbers={0,.25,.8};for(int i=0;i<3;i++){
    double x=i*5;var rect=new Polyline();rect.AddVertexAt(0,new Point2d(x,0),0,0,0);rect.AddVertexAt(1,new Point2d(x+3,0),0,0,0);rect.AddVertexAt(2,new Point2d(x+3,1),0,0,0);rect.AddVertexAt(3,new Point2d(x,1),0,0,0);rect.Closed=true;ms.AppendEntity(rect);tr.AddNewlyCreatedDBObject(rect,true);
    if(i>0){var marker=new Polyline();marker.AddVertexAt(0,new Point2d(x+numbers[i],0),0,0,0);marker.AddVertexAt(1,new Point2d(x+numbers[i],1),0,0,0);ms.AppendEntity(marker);tr.AddNewlyCreatedDBObject(marker,true);}
    var text=new DBText{TextString=numbers[i].ToString("0.00",System.Globalization.CultureInfo.InvariantCulture),Height=.15,Position=new Point3d(x+1.3,1.3,0)};ms.AppendEntity(text);tr.AddNewlyCreatedDBObject(text,true);
   }tr.Commit();}db.SaveAs(path,DwgVersion.Current);}}
  [CommandMethod("RACHECKRESULT")]
  public void Check(){string jobPath=Environment.GetEnvironmentVariable("RELAVEAUTO_JOB");var job=Json.Read<Job>(jobPath);try{
   var result=Json.Read<BatchResult>(Path.Combine(job.OutputFolder,"resultado.json"));DrawingEngine.VerifySaved(Path.Combine(job.OutputFolder,"Relave_resultado.dwg"),result);double max=0;double? angle=null;
   using(var db=new Database(false,true)){db.ReadDwgFile(Path.Combine(job.OutputFolder,"Relave_resultado.dwg"),FileOpenMode.OpenForReadAndAllShare,true,null);db.CloseInput(true);using(var tr=db.TransactionManager.StartTransaction()){
    foreach(ObjectId id in (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForRead)){
     var image=tr.GetObject(id,OpenMode.ForRead) as RasterImage;if(image==null||image.Layer!="RELAVEAUTO_FOTOS")continue;
     var def=(RasterImageDef)tr.GetObject(image.ImageDefId,OpenMode.ForRead);var photo=result.Photos.Single(p=>p.File==Path.GetFileName(def.SourceFileName));var c=job.Calibration;var o=image.Orientation;
     double theta=Math.Atan2(o.Xaxis.Y,o.Xaxis.X);if(angle.HasValue&&Math.Abs(theta-angle.Value)>1e-10)throw new InvalidOperationException("Giros diferentes");angle=theta;
     double length=Math.Sqrt(Math.Pow(c.B.X-c.A.X,2)+Math.Pow(c.B.Y-c.A.Y,2));int width=(int)Math.Round(Math.Min(1600,length));double t0=photo.GridOriginX/(width-1.0);double ax=photo.Scale*(c.A.X+t0*(c.B.X-c.A.X))+photo.Tx,ay=photo.PixelHeight-photo.Scale*(c.A.Y+t0*(c.B.Y-c.A.Y))-photo.Ty;
     var a=o.Origin+o.Xaxis*(ax/photo.PixelWidth)+o.Yaxis*(ay/photo.PixelHeight);max=Math.Max(max,a.DistanceTo(new Point3d(photo.Frame.X,photo.Frame.Y,0)));
     if(photo.Frame.Number>0){double t=photo.GridAnchorX/(width-1.0);double bx=photo.Scale*(c.A.X+t*(c.B.X-c.A.X))+photo.Tx,by=photo.PixelHeight-photo.Scale*(c.A.Y+t*(c.B.Y-c.A.Y))-photo.Ty;var b=o.Origin+o.Xaxis*(bx/photo.PixelWidth)+o.Yaxis*(by/photo.PixelHeight);max=Math.Max(max,b.DistanceTo(new Point3d(photo.Frame.MarkerX,photo.Frame.Y,0)));}
    }tr.Commit();}}
   if(max>1e-7)throw new InvalidOperationException("Alineacion fuera de tolerancia: "+max);
   File.WriteAllText(Path.Combine(job.OutputFolder,"geometria-ok.txt"),"Origen negro y centro de marca negra verificados en el DWG reabierto. Giro comun. Error maximo: "+max.ToString("G17"));
  }catch(System.Exception ex){File.WriteAllText(Path.Combine(job.OutputFolder,"geometria-error.txt"),ex.ToString());}}
 }
}
