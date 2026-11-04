using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Colors;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;
public class BuildPozaLayout {
  const string Root=@"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI\Test\PozaDistribution-20261026";
  const string Source=@"C:\Users\Cesar Ugaz\Pictures\trabajo para codE";
  static void Add(Transaction tr,BlockTableRecord ps,Entity e){ps.AppendEntity(e);tr.AddNewlyCreatedDBObject(e,true);}
  static void Guide(Transaction tr,BlockTableRecord ps,double x,double y,double w,double h,string label) {
    var p=new Polyline();p.AddVertexAt(0,new Point2d(x,y),0,0,0);p.AddVertexAt(1,new Point2d(x+w,y),0,0,0);p.AddVertexAt(2,new Point2d(x+w,y+h),0,0,0);p.AddVertexAt(3,new Point2d(x,y+h),0,0,0);p.Closed=true;p.Layer="GUIAS_DISTRIBUCION";Add(tr,ps,p);
    var t=new MText{Location=new Point3d(x+3,y+h-3,0),Width=w-6,TextHeight=2.5,Contents=label,Layer="GUIAS_DISTRIBUCION"};Add(tr,ps,t);
  }
  [CommandMethod("POZAPREPARE")]
  public void Run(){
    var report=new List<string>();Directory.CreateDirectory(Path.Combine(Root,"Preparados"));
    string rf=Directory.GetFiles(Source,"PLANO DE REFERENCIA*.dwg").Single();
    using(var reference=new Database(false,true)) {
      reference.ReadDwgFile(rf,FileOpenMode.OpenForReadAndAllShare,true,null);reference.CloseInput(true);
      foreach(var code in new[]{"008","009","010"}) {
        var file=Directory.GetFiles(Source,"*-P-"+code+".dwg").Single();
        using(var db=new Database(false,true)) {
          db.ReadDwgFile(file,FileOpenMode.OpenForReadAndAllShare,true,null);db.CloseInput(true);
          var previous=HostApplicationServices.WorkingDatabase;
          try {
            HostApplicationServices.WorkingDatabase=db;
            var lm=LayoutManager.Current;var layoutId=lm.CreateLayout("DISTRIBUCION_REFERENCIA");
            using(var tr=db.TransactionManager.StartTransaction())using(var rt=reference.TransactionManager.StartTransaction()) {
              var dict=(DBDictionary)tr.GetObject(db.LayoutDictionaryId,OpenMode.ForRead);var old=(Layout)tr.GetObject(dict.GetAt("A"),OpenMode.ForRead);
              var rd=(DBDictionary)rt.GetObject(reference.LayoutDictionaryId,OpenMode.ForRead);var rl=(Layout)rt.GetObject(rd.GetAt("101"),OpenMode.ForRead);
              var layout=(Layout)tr.GetObject(layoutId,OpenMode.ForWrite);using(var settings=new PlotSettings(false)){settings.CopyFrom(rl);layout.CopyFrom(settings);}
              var ps=(BlockTableRecord)tr.GetObject(layout.BlockTableRecordId,OpenMode.ForWrite);var oldps=(BlockTableRecord)tr.GetObject(old.BlockTableRecordId,OpenMode.ForRead);
              var own=new ObjectIdCollection();
              foreach(ObjectId id in oldps){var e=tr.GetObject(id,OpenMode.ForRead);if(e is RasterImage)own.Add(id);if(e is BlockReference br){var b=(BlockTableRecord)tr.GetObject(br.BlockTableRecord,OpenMode.ForRead);if(b.Name=="MEMB_WWL_P_A1_")own.Add(id);}}
              db.DeepCloneObjects(own,ps.ObjectId,new IdMapping(),false);
              var layers=(LayerTable)tr.GetObject(db.LayerTableId,OpenMode.ForRead);
              foreach(var name in new[]{"GUIAS_DISTRIBUCION","VENTANAS_DISTRIBUCION"})if(!layers.Has(name)){layers.UpgradeOpen();var lr=new LayerTableRecord{Name=name,IsPlottable=false,Color=Color.FromColorIndex(ColorMethod.ByAci,(short)(name.StartsWith("GUIAS")?4:6))};layers.Add(lr);tr.AddNewlyCreatedDBObject(lr,true);}
              var rps=(BlockTableRecord)rt.GetObject(rl.BlockTableRecordId,OpenMode.ForRead);var vps=new ObjectIdCollection();
              foreach(ObjectId id in rps)if(rt.GetObject(id,OpenMode.ForRead) is Viewport v && v.CenterPoint.X>0 && v.CenterPoint.X<841 && v.CenterPoint.Y>100 && v.CenterPoint.Y<590 && v.Width<1000)vps.Add(id);
              var mapping=new IdMapping();reference.WblockCloneObjects(vps,ps.ObjectId,mapping,DuplicateRecordCloning.MangleName,false);
              foreach(IdPair pair in mapping)if(pair.IsPrimary && tr.GetObject(pair.Value,OpenMode.ForWrite) is Viewport vp){vp.Layer="VENTANAS_DISTRIBUCION";vp.On=false;vp.Locked=true;report.Add(code+" VIEWPORT "+vp.CenterPoint+" "+vp.Width+"x"+vp.Height);}
              Guide(tr,ps,36.50558264928,333.68380817696,543.994417350719,244.75,"PLANTA");
              Guide(tr,ps,37.4673097518589,142.223790963227,320.738165580128,133.60020130749,"SECCION 1");
              Guide(tr,ps,354.951743034775,142.223790963227,240.919540491021,133.60020130749,"SECCION 2");
              Guide(tr,ps,604.291583688026,470.077181966072,95.1330294295207,77.2706731025658,"DETALLE");
              Guide(tr,ps,725.5,478.5,100,100,"MAPA DE LOCALIZACION");
              Guide(tr,ps,725.5,311.5,100,144,"LEYENDA");
              Guide(tr,ps,610.5,83,209,218.5,"NOTAS DEL PROYECTO");
              Guide(tr,ps,585.058041441065,386.18380817696,126.756064589775,43.5,"TABLA DE REVESTIMIENTO");
              Guide(tr,ps,535.583336800071,333.68380817696,176.230769230769,42.5,"TABLA MOVIMIENTO DE TIERRAS");
              tr.Commit();
            }
            lm.CurrentLayout="DISTRIBUCION_REFERENCIA";
            var output=Path.Combine(Root,"Preparados",Path.GetFileName(file));db.SaveAs(output,DwgVersion.Current);
            using(var verify=new Database(false,true)){verify.ReadDwgFile(output,FileOpenMode.OpenForReadAndAllShare,true,null);using(var tr=verify.TransactionManager.StartTransaction()){var bt=(BlockTable)tr.GetObject(verify.BlockTableId,OpenMode.ForRead);var ms=(BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace],OpenMode.ForRead);report.Add(code+" MODEL_COUNT="+ms.Cast<ObjectId>().Count());}}
          }finally{HostApplicationServices.WorkingDatabase=previous;}
        }
      }
    }
    File.WriteAllLines(Path.Combine(Root,"preparation-check.txt"),report);App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nPOZAPREPARE_DONE\n");
  }
}
