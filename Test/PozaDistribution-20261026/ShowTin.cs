using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.Civil;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;
public class ShowTin {
 const string Root=@"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI\Test\PozaDistribution-20261026";
 [CommandMethod("SOTRAMIMOSTRARTIN")]
 public void Run(){
  var doc=App.DocumentManager.MdiActiveDocument;var db=doc.Database;var civil=CivilDocument.GetCivilDocument(db);var name=Path.GetFileNameWithoutExtension(doc.Name);var code=name.Substring(name.Length-3);var log=new List<string>();string outdir=Path.Combine(Root,"TIN_Nativos");Directory.CreateDirectory(outdir);
  LayoutManager.Current.CurrentLayout="SOTRAMI";var surfaces=civil.GetSurfaceIds();if(surfaces.Count!=2)throw new InvalidOperationException("Se esperaban dos TIN nativos");
  using(var tr=db.TransactionManager.StartTransaction()){
   var layers=(LayerTable)tr.GetObject(db.LayerTableId,OpenMode.ForRead);
   foreach(ObjectId lid in layers){var layer=(LayerTableRecord)tr.GetObject(lid,OpenMode.ForRead);if(layer.Name.StartsWith("SOT_"+code+"_")){if(new[]{"TN","TNMAJOR","CONTOUR","CONTOURMAJOR"}.Any(s=>layer.Name=="SOT_"+code+"_"+s)){layer.UpgradeOpen();layer.IsOff=true;}if(layer.Name.EndsWith("_SUPERFICIES_TIN")){layer.UpgradeOpen();layer.IsOff=false;layer.IsFrozen=false;layer.IsLocked=false;}}}
   ObjectId surfaceLayer=layers["SOT_"+code+"_SUPERFICIES_TIN"];
   foreach(ObjectId id in surfaces){var tin=(TinSurface)tr.GetObject(id,OpenMode.ForWrite);bool ground=tin.Name.StartsWith("TN_")||tin.Name=="TERRENO EXISTENTE";
    var sid=civil.Styles.SurfaceStyles.Add("SOTRAMI_"+(ground?"TERRENO":"DISENO")+"_NATIVO_"+code);var style=(SurfaceStyle)tr.GetObject(sid,OpenMode.ForWrite);
    foreach(SurfaceDisplayStyleType part in Enum.GetValues(typeof(SurfaceDisplayStyleType))){try{style.GetDisplayStylePlan(part).Visible=false;style.GetDisplayStyleModel(part).Visible=false;}catch{}}
    foreach(var part in new[]{SurfaceDisplayStyleType.MajorContour,SurfaceDisplayStyleType.MinorContour}){
     var plan=style.GetDisplayStylePlan(part);plan.Visible=true;plan.Color=ground?Color.FromRgb(150,150,150):Color.FromRgb(90,135,165);plan.Lineweight=LineWeight.LineWeight013;
     var model=style.GetDisplayStyleModel(part);model.Visible=true;model.Color=plan.Color;
    }
    foreach(SurfaceDisplayStyleType part in Enum.GetValues(typeof(SurfaceDisplayStyleType)))if(part.ToString().ToLowerInvariant().Contains("bound")||part.ToString().ToLowerInvariant().Contains("border")){try{style.GetDisplayStyleModel(part).Visible=true;}catch{}}
    style.ContourStyle.MajorContourInterval=ground?5:1;style.ContourStyle.MinorContourInterval=ground?1:.5;
    tin.StyleId=sid;tin.Visible=true;tin.LayerId=surfaceLayer;tin.Name=ground?"TERRENO EXISTENTE":code=="010"?"Poza de agua":"Pozas";
    var triangles=tin.GetTriangles(false);log.Add(tin.Name+" | Tipo TIN nativo | Triangulos="+triangles.Count+" | Handle="+id.Handle);if(triangles.Count==0)throw new InvalidOperationException("TIN sin triangulos");
   }
   var layout=(Layout)tr.GetObject(LayoutManager.Current.GetLayoutId("SOTRAMI"),OpenMode.ForRead);var ps=(BlockTableRecord)tr.GetObject(layout.BlockTableRecordId,OpenMode.ForRead);
   foreach(ObjectId id in ps){if(!(tr.GetObject(id,OpenMode.ForRead) is Viewport vp))continue;if(vp.Width>500 && vp.Width<700 && vp.CenterPoint.Y>400 && vp.CenterPoint.Y<500){vp.UpgradeOpen();vp.ThawLayersInViewport(new[]{surfaceLayer}.GetEnumerator());log.Add("TIN visible en viewport de planta");}}
   tr.Commit();
  }
  db.TileMode=true;using(var view=doc.Editor.GetCurrentView()){view.ViewDirection=Vector3d.ZAxis;view.Target=Point3d.Origin;view.ViewTwist=0;view.CenterPoint=code=="010"?new Point2d(579154,8302485):code=="009"?new Point2d(579301,8302046):new Point2d(579299,8302113);view.Height=code=="008"?85:code=="009"?45:60;view.Width=view.Height*1.6;doc.Editor.SetCurrentView(view);}LayoutManager.Current.CurrentLayout="SOTRAMI";
  doc.Editor.Regen();db.SaveAs(Path.Combine(outdir,Path.GetFileName(doc.Name)),DwgVersion.Current);
  using(var tr=db.TransactionManager.StartTransaction()){var l=(Layout)tr.GetObject(LayoutManager.Current.GetLayoutId("SOTRAMI"),OpenMode.ForRead);using(var p=new PlotSettings(false))using(var info=new PlotInfo{Layout=l.ObjectId})using(var valid=new PlotInfoValidator()){p.CopyFrom(l);info.OverrideSettings=p;valid.MediaMatchingPolicy=MatchingPolicy.MatchEnabled;valid.Validate(info);using(var engine=PlotFactory.CreatePublishEngine()){engine.BeginPlot(null,null);engine.BeginDocument(info,doc.Name,null,1,true,Path.Combine(outdir,name+".pdf"));using(var page=new PlotPageInfo()){engine.BeginPage(page,info,true,null);engine.BeginGenerateGraphics(null);engine.EndGenerateGraphics(null);engine.EndPage(null);}engine.EndDocument(null);engine.EndPlot(null);}}}
  File.WriteAllLines(Path.Combine(outdir,"TIN_"+code+".txt"),log);doc.Editor.WriteMessage("\nSOTRAMIMOSTRARTIN_DONE\n");
 }
}
