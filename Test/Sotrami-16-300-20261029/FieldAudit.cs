using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Interop;
public class FieldAudit
{
 static void Visit(IAcSmSubset subset,List<Dictionary<string,string>> rows)
 {
  var en=subset.GetSheetEnumerator();
  for(var item=en.Next();item!=null;item=en.Next())
   if(item.GetTypeName()=="AcSmSubset")Visit((IAcSmSubset)item,rows);
   else if(item.GetTypeName()=="AcSmSheet")
   {
    var s=(IAcSmSheet)item;var bag=s.GetCustomPropertyBag();
    rows.Add(new Dictionary<string,string>{{"code",s.GetNumber()},{"title",s.GetTitle()},{"line1",Convert.ToString((object)bag.GetProperty("EXCEL_TITULO_1").GetValue())},{"line2",Convert.ToString((object)bag.GetProperty("EXCEL_TITULO_2").GetValue())}});
   }
 }
 class Binding{public string Code{get;set;}public string Line1{get;set;}public string Line2{get;set;}public string Field1{get;set;}public string Field2{get;set;}public string FieldNumber{get;set;}}
 [CommandMethod("SOTRAMIAUDITARCAMPOS")]
 public void Run()
 {
  const string root=@"G:\01. PERSONALES\Cesar\Programas con el Visual Studio para el CIVIL3D\08_SOTRAMI_Lista_de_planos_con_Excel\Registros";
  var json=new JavaScriptSerializer();var rows=new List<object>();int failures=0;
  var persisted=new List<object>();
  foreach(var file in new[]{@"Q:\Proyectos 2026\1107.001.00.16 - Pad SOTRAMI\05_Drawing\SOTRAMI.dst",@"Q:\Proyectos 2026\1107.001.00.16 - Pad SOTRAMI\05_Drawing\Xref\SOTRAMI.dst"})
  {
   var database=new AcSmDatabase();database.LoadFromFile(file);
   var sheets=new List<Dictionary<string,string>>();Visit(database.GetSheetSet(),sheets);
   persisted.Add(new{file,name=database.GetSheetSet().GetName(),rows=sheets});
  }
  File.WriteAllText(Path.Combine(root,"persisted-both-sheetsets.json"),json.Serialize(persisted));
  var bindings=json.Deserialize<Binding[]>(File.ReadAllText(Path.Combine(root,"titleblock-bindings.json")));
  foreach(var b in bindings)
   foreach(var test in new[]{new[]{"TITTLE_DRAW1",b.Field1,b.Line1},new[]{"TITTLE_DRAW2",b.Field2,b.Line2},new[]{"DRAWING_NO",b.FieldNumber,b.Code}})
    using(var field=new Field("%<"+test[1]+">%"))
    {
     field.Evaluate((int)FieldEvaluationContext.Demand,Application.DocumentManager.MdiActiveDocument.Database);
     var value=Convert.ToString(field.Value);var ok=value.Trim()==test[2].Trim()&&field.EvaluationStatus.Status.ToString()=="Success";
     if(!ok)failures++;
     rows.Add(new{code=b.Code,tag=test[0],expected=test[2],value,status=field.EvaluationStatus.Status.ToString(),success=ok});
    }
  File.WriteAllText(Path.Combine(root,"current-field-validation.json"),json.Serialize(new{success=failures==0,utc=DateTime.UtcNow,plans=bindings.Length,fields=rows.Count,failures,rows}));
  Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nCAMPOS VERIFICADOS: "+rows.Count+"; FALLAS: "+failures+"\n");
 }
}
