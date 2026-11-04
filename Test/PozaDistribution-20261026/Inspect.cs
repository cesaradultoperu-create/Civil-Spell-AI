using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using App = Autodesk.AutoCAD.ApplicationServices.Core.Application;
public class PozaInspect {
  static readonly string Root = @"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI\Test\PozaDistribution-20261026";
  [CommandMethod("POZAINSPECT")]
  public void Run() {
    string filter=Environment.GetEnvironmentVariable("POZA_INSPECT_FILTER");
    var scanRoot=Environment.GetEnvironmentVariable("POZA_SOURCE_ROOT");var reportRoot=Environment.GetEnvironmentVariable("POZA_REPORT_ROOT")??Root;Directory.CreateDirectory(reportRoot);
    foreach (var file in Directory.GetFiles(scanRoot??@"C:\Users\Cesar Ugaz\Pictures\trabajo para codE", "*.dwg").Concat(scanRoot==null&&Directory.Exists(Path.Combine(Root,"Fuentes"))?Directory.GetFiles(Path.Combine(Root,"Fuentes"),"*.dwg"):new string[0]).Where(f=>string.IsNullOrEmpty(filter)||Path.GetFileName(f).Contains(filter))) {
      var lines = new List<string>();
      try { using (var db = new Database(false,true)) {
        db.ReadDwgFile(file,FileOpenMode.OpenForReadAndAllShare,true,null); db.CloseInput(true);
        using (var tr=db.TransactionManager.StartTransaction()) {
          var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead);
          foreach(ObjectId id in bt) {
            var b=(BlockTableRecord)tr.GetObject(id,OpenMode.ForRead);
            if(b.IsFromExternalReference) lines.Add("XREF "+b.Name+" PATH="+b.PathName);
            if(!b.IsLayout) continue;
            var ents=b.Cast<ObjectId>().Select(e=>tr.GetObject(e,OpenMode.ForRead)).OfType<Entity>().ToList();
            lines.Add("SPACE "+b.Name+" COUNT="+ents.Count);
            foreach(var g in ents.GroupBy(e=>e.GetType().Name)) lines.Add("TYPE "+g.Key+" COUNT="+g.Count());
            foreach(var g in ents.GroupBy(e=>e.Layer)) {
              Extents3d? bounds=null;
              foreach(var en in g)try{var ex=en.GeometricExtents;if(bounds.HasValue){var v=bounds.Value;v.AddExtents(ex);bounds=v;}else bounds=ex;}catch{}
              lines.Add("LAYER "+g.Key+" COUNT="+g.Count()+" BOUNDS="+bounds);
            }
            foreach(var e in ents) {
              if(Path.GetFileName(file)=="TERRENO EXISTENTE.dwg" && e.GetType().Name!="TinSurface")continue;
              string s=null;
              if(b.Name=="*Model_Space") {
                string n="";try{n=Convert.ToString(e.GetType().GetProperty("Name")?.GetValue(e,null));}catch{}
                string bounds="";try{bounds=e.GeometricExtents.ToString();}catch{}
                lines.Add("ENTITY "+e.GetType().Name+" HANDLE="+e.Handle+" LAYER="+e.Layer+" NAME="+n+" COLOR="+e.Color+" BOUNDS="+bounds);
                if(e.GetType().Name=="TinSurface")try{
                  foreach(var key in new[]{"IsReferenceObject","IsReferenceValid","IsReferenceStale"})lines.Add("SURFACE_PROPERTY "+n+" "+key+"="+e.GetType().GetProperty(key)?.GetValue(e,null));
                  if(Convert.ToBoolean(e.GetType().GetProperty("IsReferenceObject")?.GetValue(e,null))){var info=e.GetType().GetMethod("GetReferenceInfo",Type.EmptyTypes)?.Invoke(e,null);if(info!=null)foreach(var prop in info.GetType().GetProperties())try{lines.Add("SURFACE_REFERENCE "+n+" "+prop.Name+"="+prop.GetValue(info,null));}catch{}}
                }catch(System.Exception ex){lines.Add("REFERENCE_INFO_ERROR "+n+" "+ex.Message);}
              }
              if(e is DBText t) s="TEXT "+t.TextString;
              if(e is Polyline green && e.ColorIndex==3)s="GREENPOLY "+e.Handle+" CLOSED="+green.Closed+" AREA="+green.Area+" VERTICES="+string.Join(";",Enumerable.Range(0,green.NumberOfVertices).Select(i=>green.GetPoint3dAt(i).ToString()));
              if(e is MText mt) s="MTEXT "+mt.Text;
              if(e is MLeader ml && ml.ContentType==ContentType.MTextContent){using(var leaderText=ml.MText)s="MLEADER "+leaderText.Text;try{foreach(int i in ml.GetLeaderIndexes())foreach(int line in ml.GetLeaderLineIndexes(i))s+=" FIRST="+ml.GetFirstVertex(line)+" LAST="+ml.GetLastVertex(line);}catch{}}
              if(e is BlockReference br) {
                var def=(BlockTableRecord)tr.GetObject(br.BlockTableRecord,OpenMode.ForRead);
                s="INSERT "+def.Name+" POS="+br.Position+" SCALE="+br.ScaleFactors;
                foreach(ObjectId a in br.AttributeCollection) { var at=tr.GetObject(a,OpenMode.ForRead) as AttributeReference; if(at!=null)s+=" | "+at.Tag+"="+at.TextString; }
              }
              if(e is Viewport vp) s="VIEWPORT number="+vp.Number+" center="+vp.CenterPoint+" width="+vp.Width+" height="+vp.Height+" view="+vp.ViewCenter+" viewHeight="+vp.ViewHeight+" scale="+vp.CustomScale+" target="+vp.ViewTarget+" twist="+vp.TwistAngle+" direction="+vp.ViewDirection+" clip="+vp.NonRectClipOn;
              if(s!=null) { try{s+=" BOUNDS="+e.GeometricExtents;}catch{} lines.Add(s.Replace("\r"," ").Replace("\n"," ")); }
            }
          }
          var dict=(DBDictionary)tr.GetObject(db.LayoutDictionaryId,OpenMode.ForRead);
          foreach(DBDictionaryEntry entry in dict) {var l=(Layout)tr.GetObject(entry.Value,OpenMode.ForRead);lines.Add("LAYOUT "+l.LayoutName+" paper="+l.PlotPaperSize+" origin="+l.PlotOrigin+" media="+l.CanonicalMediaName+" device="+l.PlotConfigurationName);}
        }
      }} catch(System.Exception ex) {lines.Add("ERROR "+ex);}
      File.WriteAllLines(Path.Combine(reportRoot,Path.GetFileNameWithoutExtension(file)+".txt"),lines);
    }
    App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nPOZAINSPECT_DONE\n");
  }
}
