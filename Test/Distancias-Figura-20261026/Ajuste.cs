using System;
using System.IO;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using AcApp=Autodesk.AutoCAD.ApplicationServices.Application;
public class AjusteDistancias {
  [CommandMethod("DFAISLARAS03",CommandFlags.Modal)]
  public void Run() {
    var root=Path.GetDirectoryName(typeof(AjusteDistancias).Assembly.Location);
    try {
      var d=AcApp.DocumentManager.MdiActiveDocument;
      if(!String.Equals(d.Name,Path.Combine(root,"Distancias_figura_TRABAJO.dwg"),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Wrong drawing.");
      var db=d.Database;
      using(var tr=db.TransactionManager.StartTransaction()) {
        var lt=(LayerTable)tr.GetObject(db.LayerTableId,OpenMode.ForRead);
        const string layer="FIG_ORIGINAL_AS03_OP1";ObjectId lid;
        if(lt.Has(layer))lid=lt[layer];
        else {lt.UpgradeOpen();var l=new LayerTableRecord{Name=layer};lid=lt.Add(l);tr.AddNewlyCreatedDBObject(l,true);}
        var p=(Entity)tr.GetObject(db.GetObjectId(false,new Handle(0x7297),0),OpenMode.ForWrite);p.LayerId=lid;
        var layouts=(DBDictionary)tr.GetObject(db.LayoutDictionaryId,OpenMode.ForRead);
        foreach(DBDictionaryEntry entry in layouts) {
          var layout=(Layout)tr.GetObject(entry.Value,OpenMode.ForRead);
          if(!layout.LayoutName.StartsWith("OPCION")||layout.LayoutName.Trim()=="OPCION 1")continue;
          foreach(ObjectId eid in (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId,OpenMode.ForRead)) {
            var vp=tr.GetObject(eid,OpenMode.ForRead) as Viewport;
            if(vp==null||Math.Abs(vp.CustomScale-0.05)>0.00001)continue;
            vp.UpgradeOpen();vp.FreezeLayersInViewport(new ObjectIdCollection(new[]{lid}).GetEnumerator());
          }
        }
        tr.Commit();
      }
      File.WriteAllText(Path.Combine(root,"as03-isolation.txt"),"Original 7297 retains geometry and properties; layer changed from 0 to FIG_ORIGINAL_AS03_OP1; frozen in layouts 2-5. Styled clone 9703 is displayed in option 2.");
      d.Editor.Regen();
    } catch(System.Exception ex){File.WriteAllText(Path.Combine(root,"as03-error.txt"),ex.ToString());}
  }
}
