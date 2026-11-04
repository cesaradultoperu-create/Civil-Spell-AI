using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DetalleAutoLado;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;
namespace DetalleLayout
{
    public class TextPlacementTests
    {
        [CommandMethod("DETALLETEXTOSEJEMPLO6")]
        public void Example()
        {
            var db=App.DocumentManager.MdiActiveDocument.Database;
            using(var tr=db.TransactionManager.StartTransaction())
            {
                var layout=(Layout)tr.GetObject(LayoutManager.Current.GetLayoutId("Layout1"),OpenMode.ForRead);
                var m=((BlockTableRecord)tr.GetObject(layout.BlockTableRecordId,OpenMode.ForRead)).Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<MLeader>().Single();
                var b=(BlockTableRecord)tr.GetObject(m.BlockContentId,OpenMode.ForRead);m.UpgradeOpen();
                foreach(var a in b.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<AttributeDefinition>())
                using(var v=m.GetBlockAttribute(a.ObjectId))
                {
                    if(a.Tag=="DET")v.TextString="DETALLE T\u00cdPICO DE REVESTIMIENTO EN BERMA - 2";
                    if(a.Tag=="TEXTO_INFERIOR")v.TextString="1107.001.00.14-200-15-P-008";
                    if(a.Tag=="LABEL")v.TextString="A";
                    m.SetBlockAttribute(a.ObjectId,v);
                }
                int line=(int)m.GetLeaderLineIndexes((int)m.GetLeaderIndexes()[0])[0];m.SetFirstVertex(line,m.BlockPosition+new Vector3d(15,35,0));SideSwitch.Update(m,tr);Check(m,tr);tr.Commit();
            }
        }
        public static void Check(MLeader m,Transaction tr)
        {
            var b=(BlockTableRecord)tr.GetObject(m.BlockContentId,OpenMode.ForRead);
            var inverse=SideSwitch.Transform(m).Inverse();
            foreach(var a in b.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<AttributeDefinition>().Where(a=>a.Tag=="DET"||a.Tag=="TEXTO_INFERIOR"))
            using(var v=m.GetBlockAttribute(a.ObjectId))using(var mt=v.MTextAttribute)
            {
                var points=mt.GetBoundingPoints().Cast<Point3d>().Select(p=>p.TransformBy(inverse)).ToArray();
                var rowEdge=a.Tag=="DET"?points.Min(p=>p.Y):points.Max(p=>p.Y);
                var expected=(a.Tag=="DET"?1:-1)*a.Height*.5;
                if(Math.Abs(rowEdge-expected)>0.05)throw new System.Exception("Vertical text drift "+m.Handle+" "+a.Tag+" expected="+expected+" actual="+rowEdge);
                bool right=SideSwitch.IsRight(b.Name);double width=SideSwitch.Width(b,tr);
                double outer=right?points.Max(p=>p.X):points.Min(p=>p.X);
                if(Math.Abs(outer-(right?width:-width))>0.05)throw new System.Exception("Text not aligned with line end "+a.Tag);
                if(mt.BackgroundFill!=SideSwitch.MaskEnabled(m,tr))throw new System.Exception("Mask state changed");
            }
        }
        [CommandMethod("DETALLEPROBARTEXTOS6")]
        public void Test()
        {
            var doc=App.DocumentManager.MdiActiveDocument;var db=doc.Database;var report=new List<string>();
            try
            {
                using(var tr=db.TransactionManager.StartTransaction())
                {
                    var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead);
                    var markers=bt.Cast<ObjectId>().Select(id=>(BlockTableRecord)tr.GetObject(id,OpenMode.ForRead)).Where(b=>b.IsLayout)
                        .SelectMany(b=>b.Cast<ObjectId>()).Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<MLeader>().Where(m=>SideSwitch.IsDetail(m,tr)).ToArray();
                    if(markers.Length==0)throw new System.Exception("No detail references found");
                    foreach(var m in markers)
                    {
                        var center=m.BlockPosition;int line=(int)m.GetLeaderLineIndexes((int)m.GetLeaderIndexes()[0])[0];var tip=m.GetFirstVertex(line);
                        var b=(BlockTableRecord)tr.GetObject(m.BlockContentId,OpenMode.ForRead);
                        var title=b.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<AttributeDefinition>().Single(a=>a.Tag=="DET");
                        string original;using(var v=m.GetBlockAttribute(title.ObjectId))original=v.TextString;
                        Check(m,tr);
                        for(int i=0;i<12;i++)
                        {
                            m.UpgradeOpen();title=((BlockTableRecord)tr.GetObject(m.BlockContentId,OpenMode.ForRead)).Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<AttributeDefinition>().Single(a=>a.Tag=="DET");
                            using(var v=m.GetBlockAttribute(title.ObjectId)){v.TextString=i%2==0?original+" - T\u00cdPICO EXTENDIDO "+i:original;m.SetBlockAttribute(title.ObjectId,v);}
                            SideSwitch.SetMinimum(m,tr,140+i);m.SetFirstVertex(line,center+new Vector3d(i%2==0?40:-40,-30,0));
                            MaskCommands.SetMask(m,tr,i%3!=0);Check(m,tr);
                        }
                        m.SetFirstVertex(line,tip);MaskCommands.SetMask(m,tr,true);Check(m,tr);
                        if(m.BlockPosition.DistanceTo(center)>1e-6)throw new System.Exception("Circle moved");
                        using(var map=new IdMapping())
                        {db.DeepCloneObjects(new ObjectIdCollection(new[]{m.ObjectId}),m.OwnerId,map,false);Check((MLeader)tr.GetObject(map[m.ObjectId].Value,OpenMode.ForRead),tr);}
                        report.Add(m.Handle+" 12_WIDTH_SIDE_TEXT_MASK_CHANGES_AND_COPY_WITHOUT_VERTICAL_DRIFT_OK");
                    }
                    tr.Abort();
                }
                report.Add("DETALLE_TEXT_PLACEMENT_TEST_OK");
            }
            catch(System.Exception ex){report.Add("DETALLE_TEXT_PLACEMENT_TEST_FAILED "+ex);}
            File.WriteAllLines(Path.Combine(Environment.GetEnvironmentVariable("DETALLE_OUTPUT"),"text-placement-validation.txt"),report);
            doc.Editor.WriteMessage("\n"+report.Last()+"\n");
        }
    }
}
