using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;
public class ExtractPoza {
  const string Root=@"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI\Test\PozaDistribution-20261026";
  static object Prop(object o,string name)=>o.GetType().GetProperty(name)?.GetValue(o,null);
  static string Num(double d)=>d.ToString("R",CultureInfo.InvariantCulture);
  [CommandMethod("POZAEXTRACT")]
  public void Run(){
    var log=new List<string>();Directory.CreateDirectory(Path.Combine(Root,"Data"));
    foreach(var filename in new[]{"POZAS.dwg","TERRENO EXISTENTE.dwg","Xref_LD-pozas.dwg","Xref_LD-Poza de agua.dwg","Xref_Ubicación plano model.dwg"})using(var db=new Database(false,true)) {
      try{
        db.ReadDwgFile(Path.Combine(Root,"Fuentes",filename),FileOpenMode.OpenForReadAndAllShare,true,null);db.CloseInput(true);
        using(var tr=db.TransactionManager.StartTransaction()){
          var ms=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForRead);
          foreach(ObjectId id in ms){var e=tr.GetObject(id,OpenMode.ForRead) as Entity;if(e==null)continue;
            if(e.GetType().Name=="TinSurface"){
              var name=Convert.ToString(Prop(e,"Name"));log.Add(filename+" SURFACE "+name);
              foreach(var m in e.GetType().GetMethods().Where(m=>m.Name=="CreateByCropping"))log.Add("CROP_API "+m);
              if(filename=="TERRENO EXISTENTE.dwg") {foreach(var xy in new[]{new[]{579274.0,8302123.0},new[]{579300.0,8302114.0},new[]{579301.0,8302046.0},new[]{579154.0,8302485.0}})try{var z=e.GetType().GetMethod("FindElevationAtXY").Invoke(e,new object[]{xy[0],xy[1]});log.Add("GROUND "+xy[0]+","+xy[1]+"="+z);}catch(System.Exception ex){log.Add("GROUND_ERROR "+ex.Message);}continue;}
              if(Convert.ToBoolean(Prop(e,"IsReferenceObject")))continue;
              if(name!="Pozas" && name!="Poza de agua")continue;
              try{
                var triangles=(IEnumerable)e.GetType().GetMethod("GetTriangles",new[]{typeof(bool)}).Invoke(e,new object[]{false});
                var safe=System.Text.RegularExpressions.Regex.Replace(name,"[^a-zA-Z0-9_-]","_");int count=0;Extents3d? bounds=null;
                using(var writer=new StreamWriter(Path.Combine(Root,"Data","TIN_"+safe+".csv"))){
                  foreach(var t in triangles){var pts=new[]{(Point3d)Prop(Prop(t,"Vertex1"),"Location"),(Point3d)Prop(Prop(t,"Vertex2"),"Location"),(Point3d)Prop(Prop(t,"Vertex3"),"Location")};writer.WriteLine(string.Join(",",pts.SelectMany(p=>new[]{Num(p.X),Num(p.Y),Num(p.Z)})));foreach(var p in pts){if(bounds.HasValue){var b=bounds.Value;b.AddPoint(p);bounds=b;}else bounds=new Extents3d(p,p);}count++;}
                }
                log.Add("TRIANGLES "+name+" COUNT="+count+" BOUNDS="+bounds);
                File.WriteAllLines(Path.Combine(Root,"extract.txt"),log);
              }catch(System.Exception ex){log.Add("TRIANGLES_ERROR "+name+" "+ex);}
            }
            if(e is Polyline pl && filename!="TERRENO EXISTENTE.dwg"){
              using(var writer=new StreamWriter(Path.Combine(Root,"Data",Path.GetFileNameWithoutExtension(filename)+"_"+e.Handle+".csv"))){for(int i=0;i<pl.NumberOfVertices;i++){var p=pl.GetPoint3dAt(i);writer.WriteLine(Num(p.X)+","+Num(p.Y)+","+Num(p.Z));}if(pl.Closed){var p=pl.GetPoint3dAt(0);writer.WriteLine(Num(p.X)+","+Num(p.Y)+","+Num(p.Z));}}
            }
            if(e.GetType().Name=="FeatureLine")try{
              var m=e.GetType().GetMethods().First(v=>v.Name=="GetPoints" && v.GetParameters().Length==1);var mode=Enum.Parse(m.GetParameters()[0].ParameterType,"AllPoints");var points=(Point3dCollection)m.Invoke(e,new[]{mode});
              using(var writer=new StreamWriter(Path.Combine(Root,"Data","FEATURE_"+e.Handle+".csv"))){foreach(Point3d p in points)writer.WriteLine(Num(p.X)+","+Num(p.Y)+","+Num(p.Z));if(Convert.ToBoolean(Prop(e,"IsClosed")) && points.Count>0){var p=points[0];writer.WriteLine(Num(p.X)+","+Num(p.Y)+","+Num(p.Z));}}
              log.Add("FEATURE "+e.Handle+" POINTS="+points.Count);
            }catch(System.Exception ex){log.Add("FEATURE_ERROR "+e.Handle+" "+ex.Message);}
            if(e is RasterImage ri){var def=(RasterImageDef)tr.GetObject(ri.ImageDefId,OpenMode.ForRead);log.Add("IMAGE "+filename+" "+def.SourceFileName);}
          }
        }
      }catch(System.Exception ex){log.Add("FILE_ERROR "+filename+" "+ex);}
    }
    File.WriteAllLines(Path.Combine(Root,"extract.txt"),log);App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nPOZAEXTRACT_DONE\n");
  }
}
