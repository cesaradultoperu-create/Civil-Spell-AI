using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
public class RelaveAreas {
 const string Root=@"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI\Test\FotosEnCuadro-Paso4-20261102";
 const string Source=@"C:\Users\Cesar Ugaz\Pictures\poner fotos en cuadro\Resultado paso 3\modelo_fotos_paso3.dwg";
 [CommandMethod("RELAVEEXPORTAREAS")]
 public void Export(){try{
  var rows=new List<Dictionary<string,object>>();string units;
  using(var db=new Database(false,true)){
   db.ReadDwgFile(Source,FileOpenMode.OpenForReadAndAllShare,true,null);db.CloseInput(true);units=db.Insunits.ToString();
   using(var tr=db.TransactionManager.StartTransaction()){
    var ms=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForRead);
    foreach(ObjectId id in ms){
     var poly=tr.GetObject(id,OpenMode.ForRead) as Polyline;if(poly==null||poly.Layer!="RELAVE_PERFILES")continue;
     if(!poly.Closed||poly.Area<=0)throw new InvalidOperationException("Perfil sin area: "+poly.Handle);
     using(var data=poly.GetXDataForApplication("RELAVE_PERFIL")){
      if(data==null)throw new InvalidOperationException("Perfil sin identificacion");var a=data.AsArray();
      var file=(string)a[1].Value;var label=(string)a[2].Value;var number=double.Parse(label,CultureInfo.InvariantCulture);
      if(Path.GetFileNameWithoutExtension(file)!=Math.Round(number*100).ToString(CultureInfo.InvariantCulture))throw new InvalidOperationException("Numero y foto distintos");
      rows.Add(new Dictionary<string,object>{{"number",number},{"label",label},{"file",file},{"handle",poly.Handle.ToString()},{"area",poly.Area},{"vertices",poly.NumberOfVertices}});
     }
    }
    tr.Commit();
   }
  }
  rows=rows.OrderBy(r=>(double)r["number"]).ToList();
  if(rows.Count!=21||rows.Select(r=>(double)r["number"]).Distinct().Count()!=21)throw new InvalidOperationException("Cantidad de perfiles incorrecta");
  File.WriteAllText(Path.Combine(Root,"areas-dwg.json"),new JavaScriptSerializer().Serialize(new{source=Source,units=units,rows=rows}));
  File.WriteAllText(Path.Combine(Root,"export-ok.txt"),"21 areas leidas directamente del DWG aprobado. INSUNITS="+units);
 }catch(System.Exception ex){File.WriteAllText(Path.Combine(Root,"export-error.txt"),ex.ToString());}}
}
