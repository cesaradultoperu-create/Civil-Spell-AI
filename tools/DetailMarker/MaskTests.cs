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
    public sealed class MaskTests
    {
        static string Root {get{return Environment.GetEnvironmentVariable("DETALLE_OUTPUT");}}
        static MLeader Primary(Database db,Transaction tr)
        {
            var d=(DBDictionary)tr.GetObject(db.LayoutDictionaryId,OpenMode.ForRead);var l=(Layout)tr.GetObject(d.GetAt("Layout1"),OpenMode.ForRead);
            return ((BlockTableRecord)tr.GetObject(l.BlockTableRecordId,OpenMode.ForRead)).Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<MLeader>().Single();
        }
        static void Check(MLeader m,Transaction tr,bool expected,Dictionary<string,string> texts=null)
        {
            var b=(BlockTableRecord)tr.GetObject(m.BlockContentId,OpenMode.ForRead);
            foreach(var a in b.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<AttributeDefinition>())
            using(var v=m.GetBlockAttribute(a.ObjectId))
            {
                if(texts!=null&&v.TextString!=texts[a.Tag])throw new System.Exception("Lost text "+a.Tag);
                if(a.Tag!="DET"&&a.Tag!="TEXTO_INFERIOR")continue;
                if(!a.IsMTextAttributeDefinition||!v.IsMTextAttribute)throw new System.Exception("Not multiline "+a.Tag);
                using(var mt=v.MTextAttribute)if(mt.BackgroundFill!=expected||mt.UseBackgroundColor!=expected||(expected&&Math.Abs(mt.BackgroundScaleFactor-1.1)>1e-6))throw new System.Exception("Mask property mismatch "+a.Tag+" expected="+expected+" block="+b.Name+" fill="+mt.BackgroundFill+" useBackground="+mt.UseBackgroundColor);
            }
        }
        [CommandMethod("DETALLEPROBARMASCARA")]
        public void Test()
        {
            var doc=App.DocumentManager.MdiActiveDocument;var db=doc.Database;var report=new List<string>();
            try
            {
                using(var tr=db.TransactionManager.StartTransaction())
                {
                    var m=Primary(db,tr);Check(m,tr,true);report.Add("NATIVE_MTEXT_BACKGROUND_MASK_YES_OK");
                    var b=(BlockTableRecord)tr.GetObject(m.BlockContentId,OpenMode.ForRead);var texts=new Dictionary<string,string>();
                    foreach(var a in b.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<AttributeDefinition>())using(var v=m.GetBlockAttribute(a.ObjectId))texts[a.Tag]=v.TextString;
                    MaskCommands.SetMask(m,tr,false);Check(m,tr,false,texts);report.Add("MASK_NO_OPTION_OK");
                    int line=(int)m.GetLeaderLineIndexes((int)m.GetLeaderIndexes()[0])[0];m.SetFirstVertex(line,m.BlockPosition+new Vector3d(40,-30,0));SideSwitch.Update(m,tr);Check(m,tr,false,texts);
                    SideSwitch.SetMinimum(m,tr,110);SideSwitch.Update(m,tr);Check(m,tr,false,texts);report.Add("MASK_NO_SURVIVES_SIDE_AND_WIDTH_CHANGE_OK");
                    MaskCommands.SetMask(m,tr,true);Check(m,tr,true,texts);m.SetFirstVertex(line,m.BlockPosition+new Vector3d(-40,-30,0));SideSwitch.Update(m,tr);Check(m,tr,true,texts);report.Add("MASK_YES_SURVIVES_SIDE_CHANGE_OK");
                    var space=(BlockTableRecord)tr.GetObject(m.OwnerId,OpenMode.ForWrite);using(var map=new IdMapping())
                    {db.DeepCloneObjects(new ObjectIdCollection(new[]{m.ObjectId}),space.ObjectId,map,false);Check((MLeader)tr.GetObject(map[m.ObjectId].Value,OpenMode.ForRead),tr,true,texts);}
                    report.Add("COPY_PRESERVES_MASK_AND_FOUR_TEXTS_OK");tr.Abort();
                }
                report.Add("DETALLE_MASK_TEST_OK");File.WriteAllLines(Path.Combine(Root,"mask-validation.txt"),report);doc.Editor.WriteMessage("\nDETALLE_MASK_TEST_OK\n");
            }
            catch(System.Exception ex){File.WriteAllText(Path.Combine(Root,"mask-error.txt"),ex.ToString());doc.Editor.WriteMessage("\nDETALLE_MASK_TEST_FAILED "+ex+"\n");}
        }
        [CommandMethod("DETALLEMASCARADEMO")]
        public void Demo()
        {
            var db=App.DocumentManager.MdiActiveDocument.Database;
            using(var tr=db.TransactionManager.StartTransaction())
            {
                var m=Primary(db,tr);var space=(BlockTableRecord)tr.GetObject(m.OwnerId,OpenMode.ForWrite);
                var b=(BlockTableRecord)tr.GetObject(m.BlockContentId,OpenMode.ForRead);double width=SideSwitch.Width(b,tr);bool right=SideSwitch.IsRight(b.Name);var ids=new ObjectIdCollection();
                for(double y=-12;y<=15;y+=1.5)
                {
                    var a=new Point3d(right?-5:-width-10,y,0).TransformBy(SideSwitch.Transform(m));var z=new Point3d(right?width+10:5,y+3,0).TransformBy(SideSwitch.Transform(m));
                    var line=new Line(a,z){ColorIndex=8,LineWeight=LineWeight.LineWeight005};space.AppendEntity(line);tr.AddNewlyCreatedDBObject(line,true);ids.Add(line.ObjectId);
                }
                var order=(DrawOrderTable)tr.GetObject(space.DrawOrderTableId,OpenMode.ForWrite);order.MoveToBottom(ids);tr.Commit();
            }
        }
    }
}

