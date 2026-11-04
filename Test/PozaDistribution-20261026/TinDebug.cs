using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;
public class TinDebug {
 const string Root=@"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI\Test\PozaDistribution-20261026";
 static void Log(string s){File.AppendAllText(Path.Combine(Root,"tin-debug.txt"),s+Environment.NewLine);}
 [CommandMethod("SOTRAMITINCHECK")]
 public void Info(){File.WriteAllText(Path.Combine(Root,"tin-debug.txt"),"START\n");try{
  var db=App.DocumentManager.MdiActiveDocument.Database;var civilType=typeof(CivilDocument);Log("CIVIL_TYPE "+civilType);var civil=CivilDocument.GetCivilDocument(db);Log("CIVIL_OK");var collection=civil.GetSurfaceIds();Log("SURFACE_COLLECTION_TYPE "+collection.GetType());var ids=((IEnumerable)collection).Cast<ObjectId>().ToArray();Log("REGISTERED_SURFACES="+ids.Length);foreach(var id in ids)Log("REGISTERED "+id.Handle);
  using(var tr=db.TransactionManager.StartTransaction()){var ms=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForRead);foreach(ObjectId id in ms){var e=tr.GetObject(id,OpenMode.ForRead);if(e.GetType().Name!="TinSurface")continue;var t=e.GetType();Log("SURFACE "+id.Handle+" "+t.GetProperty("Name").GetValue(e,null));foreach(var m in t.GetMethods().Where(m=>new[]{"PasteSurface","Create","Rebuild","CreateSnapshot","RemoveSnapshot"}.Contains(m.Name)||m.Name.Contains("LandXML")))Log("API "+m);foreach(var n in new[]{"StyleId","IsOutOfDate","IsReferenceObject","Visible"})try{Log(n+"="+t.GetProperty(n)?.GetValue(e,null));}catch{}var sid=civil.Styles.SurfaceStyles[0];if(sid.IsNull){Log("STYLE_NULL");continue;}var style=tr.GetObject(sid,OpenMode.ForRead);Log("STYLE_TYPE="+style.GetType().FullName);Log("STYLE_NAME="+style.GetType().GetProperty("Name").GetValue(style,null));foreach(var m in style.GetType().GetMethods().Where(m=>m.Name.Contains("DisplayStyle"))){Log("STYLE_API "+m);foreach(var p in m.GetParameters())if(p.ParameterType.IsEnum)Log("DISPLAY_ENUM="+string.Join(",",Enum.GetNames(p.ParameterType)));}foreach(var p in style.GetType().GetProperties().Where(p=>p.Name.Contains("Contour")))Log("STYLE_PROPERTY "+p.Name+" "+p.PropertyType.FullName);var contours=style.GetType().GetProperty("ContourStyle")?.GetValue(style,null);if(contours!=null)foreach(var p in contours.GetType().GetProperties())Log("CONTOUR_PROPERTY "+p.Name+" "+p.PropertyType.FullName);
  }}
 }catch(System.Exception ex){Log("ERROR "+ex);}App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nSOTRAMITINCHECK_DONE\n");}
}
