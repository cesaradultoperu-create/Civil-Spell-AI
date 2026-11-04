using System;
using System.IO;
using System.Xml;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Interop;
class Program {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern bool SetDllDirectory(string p);
 [STAThread]static int Main(string[] args){try{
  string path=Path.GetFullPath(args[0]); if(Path.GetPathRoot(path).Equals(@"Q:\",StringComparison.OrdinalIgnoreCase)||path.StartsWith(@"\\"))throw new Exception("Solo copias locales.");
  SetDllDirectory(@"C:\Program Files\Autodesk\AutoCAD 2024");
  var mgr=new AcSmSheetSetMgrClass();var db=mgr.OpenDatabase(path,false);var set=db.GetSheetSet();
  var xml=new XmlDocument();var root=xml.CreateElement("SheetSetReadOnly");xml.AppendChild(root);
  root.SetAttribute("requested",path);root.SetAttribute("actual",db.GetFileName());root.SetAttribute("guid",set.GetObjectId().GetHandle());
  var en=set.GetCustomPropertyBag().GetPropertyEnumerator();string name;AcSmCustomPropertyValue prop;
  while(true){en.Next(out name,out prop);if(prop==null)break;var e=xml.CreateElement("Property");e.SetAttribute("name",name);e.InnerText=Convert.ToString(prop.GetValue());root.AppendChild(e);}
  xml.Save(args[1]);Console.WriteLine("READ_ONLY_OK "+db.GetFileName());mgr.Close(db);return 0;
 }catch(Exception e){Console.WriteLine(e);return 1;}}
}