using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using Autodesk.AutoCAD.Interop;
class PersistedSheetVerifier
{
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern bool SetDllDirectory(string path);
 static void Visit(IAcSmSubset subset,List<Dictionary<string,string>> rows)
 {
  var en=subset.GetSheetEnumerator();
  for(var item=en.Next();item!=null;item=en.Next())
   if(item.GetTypeName()=="AcSmSubset")Visit((IAcSmSubset)item,rows);
   else if(item.GetTypeName()=="AcSmSheet")
   {
    var s=(IAcSmSheet)item;var bag=s.GetCustomPropertyBag();
    rows.Add(new Dictionary<string,string>{{"code",s.GetNumber()},{"title",s.GetTitle()},{"line1",Convert.ToString(bag.GetProperty("EXCEL_TITULO_1").GetValue())},{"line2",Convert.ToString(bag.GetProperty("EXCEL_TITULO_2").GetValue())}});
   }
 }
 [STAThread]static void Main()
 {
  SetDllDirectory(@"C:\Program Files\Autodesk\AutoCAD 2024");
  var mgr=new AcSmSheetSetMgr();var reports=new List<object>();
  foreach(var file in new[]{@"Q:\Proyectos 2026\1107.001.00.16 - Pad SOTRAMI\05_Drawing\SOTRAMI.dst",@"Q:\Proyectos 2026\1107.001.00.16 - Pad SOTRAMI\05_Drawing\Xref\SOTRAMI.dst"})
  {
   var db=mgr.OpenDatabase(file,false);var rows=new List<Dictionary<string,string>>();Visit(db.GetSheetSet(),rows);
   reports.Add(new{file,name=db.GetSheetSet().GetName(),rows});mgr.Close(db);
  }
  var json=new JavaScriptSerializer().Serialize(reports);
  File.WriteAllText(@"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI\artifacts\plan-list-update-20261016\persisted-both-sheetsets.json",json);
  Console.WriteLine("DST LEIDOS DESDE DISCO EN PROCESO INDEPENDIENTE.");
 }
}
