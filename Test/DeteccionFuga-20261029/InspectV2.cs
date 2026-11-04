using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;
public partial class Builder {
 [CommandMethod("FUGAINSPECTV2")]
 public void InspectV2(){try{db=App.DocumentManager.MdiActiveDocument.Database;Fuga.Dump(db,"v2-source.json");var data=new List<object>();using(var t=db.TransactionManager.StartTransaction()){
 var bt=(BlockTable)t.GetObject(db.BlockTableId,OpenMode.ForRead);
 foreach(ObjectId bid in bt){var b=(BlockTableRecord)t.GetObject(bid,OpenMode.ForRead);if(!b.Name.ToLowerInvariant().Contains("codo")&&!b.IsLayout)continue;data.Add(new{block=b.Name,id=b.Handle.ToString(),units=b.Units.ToString()});foreach(ObjectId id in b){var e=t.GetObject(id,OpenMode.ForRead) as Entity;if(e==null)continue;var r=new Dictionary<string,object>{{"block",b.Name},{"h",e.Handle.ToString()},{"type",e.GetType().Name},{"layer",e.Layer},{"color",e.Color.ToString()},{"lw",e.LineWeight.ToString()},{"transparency",e.Transparency.ToString()}};try{r["min"]=new[]{e.GeometricExtents.MinPoint.X,e.GeometricExtents.MinPoint.Y};r["max"]=new[]{e.GeometricExtents.MaxPoint.X,e.GeometricExtents.MaxPoint.Y};}catch{}var p=e as Polyline;if(p!=null){r["closed"]=p.Closed;r["points"]=Enumerable.Range(0,p.NumberOfVertices).Select(i=>new[]{p.GetPoint2dAt(i).X,p.GetPoint2dAt(i).Y,p.GetBulgeAt(i),p.GetStartWidthAt(i),p.GetEndWidthAt(i)}).ToArray();}var a=e as Arc;if(a!=null)r["arc"]=new[]{a.Center.X,a.Center.Y,a.Radius,a.StartAngle,a.EndAngle};var c=e as Circle;if(c!=null)r["circle"]=new[]{c.Center.X,c.Center.Y,c.Radius};var h=e as Hatch;if(h!=null){r["hatch"]=h.PatternName;r["scale"]=h.PatternScale;r["angle"]=h.PatternAngle;r["loops"]=h.NumberOfLoops;}var br=e as BlockReference;if(br!=null){r["ref"]=((BlockTableRecord)t.GetObject(br.BlockTableRecord,OpenMode.ForRead)).Name;r["pos"]=new[]{br.Position.X,br.Position.Y};}data.Add(r);}}
 foreach(ObjectId id in (LayerTable)t.GetObject(db.LayerTableId,OpenMode.ForRead)){var l=(LayerTableRecord)t.GetObject(id,OpenMode.ForRead);data.Add(new{layer=l.Name,color=l.Color.ToString(),lw=l.LineWeight.ToString(),alpha=l.Transparency.ToString()});}
 }File.WriteAllText(Path.Combine(Root,"v2-block.json"),new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Serialize(data));
 LayoutManager.Current.CurrentLayout="A";using(tr=db.TransactionManager.StartTransaction()){var l=(Layout)tr.GetObject(LayoutManager.Current.GetLayoutId("A"),OpenMode.ForRead);ps=(BlockTableRecord)tr.GetObject(l.BlockTableRecordId,OpenMode.ForWrite);ms=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForWrite);foreach(ObjectId id in ps.Cast<ObjectId>().ToArray()){var e=tr.GetObject(id,OpenMode.ForWrite) as Entity;if(e!=null&&!(e is Viewport&&((Viewport)e).Number==1))e.Erase();}Styles();View(420.5,297,800,540,578919,8297852,27);tr.Commit();}db.SaveAs(Path.Combine(Root,"v2-refs-preview.dwg"),DwgVersion.Current);
 }catch(System.Exception e){File.WriteAllText(Path.Combine(Root,"error-v2-inspect.txt"),e.ToString());}}
 [CommandMethod("FUGAPREVIEWSOURCE")]
 public void PreviewSource(){try{db=App.DocumentManager.MdiActiveDocument.Database;using(var t=db.TransactionManager.StartTransaction()){var l=(Layout)t.GetObject(LayoutManager.Current.GetLayoutId("A"),OpenMode.ForRead);foreach(ObjectId id in (BlockTableRecord)t.GetObject(l.BlockTableRecordId,OpenMode.ForRead)){var v=t.GetObject(id,OpenMode.ForRead) as Viewport;if(v!=null&&v.Layer=="DF_VENTANAS"){v.UpgradeOpen();v.On=true;}}t.Commit();}Fuga.Plot(db,"v2-refs-preview.pdf");}catch(System.Exception e){File.WriteAllText(Path.Combine(Root,"error-v2-preview.txt"),e.ToString());}}
}
