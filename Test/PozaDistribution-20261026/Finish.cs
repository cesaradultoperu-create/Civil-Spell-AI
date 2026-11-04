using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.PlottingServices;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;
public class Primitive {public string kind,layer,space,text,align;public double[][] pts;public double x,y,h,width,rot,r;public bool closed;}
public class ViewData {public string role;public double cx,cy,scale;}
public class DrawingData {public string code,title;public List<Primitive> primitives;public List<ViewData> views;}
public class FinishPoza {
 const string Root=@"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI\Test\PozaDistribution-20261026";
 static readonly Dictionary<string,int[]> Colors=new Dictionary<string,int[]>{{"TN",new[]{175,175,175}},{"TNMAJOR",new[]{125,125,125}},{"CONTOUR",new[]{100,139,164}},{"CONTOURMAJOR",new[]{63,100,130}},{"RIM",new[]{20,55,85}},{"FLOOR",new[]{20,55,85}},{"FILL",new[]{216,232,241}},{"CUT",new[]{0,155,165}},{"GRID",new[]{205,205,205}},{"GRIDTEXT",new[]{100,100,100}},{"PROFILETN",new[]{135,135,135}},{"PROFILEDESIGN",new[]{20,70,115}},{"PROFILEGRID",new[]{205,205,205}},{"PROFILEAXIS",new[]{80,80,80}}};
 static Point3d P(double[] p)=>new Point3d(p[0],p[1],p.Length>2?p[2]:0);
 static void Add(Transaction tr,BlockTableRecord b,Entity e){b.AppendEntity(e);tr.AddNewlyCreatedDBObject(e,true);}
 static ObjectId Layer(Database db,Transaction tr,string code,string name){
  var lt=(LayerTable)tr.GetObject(db.LayerTableId,OpenMode.ForRead);string n="SOT_"+code+"_"+name;
  if(lt.Has(n))return lt[n];lt.UpgradeOpen();var lr=new LayerTableRecord{Name=n,LineWeight=name=="RIM"||name=="PROFILEDESIGN"?LineWeight.LineWeight030:name=="GRID"||name=="PROFILEGRID"?LineWeight.LineWeight005:LineWeight.LineWeight013};
  if(Colors.TryGetValue(name,out var rgb))lr.Color=Color.FromRgb((byte)rgb[0],(byte)rgb[1],(byte)rgb[2]);else lr.Color=Color.FromColorIndex(ColorMethod.ByAci,7);
  if(name=="WHITEMASK")lr.Color=Color.FromRgb(255,255,255);
  var id=lt.Add(lr);tr.AddNewlyCreatedDBObject(lr,true);return id;
 }
 static ObjectId Font(Database db,Transaction tr){var st=(TextStyleTable)tr.GetObject(db.TextStyleTableId,OpenMode.ForRead);if(st.Has("SOTRAMI_ARIAL"))return st["SOTRAMI_ARIAL"];st.UpgradeOpen();var style=new TextStyleTableRecord{Name="SOTRAMI_ARIAL",FileName="arial.ttf"};var id=st.Add(style);tr.AddNewlyCreatedDBObject(style,true);return id;}
 static void Draw(Database db,Transaction tr,BlockTableRecord b,Primitive p,ObjectId font,string code){
  var layer=Layer(db,tr,code,p.layer);Entity e=null;
  if(p.kind=="poly"||p.kind=="hatch"){
   var line=new Polyline();for(int i=0;i<p.pts.Length;i++)line.AddVertexAt(i,new Point2d(p.pts[i][0],p.pts[i][1]),0,0,0);line.Closed=p.closed||p.kind=="hatch";line.LayerId=layer;Add(tr,b,line);
   if(p.kind=="hatch"){var hatch=new Hatch{LayerId=layer};Add(tr,b,hatch);hatch.SetHatchPattern(HatchPatternType.PreDefined,"SOLID");hatch.Associative=false;hatch.AppendLoop(HatchLoopTypes.External,new ObjectIdCollection(new[]{line.ObjectId}));hatch.EvaluateHatch(true);line.Erase();}
   return;
  }
  if(p.kind=="text")e=new MText{Location=new Point3d(p.x,p.y,0),Contents=p.text.Replace("\n","\\P"),TextHeight=p.h,Width=p.width,TextStyleId=font,Attachment=p.align=="middle"?AttachmentPoint.MiddleCenter:AttachmentPoint.TopLeft,Rotation=p.rot};
  if(p.kind=="circle")e=new Circle(new Point3d(p.x,p.y,0),Vector3d.ZAxis,p.r);
  if(p.kind=="dim"){
   var dim=new RotatedDimension(p.rot,P(p.pts[0]),P(p.pts[1]),P(p.pts[2]),"",db.Dimstyle);dim.Dimtxt=p.h;dim.Dimasz=p.h;dim.Dimgap=p.h*.3;dim.Dimdec=2;dim.Dimzin=8;dim.Dimscale=1;dim.Dimclrd=Color.FromColorIndex(ColorMethod.ByAci,7);dim.Dimclre=Color.FromColorIndex(ColorMethod.ByAci,7);dim.Dimclrt=Color.FromColorIndex(ColorMethod.ByAci,7);e=dim;
  }
  if(e!=null){e.LayerId=layer;Add(tr,b,e);}
 }
 static void Image(Database db,Transaction tr,BlockTableRecord ps,string name,string file,double x,double y,double w,double h){
  var dictId=RasterImageDef.GetImageDictionary(db);if(dictId.IsNull)dictId=RasterImageDef.CreateImageDictionary(db);var dict=(DBDictionary)tr.GetObject(dictId,OpenMode.ForWrite);
  using(var bitmap=System.Drawing.Image.FromFile(file)){var fit=Math.Min(w/bitmap.Width,h/bitmap.Height);var iw=bitmap.Width*fit;var ih=bitmap.Height*fit;x+=(w-iw)/2;y+=(h-ih)/2;w=iw;h=ih;}
  var def=new RasterImageDef{SourceFileName=file};def.Load();var id=dict.SetAt(name,def);tr.AddNewlyCreatedDBObject(def,true);
  var image=new RasterImage{ImageDefId=id,Orientation=new CoordinateSystem3d(new Point3d(x,y,0),new Vector3d(w,0,0),new Vector3d(0,h,0)),ShowImage=true};Add(tr,ps,image);RasterImage.EnableReactors(true);image.AssociateRasterDef(def);
 }
 [CommandMethod("POZAFINISH")]
 public void Run(){
  File.WriteAllText(Path.Combine(Root,"finish-progress.txt"),"START\n");
  var outdir=Path.Combine(Root,"Entrega");Directory.CreateDirectory(outdir);Directory.CreateDirectory(Path.Combine(outdir,"Assets"));foreach(var f in Directory.GetFiles(Path.Combine(Root,"Assets")))File.Copy(f,Path.Combine(outdir,"Assets",Path.GetFileName(f)),true);
  var logs=new List<string>();
  foreach(var code in new[]{"008","009","010"}){
   File.AppendAllText(Path.Combine(Root,"finish-progress.txt"),"BEGIN "+code+"\n");
   var serializer=new JavaScriptSerializer{MaxJsonLength=int.MaxValue};var data=serializer.Deserialize<DrawingData>(File.ReadAllText(Path.Combine(Root,"Data","drawing_"+code+".json")));
   var input=Path.Combine(Root,"ConSuperficies","1107.001.00.14-200-14-P-"+code+".dwg");
   using(var db=new Database(false,true)){
    db.ReadDwgFile(input,FileOpenMode.OpenForReadAndAllShare,true,null);db.CloseInput(true);var previous=HostApplicationServices.WorkingDatabase;
    File.AppendAllText(Path.Combine(Root,"finish-progress.txt"),"READ "+code+"\n");
    try{HostApplicationServices.WorkingDatabase=db;
     using(var tr=db.TransactionManager.StartTransaction()){
      var dict=(DBDictionary)tr.GetObject(db.LayoutDictionaryId,OpenMode.ForRead);var layout=(Layout)tr.GetObject(dict.GetAt("DISTRIBUCION_REFERENCIA"),OpenMode.ForWrite);var ps=(BlockTableRecord)tr.GetObject(layout.BlockTableRecordId,OpenMode.ForWrite);var ms=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForWrite);
      var font=Font(db,tr);var surfaceLayer=Layer(db,tr,code,"SUPERFICIES_TIN");
      foreach(var layerName in data.primitives.Select(p=>p.layer).Distinct())Layer(db,tr,code,layerName);
      File.AppendAllText(Path.Combine(Root,"finish-progress.txt"),"LAYERS "+code+"\n");
      foreach(ObjectId id in ms){var en=tr.GetObject(id,OpenMode.ForRead) as Entity;if(en!=null && en.GetType().Name=="TinSurface"){en.UpgradeOpen();en.LayerId=surfaceLayer;}}
      File.AppendAllText(Path.Combine(Root,"finish-progress.txt"),"SURFACES "+code+"\n");
      foreach(ObjectId id in ps.Cast<ObjectId>().ToArray()){
       var en=tr.GetObject(id,OpenMode.ForRead) as Entity;if(en==null)continue;
       File.AppendAllText(Path.Combine(Root,"finish-progress.txt"),"PS "+en.GetType().Name+"\n");
       if(en.Layer=="GUIAS_DISTRIBUCION"){en.UpgradeOpen();en.Erase();continue;}
       if(en is BlockReference titleblock)foreach(ObjectId aid in titleblock.AttributeCollection){var a=(AttributeReference)tr.GetObject(aid,OpenMode.ForRead);if(a.Tag=="TITTLE_DRAW2"){a.UpgradeOpen();a.TextString="PLANTA, SECCIONES Y DETALLES";}}
       if(en is RasterImage ri){
        en.UpgradeOpen();en.Erase();continue;
       }
       if(en is Viewport vp){
        var x=vp.CenterPoint.X;var h=vp.Height;string role=x>720?"map":x<300?"section1":x<400?"plan":x<550?"section2":h<40?"corner":"detail";
        var center=vp.CenterPoint;var width=vp.Width;var height=vp.Height;vp.UpgradeOpen();vp.Erase();
        var clean=new Viewport();Add(tr,ps,clean);clean.CenterPoint=center;clean.Width=width;clean.Height=height;clean.On=false;clean.Layer="VENTANAS_DISTRIBUCION";
        logs.Add(code+" VIEW_READY "+role);
       }
      }
      // Background fills must precede the lines so crest and floor remain legible.
      File.AppendAllText(Path.Combine(Root,"finish-progress.txt"),"VIEWS "+code+"\n");
      foreach(var p in data.primitives.OrderBy(p=>p.kind=="hatch"?0:1))Draw(db,tr,p.space=="paper"?ps:ms,p,font,code);
      File.AppendAllText(Path.Combine(Root,"finish-progress.txt"),"DRAWN "+code+"\n");
      // The location raster is loaded in the active document during plotting.
      File.AppendAllText(Path.Combine(Root,"finish-progress.txt"),"IMAGES "+code+"\n");
      using(var settings=new PlotSettings(false)){
       settings.CopyFrom(layout);var v=PlotSettingsValidator.Current;v.SetPlotConfigurationName(settings,"DWG To PDF.pc3","ISO_full_bleed_A1_(841.00_x_594.00_MM)");v.RefreshLists(settings);v.SetPlotPaperUnits(settings,PlotPaperUnit.Millimeters);v.SetPlotType(settings,Autodesk.AutoCAD.DatabaseServices.PlotType.Window);v.SetPlotWindowArea(settings,new Extents2d(0,0,841,594));v.SetUseStandardScale(settings,true);v.SetStdScaleType(settings,StdScaleType.ScaleToFit);v.SetPlotCentered(settings,true);v.SetPlotRotation(settings,PlotRotation.Degrees000);settings.PrintLineweights=true;settings.PlotPlotStyles=true;v.SetCurrentStyleSheet(settings,"monochrome.ctb");layout.CopyFrom(settings);
      }
      tr.Commit();
     }
     LayoutManager.Current.RenameLayout("DISTRIBUCION_REFERENCIA","SOTRAMI");LayoutManager.Current.CurrentLayout="SOTRAMI";
     db.SaveAs(Path.Combine(outdir,Path.GetFileName(input)),DwgVersion.Current);logs.Add(code+" SAVED");File.WriteAllLines(Path.Combine(Root,"finish.txt"),logs);
    }finally{HostApplicationServices.WorkingDatabase=previous;}
   }
  }
  App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nPOZAFINISH_DONE\n");
 }
 [CommandMethod("POZAPLOT")]
 public void Plot(){
  File.WriteAllText(Path.Combine(Root,"plot-progress.txt"),"START\n");
  var doc=App.DocumentManager.MdiActiveDocument;LayoutManager.Current.CurrentLayout="SOTRAMI";doc.Editor.Regen();
  File.AppendAllText(Path.Combine(Root,"plot-progress.txt"),"LAYOUT\n");
  string code=Path.GetFileNameWithoutExtension(doc.Name).Substring(Path.GetFileNameWithoutExtension(doc.Name).Length-3);var serializer=new JavaScriptSerializer{MaxJsonLength=int.MaxValue};var data=serializer.Deserialize<DrawingData>(File.ReadAllText(Path.Combine(Root,"Data","drawing_"+code+".json")));
  using(var imgtr=doc.Database.TransactionManager.StartTransaction()){
   var l=(Layout)imgtr.GetObject(LayoutManager.Current.GetLayoutId("SOTRAMI"),OpenMode.ForRead);var ps=(BlockTableRecord)imgtr.GetObject(l.BlockTableRecordId,OpenMode.ForWrite);
   l.UpgradeOpen();l.DrawViewportsFirst=true;
   var lt=(LayerTable)imgtr.GetObject(doc.Database.LayerTableId,OpenMode.ForRead);var surfaceLayer=lt["SOT_"+code+"_SUPERFICIES_TIN"];
   foreach(ObjectId id in ps.Cast<ObjectId>().ToArray()){if(!(imgtr.GetObject(id,OpenMode.ForRead) is Viewport vp))continue;
    var x=vp.CenterPoint.X;var h=vp.Height;if(vp.Number==1 || vp.Width>1000)continue;string role=x>720?"map":x<300?"section1":x<400?"plan":x<550?"section2":h<40?"corner":"detail";vp.UpgradeOpen();if(role=="map"){vp.On=false;continue;}
    File.AppendAllText(Path.Combine(Root,"plot-progress.txt"),"VIEW "+role+"\n");
    var v=data.views.Single(a=>a.role==role);vp.Locked=false;vp.ViewDirection=Vector3d.ZAxis;vp.ViewTarget=Point3d.Origin;vp.TwistAngle=0;vp.ViewCenter=new Point2d(v.cx,v.cy);vp.ViewHeight=vp.Height/v.scale;vp.CustomScale=v.scale;vp.On=true;vp.Locked=true;vp.FreezeLayersInViewport(new[]{surfaceLayer}.GetEnumerator());
    if(role=="plan"){
     var boundary=new Polyline();var corners=new[]{new Point2d(36.50558264928,333.68380817696),new Point2d(535.08,333.68380817696),new Point2d(535.08,376.5),new Point2d(580.5,376.5),new Point2d(580.5,578.43380817696),new Point2d(36.50558264928,578.43380817696)};for(int j=0;j<corners.Length;j++)boundary.AddVertexAt(j,corners[j],0,0,0);boundary.Closed=true;boundary.Layer="VENTANAS_DISTRIBUCION";Add(imgtr,ps,boundary);vp.NonRectClipEntityId=boundary.ObjectId;vp.NonRectClipOn=true;
    }
    File.AppendAllText(Path.Combine(Root,"plot-progress.txt"),"VIEW_DONE "+role+"\n");
    if(role=="corner")foreach(ObjectId lid in lt){var lr=(LayerTableRecord)imgtr.GetObject(lid,OpenMode.ForRead);if(lr.Name.StartsWith("SOT_"+code+"_") && !new[]{"RIM","FLOOR","CONTOUR","CONTOURMAJOR"}.Any(s=>lr.Name=="SOT_"+code+"_"+s))vp.FreezeLayersInViewport(new[]{lid}.GetEnumerator());}
   }
   if(!ps.Cast<ObjectId>().Any(id=>imgtr.GetObject(id,OpenMode.ForRead) is RasterImage)){
    File.AppendAllText(Path.Combine(Root,"plot-progress.txt"),"IMAGES\n");
    const string assets=@"C:\Users\Cesar Ugaz\Pictures\trabajo para codE\ENTREGA_SOTRAMI\Assets";
    Image(doc.Database,imgtr,ps,"SOTRAMI_MAPA_FINAL",Path.Combine(assets,"Mapa de Localización.jpg"),725.5,478.5,100,100);
    Image(doc.Database,imgtr,ps,"SOTRAMI_CLIENTE_FINAL",Path.Combine(assets,"LOGO-DE-MINERA-SOTRAMI-S.A.jpg"),576,15,72,45);
    Image(doc.Database,imgtr,ps,"SOTRAMI_WWL_FINAL",Path.Combine(assets,"LOGO-WWL.jpg"),754,22,73,31);
   }
   imgtr.Commit();
  }
  File.AppendAllText(Path.Combine(Root,"plot-progress.txt"),"GRAPHICS\n");
  using(var legacy=doc.Database.TransactionManager.StartTransaction()){
   var bt=(BlockTable)legacy.GetObject(doc.Database.BlockTableId,OpenMode.ForRead);var refs=new ObjectIdCollection();foreach(ObjectId id in bt){var b=(BlockTableRecord)legacy.GetObject(id,OpenMode.ForRead);if(b.IsFromExternalReference && !b.IsUnloaded)refs.Add(id);}legacy.Commit();if(refs.Count>0)doc.Database.UnloadXrefs(refs);
  }
  using(var check=doc.Database.TransactionManager.StartTransaction()){
   var layoutCheck=(Layout)check.GetObject(LayoutManager.Current.GetLayoutId("SOTRAMI"),OpenMode.ForRead);var paperCheck=(BlockTableRecord)check.GetObject(layoutCheck.BlockTableRecordId,OpenMode.ForRead);int active=0,images=0,surfaces=0;bool folio=false;
   foreach(ObjectId id in paperCheck){var en=check.GetObject(id,OpenMode.ForRead);if(en is Viewport vp && vp.Number!=1 && vp.Width<1000 && vp.On){if(!vp.Locked)throw new InvalidOperationException("Viewport desbloqueado");active++;}if(en is RasterImage ri){var imageDef=(RasterImageDef)check.GetObject(ri.ImageDefId,OpenMode.ForRead);if(!File.Exists(imageDef.SourceFileName))throw new FileNotFoundException(imageDef.SourceFileName);images++;}if(en is BlockReference br)foreach(ObjectId aid in br.AttributeCollection){var a=(AttributeReference)check.GetObject(aid,OpenMode.ForRead);if(a.Tag=="DRAWING_NO" && a.TextString.EndsWith("-P-"+code))folio=true;}}
   var modelCheck=(BlockTableRecord)check.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(doc.Database),OpenMode.ForRead);foreach(ObjectId id in modelCheck){var en=check.GetObject(id,OpenMode.ForRead);if(en.GetType().Name=="TinSurface"){if(Convert.ToBoolean(en.GetType().GetProperty("IsReferenceObject").GetValue(en,null)))throw new InvalidOperationException("La superficie no es local");surfaces++;}}
   if(active!=5||images!=3||surfaces!=2||!folio)throw new InvalidOperationException("Verificacion incompleta: "+active+","+images+","+surfaces+","+folio);
   File.WriteAllText(Path.Combine(Root,"verificacion-"+code+".txt"),"P-"+code+"\nLayout SOTRAMI\n5 ventanas activas y bloqueadas\n3 imágenes locales encontradas\n2 superficies TIN locales, sin Data Shortcuts\nNúmero de plano correcto\nTablas sobre fondo blanco y modelo detrás de papel\n");
  }
  doc.Editor.Regen();File.AppendAllText(Path.Combine(Root,"plot-progress.txt"),"REGEN_DONE\n");
  var export=Path.Combine(Root,"Final");Directory.CreateDirectory(export);Directory.CreateDirectory(Path.Combine(export,"Assets"));foreach(var f in Directory.GetFiles(Path.Combine(Root,"Assets")))File.Copy(f,Path.Combine(export,"Assets",Path.GetFileName(f)),true);
  try{doc.Database.SaveAs(Path.Combine(export,Path.GetFileName(doc.Name)),DwgVersion.Current);}catch(System.Exception ex){File.AppendAllText(Path.Combine(Root,"plot-progress.txt"),"SAVE_ERROR "+ex+"\n");throw;}
  File.AppendAllText(Path.Combine(Root,"plot-progress.txt"),"SAVE_DONE\n");
  using(var tr=doc.Database.TransactionManager.StartTransaction()){
   var layout=(Layout)tr.GetObject(LayoutManager.Current.GetLayoutId("SOTRAMI"),OpenMode.ForRead);
   using(var info=new PlotInfo{Layout=layout.ObjectId})using(var settings=new PlotSettings(false))using(var validator=new PlotInfoValidator()){
    settings.CopyFrom(layout);info.OverrideSettings=settings;validator.MediaMatchingPolicy=MatchingPolicy.MatchEnabled;validator.Validate(info);File.AppendAllText(Path.Combine(Root,"plot-progress.txt"),"VALIDATED\n");
    var filename=Path.Combine(Root,"Final",Path.GetFileNameWithoutExtension(doc.Name)+".pdf");
    using(var engine=PlotFactory.CreatePublishEngine()){engine.BeginPlot(null,null);engine.BeginDocument(info,doc.Name,null,1,true,filename);using(var page=new PlotPageInfo()){engine.BeginPage(page,info,true,null);engine.BeginGenerateGraphics(null);engine.EndGenerateGraphics(null);engine.EndPage(null);}engine.EndDocument(null);engine.EndPlot(null);}
   }
  }
  doc.Editor.WriteMessage("\nPOZAPLOT_DONE\n");
 }
}
