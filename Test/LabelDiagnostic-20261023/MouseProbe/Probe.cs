using System;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;
public class LabelMouseProbe
{
 [DllImport("accore.dll",CallingConvention=CallingConvention.Cdecl,EntryPoint="?acedCoordFromWorldToPixel@@YAHHQEBNAEAVCPoint@@@Z")]
 static extern int WorldToPixel(int viewport,double[] point,out System.Drawing.Point pixel);
 [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr window,ref System.Drawing.Point pixel);
 [CommandMethod("CSLABELMOUSEPOINT")]
 public void Point()
 {
  var doc=App.DocumentManager.MdiActiveDocument;
  using(var tr=doc.Database.TransactionManager.StartOpenCloseTransaction())
  {
   var id=doc.Database.GetObjectId(false,new Handle(0x22C61),0);
   var curve=(Curve)tr.GetObject(id,OpenMode.ForRead);
   Point3d point=curve.GetPointAtParameter(0.5);
   int cv=Convert.ToInt32(App.GetSystemVariable("CVPORT"));
   var screen=doc.Editor.PointToScreen(point,cv);
   System.Drawing.Point pixel;
   WorldToPixel(cv,point.ToArray(),out pixel);
   ClientToScreen(doc.Window.Handle,ref pixel);
   string path=Path.Combine(Path.GetDirectoryName(typeof(LabelMouseProbe).Assembly.Location),"mouse-point.txt");
   File.WriteAllText(path,String.Format(CultureInfo.InvariantCulture,"{0},{1}\nDIP={2:R},{3:R}\nWCS={4}\nDOC={5}\nCTAB={6} CVPORT={7}\n",pixel.X,pixel.Y,screen.X,screen.Y,point,doc.Name,App.GetSystemVariable("CTAB"),cv));
  }
 }
 [CommandMethod("CSLABELLABELCOUNT")]
 public void Count()
 {
  var doc=App.DocumentManager.MdiActiveDocument;
  int count=0;
  using(var tr=doc.Database.TransactionManager.StartOpenCloseTransaction())
  {
   var bt=(BlockTable)tr.GetObject(doc.Database.BlockTableId,OpenMode.ForRead);
   var ms=(BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace],OpenMode.ForRead);
   foreach(ObjectId id in ms)if(id.ObjectClass.DxfName=="AECC_GENERAL_SEGMENT_LABEL")count++;
  }
  File.AppendAllText(Path.Combine(Path.GetDirectoryName(typeof(LabelMouseProbe).Assembly.Location),"mouse-count.txt"),DateTime.Now.ToString("HH:mm:ss")+" "+doc.Name+" labels="+count+Environment.NewLine);
 }
}
