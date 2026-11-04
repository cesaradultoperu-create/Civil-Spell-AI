using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using System.Globalization;
namespace RelaveAuto {
 // Portable application export. No Excel process or Office installation needed.
 internal static class ExcelWriter {
  static XNamespace S="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
  static XElement Text(string cell,string text,int style){return new XElement(S+"c",new XAttribute("r",cell),new XAttribute("t","inlineStr"),new XAttribute("s",style),new XElement(S+"is",new XElement(S+"t",text)));}
  static XElement Number(string cell,double value,int style){return new XElement(S+"c",new XAttribute("r",cell),new XAttribute("s",style),new XElement(S+"v",value.ToString("G17",CultureInfo.InvariantCulture)));}
  static void Add(ZipArchive zip,string name,string xml){using(var w=new StreamWriter(zip.CreateEntry(name).Open(),new System.Text.UTF8Encoding(false)))w.Write(xml);}
  static void Begin(ZipArchive zip,string sheetName){
    Add(zip,"[Content_Types].xml","<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>");
    Add(zip,"_rels/.rels","<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
    Add(zip,"xl/workbook.xml","<workbook xmlns=\""+S+"\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\""+sheetName+"\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
    Add(zip,"xl/_rels/workbook.xml.rels","<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
    Add(zip,"xl/styles.xml","<styleSheet xmlns=\""+S+"\"><numFmts count=\"2\"><numFmt numFmtId=\"164\" formatCode=\"0.00\"/><numFmt numFmtId=\"165\" formatCode=\"0.0000\"/></numFmts><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Arial\"/></font><font><b/><sz val=\"11\"/><name val=\"Arial\"/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs><cellXfs count=\"4\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"164\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/><xf numFmtId=\"165\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/></cellXfs><cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>");
  }
  public static void SaveHeights(string path,HeightSample[] samples){
   if(samples.Any(s=>double.IsNaN(s.Height)||double.IsInfinity(s.Height)||s.Height<0))throw new InvalidOperationException("Hay alturas invalidas para el Excel.");
   using(var file=new FileStream(path,FileMode.CreateNew))using(var zip=new ZipArchive(file,ZipArchiveMode.Create)){
    Begin(zip,"Alturas");
    var data=new XElement(S+"sheetData",new XElement(S+"row",new XAttribute("r",1),Text("A1","Distancia desde A (m)",3),Text("B1","Altura (m)",3),Text("C1","Seccion",3),Text("D1","Perfil",3),Text("E1","Foto",3),Text("F1","Polilinea altura",3),Text("G1","Polilinea relave",3),Text("H1","Observacion",3)));
    int row=2;foreach(var s in samples){
     string note=s.AtTip?"Punta final":s.Index==0?"Origen A":"Intervalo 0.10 m";if(s.Height==0)note+="; altura cero, sin trazo";
     bool onGrid=Math.Abs(s.Distance/.10-Math.Round(s.Distance/.10))<1e-7;
     data.Add(new XElement(S+"row",new XAttribute("r",row),Number("A"+row,s.Distance,onGrid?1:2),Number("B"+row,s.Height,2),Number("C"+row,s.Index,0),Number("D"+row,s.ProfileNumber,1),Text("E"+row,s.ProfileFile,0),Text("F"+row,s.Handle??"",0),Text("G"+row,s.ProfileHandle,0),Text("H"+row,note,0)));row++;
    }
    var sheet=new XElement(S+"worksheet",new XElement(S+"sheetViews",new XElement(S+"sheetView",new XAttribute("workbookViewId",0),new XElement(S+"pane",new XAttribute("ySplit",1),new XAttribute("topLeftCell","A2"),new XAttribute("activePane","bottomLeft"),new XAttribute("state","frozen")))),new XElement(S+"cols",new XElement(S+"col",new XAttribute("min",1),new XAttribute("max",2),new XAttribute("width",23),new XAttribute("customWidth",1)),new XElement(S+"col",new XAttribute("min",3),new XAttribute("max",4),new XAttribute("width",12),new XAttribute("customWidth",1)),new XElement(S+"col",new XAttribute("min",5),new XAttribute("max",7),new XAttribute("width",20),new XAttribute("customWidth",1)),new XElement(S+"col",new XAttribute("min",8),new XAttribute("max",8),new XAttribute("width",36),new XAttribute("customWidth",1))),data);
    Add(zip,"xl/worksheets/sheet1.xml",sheet.ToString(SaveOptions.DisableFormatting));
   }
  }
  public static void Save(string path,PhotoResult[] photos){
   if(photos.Any(p=>p.Area<=0))throw new InvalidOperationException("No se exportan areas pendientes o nulas.");
   using(var file=new FileStream(path,FileMode.CreateNew))using(var zip=new ZipArchive(file,ZipArchiveMode.Create)){
    Begin(zip,"Areas");
    var data=new XElement(S+"sheetData",new XElement(S+"row",new XAttribute("r",1),Text("A1","Numero",3),Text("B1","Area (m²)",3),Text("D1","Foto",3),Text("E1","Polilinea",3)));
    int row=2;foreach(var p in photos.OrderBy(p=>p.Frame.Number)){data.Add(new XElement(S+"row",new XAttribute("r",row),Number("A"+row,p.Frame.Number,1),Number("B"+row,p.Area,2),Text("D"+row,p.File,0),Text("E"+row,p.Handle,0)));row++;}
    var sheet=new XElement(S+"worksheet",new XElement(S+"sheetViews",new XElement(S+"sheetView",new XAttribute("workbookViewId",0))),new XElement(S+"cols",new XElement(S+"col",new XAttribute("min",1),new XAttribute("max",2),new XAttribute("width",18),new XAttribute("customWidth",1)),new XElement(S+"col",new XAttribute("min",4),new XAttribute("max",5),new XAttribute("width",16),new XAttribute("customWidth",1))),data);
    Add(zip,"xl/worksheets/sheet1.xml",sheet.ToString(SaveOptions.DisableFormatting));
   }
  }
 }
}
