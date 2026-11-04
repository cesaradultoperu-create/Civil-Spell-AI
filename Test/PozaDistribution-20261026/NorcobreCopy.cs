using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.PlottingServices;
using System.Text.RegularExpressions;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;
public class NorcobreCopy {
 const string Root=@"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI\Test\PozaDistribution-20261026";
 static string Reference=>Directory.GetFiles(@"C:\Users\Cesar Ugaz\Pictures\trabajo para codE","PLANO DE REFERENCIA*.dwg").Single();
 static void Add(Transaction tr,BlockTableRecord b,Entity e){b.AppendEntity(e);tr.AddNewlyCreatedDBObject(e,true);}
 static bool In(Extents3d b,double x0,double y0,double x1,double y1)=>b.MinPoint.X>=x0 && b.MaxPoint.X<=x1 && b.MinPoint.Y>=y0 && b.MaxPoint.Y<=y1;
 static string N(double n)=>n.ToString("N2",CultureInfo.InvariantCulture);
 static void ScaleTexts(Transaction tr,ObjectId blockId,double factor,string caption,HashSet<ObjectId> seen){if(!seen.Add(blockId))return;var b=(BlockTableRecord)tr.GetObject(blockId,OpenMode.ForRead);foreach(ObjectId id in b){var en=tr.GetObject(id,OpenMode.ForRead);if(en is BlockReference br){ScaleTexts(tr,br.BlockTableRecord,factor,caption,seen);continue;}string s=en is DBText d?d.TextString:en is MText m?m.Text:null;if(s==null)continue;string value=s;if(s.ToUpperInvariant().Contains("ESCALA"))value=caption;else if(Math.Abs(factor-1)>1e-10){var match=Regex.Match(s.Trim(),@"^(\d+(?:[.,]\d+)?)(\s*m)?$");if(match.Success && double.TryParse(match.Groups[1].Value.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out var number))value=(number*factor).ToString("0.##",CultureInfo.InvariantCulture)+match.Groups[2].Value;}if(value!=s){en.UpgradeOpen();if(en is DBText dt)dt.TextString=value;else ((MText)en).Contents=value;}}}
 [CommandMethod("NORCCOPY")]
 public void Run(){
  var doc=App.DocumentManager.MdiActiveDocument;var db=doc.Database;var filename=Path.GetFileNameWithoutExtension(doc.Name);var code=filename.Substring(filename.Length-3);var serializer=new JavaScriptSerializer{MaxJsonLength=int.MaxValue};var data=serializer.Deserialize<DrawingData>(File.ReadAllText(Path.Combine(Root,"Data","drawing_"+code+".json")));var raw=(Dictionary<string,object>)serializer.DeserializeObject(File.ReadAllText(Path.Combine(Root,"Data","drawing_"+code+".json")));var metric=(Dictionary<string,object>)raw["metrics"];
  double lining=Convert.ToDouble(metric["lining"]),cut=Convert.ToDouble(metric["cut"]),fill=Convert.ToDouble(metric["fill"]),storage=Convert.ToDouble(metric["storage"]),level=Convert.ToDouble(metric["level"]);var rfile=code=="008"?"FEATURE_872E.csv":code=="009"?"FEATURE_17D58.csv":"POZAS_1C44A.csv";var rim=File.ReadAllLines(Path.Combine(Root,"Data",rfile)).Where(s=>s.Length>0).Select(s=>s.Split(',').Select(v=>double.Parse(v,CultureInfo.InvariantCulture)).ToArray()).ToArray();double perimeter=0;for(int i=1;i<rim.Length;i++)perimeter+=new Point2d(rim[i][0],rim[i][1]).GetDistanceTo(new Point2d(rim[i-1][0],rim[i-1][1]));
  string outdir=Path.Combine(Root,"Copia_Norcobre");Directory.CreateDirectory(outdir);var report=new List<string>();
  LayoutManager.Current.CurrentLayout="SOTRAMI";
  using(var source=new Database(false,true)){
   source.ReadDwgFile(Reference,FileOpenMode.OpenForReadAndAllShare,true,null);source.CloseInput(true);
   using(var tr=db.TransactionManager.StartTransaction())using(var st=source.TransactionManager.StartTransaction()){
    var ld=(DBDictionary)tr.GetObject(db.LayoutDictionaryId,OpenMode.ForRead);var layout=(Layout)tr.GetObject(ld.GetAt("SOTRAMI"),OpenMode.ForWrite);var ps=(BlockTableRecord)tr.GetObject(layout.BlockTableRecordId,OpenMode.ForWrite);
    var clips=new HashSet<ObjectId>();foreach(ObjectId id in ps)if(tr.GetObject(id,OpenMode.ForRead) is Viewport cv && cv.NonRectClipOn && !cv.NonRectClipEntityId.IsNull)clips.Add(cv.NonRectClipEntityId);
    foreach(ObjectId id in ps.Cast<ObjectId>().ToArray()){
     var en=tr.GetObject(id,OpenMode.ForRead) as Entity;if(en==null)continue;if(en is RasterImage||en is Viewport||clips.Contains(id))continue;
     if(en is BlockReference br && ((BlockTableRecord)tr.GetObject(br.BlockTableRecord,OpenMode.ForRead)).Name=="MEMB_WWL_P_A1_")continue;en.UpgradeOpen();en.Erase();
    }
    var sd=(DBDictionary)st.GetObject(source.LayoutDictionaryId,OpenMode.ForRead);var sl=(Layout)st.GetObject(sd.GetAt("101"),OpenMode.ForRead);var sp=(BlockTableRecord)st.GetObject(sl.BlockTableRecordId,OpenMode.ForRead);var selected=new ObjectIdCollection();var roles=new Dictionary<ObjectId,string>();var sourceViews=new List<Viewport>();
    foreach(ObjectId id in sp){var en=st.GetObject(id,OpenMode.ForRead) as Entity;if(en==null)continue;if(en is Viewport vp){if(vp.CenterPoint.X>600 && vp.CenterPoint.X<715 && vp.CenterPoint.Y>430 && vp.CenterPoint.Y<590)sourceViews.Add(vp);continue;}if(en is RasterImage)continue;
     Extents3d bounds;try{bounds=en.GeometricExtents;}catch{continue;}string role=null;
     if(en is Table table && table.Position.Y<600 && table.Position.X>530 && table.Position.X<720)role="table";
     else if(en is MText note && note.Text.StartsWith("NOTAS:") && In(bounds,600,70,835,310))role="notes";
     else if(In(bounds,725,310,831,478))role="legend-map";
     else if(In(bounds,580,430,716,590))role="anchor";
     else if(In(bounds,70,525,100,565))role="north";
     else if(en is MText mt && In(bounds,320,309,435,331))role="plan-title";
     else if(en is BlockReference b){var def=(BlockTableRecord)st.GetObject(b.BlockTableRecord,OpenMode.ForRead);if(def.Name=="Section Title" && b.Position.X>0 && b.Position.X<600 && b.Position.Y>120 && b.Position.Y<160)role="section-title";if(def.Name=="*U177")role="scale-bar";}
     if(role!=null){selected.Add(id);roles[id]=role;}
    }
    var map=new IdMapping();source.WblockCloneObjects(selected,ps.ObjectId,map,DuplicateRecordCloning.MangleName,false);
    foreach(IdPair pair in map){if(!pair.IsPrimary||!roles.ContainsKey(pair.Key))continue;var role=roles[pair.Key];var en=tr.GetObject(pair.Value,OpenMode.ForWrite) as Entity;if(en==null)continue;
     if(en is Table table){if(table.Rows.Count==5 && table.Columns.Count==2){table.Cells[2,0].TextString="GEOTEXTIL NO TEJIDO DE 400 g/m²";table.Cells[2,1].TextString=N(lining)+" m²";table.Cells[3,1].TextString=N(lining)+" m²";table.Cells[4,0].TextString="PERÍMETRO DE CORONACIÓN";table.Cells[4,1].TextString=N(perimeter)+" m";}
      if(table.Rows.Count==4 && table.Columns.Count==5){table.Cells[2,0].TextString="CORTE TOTAL TN / DISEÑO";table.Cells[2,1].TextString=N(cut);table.Cells[3,0].TextString="RELLENO TOTAL TN / DISEÑO";table.Cells[3,1].TextString=N(fill);for(int r=2;r<4;r++)for(int c=2;c<5;c++)table.Cells[r,c].TextString="—";}table.GenerateLayout();table.RecomputeTableBlock(true);report.Add("TABLA NATIVA "+table.Position);continue;
     }
     if(en is MText mt){
      string c=mt.Contents;
      if(role=="notes"){
       c=c.Replace("NORCOBRE S.A.C.","SOTRAMI").Replace("MARZO DEL 2025","AGOSTO DEL 2026");
       c=c.Replace("LA POZA DE SUBDRENAJE ESTÁ DISEÑADA PARA LA CAPTACIÓN DE AGUAS SUBSUPERFICIALES GENERADAS EN EL INTERIOR DEL ÁREA DE LA DESMONTERA, PROVENIENTES DEL SISTEMA DE SUBDRENAJE INSTALADO.","LA GEOMETRÍA DE LA "+data.title+" CORRESPONDE A LAS SUPERFICIES DE DISEÑO DEL PROYECTO SOTRAMI REPRESENTADAS EN ESTA LÁMINA.");
       c=c.Replace("LA POZA CUENTA CON UNA CAPACIDAD DE ALMACENAMIENTO DE 19m3, DISEÑADA PARA GARANTIZAR LA CAPTACIÓN, MANEJO Y EVACUACIÓN DE LAS AGUAS MEDIANTE UN ALIVIADERO DE CONCRETO, EL CUAL DESCARGA EL AGUA HACIA EL CANAL EXISTENTE UBICADO CONTIGUOP A LA POZA.","EL VOLUMEN GEOMÉTRICO HASTA EL. "+N(level)+" m ES "+N(storage)+" m³. LAS CANTIDADES DE CORTE Y RELLENO COMPARAN TN Y DISEÑO EN MALLA DE 0.25 m, SIN FACTORES NI SOBREEXCAVACIÓN POR REVESTIMIENTO.");
       c=c.Replace("LA POZA DE SUBDRENAJE CONTARÁ", "LA "+data.title+" CONTARÁ");
      }else if(role=="plan-title"){if(mt.Text.Contains("ESCALA"))c="ESCALA 1:"+Math.Round(1000/data.views.Single(v=>v.role=="plan").scale);else if(mt.Text.Contains("POZA DE"))c=data.title;}
      else if(role=="legend-map")c=c.Replace("POZA DE SUBDRENAJE",data.title);
      mt.Contents=c;
     }
     if(en is MLeader ml && ml.ContentType==ContentType.MTextContent){using(var label=ml.MText){if(label.Text.ToUpperInvariant().Contains("GEOTEXTIL")){label.Contents=label.Contents.Replace("g/cm","g/m");ml.MText=label;}}}
     if(en is BlockReference scaleRef && (role=="scale-bar" || role=="legend-map")){
      string blockName=((BlockTableRecord)tr.GetObject(scaleRef.BlockTableRecord,OpenMode.ForRead)).Name;if(role=="scale-bar" || blockName.ToLowerInvariant().Contains("scale") || (scaleRef.Position.X>725 && scaleRef.Position.Y>447 && scaleRef.Position.Y<462)){double denom=role=="scale-bar"?Math.Round(1000/data.views.Single(v=>v.role=="plan").scale):30000;ScaleTexts(tr,scaleRef.BlockTableRecord,role=="scale-bar"?denom/100:1,"ESCALA 1:"+denom.ToString("#,0",CultureInfo.InvariantCulture),new HashSet<ObjectId>());foreach(ObjectId aid in scaleRef.AttributeCollection){var a=(AttributeReference)tr.GetObject(aid,OpenMode.ForWrite);if(a.TextString.ToUpperInvariant().Contains("ESCALA")){a.TextString="ESCALA 1:"+denom.ToString("#,0",CultureInfo.InvariantCulture);a.AdjustAlignment(db);}}}
     }
     if(en is BlockReference br && role=="section-title"){
      var view=data.views.Single(v=>v.role==(br.Position.X<300?"section1":"section2"));double den=Math.Round(1000/view.scale),vex=code=="009"?1:2;
      foreach(ObjectId aid in br.AttributeCollection){var a=(AttributeReference)tr.GetObject(aid,OpenMode.ForWrite);if(a.Tag=="TITLE"){a.TextString=data.title.Replace("POZA DE ","POZA ");if(code=="010")a.WidthFactor*=.8;a.AdjustAlignment(db);}if(a.Tag=="SCALE")a.TextString="H1:"+den+" V1:"+(den/vex).ToString("0.##",CultureInfo.InvariantCulture);}
     }
     report.Add(role+" "+en.GetType().Name);
    }
    // Copy the actual anchor and pipe detail geometry, without importing the NORCOBRE terrain.
    var ms=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForWrite);var sm=(BlockTableRecord)st.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(source),OpenMode.ForRead);var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForWrite);
    foreach(var sv in sourceViews){var center=new Point3d(sv.ViewCenter.X+sv.ViewTarget.X,sv.ViewCenter.Y+sv.ViewTarget.Y,0);var ids=new ObjectIdCollection();foreach(ObjectId id in sm){var en=st.GetObject(id,OpenMode.ForRead) as Entity;if(en==null || en.GetType().Namespace!="Autodesk.AutoCAD.DatabaseServices")continue;try{var b=en.GeometricExtents;if(b.MaxPoint.X-b.MinPoint.X<10 && b.MaxPoint.Y-b.MinPoint.Y<10 && b.MinPoint.X<center.X+3 && b.MaxPoint.X>center.X-3 && b.MinPoint.Y<center.Y+3 && b.MaxPoint.Y>center.Y-3)ids.Add(id);}catch{}}
     bool small=sv.Height<40;var target=new Point3d(580000+(small?20:0),8302800,0);var block=new BlockTableRecord{Name="NORCOBRE_"+(small?"TUBO_":"ZANJA_")+code,Origin=center};bt.Add(block);tr.AddNewlyCreatedDBObject(block,true);source.WblockCloneObjects(ids,block.ObjectId,new IdMapping(),DuplicateRecordCloning.MangleName,false);Add(tr,ms,new BlockReference(target,block.ObjectId));
     foreach(ObjectId vid in ps.Cast<ObjectId>().ToArray())if(tr.GetObject(vid,OpenMode.ForRead) is Viewport dv && Math.Abs(dv.CenterPoint.X-sv.CenterPoint.X)<.1 && Math.Abs(dv.CenterPoint.Y-sv.CenterPoint.Y)<.1){dv.UpgradeOpen();dv.Locked=false;dv.ViewDirection=Vector3d.ZAxis;dv.TwistAngle=0;dv.ViewTarget=target;dv.ViewCenter=Point2d.Origin;dv.ViewHeight=sv.Height/sv.CustomScale;dv.CustomScale=sv.CustomScale;dv.On=true;dv.Locked=true;}
     report.Add("GEOMETRIA "+(small?"TUBO":"ZANJA")+" entidades="+ids.Count);
    }
    // A fine black border for the SOTRAMI location raster.
    var border=new Polyline();var corners=new[]{new Point2d(725.5,478.5),new Point2d(825.5,478.5),new Point2d(825.5,578.5),new Point2d(725.5,578.5)};for(int i=0;i<4;i++)border.AddVertexAt(i,corners[i],0,0,0);border.Closed=true;border.ColorIndex=7;Add(tr,ps,border);
    layout.DrawViewportsFirst=true;layout.PlotPlotStyles=false;tr.Commit();
   }
  }
  doc.Editor.Regen();db.SaveAs(Path.Combine(outdir,Path.GetFileName(doc.Name)),DwgVersion.Current);
  using(var tr=db.TransactionManager.StartTransaction()){var l=(Layout)tr.GetObject(LayoutManager.Current.GetLayoutId("SOTRAMI"),OpenMode.ForRead);using(var p=new PlotSettings(false))using(var info=new PlotInfo{Layout=l.ObjectId})using(var valid=new PlotInfoValidator()){p.CopyFrom(l);info.OverrideSettings=p;valid.MediaMatchingPolicy=MatchingPolicy.MatchEnabled;valid.Validate(info);using(var engine=PlotFactory.CreatePublishEngine()){engine.BeginPlot(null,null);engine.BeginDocument(info,doc.Name,null,1,true,Path.Combine(outdir,filename+".pdf"));using(var page=new PlotPageInfo()){engine.BeginPage(page,info,true,null);engine.BeginGenerateGraphics(null);engine.EndGenerateGraphics(null);engine.EndPage(null);}engine.EndDocument(null);engine.EndPlot(null);}}}
  File.WriteAllLines(Path.Combine(outdir,"copia-"+code+".txt"),report);doc.Editor.WriteMessage("\nNORCCOPY_DONE\n");
 }
 [CommandMethod("NORCPROBE")]
 public void Probe(){var log=new List<string>();using(var s=new Database(false,true)){s.ReadDwgFile(Reference,FileOpenMode.OpenForReadAndAllShare,true,null);s.CloseInput(true);using(var t=s.TransactionManager.StartTransaction()){var ld=(DBDictionary)t.GetObject(s.LayoutDictionaryId,OpenMode.ForRead);var l=(Layout)t.GetObject(ld.GetAt("101"),OpenMode.ForRead);var ps=(BlockTableRecord)t.GetObject(l.BlockTableRecordId,OpenMode.ForRead);foreach(ObjectId id in ps){var en=t.GetObject(id,OpenMode.ForRead);if(en is Table table){log.Add("TABLE "+table.Handle+" "+table.Position+" rows="+table.Rows.Count+" cols="+table.Columns.Count);for(int r=0;r<table.Rows.Count;r++)for(int c=0;c<table.Columns.Count;c++)log.Add(r+","+c+" "+table.Cells[r,c].TextString);}if(en is Viewport vp && vp.CenterPoint.X>600 && vp.CenterPoint.X<715 && vp.CenterPoint.Y>430 && vp.CenterPoint.Y<590)log.Add("DETAILVIEW "+vp.ViewCenter+" "+vp.ViewTarget+" twist="+vp.TwistAngle+" dir="+vp.ViewDirection);}}}File.WriteAllLines(Path.Combine(Root,"norcobre-probe.txt"),log);App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nNORCPROBE_DONE\n");}
}
