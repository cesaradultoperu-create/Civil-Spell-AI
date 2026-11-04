using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;
public class TinFix {
 const string Root=@"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI\Test\PozaDistribution-20261026";
 [CommandMethod("SOTRAMITININFO")]
 public void Info(){var db=App.DocumentManager.MdiActiveDocument.Database;var log=new List<string>();var civilType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Autodesk.Civil.ApplicationServices.CivilDocument")).First(t=>t!=null);var civil=civilType.GetMethod("GetCivilDocument").Invoke(null,new object[]{db});var ids=(ObjectIdCollection)civilType.GetMethod("GetSurfaceIds").Invoke(civil,null);log.Add("REGISTERED_SURFACES="+ids.Count);foreach(ObjectId id in ids)log.Add("REGISTERED "+id.Handle);
  using(var tr=db.TransactionManager.StartTransaction()){var ms=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForRead);foreach(ObjectId id in ms){var e=tr.GetObject(id,OpenMode.ForRead);if(e.GetType().Name!="TinSurface")continue;var t=e.GetType();log.Add("SURFACE "+id.Handle+" "+t.GetProperty("Name").GetValue(e,null));foreach(var m in t.GetMethods().Where(m=>new[]{"PasteSurface","Create","Rebuild","CreateSnapshot","RemoveSnapshot"}.Contains(m.Name)))log.Add("API "+m);foreach(var n in new[]{"StyleId","IsOutOfDate","IsReferenceObject","Visible"})try{log.Add(n+"="+t.GetProperty(n)?.GetValue(e,null));}catch{}var sid=(ObjectId)t.GetProperty("StyleId").GetValue(e,null);var style=tr.GetObject(sid,OpenMode.ForRead);log.Add("STYLE_TYPE="+style.GetType().FullName);log.Add("STYLE_NAME="+style.GetType().GetProperty("Name").GetValue(style,null));foreach(var m in style.GetType().GetMethods().Where(m=>m.Name.Contains("DisplayStyle"))){log.Add("STYLE_API "+m);foreach(var p in m.GetParameters())if(p.ParameterType.IsEnum)log.Add("DISPLAY_ENUM="+string.Join(",",Enum.GetNames(p.ParameterType)));}foreach(var p in style.GetType().GetProperties().Where(p=>p.Name.Contains("Contour")))log.Add("STYLE_PROPERTY "+p.Name+" "+p.PropertyType.FullName);
   var contours=style.GetType().GetProperty("ContourStyle")?.GetValue(style,null);if(contours!=null)foreach(var p in contours.GetType().GetProperties())log.Add("CONTOUR_PROPERTY "+p.Name+" "+p.PropertyType.FullName);
  }}File.WriteAllLines(Path.Combine(Root,"tin-info.txt"),log);App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nSOTRAMITININFO_DONE\n");}
}
