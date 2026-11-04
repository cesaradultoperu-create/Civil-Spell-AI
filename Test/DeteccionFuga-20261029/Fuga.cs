using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.PlottingServices;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;
public class Fuga {
 public const string Root=@"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI\Test\DeteccionFuga-20261029";
 static double[] P(Point3d p){return new[]{p.X,p.Y,p.Z};}
 static double[] P(Point2d p){return new[]{p.X,p.Y};}
 [CommandMethod("FUGADIAG")]
 public void Diag(){try{var db=App.DocumentManager.MdiActiveDocument.Database; Dump(db,"inicial.json"); Plot(db,"inicial.pdf");}catch(System.Exception e){File.WriteAllText(Path.Combine(Root,"error-diag.txt"),e.ToString());}}
 public static void Dump(Database db,string name){var rows=new List<object>();using(var tr=db.TransactionManager.StartTransaction()){
 foreach(DBDictionaryEntry item in (DBDictionary)tr.GetObject(db.LayoutDictionaryId,OpenMode.ForRead)){
 var l=(Layout)tr.GetObject(item.Value,OpenMode.ForRead);rows.Add(new{kind="layout",name=l.LayoutName,paper=P(l.PlotPaperSize),origin=P(l.PlotOrigin),device=l.PlotConfigurationName,media=l.CanonicalMediaName,style=l.CurrentStyleSheet});
 var b=(BlockTableRecord)tr.GetObject(l.BlockTableRecordId,OpenMode.ForRead);
 foreach(ObjectId id in b){var e=tr.GetObject(id,OpenMode.ForRead) as Entity;if(e==null)continue;
 var r=new Dictionary<string,object>{{"kind","entity"},{"layout",l.LayoutName},{"h",e.Handle.ToString()},{"type",e.GetType().Name},{"layer",e.Layer},{"visible",e.Visible}};
 try{r["min"]=P(e.GeometricExtents.MinPoint);r["max"]=P(e.GeometricExtents.MaxPoint);}catch{}
 if(e is BlockReference){var br=(BlockReference)e;r["block"]=((BlockTableRecord)tr.GetObject(br.BlockTableRecord,OpenMode.ForRead)).Name;r["pos"]=P(br.Position);r["scale"]=new[]{br.ScaleFactors.X,br.ScaleFactors.Y,br.ScaleFactors.Z};var attrs=new List<object>();foreach(ObjectId aid in br.AttributeCollection){var a=(AttributeReference)tr.GetObject(aid,OpenMode.ForRead);attrs.Add(new{h=a.Handle.ToString(),tag=a.Tag,text=a.TextString,pos=P(a.Position)});}r["attrs"]=attrs;}
 if(e is Viewport){var v=(Viewport)e;r["number"]=v.Number;r["center"]=P(v.CenterPoint);r["width"]=v.Width;r["height"]=v.Height;r["viewcenter"]=P(v.ViewCenter);r["viewheight"]=v.ViewHeight;r["target"]=P(v.ViewTarget);r["twist"]=v.TwistAngle;r["scale"]=v.CustomScale;r["on"]=v.On;r["frozen"]=v.GetFrozenLayers().Cast<ObjectId>().Select(i=>((LayerTableRecord)tr.GetObject(i,OpenMode.ForRead)).Name).ToArray();}
 if(e is DBText)r["text"]=((DBText)e).TextString;
 if(e is MText)r["text"]=((MText)e).Text;
 if(e is MLeader){var m=(MLeader)e;if(m.ContentType==ContentType.MTextContent)using(var t=m.MText)r["text"]=t.Text;}
 if(e is Dimension){var d=(Dimension)e;r["measurement"]=d.Measurement;r["text"]=d.DimensionText;}
 rows.Add(r);
 }}
 foreach(ObjectId id in (BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead)){var b=(BlockTableRecord)tr.GetObject(id,OpenMode.ForRead);if(b.IsFromExternalReference)rows.Add(new{kind="xref",name=b.Name,path=b.PathName,status=b.XrefStatus.ToString()});}
 foreach(ObjectId id in (LayerTable)tr.GetObject(db.LayerTableId,OpenMode.ForRead)){var l=(LayerTableRecord)tr.GetObject(id,OpenMode.ForRead);rows.Add(new{kind="layer",name=l.Name,off=l.IsOff,frozen=l.IsFrozen,plot=l.IsPlottable});}
 var imgs=RasterImageDef.GetImageDictionary(db);if(!imgs.IsNull)foreach(DBDictionaryEntry p in (DBDictionary)tr.GetObject(imgs,OpenMode.ForRead)){var im=tr.GetObject(p.Value,OpenMode.ForRead) as RasterImageDef;if(im!=null)rows.Add(new{kind="image",name=p.Key,path=im.SourceFileName});}
 }File.WriteAllText(Path.Combine(Root,name),new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Serialize(rows));}
 public static void Plot(Database db,string name){LayoutManager.Current.CurrentLayout="A";App.DocumentManager.MdiActiveDocument.Editor.Regen();using(var tr=db.TransactionManager.StartTransaction()){
 var lid=LayoutManager.Current.GetLayoutId("A");var l=(Layout)tr.GetObject(lid,OpenMode.ForRead);
 using(var s=new PlotSettings(false))using(var info=new PlotInfo{Layout=lid})using(var val=new PlotInfoValidator()){
 s.CopyFrom(l);var v=PlotSettingsValidator.Current;v.SetPlotConfigurationName(s,"DWG To PDF.pc3","ISO_full_bleed_A1_(841.00_x_594.00_MM)");v.SetPlotPaperUnits(s,PlotPaperUnit.Millimeters);v.SetPlotType(s,Autodesk.AutoCAD.DatabaseServices.PlotType.Window);v.SetPlotWindowArea(s,new Extents2d(0,0,841,594));v.SetUseStandardScale(s,true);v.SetStdScaleType(s,StdScaleType.ScaleToFit);v.SetPlotCentered(s,true);v.SetPlotRotation(s,PlotRotation.Degrees000);s.DrawViewportsFirst=true;s.PlotPlotStyles=true;try{v.SetCurrentStyleSheet(s,"monochrome.ctb");}catch{}
 s.PrintLineweights=true;info.OverrideSettings=s;val.MediaMatchingPolicy=MatchingPolicy.MatchEnabled;val.Validate(info);
 using(var engine=PlotFactory.CreatePublishEngine()){engine.BeginPlot(null,null);engine.BeginDocument(info,"Detalles de deteccion de fuga",null,1,true,Path.Combine(Root,name));using(var p=new PlotPageInfo()){engine.BeginPage(p,info,true,null);engine.BeginGenerateGraphics(null);engine.EndGenerateGraphics(null);engine.EndPage(null);}engine.EndDocument(null);engine.EndPlot(null);}
 }}}
}
