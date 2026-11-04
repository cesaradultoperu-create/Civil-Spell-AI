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
public class XrefLayoutA {
 public const string Root=@"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI\Test\XrefLayoutA-20261027";
 public const string Xname="Xref_Ubicación Hidráulica_ubicacion";
 [CommandMethod("XREFAUTOSAVEDIAG")]
 public void AutoSaveDiag(){try{using(var db=new Database(false,true)){db.ReadDwgFile(Path.Combine(Root,"Autoguardado-1829.dwg"),FileOpenMode.OpenForReadAndAllShare,true,null);Dump(db,Path.Combine(Root,"autoguardado-1829.json"));File.WriteAllText(Path.Combine(Root,"autoguardado-meta.txt"),"INSUNITS="+db.Insunits+"; INSBASE="+db.Insbase+"; TILEMODE="+db.TileMode);}}catch(System.Exception ex){File.WriteAllText(Path.Combine(Root,"autosave-diag-error.txt"),ex.ToString());}}
 [CommandMethod("XREFEXPORTDIAG")]
 public void ExportDiag(){try{using(var db=new Database(false,true)){db.ReadDwgFile(Path.Combine(Root,"layout-export.dwg"),FileOpenMode.OpenForReadAndAllShare,true,null);Dump(db,Path.Combine(Root,"layout-export.json"));}}catch(System.Exception ex){File.WriteAllText(Path.Combine(Root,"export-diag-error.txt"),ex.ToString());}}
 static readonly string[] Dependencies={Xname,"Xref_Ubicación Hidráulica","Xref_Estructuras_Proyectadas_","Xref_LD-pozas","Xref_LD-Poza de agua","Xref_Ubicación plano model"};
 [CommandMethod("XREFATUNECOPY")]
 public void TuneCopy(){try{
 var doc=App.DocumentManager.MdiActiveDocument;var db=doc.Database;
 if(!doc.Name.StartsWith(Path.Combine(Root,"Entrega"),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Only the delivery copy may be changed.");
 using(var tr=db.TransactionManager.StartTransaction()){
 var vp=(Viewport)tr.GetObject(db.GetObjectId(false,new Handle(0x14605),0),OpenMode.ForWrite);
 var lt=(LayerTable)tr.GetObject(db.LayerTableId,OpenMode.ForRead);
 var keep=lt.Cast<ObjectId>().Where(id=>{string n=((LayerTableRecord)tr.GetObject(id,OpenMode.ForRead)).Name;return n=="_XREF_UBICACION_LAYOUT_A"||n=="_Ubi_Hidraulia"||Dependencies.Any(x=>n.StartsWith(x+"|",StringComparison.OrdinalIgnoreCase));}).ToArray();
 vp.ThawLayersInViewport(keep.GetEnumerator());
 var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead);
 foreach(var n in Dependencies){if(bt.Has(n)){var b=(BlockTableRecord)tr.GetObject(bt[n],OpenMode.ForWrite);b.PathName=@".\Xref\"+n+".dwg";}}
 tr.Commit();}
 doc.Editor.Regen();db.SaveAs(Path.Combine(Root,"Entrega","1107.001.00.14-200-14-P-005-XREF-A-LISTO.dwg"),DwgVersion.Current);
 using(var check=new Database(false,true)){check.ReadDwgFile(Path.Combine(Root,"Entrega","1107.001.00.14-200-14-P-005-XREF-A-LISTO.dwg"),FileOpenMode.OpenForReadAndAllShare,true,null);Dump(check,Path.Combine(Root,"final-saved.json"));}
 }catch(System.Exception ex){File.WriteAllText(Path.Combine(Root,"tune-error.txt"),ex.ToString());}}
 [CommandMethod("XREFAVERIFY")]
 public void Verify(){try{
 var doc=App.DocumentManager.MdiActiveDocument;var db=doc.Database;
 if(!doc.Name.StartsWith(Path.Combine(Root,"Entrega"),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Only the delivery copy may be rendered.");
 LayoutManager.Current.CurrentLayout="A";doc.Editor.Regen();Dump(db,Path.Combine(Root,"main-reopened.json"));
 var layoutId=LayoutManager.Current.GetLayoutId("A");
 using(var tr=db.TransactionManager.StartTransaction()){
 var layout=(Layout)tr.GetObject(layoutId,OpenMode.ForRead);
 using(var settings=new PlotSettings(false))using(var info=new PlotInfo{Layout=layoutId})using(var validator=new PlotInfoValidator()){
 settings.CopyFrom(layout);var v=PlotSettingsValidator.Current;
 v.SetPlotConfigurationName(settings,"DWG To PDF.pc3","ISO_full_bleed_A1_(841.00_x_594.00_MM)");v.SetPlotPaperUnits(settings,PlotPaperUnit.Millimeters);
 v.SetPlotType(settings,Autodesk.AutoCAD.DatabaseServices.PlotType.Window);v.SetPlotWindowArea(settings,new Extents2d(0,0,841,594));v.SetUseStandardScale(settings,true);v.SetStdScaleType(settings,StdScaleType.ScaleToFit);v.SetPlotCentered(settings,true);v.SetPlotRotation(settings,PlotRotation.Degrees000);settings.DrawViewportsFirst=true;settings.PlotPlotStyles=true;
 try{v.SetCurrentStyleSheet(settings,"WWL-Full.ctb");}catch{}
 info.OverrideSettings=settings;validator.MediaMatchingPolicy=MatchingPolicy.MatchEnabled;validator.Validate(info);
 using(var engine=PlotFactory.CreatePublishEngine()){
 engine.BeginPlot(null,null);engine.BeginDocument(info,doc.Name,null,1,true,Path.Combine(Root,"preview.pdf"));
 using(var page=new PlotPageInfo()){engine.BeginPage(page,info,true,null);engine.BeginGenerateGraphics(null);engine.EndGenerateGraphics(null);engine.EndPage(null);}engine.EndDocument(null);engine.EndPlot(null);
 }} }
 }catch(System.Exception ex){File.WriteAllText(Path.Combine(Root,"verify-error.txt"),ex.ToString());}}
 [CommandMethod("XREFAFIXCOPIES")]
 public void FixCopies(){try{
 var output=Path.Combine(Root,"Entrega");Directory.CreateDirectory(Path.Combine(output,"Xref"));
 var source=Path.Combine(Root,"Fuente","Xref",Xname+".dwg");
 var corrected=Path.Combine(output,"Xref",Xname+".dwg");
 var logs=new List<string>();
 using(var db=new Database(false,true)){
 db.ReadDwgFile(source,FileOpenMode.OpenForReadAndAllShare,true,null);db.CloseInput(true);
 using(var tr=db.TransactionManager.StartTransaction()){
 foreach(ObjectId id in (BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead)){
 var b=(BlockTableRecord)tr.GetObject(id,OpenMode.ForRead);
 if(b.IsFromExternalReference&&b.IsFromOverlayReference){b.UpgradeOpen();b.IsFromOverlayReference=false;logs.Add("Overlay -> Attach: "+b.Name);}
 }tr.Commit();}
 db.SaveAs(corrected,DwgVersion.Current);Dump(db,Path.Combine(Root,"xref-fixed.json"));
 }
 {
 var doc=App.DocumentManager.MdiActiveDocument;
 if(!doc.Name.StartsWith(Path.Combine(Root,"Fuente"),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Only the isolated source copy may be changed.");
 var db=doc.Database;
 var previous=HostApplicationServices.WorkingDatabase;
 try{
 HostApplicationServices.WorkingDatabase=db;
 ObjectId xref=db.OverlayXref(corrected,Xname);
 using(var tr=db.TransactionManager.StartTransaction()){
 var lt=(LayerTable)tr.GetObject(db.LayerTableId,OpenMode.ForRead);
 const string lname="_XREF_UBICACION_LAYOUT_A";
 ObjectId lid;
 if(lt.Has(lname))lid=lt[lname];else{lt.UpgradeOpen();var l=new LayerTableRecord{Name=lname};lid=lt.Add(l);tr.AddNewlyCreatedDBObject(l,true);}
 var ms=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForWrite);
 var br=new BlockReference(Point3d.Origin,xref){LayerId=lid};ms.AppendEntity(br);tr.AddNewlyCreatedDBObject(br,true);
 var laydict=(DBDictionary)tr.GetObject(db.LayoutDictionaryId,OpenMode.ForRead);
 var a=(Layout)tr.GetObject(laydict.GetAt("A"),OpenMode.ForRead);
 foreach(ObjectId id in (BlockTableRecord)tr.GetObject(a.BlockTableRecordId,OpenMode.ForRead)){
 var vp=tr.GetObject(id,OpenMode.ForRead) as Viewport;if(vp==null||vp.Handle.ToString()=="75A8")continue;
 vp.UpgradeOpen();
 if(vp.Handle.ToString()=="14605"){
 var keep=lt.Cast<ObjectId>().Where(i=>i==lid||((LayerTableRecord)tr.GetObject(i,OpenMode.ForRead)).Name.StartsWith(Xname+"|",StringComparison.OrdinalIgnoreCase)||((LayerTableRecord)tr.GetObject(i,OpenMode.ForRead)).Name=="_Ubi_Hidraulia").ToArray();
 vp.FreezeLayersInViewport(lt.Cast<ObjectId>().Except(keep).ToArray().GetEnumerator());vp.ThawLayersInViewport(keep.GetEnumerator());
 vp.Locked=false;vp.On=true;vp.ViewDirection=Vector3d.ZAxis;vp.ViewTarget=Point3d.Origin;vp.TwistAngle=0;
 vp.ViewCenter=new Point2d(579257.262041458,8302264.916087536);vp.ViewHeight=vp.Height/0.2;vp.Locked=true;
 logs.Add("Layout A: viewport 14605; Xref at 0,0,0; scale 1; viewport 1:5000 locked.");
 }else vp.FreezeLayersInViewport(new[]{lid}.GetEnumerator());
 }
 ((BlockTableRecord)tr.GetObject(xref,OpenMode.ForWrite)).PathName=@".\Xref\"+Xname+".dwg";
 tr.Commit();}
 Dump(db,Path.Combine(Root,"main-fixed.json"));
 doc.Editor.Regen();
 db.SaveAs(Path.Combine(output,"1107.001.00.14-200-14-P-005-XREF-A.dwg"),DwgVersion.Current);
 }finally{HostApplicationServices.WorkingDatabase=previous;}
 }
 File.WriteAllLines(Path.Combine(Root,"correccion.txt"),logs);
 }catch(System.Exception ex){File.WriteAllText(Path.Combine(Root,"fix-error.txt"),ex.ToString());}}
 static double[] P(Point3d p)=>new[]{p.X,p.Y,p.Z};
 static double[] P(Point2d p)=>new[]{p.X,p.Y};
 [CommandMethod("XREFADIAG")]
 public void Run(){try{
   using(var db=new Database(false,true)){
     db.ReadDwgFile(Path.Combine(Root,"Fuente","1107.001.00.14-200-14-P-005.dwg"),FileOpenMode.OpenForReadAndAllShare,true,null);
     Dump(db,Path.Combine(Root,"main.json"));
   }
   using(var db=new Database(false,true)){
     db.ReadDwgFile(Path.Combine(Root,"Fuente","Xref","Xref_Ubicación Hidráulica_ubicacion.dwg"),FileOpenMode.OpenForReadAndAllShare,true,null);
     Dump(db,Path.Combine(Root,"xref.json"));
   }
 }catch(System.Exception ex){File.WriteAllText(Path.Combine(Root,"error.txt"),ex.ToString());}}
 static void Dump(Database db,string path){var rows=new List<object>();using(var tr=db.TransactionManager.StartTransaction()){
 var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead);
 foreach(ObjectId bid in bt){var b=(BlockTableRecord)tr.GetObject(bid,OpenMode.ForRead);
 if(b.IsFromExternalReference){rows.Add(new{kind="xref",name=b.Name,path=b.PathName,unloaded=b.IsUnloaded,overlay=b.IsFromOverlayReference,h=b.Handle.ToString()});continue;}
 if(!b.IsLayout)continue; string layout=b.IsLayout?((Layout)tr.GetObject(b.LayoutId,OpenMode.ForRead)).LayoutName:b.Name;
 foreach(ObjectId id in b){var e=tr.GetObject(id,OpenMode.ForRead) as Entity;if(e==null)continue;
 var r=new Dictionary<string,object>{{"kind","entity"},{"layout",layout},{"h",e.Handle.ToString()},{"type",e.GetType().Name},{"layer",e.Layer},{"visible",e.Visible}};
 try{r["min"]=P(e.GeometricExtents.MinPoint);r["max"]=P(e.GeometricExtents.MaxPoint);}catch{}
 if(e is BlockReference br){r["block"]=((BlockTableRecord)tr.GetObject(br.BlockTableRecord,OpenMode.ForRead)).Name;r["pos"]=P(br.Position);r["scale"]=new[]{br.ScaleFactors.X,br.ScaleFactors.Y,br.ScaleFactors.Z};r["rotation"]=br.Rotation;}
 if(e is Viewport vp){r["number"]=vp.Number;r["center"]=P(vp.CenterPoint);r["width"]=vp.Width;r["height"]=vp.Height;r["viewcenter"]=P(vp.ViewCenter);r["viewheight"]=vp.ViewHeight;r["target"]=P(vp.ViewTarget);r["direction"]=P(new Point3d(vp.ViewDirection.X,vp.ViewDirection.Y,vp.ViewDirection.Z));r["twist"]=vp.TwistAngle;r["scale"]=vp.CustomScale;r["on"]=vp.On;r["locked"]=vp.Locked;r["nonrect"]=vp.NonRectClipOn;r["clip"]=vp.NonRectClipEntityId.IsNull?"":vp.NonRectClipEntityId.Handle.ToString();r["frozen"]=vp.GetFrozenLayers().Cast<ObjectId>().Select(l=>((LayerTableRecord)tr.GetObject(l,OpenMode.ForRead)).Name).ToArray();}
 if(e is DBText tx)r["text"]=tx.TextString;
 if(e is MText mt)r["text"]=mt.Text;
 if(e is Polyline pl){r["vertices"]=Enumerable.Range(0,pl.NumberOfVertices).Select(i=>P(pl.GetPoint3dAt(i))).ToArray();r["closed"]=pl.Closed;}
 rows.Add(r);
 }}
 foreach(ObjectId id in (LayerTable)tr.GetObject(db.LayerTableId,OpenMode.ForRead)){var l=(LayerTableRecord)tr.GetObject(id,OpenMode.ForRead);rows.Add(new{kind="layer",name=l.Name,h=l.Handle.ToString(),off=l.IsOff,frozen=l.IsFrozen,plot=l.IsPlottable});}
 }
 File.WriteAllText(path,new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Serialize(rows));}
}
