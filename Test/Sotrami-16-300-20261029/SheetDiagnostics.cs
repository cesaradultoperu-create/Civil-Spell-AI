using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Interop;
public class SheetDiagnostics
{
    const string Root = @"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI\artifacts\plan-list-update-20261016";
    [CommandMethod("SOTRAMIPROBARCUATRO",CommandFlags.Session)]
    public void TestFour()
    {
        var manager=Application.DocumentManager;var original=manager.MdiActiveDocument;var reports=new List<object>();
        foreach(var suffix in new[]{"006","007","008","009"})
        {
            var file=@"Q:\Proyectos 2026\1107.001.00.16 - Pad SOTRAMI\05_Drawing\1107.001.00.14-200-14-P-"+suffix+".dwg";
            Document doc=null;bool owned=false;
            foreach(Document existing in manager)if(String.Equals(existing.Database.Filename,file,StringComparison.OrdinalIgnoreCase))doc=existing;
            if(doc==null){doc=manager.Open(file,true);owned=true;}
            if(!String.Equals(doc.Database.Filename,file,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Documento diferente al solicitado.");
            manager.MdiActiveDocument=doc;
            using(doc.LockDocument()){doc.Editor.Regen();Run();}
            reports.Add(new {file,readonlyDrawing=doc.IsReadOnly,diagnostic=new JavaScriptSerializer().DeserializeObject(File.ReadAllText(Path.Combine(Root,"sheet-diagnostics.json")))});
            File.WriteAllText(Path.Combine(Root,"four-drawings-diagnostics.json"),new JavaScriptSerializer().Serialize(reports));
            if(owned)doc.CloseAndDiscard();
        }
        manager.MdiActiveDocument=original;
    }
    [CommandMethod("SOTRAMIMOSTRARACTUAL")]
    public void ShowActual()
    {
        const string target=@"Q:\Proyectos 2026\1107.001.00.16 - Pad SOTRAMI\05_Drawing\SOTRAMI.dst";
        var mgr=new AcSmSheetSetMgr();var current=mgr.FindOpenDatabase(target);
        if(current==null)throw new InvalidOperationException("El conjunto actualizado no esta abierto.");
        var id=current.GetSheetSet().GetObjectId().GetHandle();
        var en=mgr.GetDatabaseEnumerator();var duplicates=new List<AcSmDatabase>();
        for(var db=en.Next();db!=null;db=en.Next())
            if(db.GetSheetSet().GetObjectId().GetHandle()==id)duplicates.Add(db);
        if(duplicates.Any(d=>d.GetIsDirty()||d.GetLockStatus()!=AcSmLockStatus.AcSmLockStatus_UnLocked))throw new InvalidOperationException("Hay cambios o bloqueos en un conjunto; no se cierra.");
        var paths=duplicates.Select(d=>d.GetFileName()).ToArray();
        foreach(var db in duplicates)mgr.Close(db);
        Application.DocumentManager.MdiActiveDocument.Editor.Command("_.-OPENSHEETSET",target);
        File.WriteAllText(Path.Combine(Root,"sheet-display-fix.json"),new JavaScriptSerializer().Serialize(new {closedMetadataOnly=paths,opened=target}));
        Run();
    }
    static void Visit(IAcSmSubset subset, List<object> records)
    {
        var e = subset.GetSheetEnumerator();
        for(var item=e.Next();item!=null;item=e.Next())
            if(item.GetTypeName()=="AcSmSubset")Visit((IAcSmSubset)item,records);
            else if(item.GetTypeName()=="AcSmSheet") { var s=(IAcSmSheet)item;records.Add(new {code=s.GetNumber(),title=s.GetTitle(),id=s.GetObjectId().GetHandle()}); }
    }
    [CommandMethod("SOTRAMIDIAGNOSTICO")]
    public void Run()
    {
        var mgr=new AcSmSheetSetMgr();var en=mgr.GetDatabaseEnumerator();var sets=new List<object>();
        for(var db=en.Next();db!=null;db=en.Next()) { var records=new List<object>();Visit(db.GetSheetSet(),records);sets.Add(new {file=db.GetFileName(),name=db.GetSheetSet().GetName(),id=db.GetSheetSet().GetObjectId().GetHandle(),dirty=db.GetIsDirty(),locked=db.GetLockStatus().ToString(),sheets=records}); }
        var documents=new List<object>();
        foreach(Document doc in Application.DocumentManager)
        {
            var attrs=new List<object>();
            using(var tr=doc.Database.TransactionManager.StartTransaction())
            {
                var layouts=(DBDictionary)tr.GetObject(doc.Database.LayoutDictionaryId,OpenMode.ForRead);
                foreach(DBDictionaryEntry entry in layouts)
                {
                    var layout=(Layout)tr.GetObject(entry.Value,OpenMode.ForRead);if(layout.ModelType)continue;
                    var space=(BlockTableRecord)tr.GetObject(layout.BlockTableRecordId,OpenMode.ForRead);
                    foreach(ObjectId id in space)
                    {
                        var block=tr.GetObject(id,OpenMode.ForRead) as BlockReference;if(block==null)continue;
                        foreach(ObjectId aid in block.AttributeCollection)
                        {
                            var a=(AttributeReference)tr.GetObject(aid,OpenMode.ForRead);
                            if(a.Tag!="TITTLE_DRAW1"&&a.Tag!="TITTLE_DRAW2"&&a.Tag!="DRAWING_NO")continue;
                            var f=a.HasFields?(Field)tr.GetObject(a.GetField(),OpenMode.ForRead):null;
                            attrs.Add(new {layout=layout.LayoutName,handle=a.Handle.ToString(),tag=a.Tag,text=a.TextString,field=f==null?null:f.GetFieldCode(),value=f==null?null:Convert.ToString(f.Value),status=f==null?null:f.EvaluationStatus.Status.ToString()});
                        }
                    }
                }
            }
            documents.Add(new {file=doc.Database.Filename,readOnly=doc.IsReadOnly,attrs});
        }
        File.WriteAllText(Path.Combine(Root,"sheet-diagnostics.json"),new JavaScriptSerializer().Serialize(new {sets,documents}));
        Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nDIAGNOSTICO SOTRAMI GUARDADO.\n");
    }
}
