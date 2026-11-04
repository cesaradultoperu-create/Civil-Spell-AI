using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.PlottingServices;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;
public class CalloutDetails {
 const string Root=@"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI\Test\PozaDistribution-20261026";
 static void Add(Transaction tr,BlockTableRecord b,Entity e){b.AppendEntity(e);tr.AddNewlyCreatedDBObject(e,true);}
 static void Text(Database db,Transaction tr,BlockTableRecord b,string s,double x,double y,double h,double width){var styles=(TextStyleTable)tr.GetObject(db.TextStyleTableId,OpenMode.ForRead);var t=new MText{Contents=s,Location=new Point3d(x,y,0),TextHeight=h,Width=width,TextStyleId=styles["SOTRAMI_ARIAL"],Attachment=AttachmentPoint.TopLeft};t.ColorIndex=7;Add(tr,b,t);}
 [CommandMethod("POZADETAILS")]
 public void Run(){
  var doc=App.DocumentManager.MdiActiveDocument;var db=doc.Database;var name=Path.GetFileNameWithoutExtension(doc.Name);string code=name.Substring(name.Length-3);var log=new List<string>();
  string outdir=Path.Combine(Root,"Revision_llamados");Directory.CreateDirectory(outdir);
  LayoutManager.Current.CurrentLayout="SOTRAMI";ObjectId annexId=LayoutManager.Current.CreateLayout("DETALLES_P011");
  LayoutManager.Current.CurrentLayout="DETALLES_P011";
  using(var source=new Database(false,true)){
   source.ReadDwgFile(Path.Combine(Root,"Fuentes","1107.001.00.14-200-14-P-011.dwg"),FileOpenMode.OpenForReadAndAllShare,true,null);source.CloseInput(true);
   using(var tr=db.TransactionManager.StartTransaction())using(var st=source.TransactionManager.StartTransaction()){
    var ld=(DBDictionary)tr.GetObject(db.LayoutDictionaryId,OpenMode.ForRead);var main=(Layout)tr.GetObject(ld.GetAt("SOTRAMI"),OpenMode.ForRead);var mainps=(BlockTableRecord)tr.GetObject(main.BlockTableRecordId,OpenMode.ForWrite);
    var annex=(Layout)tr.GetObject(annexId,OpenMode.ForWrite);using(var p=new PlotSettings(false)){p.CopyFrom(main);annex.CopyFrom(p);}var aps=(BlockTableRecord)tr.GetObject(annex.BlockTableRecordId,OpenMode.ForWrite);
    var own=new ObjectIdCollection();foreach(ObjectId id in mainps){var en=tr.GetObject(id,OpenMode.ForRead);if(en is BlockReference br && ((BlockTableRecord)tr.GetObject(br.BlockTableRecord,OpenMode.ForRead)).Name=="MEMB_WWL_P_A1_")own.Add(id);if(en is RasterImage image && image.Orientation.Origin.Y<70)own.Add(id);}
    var ownmap=new IdMapping();db.DeepCloneObjects(own,aps.ObjectId,ownmap,false);
    foreach(IdPair pair in ownmap)if(pair.IsPrimary && tr.GetObject(pair.Value,OpenMode.ForRead) is BlockReference br)foreach(ObjectId aid in br.AttributeCollection){var a=(AttributeReference)tr.GetObject(aid,OpenMode.ForRead);if(a.Tag=="TITTLE_DRAW2"){a.UpgradeOpen();a.TextString="DETALLES (REF. 14-P-011)";}}
    var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForWrite);var block=new BlockTableRecord{Name="SOTRAMI_P011_GEOMETRIA_"+code};bt.Add(block);tr.AddNewlyCreatedDBObject(block,true);
    var sm=(BlockTableRecord)st.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(source),OpenMode.ForRead);var modelIds=new ObjectIdCollection(sm.Cast<ObjectId>().ToArray());source.WblockCloneObjects(modelIds,block.ObjectId,new IdMapping(),DuplicateRecordCloning.MangleName,false);
    var ms=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForWrite);var origin=new Point3d(580500,8303000,0);Add(tr,ms,new BlockReference(origin,block.ObjectId));
    var sd=(DBDictionary)st.GetObject(source.LayoutDictionaryId,OpenMode.ForRead);var sl=(Layout)st.GetObject(sd.GetAt("A"),OpenMode.ForRead);var sp=(BlockTableRecord)st.GetObject(sl.BlockTableRecordId,OpenMode.ForRead);
    var annotations=new ObjectIdCollection();var viewportSpecs=new List<double[]>();
    foreach(ObjectId id in sp){var en=st.GetObject(id,OpenMode.ForRead) as Entity;if(en==null)continue;
     if(en is Viewport vp){if(vp.Width<1000 && vp.CenterPoint.X>35 && vp.CenterPoint.X<830 && vp.CenterPoint.Y>70 && vp.CenterPoint.Y<590){
       var eye=new Point3d(vp.ViewCenter.X,vp.ViewCenter.Y,0).TransformBy(Matrix3d.Rotation(vp.TwistAngle,Vector3d.ZAxis,Point3d.Origin));var world=vp.ViewTarget+eye.GetAsVector();viewportSpecs.Add(new[]{vp.CenterPoint.X,vp.CenterPoint.Y,vp.Width,vp.Height,world.X+origin.X,world.Y+origin.Y,vp.TwistAngle,1000/Math.Round(1000/vp.CustomScale)});
      }continue;}
     if(en is RasterImage)continue;
     if(en is BlockReference b && ((BlockTableRecord)st.GetObject(b.BlockTableRecord,OpenMode.ForRead)).Name=="MEMB_WWL_P_A1_")continue;
     try{var box=en.GeometricExtents;if(box.MinPoint.X>=35 && box.MaxPoint.X<=835 && box.MinPoint.Y>=70 && box.MaxPoint.Y<=582)annotations.Add(id);}catch{}
    }
    var amap=new IdMapping();source.WblockCloneObjects(annotations,aps.ObjectId,amap,DuplicateRecordCloning.MangleName,false);
    foreach(IdPair pair in amap)if(pair.IsPrimary && tr.GetObject(pair.Value,OpenMode.ForRead) is BlockReference br)foreach(ObjectId aid in br.AttributeCollection){var a=(AttributeReference)tr.GetObject(aid,OpenMode.ForRead);if(a.Tag=="SCALE" && a.TextString.Contains("1:50")){a.UpgradeOpen();a.TextString=a.TextString.Replace("1:50","1:25");}}
    foreach(var v in viewportSpecs){var vp=new Viewport();Add(tr,aps,vp);vp.CenterPoint=new Point3d(v[0],v[1],0);vp.Width=v[2];vp.Height=v[3];vp.ViewDirection=Vector3d.ZAxis;vp.ViewTarget=new Point3d(v[4],v[5],0);vp.ViewCenter=Point2d.Origin;vp.TwistAngle=v[6];vp.CustomScale=v[7];vp.ViewHeight=vp.Height/v[7];vp.On=true;vp.Locked=true;vp.Layer="VENTANAS_DISTRIBUCION";}
    Text(db,tr,aps,"DETALLES DEL PROYECTO SOTRAMI - FUENTE 14-P-011",42,588,3,760);
    Text(db,tr,aps,"DETALLES A Y C Y SUS SECCIONES, CONSERVADOS CON LOS NOMBRES DE LA FUENTE.\\PLa correspondencia con cada estructura se revisa mediante los llamados del P-001. El detalle B no está identificado en el P-011 recibido.",638,566,2.4,184);
    foreach(ObjectId id in mainps.Cast<ObjectId>().ToArray())if(tr.GetObject(id,OpenMode.ForRead) is MText mt && (mt.Text.StartsWith("SOTRAMI |")||mt.Text.StartsWith("SUPERFICIES LOCALES"))){mt.UpgradeOpen();mt.Erase();}
    string coordination=code=="008"?"POZA DE MAYORES EVENTOS (PME) - P-008.\\PEn P-001: estructura de descarga 3, detalle B / 14-P-011. Revisar la identificación B en la fuente.\\PDetalles A y C disponibles: presentación DETALLES_P011. El llamado de la poza en P-001 indica P-007; verificar su actualización.":code=="009"?"POZA DE SUBDRENAJE - P-009.\\PLlamado del P-001: estructura de descarga 2, detalle A / 14-P-011.\\PVer detalle A y sus secciones en la presentación DETALLES_P011.":"POZA DE AGUA PARA PROCESOS - P-010.\\PIdentificación y localización según el llamado del P-001.\\PDetalles del proyecto conservados en DETALLES_P011 con sus nombres de origen; verificar su correspondencia antes de aplicarlos.";
    Text(db,tr,mainps,coordination,36.5,113,2.3,548);
    tr.Commit();log.Add("Code="+code);log.Add("Cajetin MEMB_WWL_P_A1_ original SOTRAMI conservado");log.Add("SourceModelEntities="+modelIds.Count);log.Add("SourceAnnotations="+annotations.Count);log.Add("DetailViewports="+viewportSpecs.Count);
   }
  }
  LayoutManager.Current.CurrentLayout="SOTRAMI";doc.Editor.Regen();db.SaveAs(Path.Combine(outdir,Path.GetFileName(doc.Name)),DwgVersion.Current);
  using(var engine=PlotFactory.CreatePublishEngine()){
   engine.BeginPlot(null,null);bool started=false;var layouts=new[]{"SOTRAMI","DETALLES_P011"};
   for(int i=0;i<layouts.Length;i++){
    LayoutManager.Current.CurrentLayout=layouts[i];doc.Editor.Regen();using(var tr=db.TransactionManager.StartTransaction()){
     var l=(Layout)tr.GetObject(LayoutManager.Current.GetLayoutId(layouts[i]),OpenMode.ForRead);using(var info=new PlotInfo{Layout=l.ObjectId})using(var p=new PlotSettings(false))using(var validator=new PlotInfoValidator()){
      p.CopyFrom(l);info.OverrideSettings=p;validator.MediaMatchingPolicy=MatchingPolicy.MatchEnabled;validator.Validate(info);if(!started){engine.BeginDocument(info,doc.Name,null,1,true,Path.Combine(outdir,name+".pdf"));started=true;}
      using(var page=new PlotPageInfo()){engine.BeginPage(page,info,i==layouts.Length-1,null);engine.BeginGenerateGraphics(null);engine.EndGenerateGraphics(null);engine.EndPage(null);}
     }
    }
   }
   engine.EndDocument(null);engine.EndPlot(null);
  }
  File.WriteAllLines(Path.Combine(outdir,"verificacion-"+code+".txt"),log);doc.Editor.WriteMessage("\nPOZADETAILS_DONE\n");
 }
}
