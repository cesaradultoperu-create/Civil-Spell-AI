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
    public sealed class WidthTests
    {
        const string LongText="1107.001.00.14-200-15-P-008";
        static string Root {get{return Environment.GetEnvironmentVariable("DETALLE_OUTPUT");}}
        static void Assert(bool condition,string text){if(!condition)throw new System.Exception(text);}
        static MLeader Primary(Database db,Transaction tr)
        {
            var dict=(DBDictionary)tr.GetObject(db.LayoutDictionaryId,OpenMode.ForRead);
            var layout=(Layout)tr.GetObject(dict.GetAt("Layout1"),OpenMode.ForRead);
            var space=(BlockTableRecord)tr.GetObject(layout.BlockTableRecordId,OpenMode.ForRead);
            return space.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<MLeader>().Single();
        }
        static AttributeDefinition Attr(MLeader m,Transaction tr,string tag)
        {return ((BlockTableRecord)tr.GetObject(m.BlockContentId,OpenMode.ForRead)).Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<AttributeDefinition>().Single(a=>a.Tag==tag);}
        static void Set(MLeader m,Transaction tr,string tag,string text)
        {m.UpgradeOpen();var a=Attr(m,tr,tag);using(var v=m.GetBlockAttribute(a.ObjectId)){v.TextString=text;m.SetBlockAttribute(a.ObjectId,v);}}
        static void Safe(MLeader m,Transaction tr)
        {
            var b=(BlockTableRecord)tr.GetObject(m.BlockContentId,OpenMode.ForRead);bool right=SideSwitch.IsRight(b.Name);double width=SideSwitch.Width(b,tr);
            var c=b.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<Circle>().Single();
            Assert(Math.Abs(c.Diameter-17.5)<1e-6&&c.ColorIndex==4,"Circle appearance changed");
            var line=b.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<Line>().Single();
            Assert(line.ColorIndex==4&&Math.Abs(line.Length-width-c.Radius)<1e-6,"Central line geometry/color changed");
            foreach(string tag in new[]{"DET","TEXTO_INFERIOR"})
            {
                var a=Attr(m,tr,tag);using(var v=m.GetBlockAttribute(a.ObjectId))
                {
                    double measured=SideSwitch.TextWidth(a,v.TextString);Assert(width-measured>=c.Radius+2-1e-6,"Text overlaps circle: "+tag);
                    if(v.IsMTextAttribute)
                    {
                        using(var mt=v.MTextAttribute)
                        {
                            var xs=mt.GetBoundingPoints().Cast<Point3d>().Select(p=>p.TransformBy(SideSwitch.Transform(m).Inverse()).X).ToArray();
                            double edge=right?xs.Max():xs.Min();
                            Assert(Math.Abs(edge-(right?width:-width))<0.05,"Multiline text edge not moved with line: "+tag+" edge="+edge+" width="+width);
                            var ys=mt.GetBoundingPoints().Cast<Point3d>().Select(p=>p.TransformBy(SideSwitch.Transform(m).Inverse()).Y).ToArray();
                            Assert(tag=="DET"?ys.Min()>=a.Height*.5-0.05:ys.Max()<=-a.Height*.5+0.05,"Text crosses central line: "+tag+" bounds="+ys.Min()+","+ys.Max());
                        }
                    }
                    else
                    {
                        var anchor=(right?v.AlignmentPoint:v.Position).TransformBy(SideSwitch.Transform(m).Inverse());
                        Assert(Math.Abs(anchor.X-(right?width:-width))<1e-6,"Text anchor not moved with line: "+tag);
                    }
                    Assert(v.TextString==(tag=="DET"?"DETALLE":LongText),"Text value lost: "+tag);
                }
            }
        }
        [CommandMethod("DETALLEPROBARANCHO")]
        public void Check()
        {
            var doc=App.DocumentManager.MdiActiveDocument;var db=doc.Database;var report=new List<string>();
            try
            {
                using(var tr=db.TransactionManager.StartTransaction())
                {
                    var m=Primary(db,tr);var center=m.BlockPosition;int index=(int)m.GetLeaderLineIndexes((int)m.GetLeaderIndexes()[0])[0];var tip=m.GetFirstVertex(index);
                    var space=(BlockTableRecord)tr.GetObject(m.OwnerId,OpenMode.ForWrite);
                    var sibling=(MLeader)m.Clone();space.AppendEntity(sibling);tr.AddNewlyCreatedDBObject(sibling,true);var siblingBlock=sibling.BlockContentId;
                    Set(m,tr,"TEXTO_INFERIOR",LongText);SideSwitch.Update(m,tr);Safe(m,tr);
                    var b=(BlockTableRecord)tr.GetObject(m.BlockContentId,OpenMode.ForRead);
                    Assert(SideSwitch.Width(b,tr)>35,"Long text did not expand the line");
                    Assert(sibling.BlockContentId==siblingBlock,"Editing one reference changed its sibling");
                    report.Add("AUTO_FIT_LONG_REFERENCE_OK width="+SideSwitch.Width(b,tr));
                    SideSwitch.SetMinimum(m,tr,100);SideSwitch.Update(m,tr);Safe(m,tr);
                    Assert(Math.Abs(SideSwitch.Width((BlockTableRecord)tr.GetObject(m.BlockContentId,OpenMode.ForRead),tr)-100)<1e-6,"Manual line width not retained");
                    Assert(m.BlockPosition.DistanceTo(center)<1e-6&&m.GetFirstVertex(index).DistanceTo(tip)<1e-6,"Stretch moved circle or arrow");
                    report.Add("MANUAL_STRETCH_100_OK_CIRCLE_AND_ARROW_FIXED");
                    foreach(double dx in new[]{40.0,-40.0})
                    {m.SetFirstVertex(index,center+new Vector3d(dx,-30,0));SideSwitch.Update(m,tr);Safe(m,tr);Assert(Math.Abs(SideSwitch.Minimum(m,tr)-100)<1e-6,"Side flip lost custom width");}
                    report.Add("BOTH_SIDES_KEEP_WIDTH_TEXT_AND_STYLE_OK");
                    var ids=new ObjectIdCollection(new[]{m.ObjectId});using(var map=new IdMapping())
                    {
                        db.DeepCloneObjects(ids,space.ObjectId,map,false);var clone=(MLeader)tr.GetObject(map[m.ObjectId].Value,OpenMode.ForRead);
                        Assert(Math.Abs(SideSwitch.Minimum(clone,tr)-100)<1e-6,"Copied instance lost custom width");Safe(clone,tr);
                    }
                    report.Add("COPIED_INSTANCE_PRESERVES_CUSTOM_WIDTH_OK");
                    using(var custom=new DetailWidthGrip(SideSwitch.Endpoint(m,tr)))
                    {
                        Assert(custom.GripPoint.DistanceTo(SideSwitch.Endpoint(m,tr))<1e-6,"Custom grip misplaced");Assert(custom.TriggerGrip,"Custom grip trigger missing");
                        report.Add("CUSTOM_END_GRIP_DATA_OK");
                    }
                    using(var other=new MLeader()){other.ContentType=ContentType.MTextContent;Assert(!SideSwitch.IsDetail(other,tr),"Unrelated MLeader modified");}
                    tr.Abort();
                }
                report.Add("DETALLE_WIDTH_TEST_OK");File.WriteAllLines(Path.Combine(Root,"width-validation.txt"),report);doc.Editor.WriteMessage("\nDETALLE_WIDTH_TEST_OK\n");
            }
            catch(System.Exception ex){File.WriteAllText(Path.Combine(Root,"width-error.txt"),ex.ToString());doc.Editor.WriteMessage("\nDETALLE_WIDTH_TEST_FAILED "+ex+"\n");}
        }
        [CommandMethod("DETALLEEJEMPLOANCHO")]
        public void Example()
        {
            var db=App.DocumentManager.MdiActiveDocument.Database;
            using(var tr=db.TransactionManager.StartTransaction()){var m=Primary(db,tr);Set(m,tr,"TEXTO_INFERIOR",LongText);SideSwitch.Update(m,tr);tr.Commit();}
            App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nWIDTH_EXAMPLE_OK\n");
        }
    }
}
