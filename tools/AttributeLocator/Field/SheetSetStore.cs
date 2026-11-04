using System;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.Interop;
using Autodesk.AutoCAD.DatabaseServices;
using LocalizarCasillas;
using AcApp=Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CasillasField
{
    // Project access is restricted to the registered drawing root, DST and Sheet Set GUID.
    public static class SheetSetStore
    {
        public static string DemoPath(string drawing)=>Path.Combine(Path.GetDirectoryName(drawing),"Casillas-prueba.dst");
        static AcSmSheetSetMgr Manager()=>new AcSmSheetSetMgrClass();
        static AcSmDatabase Open(AcSmSheetSetMgr mgr,string path)
        {
            try{var existing=mgr.FindOpenDatabase(path);if(existing!=null)return existing;}
            catch(System.Runtime.InteropServices.COMException ex) when(ex.ErrorCode==unchecked((int)0x80004005)){ }
            return mgr.OpenDatabase(path,false);
        }
        public static bool IsDemo(string drawing,FieldIdentity id)
        {
            if(id==null || id.Component.Length!=0)return false;
            return String.Equals(Path.GetFullPath(id.DatabasePath),Path.GetFullPath(DemoPath(drawing)),StringComparison.OrdinalIgnoreCase)
                && File.Exists(DemoPath(drawing)+".demo")
                && String.Equals(File.ReadAllText(DemoPath(drawing)+".demo").Trim(),id.SheetSet,StringComparison.OrdinalIgnoreCase);
        }
        static AcSmSheetSet Verify(AcSmDatabase db,FieldIdentity id)
        {
            var set=db.GetSheetSet();
            if(!String.Equals(set.GetObjectId().GetHandle(),id.SheetSet,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("El identificador del Sheet Set no coincide con el Field.");
            return set;
        }
        public static bool CanEdit(string drawing,FieldIdentity id)=>IsDemo(drawing,id)||ProjectAccess.Editable(drawing,id)!=null;
        static void CheckAccess(string drawing,FieldIdentity id)
        {
            if(!CanEdit(drawing,id))throw new InvalidOperationException(
                "Este vínculo no es una propiedad personalizada del conjunto habilitado para este plano. Se conserva en modo consulta. No se cambia su origen ni su Field.");
        }
        public static string Read(string drawing,FieldIdentity id)
        {
            CheckAccess(drawing,id);
            var mgr=Manager();var db=Open(mgr,id.DatabasePath);
            return Convert.ToString(Verify(db,id).GetCustomPropertyBag().GetProperty(id.Property).GetValue());
        }
        public static void Write(string drawing,FieldIdentity id,string expected,string value)
        {
            CheckAccess(drawing,id);
            var mgr=Manager();var db=Open(mgr,id.DatabasePath);var owner=new object();bool locked=false;
            string backup=null;
            try
            {
                db.LockDb(owner);locked=true;
                var bag=Verify(db,id).GetCustomPropertyBag();var property=bag.GetProperty(id.Property);
                if(Convert.ToString(property.GetValue())!=expected)throw new InvalidOperationException("La propiedad cambio desde que se abrio. Vuelve a consultar la casilla.");
                if(!IsDemo(drawing,id))backup=ProjectAccess.PrepareBackup(drawing,id,expected,value);
                property.SetValue(value);bag.SetProperty(id.Property,property);
                db.UnlockDb(owner,true);locked=false;
                if(backup!=null)ProjectAccess.FinishBackup(backup,"Committed");
            }
            finally{if(locked){db.UnlockDb(owner,false);if(backup!=null)ProjectAccess.FinishBackup(backup,"Aborted");}}
        }
        public static void Evaluate(Database database)
        {
            using(var tr=database.TransactionManager.StartTransaction())
            {
                foreach(var slot in Catalog.Read(database).Where(s=>s.HasFields && CanEdit(database.Filename,FieldIdentity.Parse(s.Code))))
                {
                    var attribute=(AttributeReference)tr.GetObject(slot.AttributeId,OpenMode.ForWrite);
                    var field=(Field)tr.GetObject(attribute.GetField(),OpenMode.ForWrite);
                    field.Evaluate((int)FieldEvaluationContext.Demand,database);
                    attribute.RecordGraphicsModified(true);
                }
                tr.Commit();
            }
        }
        [Autodesk.AutoCAD.Runtime.CommandMethod("CASILLASPREPARARPRUEBA")]
        public static void Prepare()
        {
            var doc=AcApp.DocumentManager.MdiActiveDocument;
            if(Path.GetFileName(doc.Name)!="Ejemplo-Field-v3.dwg")throw new InvalidOperationException("Solo se prepara Ejemplo-Field-v3.dwg.");
            string path=DemoPath(doc.Name);
            if(File.Exists(path))throw new InvalidOperationException("El DST de prueba ya existe; no se sobrescribe.");
            var slots=Catalog.Read(doc.Database);
            var editable=slots.Where(s=>{var id=FieldIdentity.Parse(s.Code);return id!=null && id.Component.Length==0 && System.Text.RegularExpressions.Regex.IsMatch(id.Property,@"^\d\d_");}).ToArray();
            var mgr=Manager();var db=mgr.CreateDatabase(path,"",true);var owner=new object();bool locked=false;
            string guid;
            try
            {
                db.LockDb(owner);locked=true;var set=db.GetSheetSet();set.SetName("CASILLAS - PRUEBA LOCAL");guid=set.GetObjectId().GetHandle();
                var bag=set.GetCustomPropertyBag();
                foreach(var group in editable.GroupBy(s=>s.Property,StringComparer.OrdinalIgnoreCase))
                {
                    var values=group.Select(s=>s.Value).Distinct().ToArray();
                    if(values.Length!=1)throw new InvalidOperationException("Valores distintos para "+group.Key+"; no se mezclan.");
                    var prop=new AcSmCustomPropertyValueClass();prop.InitNew(bag);prop.SetFlags(PropertyFlags.CUSTOM_SHEETSET_PROP);prop.SetValue(values[0]);bag.SetProperty(group.Key,prop);
                }
                db.UnlockDb(owner,true);locked=false;
            }
            finally{if(locked)db.UnlockDb(owner,false);}
            using(var tr=doc.Database.TransactionManager.StartTransaction())
            {
                foreach(var slot in editable)
                {
                    var a=(AttributeReference)tr.GetObject(slot.AttributeId,OpenMode.ForWrite);var f=(Field)tr.GetObject(a.GetField(),OpenMode.ForWrite);
                    // Keep format suffixes; change only database and identity of this copied drawing.
                    string code=System.Text.RegularExpressions.Regex.Replace(slot.Code,@"Database\(""[^""]+""\)\.SheetSet\(""[^""]+""\)(?:\.Component\(""[^""]+""\))?",
                        m=>"Database(\""+path+"\").SheetSet(\""+guid+"\")");
                    f.SetFieldCode(code);f.Evaluate((int)FieldEvaluationContext.Demand,doc.Database);
                }
                tr.Commit();
            }
            File.WriteAllText(path+".demo",guid);
            doc.Editor.WriteMessage("\nSheet Set local creado. "+editable.Length+" Fields de la copia apuntan a Casillas-prueba.dst.\n");
        }
    }
}
