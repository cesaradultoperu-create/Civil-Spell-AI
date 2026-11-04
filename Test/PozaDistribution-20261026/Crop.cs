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
public class CropPoza {
 const string Root=@"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI\Test\PozaDistribution-20261026";
 static object Prop(object o,string n)=>o.GetType().GetProperty(n)?.GetValue(o,null);
 static ObjectId FindSurface(Database db,string name){using(var tr=db.TransactionManager.StartTransaction()){var ms=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForRead);foreach(ObjectId id in ms){var e=tr.GetObject(id,OpenMode.ForRead);if(e.GetType().Name=="TinSurface" && Convert.ToString(Prop(e,"Name"))==name)return id;}}throw new InvalidOperationException("No se encuentra "+name);}
 static void Csv(Database db,ObjectId id,string path){using(var tr=db.TransactionManager.StartTransaction()){var e=tr.GetObject(id,OpenMode.ForRead);var triangles=(IEnumerable)e.GetType().GetMethod("GetTriangles",new[]{typeof(bool)}).Invoke(e,new object[]{false});using(var w=new StreamWriter(path))foreach(var t in triangles){var p=new[]{(Point3d)Prop(Prop(t,"Vertex1"),"Location"),(Point3d)Prop(Prop(t,"Vertex2"),"Location"),(Point3d)Prop(Prop(t,"Vertex3"),"Location")};w.WriteLine(string.Join(",",p.SelectMany(v=>new[]{v.X.ToString("R",CultureInfo.InvariantCulture),v.Y.ToString("R",CultureInfo.InvariantCulture),v.Z.ToString("R",CultureInfo.InvariantCulture)})));}}}
 static ObjectId Crop(Database dest,Database source,ObjectId id,string name,double[] box){using(var tr=source.TransactionManager.StartTransaction()){var e=tr.GetObject(id,OpenMode.ForRead);var method=e.GetType().GetMethods().Single(m=>m.Name=="CreateByCropping" && m.GetParameters().Length==4 && m.GetParameters()[3].ParameterType==typeof(Point2dCollection));var p=new Point2dCollection(new[]{new Point2d(box[0],box[1]),new Point2d(box[2],box[1]),new Point2d(box[2],box[3]),new Point2d(box[0],box[3])});return (ObjectId)method.Invoke(null,new object[]{dest,name,id,p});}}
 [CommandMethod("POZACROP")]
 public void Run(){
  System.Diagnostics.Process.GetCurrentProcess().PriorityClass=System.Diagnostics.ProcessPriorityClass.BelowNormal;
  var log=new List<string>();Directory.CreateDirectory(Path.Combine(Root,"ConSuperficies"));
  using(var ground=new Database(false,true))using(var design=new Database(false,true)){
   ground.ReadDwgFile(Path.Combine(Root,"Fuentes","TERRENO EXISTENTE.dwg"),FileOpenMode.OpenForReadAndAllShare,true,null);ground.CloseInput(true);
   design.ReadDwgFile(Path.Combine(Root,"Fuentes","POZAS.dwg"),FileOpenMode.OpenForReadAndAllShare,true,null);design.CloseInput(true);
   var gid=FindSurface(ground,"TERRENO EXISTENTE");
   var codes=new[]{"008","009","010"};var boxes=new[]{new[]{579230d,8302078d,579370d,8302150d},new[]{579270d,8302020d,579335d,8302072d},new[]{579110d,8302458d,579220d,8302520d}};
   for(int i=0;i<codes.Length;i++){
    string code=codes[i];var input=Path.Combine(Root,"Preparados","1107.001.00.14-200-14-P-"+code+".dwg");
    using(var db=new Database(false,true)){
     db.ReadDwgFile(input,FileOpenMode.OpenForReadAndAllShare,true,null);db.CloseInput(true);var prev=HostApplicationServices.WorkingDatabase;
     try{HostApplicationServices.WorkingDatabase=db;
      log.Add("BEGIN "+code);File.WriteAllLines(Path.Combine(Root,"crop.txt"),log);
      var ng=Crop(db,ground,gid,"TN_SOTRAMI_P"+code,boxes[i]);log.Add("GROUND "+ng);File.WriteAllLines(Path.Combine(Root,"crop.txt"),log);
      var nd=Crop(db,design,FindSurface(design,i==2?"Poza de agua":"Pozas"),"DISENO_SOTRAMI_P"+code,boxes[i]);log.Add("DESIGN "+nd);File.WriteAllLines(Path.Combine(Root,"crop.txt"),log);
      Csv(db,ng,Path.Combine(Root,"Data","GROUND_"+code+".csv"));Csv(db,nd,Path.Combine(Root,"Data","DESIGN_"+code+".csv"));
      db.SaveAs(Path.Combine(Root,"ConSuperficies",Path.GetFileName(input)),DwgVersion.Current);log.Add("SAVED "+code);File.WriteAllLines(Path.Combine(Root,"crop.txt"),log);
     }finally{HostApplicationServices.WorkingDatabase=prev;}
    }
   }
  }
  App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nPOZACROP_DONE\n");
 }
}
