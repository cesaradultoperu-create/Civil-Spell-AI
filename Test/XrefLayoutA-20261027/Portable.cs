using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices.Filters;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.PlottingServices;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;
public class Portable {
 const string Root=@"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI\Test\XrefLayoutA-20261027";
 const string Name="Xref_Ubicacion_Hidraulica_LAYOUT.dwg";
 static string Folder=>Path.Combine(Root,"Para_Layout");
 static void Log(string s)=>File.AppendAllText(Path.Combine(Root,"portable-log.txt"),s+Environment.NewLine);
 static void RequireEmpty(Database db){using(var t=db.TransactionManager.StartTransaction()){if(((BlockTableRecord)t.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForRead)).Cast<ObjectId>().Any())throw new InvalidOperationException("Only a new isolated empty drawing may be used.");}}
 [CommandMethod("UBICACIONCREARLAYOUT")]
 public void Build(){try{
 var doc=App.DocumentManager.MdiActiveDocument;var db=doc.Database;RequireEmpty(db);Directory.CreateDirectory(Path.Combine(Folder,"Assets"));
 var source=Path.Combine(Root,"Entrega","Xref","Xref_Ubicación Hidráulica_ubicacion.dwg");
 db.Insunits=UnitsValue.Meters;db.Insbase=Point3d.Origin;
 var rootId=db.AttachXref(source,"UBICACION_BASE");ObjectId brId;
 using(var tr=db.TransactionManager.StartTransaction()){
 var ms=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForWrite);var br=new BlockReference(Point3d.Origin,rootId);brId=ms.AppendEntity(br);tr.AddNewlyCreatedDBObject(br,true);tr.Commit();}
 var imageSources=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
 using(var tr=db.TransactionManager.StartTransaction()){
 foreach(ObjectId id in (BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead)){
 var b=(BlockTableRecord)tr.GetObject(id,OpenMode.ForRead);if(!b.IsFromExternalReference)continue;
 var child=b.GetXrefDatabase(false);if(child==null)continue;
 using(var ct=child.TransactionManager.StartTransaction()){
 var imageDict=RasterImageDef.GetImageDictionary(child);if(imageDict.IsNull)continue;
 foreach(DBDictionaryEntry entry in (DBDictionary)ct.GetObject(imageDict,OpenMode.ForRead)){
 var def=ct.GetObject(entry.Value,OpenMode.ForRead) as RasterImageDef;if(def==null)continue;
 var raw=def.SourceFileName;var candidates=new[]{raw,Path.Combine(Path.GetDirectoryName(child.Filename),raw)};
 var found=candidates.FirstOrDefault(File.Exists);if(found!=null)imageSources[Path.GetFileName(raw)]=found;Log("Raster source "+raw+" -> "+found);
 }}}
 }
 db.BindXrefs(new ObjectIdCollection(new[]{rootId}),false);Log("Bound source drawing and attached children.");
 const double width=99.2437129890468,height=99.1468898544233,scale=0.2;
 double xmin=579257.262041458-width/(2*scale),ymin=8302264.916087536-height/(2*scale);
 using(var tr=db.TransactionManager.StartTransaction()){
 var br=(BlockReference)tr.GetObject(brId,OpenMode.ForWrite);
 var master=(BlockTableRecord)tr.GetObject(br.BlockTableRecord,OpenMode.ForRead);
 foreach(ObjectId id in master){var mt=tr.GetObject(id,OpenMode.ForRead) as MText;if(mt!=null&&mt.Text=="8 302 018 mN"){mt.UpgradeOpen();mt.Visible=false;}}
 br.CreateExtensionDictionary();var extension=(DBDictionary)tr.GetObject(br.ExtensionDictionary,OpenMode.ForWrite);var filters=new DBDictionary();extension.SetAt("ACAD_FILTER",filters);tr.AddNewlyCreatedDBObject(filters,true);
 var sf=new SpatialFilter{Definition=new SpatialFilterDefinition(new Point2dCollection(new[]{new Point2d(xmin,ymin),new Point2d(xmin+width/scale,ymin+height/scale)}),Vector3d.ZAxis,0,0,0,true)};filters.SetAt("SPATIAL",sf);tr.AddNewlyCreatedDBObject(sf,true);
 br.TransformBy(Matrix3d.Displacement(new Vector3d(-xmin*scale,-ymin*scale,0))*Matrix3d.Scaling(scale,Point3d.Origin));
 var ms=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForWrite);
 var frame=new Polyline();frame.AddVertexAt(0,new Point2d(0,0),0,0,0);frame.AddVertexAt(1,new Point2d(width,0),0,0,0);frame.AddVertexAt(2,new Point2d(width,height),0,0,0);frame.AddVertexAt(3,new Point2d(0,height),0,0,0);frame.Closed=true;ms.AppendEntity(frame);tr.AddNewlyCreatedDBObject(frame,true);
 for(int i=0;i<4;i++){double east=579100+100*i;var tx=new DBText{TextString=east.ToString("### ###")+" mE",Height=1.5,HorizontalMode=TextHorizontalMode.TextCenter,VerticalMode=TextVerticalMode.TextVerticalMid,Position=new Point3d((east-xmin)*scale,height-1.3,0),AlignmentPoint=new Point3d((east-xmin)*scale,height-1.3,0)};ms.AppendEntity(tx);tr.AddNewlyCreatedDBObject(tx,true);}
 tr.Commit();}
 ClonePaper(db,Path.Combine(Root,"Entrega","1107.001.00.14-200-14-P-005-XREF-A-LISTO.dwg"),new[]{0x144EE,0x14553,0x14554,0x1C3BF});
 using(var tr=db.TransactionManager.StartTransaction()){
 var imageDict=RasterImageDef.GetImageDictionary(db);if(!imageDict.IsNull){foreach(DBDictionaryEntry entry in (DBDictionary)tr.GetObject(imageDict,OpenMode.ForRead)){
 var def=tr.GetObject(entry.Value,OpenMode.ForWrite) as RasterImageDef;if(def==null)continue;
 var raw=def.SourceFileName;string found;if(!imageSources.TryGetValue(Path.GetFileName(raw),out found)){found=File.Exists(raw)?raw:null;}
 if(found==null)throw new FileNotFoundException("The map raster could not be packaged",raw);
 if(!File.Exists(raw))def.SourceFileName=found;def.Load();Log("Raster retained at original project path: "+def.SourceFileName);
 }}
 int external=((BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead)).Cast<ObjectId>().Select(id=>(BlockTableRecord)tr.GetObject(id,OpenMode.ForRead)).Count(b=>b.IsFromExternalReference);if(external!=0)throw new InvalidOperationException("Nested DWGs remain: "+external);tr.Commit();}
 db.TileMode=true;using(var view=new ViewTableRecord{CenterPoint=new Point2d(width/2,height/2),Height=110,Width=110,ViewDirection=Vector3d.ZAxis,Target=Point3d.Origin})doc.Editor.SetCurrentView(view);
 doc.Editor.Regen();db.SaveAs(Path.Combine(Folder,Name),DwgVersion.Current);File.WriteAllText(Path.Combine(Root,"portable-built.txt"),"No viewports; no nested DWG references; origin 0,0; size "+width+" x "+height+"; units matching project: Meters.");
 }catch(System.Exception ex){File.WriteAllText(Path.Combine(Root,"portable-error.txt"),ex.ToString());}}
 static void ClonePaper(Database destination,string source,int[] handles){using(var src=new Database(false,true)){src.ReadDwgFile(source,FileOpenMode.OpenForReadAndAllShare,true,null);src.CloseInput(true);var ids=new ObjectIdCollection(handles.Select(h=>src.GetObjectId(false,new Handle(h),0)).ToArray());var map=new IdMapping();src.WblockCloneObjects(ids,SymbolUtilityServices.GetBlockModelSpaceId(destination),map,DuplicateRecordCloning.MangleName,false);using(var tr=destination.TransactionManager.StartTransaction()){foreach(IdPair p in map){if(!p.IsPrimary)continue;var e=tr.GetObject(p.Value,OpenMode.ForWrite) as Entity;if(e!=null)e.TransformBy(Matrix3d.Displacement(new Vector3d(-725.8781435054627,-478.50000000051523,0)));}tr.Commit();}}}
 [CommandMethod("UBICACIONPROBARLAYOUT")]
 public void Test(){try{
 var doc=App.DocumentManager.MdiActiveDocument;var db=doc.Database;RequireEmpty(db);db.Insunits=UnitsValue.Meters;LayoutManager.Current.CurrentLayout="Layout1";var layoutId=LayoutManager.Current.GetLayoutId("Layout1");var xref=db.OverlayXref(Path.Combine(Folder,Name),"PRUEBA_UBICACION");
 using(var tr=db.TransactionManager.StartTransaction()){
 var layout=(Layout)tr.GetObject(layoutId,OpenMode.ForRead);var ps=(BlockTableRecord)tr.GetObject(layout.BlockTableRecordId,OpenMode.ForWrite);foreach(ObjectId id in ps){var vp=tr.GetObject(id,OpenMode.ForRead) as Viewport;if(vp!=null&&vp.Number>1){vp.UpgradeOpen();vp.On=false;}}
 var br=new BlockReference(new Point3d(50,50,0),xref);ps.AppendEntity(br);tr.AddNewlyCreatedDBObject(br,true);tr.Commit();}
 doc.Editor.Regen();Directory.CreateDirectory(Path.Combine(Root,"Prueba_Layout"));db.SaveAs(Path.Combine(Root,"Prueba_Layout","Insertado_en_otro_dibujo.dwg"),DwgVersion.Current);
 using(var tr=db.TransactionManager.StartTransaction()){
 var layout=(Layout)tr.GetObject(layoutId,OpenMode.ForRead);
 using(var settings=new PlotSettings(false))using(var info=new PlotInfo{Layout=layoutId})using(var validator=new PlotInfoValidator()){
 settings.CopyFrom(layout);var v=PlotSettingsValidator.Current;v.SetPlotConfigurationName(settings,"DWG To PDF.pc3","ISO_full_bleed_A4_(210.00_x_297.00_MM)");v.SetPlotPaperUnits(settings,PlotPaperUnit.Millimeters);v.SetPlotWindowArea(settings,new Extents2d(0,0,210,297));v.SetPlotType(settings,Autodesk.AutoCAD.DatabaseServices.PlotType.Window);v.SetUseStandardScale(settings,true);v.SetStdScaleType(settings,StdScaleType.ScaleToFit);v.SetPlotCentered(settings,true);v.SetPlotRotation(settings,PlotRotation.Degrees000);settings.PlotPlotStyles=true;settings.DrawViewportsFirst=true;
 try{v.SetCurrentStyleSheet(settings,"WWL-Full.ctb");}catch{}
 info.OverrideSettings=settings;validator.MediaMatchingPolicy=MatchingPolicy.MatchEnabled;validator.Validate(info);using(var engine=PlotFactory.CreatePublishEngine()){
 engine.BeginPlot(null,null);engine.BeginDocument(info,doc.Name,null,1,true,Path.Combine(Root,"portable-test.pdf"));using(var page=new PlotPageInfo()){engine.BeginPage(page,info,true,null);engine.BeginGenerateGraphics(null);engine.EndGenerateGraphics(null);engine.EndPage(null);}engine.EndDocument(null);engine.EndPlot(null);
 }} }
 }catch(System.Exception ex){File.WriteAllText(Path.Combine(Root,"portable-test-error.txt"),ex.ToString());}}
}
