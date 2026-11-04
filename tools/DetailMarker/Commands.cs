using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace DetalleLayout
{
    public class Commands
    {
        static readonly string Root = Path.GetFullPath(Environment.GetEnvironmentVariable("DETALLE_OUTPUT") ?? Path.Combine(Environment.CurrentDirectory,"Test","DetalleLayout-20261021"));
        const string FlexibleName = "DETALLE_LAYOUT_REFERENCIA_V2";
        const string StyleName = "DETALLE_FLECHA_REFERENCIA_V2";

        static MLeader Primary(Database db,Transaction tr)
        {
            var space=LayoutSpace(db,tr,"Layout1");
            return space.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<MLeader>().Single();
        }
        [CommandMethod("DETALLEAUTOEXPORT")]
        public void ExportAuto()
        {
            var db=AcApp.DocumentManager.MdiActiveDocument.Database;ObjectId id;Point3d center;
            using(var tr=db.TransactionManager.StartTransaction()) {var m=Primary(db,tr);id=m.ObjectId;center=m.BlockPosition;tr.Abort();}
            using(var library=db.Wblock(new ObjectIdCollection(new[]{id}),center))
            {
                library.Insunits=UnitsValue.Millimeters;
                using(var tr=library.TransactionManager.StartTransaction())
                {var dict=(DBDictionary)tr.GetObject(library.MLeaderStyleDictionaryId,OpenMode.ForRead);library.MLeaderstyle=dict.GetAt(StyleName);tr.Commit();}
                library.SaveAs(Path.Combine(Root,"Detalle - biblioteca automatica V3.dwg"),DwgVersion.AC1032);
            }
            AcApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nDETALLE_AUTO_EXPORT_OK\n");
        }
        [CommandMethod("DETALLETESTAUTO")]
        public void TestAuto()
        {
            var doc=AcApp.DocumentManager.MdiActiveDocument;var db=doc.Database;
            try
            {
                var report=new List<string>();
                using(var tr=db.TransactionManager.StartTransaction())
                {
                    var m=Primary(db,tr);m.UpgradeOpen();var center=m.BlockPosition;
                    var left=(BlockTableRecord)tr.GetObject(m.BlockContentId,OpenMode.ForRead);
                    var snapshot=new Dictionary<string,string>();
                    foreach(ObjectId id in left)
                    {
                        var a=tr.GetObject(id,OpenMode.ForRead) as AttributeDefinition;if(a==null)continue;
                        using(var value=m.GetBlockAttribute(id)) {value.TextString="VALOR_"+a.Tag;snapshot[a.Tag]=value.TextString;m.SetBlockAttribute(id,value);}
                    }
                    int line=(int)m.GetLeaderLineIndexes((int)m.GetLeaderIndexes()[0])[0];
                    foreach(var delta in new[]{new Vector3d(-35,-30,0),new Vector3d(35,-30,0),new Vector3d(-40,25,0),new Vector3d(40,25,0),new Vector3d(-0.01,-30,0),new Vector3d(-35,-30,0)})
                    {
                        m.SetFirstVertex(line,center+delta);DetalleAutoLado.SideSwitch.Update(m,tr);
                        var b=(BlockTableRecord)tr.GetObject(m.BlockContentId,OpenMode.ForRead);
                        if(Math.Abs(delta.X)>0.05)Require(b.Name.EndsWith("DERECHA")== (delta.X<0),"Text is not opposite to arrow");
                        Require(m.BlockPosition.DistanceTo(center)<1e-6 && m.GetFirstVertex(line).DistanceTo(center+delta)<1e-6,"Side switch changed leader geometry");
                        Require(m.BlockRotation==0,"Side switch rotated text");
                        foreach(ObjectId id in b)
                        {
                            var a=tr.GetObject(id,OpenMode.ForRead) as AttributeDefinition;if(a==null)continue;
                            using(var value=m.GetBlockAttribute(id))Require(value.TextString==snapshot[a.Tag],"Side switch lost edited value "+a.Tag);
                            if(a.Tag=="DET" || a.Tag=="TEXTO_INFERIOR")Require(b.Name.EndsWith("DERECHA")?a.Justify==AttachmentPoint.BaseRight:a.Justify==AttachmentPoint.BaseLeft,"Text justification invalid");
                        }
                        var c=b.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<Circle>().Single();
                        Require(Math.Abs(c.Diameter-17.5)<1e-6 && c.ColorIndex==4,"Circle style changed");
                        var central=b.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<Line>().Single();
                        Require(central.ColorIndex==4 && Math.Abs(central.Length-43.75)<1e-6,"Central line style changed");
                        report.Add("SIDE_CASE_OK arrow="+delta+" content="+b.Name);
                    }
                    tr.Abort();
                }
                report.Add("DETALLE_AUTO_TEST_OK");File.WriteAllLines(Path.Combine(Root,"auto-validation.txt"),report);
                doc.Editor.WriteMessage("\nDETALLE_AUTO_TEST_OK\n");
            }
            catch(System.Exception ex){File.WriteAllText(Path.Combine(Root,"auto-test-error.txt"),ex.ToString());doc.Editor.WriteMessage("\nDETALLE_AUTO_TEST_FAILED "+ex+"\n");}
        }
        [CommandMethod("DETALLEMOVERIZQUIERDA")]
        public void MoveLeft(){MoveArrow(-35);}
        [CommandMethod("DETALLEMOVERDERECHA")]
        public void MoveRight(){MoveArrow(35);}
        static void MoveArrow(double x)
        {
            var db=AcApp.DocumentManager.MdiActiveDocument.Database;
            using(var tr=db.TransactionManager.StartTransaction())
            {
                var m=Primary(db,tr);m.UpgradeOpen();int line=(int)m.GetLeaderLineIndexes((int)m.GetLeaderIndexes()[0])[0];
                m.SetFirstVertex(line,m.BlockPosition+new Vector3d(x,-30,0));tr.Commit();
            }
        }
        [CommandMethod("DETALLECHECKDERECHA")]
        public void CheckRight(){CheckSide(true);}
        [CommandMethod("DETALLECHECKIZQUIERDA")]
        public void CheckLeft(){CheckSide(false);}
        static void CheckSide(bool right)
        {
            var doc=AcApp.DocumentManager.MdiActiveDocument;var db=doc.Database;
            using(var tr=db.TransactionManager.StartTransaction())
            {
                var m=Primary(db,tr);var b=(BlockTableRecord)tr.GetObject(m.BlockContentId,OpenMode.ForRead);
                Require(b.Name.EndsWith("DERECHA")==right,"Automatic command reactor did not switch the text side");
                tr.Abort();
            }
            var result=right?"AUTO_EVENT_RIGHT_OK":"AUTO_EVENT_LEFT_OK";
            File.AppendAllText(Path.Combine(Root,"auto-events.txt"),result+Environment.NewLine);doc.Editor.WriteMessage("\n"+result+"\n");
        }

        [CommandMethod("DETALLENATIVE")]
        public void NativeDrawing()
        {
            var doc=AcApp.DocumentManager.MdiActiveDocument;var db=doc.Database;
            ObjectId libraryId=ObjectId.Null;Point3d center=Point3d.Origin;
            using(var tr=db.TransactionManager.StartTransaction())
            {
                var space=LayoutSpace(db,tr,"Layout1");
                var source=space.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<BlockReference>().Single();
                var b=(BlockTableRecord)tr.GetObject(source.BlockTableRecord,OpenMode.ForRead);
                var circle=b.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<Circle>().Single();
                center=circle.Center.TransformBy(source.BlockTransform);
                var poly=b.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<Polyline>().Single();
                var tip=poly.GetPoint3dAt(poly.NumberOfVertices-1).TransformBy(source.BlockTransform);
                var original=LayoutSpace(db,tr,"ORIGINAL");
                using(var map=new IdMapping()) db.DeepCloneObjects(new ObjectIdCollection(new[]{source.ObjectId}),original.ObjectId,map,false);
                // Keep the user's previously pasted symbol available in its own layout.
                var previous=space.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<MLeader>().Select(m=>m.ObjectId).ToArray();
                if(previous.Length>0)
                {
                    var oldSpace=LayoutSpace(db,tr,"VERSION ANTERIOR");
                    using(var map=new IdMapping()) db.DeepCloneObjects(new ObjectIdCollection(previous),oldSpace.ObjectId,map,false);
                    foreach(var id in previous)tr.GetObject(id,OpenMode.ForWrite).Erase();
                }
                var sid=Flexible(db,tr,source,center);db.MLeaderstyle=sid;
                libraryId=AddLeader(db,tr,space,sid,center,tip,"B").ObjectId;
                var comparison=LayoutSpace(db,tr,"COMPARACION");
                using(var map=new IdMapping()) db.DeepCloneObjects(new ObjectIdCollection(new[]{source.ObjectId}),comparison.ObjectId,map,false);
                var shift=new Vector3d(80,0,0);
                AddLeader(db,tr,comparison,sid,center+shift,tip+shift,"B");
                Note(db,tr,comparison,"REFERENCIA ORIGINAL",center.X-35,center.Y+18,2.5);
                Note(db,tr,comparison,"CORREGIDO V2",center.X+45,center.Y+18,2.5);
                source.UpgradeOpen();source.Erase();
                var examples=LayoutSpace(db,tr,"DETALLE LIBRE");
                Note(db,tr,examples,"DETALLE CON FLECHA LIBRE - EJEMPLOS",35,175,4);
                Note(db,tr,examples,"Doble clic: editar textos. Pinzamientos: mover rotulo o punta de flecha.",35,164,2.5);
                AddLeader(db,tr,examples,sid,new Point3d(90,125,0),new Point3d(125,90,0),"B");
                AddLeader(db,tr,examples,sid,new Point3d(210,125,0),new Point3d(165,93,0),"C");
                AddLeader(db,tr,examples,sid,new Point3d(90,55,0),new Point3d(132,79,0),"D");
                AddLeader(db,tr,examples,sid,new Point3d(210,55,0),new Point3d(170,80,0),"E");
                Note(db,tr,examples,"MLEADER: estilo DETALLE_FLECHA_REFERENCIA_V2.",35,20,2.5);
                tr.Commit();
            }
            using(var library=db.Wblock(new ObjectIdCollection(new[]{libraryId}),center))
            {
                library.Insunits=UnitsValue.Millimeters;
                using(var tr=library.TransactionManager.StartTransaction())
                {
                    var styles=(DBDictionary)tr.GetObject(library.MLeaderStyleDictionaryId,OpenMode.ForRead);
                    library.MLeaderstyle=styles.GetAt(StyleName);tr.Commit();
                }
                library.SaveAs(Path.Combine(Root,"Detalle - biblioteca.dwg"),DwgVersion.AC1032);
            }
            AcApp.SetSystemVariable("MIRRTEXT",0);AcApp.SetSystemVariable("TILEMODE",0);
            AcApp.SetSystemVariable("WIPEOUTFRAME",0);
            LayoutManager.Current.CurrentLayout="Layout1";
            doc.Editor.WriteMessage("\nDETALLE_NATIVE_OK\n");
        }

        [CommandMethod("DETALLELIBCHECK")]
        public void LibraryCheck()
        {
            try {LibraryCheckCore();}
            catch(System.Exception ex) {File.WriteAllText(Path.Combine(Root,"library-check-error.txt"),ex.ToString());AcApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nDETALLE_LIBRARY_CHECK_FAILED "+ex+"\n");}
        }
        void LibraryCheckCore()
        {
            var doc=AcApp.DocumentManager.MdiActiveDocument;var db=doc.Database;
            using(var tr=db.TransactionManager.StartTransaction())
            {
                var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead);
                var model=(BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace],OpenMode.ForRead);
                var m=model.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<MLeader>().Single();
                var b=(BlockTableRecord)tr.GetObject(m.BlockContentId,OpenMode.ForRead);
                var attrs=b.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<AttributeDefinition>().ToArray();
                Require(attrs.Length==4,"Library must retain four attributes");
                foreach(var a in attrs) using(var value=m.GetBlockAttribute(a.ObjectId)) Require(!string.IsNullOrWhiteSpace(value.TextString),"Library missing text "+a.Tag);
                Require(m.BlockPosition.DistanceTo(db.Insbase)<1e-6,"Library base point invalid");
                Require(db.MLeaderstyle==m.MLeaderStyle,"Library active style missing");
                tr.Abort();
            }
            File.WriteAllText(Path.Combine(Root,"library-validation.txt"),"DETALLE_LIBRARY_CHECK_OK");
            doc.Editor.WriteMessage("\nDETALLE_LIBRARY_CHECK_OK\n");
        }

        [CommandMethod("DETALLENATIVECHECK")]
        public void NativeCheck()
        {
            try
            {
                var doc=AcApp.DocumentManager.MdiActiveDocument;var db=doc.Database;var report=new List<string>();
                using(var tr=db.TransactionManager.StartTransaction())
                {
                    var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead);
                    var content=(BlockTableRecord)tr.GetObject(bt[FlexibleName],OpenMode.ForRead);
                    var attrs=content.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<AttributeDefinition>().ToArray();
                    Require(attrs.Length==4,"Expected four editable attributes");
                    var primary=LayoutSpace(db,tr,"Layout1").Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<MLeader>().Single();
                    var examples=LayoutSpace(db,tr,"DETALLE LIBRE").Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<MLeader>().ToArray();
                    Require(examples.Length==4,"Expected four flexible examples");
                    foreach(var m in new[]{primary}.Concat(examples))
                    {
                        Require(m.BlockRotation==0 && m.LeaderLineCount==1 && m.ArrowSize>0,"Leader configuration invalid");
                        foreach(var a in attrs) using(var value=m.GetBlockAttribute(a.ObjectId)) Require(value!=null&&!string.IsNullOrWhiteSpace(value.TextString),"Missing text "+a.Tag);
                        report.Add("NATIVE_LEADER_OK "+m.Handle+" center="+m.BlockPosition+" last="+m.GetLastVertex(0)+" arrow="+m.GetFirstVertex(0));
                    }
                    primary.UpgradeOpen();var originalCenter=primary.BlockPosition;
                    int cases=0;
                    foreach(var delta in new[]{new Vector3d(40,35,0),new Vector3d(-60,30,0),new Vector3d(-40,-35,0),new Vector3d(50,-30,0)})
                    {
                        primary.SetFirstVertex(0,originalCenter+delta);
                        Require(primary.BlockPosition.DistanceTo(originalCenter)<1e-6,"Moving arrow moved the label");
                        Require(primary.GetFirstVertex(0).DistanceTo(originalCenter+delta)<1e-6,"Arrow did not follow grip");
                        Require(primary.BlockRotation==0,"Arrow movement rotated text");
                        cases++;
                    }
                    foreach(var a in attrs)
                    {
                        using(var value=primary.GetBlockAttribute(a.ObjectId)) {value.TextString="PRUEBA "+a.Tag;primary.SetBlockAttribute(a.ObjectId,value);}
                        using(var value=primary.GetBlockAttribute(a.ObjectId)) Require(value.TextString=="PRUEBA "+a.Tag,"Attribute not editable "+a.Tag);
                    }
                    report.Add("FOUR_DIRECTIONS_OK "+cases);report.Add("FOUR_ATTRIBUTES_EDITABLE_OK");
                    var original=LayoutSpace(db,tr,"ORIGINAL").Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<BlockReference>().Single();
                    Require(original.IsDynamicBlock,"Original reference was not preserved");
                    report.Add("ORIGINAL_DYNAMIC_BLOCK_PRESERVED");
                    var originalDef=(BlockTableRecord)tr.GetObject(original.BlockTableRecord,OpenMode.ForRead);
                    var originalCircle=originalDef.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<Circle>().Single();
                    var actualCircle=content.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<Circle>().Single();
                    var origin=originalCircle.Center.TransformBy(original.BlockTransform);
                    var transform=Matrix3d.Displacement(-origin.GetAsVector())*original.BlockTransform;
                    using(var expected=(Circle)originalCircle.GetTransformedCopy(transform))
                    {
                        Require(Math.Abs(expected.Radius-actualCircle.Radius)<1e-6,"Circle diameter differs from original");
                        Require(expected.Color.Equals(actualCircle.Color),"Circle color differs from original");
                        Require(expected.LineWeight==actualCircle.LineWeight,"Circle lineweight differs from original");
                        report.Add("CIRCLE_MATCH_OK diameter="+actualCircle.Diameter+" color="+actualCircle.Color);
                    }
                    var originalLine=originalDef.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<Line>().Single();
                    var actualLine=content.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<Line>().Single();
                    using(var expected=(Line)originalLine.GetTransformedCopy(transform))
                    {
                        Require(expected.StartPoint.DistanceTo(actualLine.StartPoint)<1e-6 && expected.EndPoint.DistanceTo(actualLine.EndPoint)<1e-6,"Central line geometry differs from original");
                        Require(expected.Color.Equals(actualLine.Color),"Central line color differs from original");
                        report.Add("CENTER_LINE_MATCH_OK color="+actualLine.Color);
                    }
                    foreach(ObjectId id in original.AttributeCollection)
                    {
                        var expected=(AttributeReference)tr.GetObject(id,OpenMode.ForRead);
                        var actual=attrs.Single(a=>a.Tag==expected.Tag);
                        Require(Math.Abs(expected.Height-actual.Height)<1e-6 && Math.Abs(expected.WidthFactor-actual.WidthFactor)<1e-6 && expected.TextStyleId==actual.TextStyleId,"Text appearance differs "+expected.Tag);
                        Require(actual.Position.DistanceTo(expected.Position-origin.GetAsVector())<1e-6,"Text position differs "+expected.Tag);
                    }
                    report.Add("ORIGINAL_TEXT_STYLE_SIZE_AND_POSITION_MATCH_OK");
                    tr.Abort();
                }
                report.Add("DETALLE_NATIVE_CHECK_OK");File.WriteAllLines(Path.Combine(Root,"native-validation.txt"),report);
                doc.Editor.WriteMessage("\nDETALLE_NATIVE_CHECK_OK\n");
            }
            catch(System.Exception ex) {File.WriteAllText(Path.Combine(Root,"native-check-error.txt"),ex.ToString());AcApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nDETALLE_NATIVE_CHECK_FAILED "+ex+"\n");}
        }

        static AttributeDefinition Attr(BlockTableRecord b, Transaction tr, Database db, string tag, string text, string prompt, double x, double y, double h, bool center=false)
        {
            var a=new AttributeDefinition(new Point3d(x,y,0),text,tag,prompt,db.Textstyle) {Height=h,LockPositionInBlock=true,Layer="0",ColorIndex=0};
            if(center) {a.Justify=AttachmentPoint.MiddleCenter;a.AlignmentPoint=new Point3d(x,y,0);}
            b.AppendEntity(a);tr.AddNewlyCreatedDBObject(a,true);a.AdjustAlignment(db);return a;
        }
        static ObjectId Flexible(Database db, Transaction tr, BlockReference reference, Point3d center)
        {
            var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForWrite);
            var b=new BlockTableRecord {Name=FlexibleName,Units=UnitsValue.Millimeters};
            bt.Add(b);tr.AddNewlyCreatedDBObject(b,true);
            var original=(BlockTableRecord)tr.GetObject(reference.BlockTableRecord,OpenMode.ForRead);
            var transform=Matrix3d.Displacement(-center.GetAsVector())*reference.BlockTransform;
            var originalCircle=original.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<Circle>().Single();
            var c=(Circle)originalCircle.GetTransformedCopy(transform);
            var boundary=new Point2dCollection();
            for(int i=0;i<=64;i++) {double angle=i*Math.PI/32;boundary.Add(new Point2d(c.Radius*Math.Cos(angle),c.Radius*Math.Sin(angle)));}
            var mask=new Wipeout();mask.SetFrom(boundary,Vector3d.ZAxis);mask.Layer="0";
            b.AppendEntity(mask);tr.AddNewlyCreatedDBObject(mask,true);
            b.AppendEntity(c);tr.AddNewlyCreatedDBObject(c,true);
            var originalLine=original.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<Line>().Single();
            var line=(Line)originalLine.GetTransformedCopy(transform);
            b.AppendEntity(line);tr.AddNewlyCreatedDBObject(line,true);
            AttributeDefinition title=null;
            foreach(ObjectId id in reference.AttributeCollection)
            {
                var source=(AttributeReference)tr.GetObject(id,OpenMode.ForRead);
                if(source.Tag!="LABEL" && source.Tag!="REF" && source.Tag!="DET")continue;
                var a=CopyAttribute(source,center);
                b.AppendEntity(a);tr.AddNewlyCreatedDBObject(a,true);a.AdjustAlignment(db);
                if(a.Tag=="DET") title=a;
            }
            Require(title!=null,"Original title missing");
            var lower=(AttributeDefinition)title.Clone();lower.Tag="TEXTO_INFERIOR";
            lower.Prompt="Texto inferior";lower.TextString="ESC. 1:25";
            lower.TransformBy(Matrix3d.Displacement(new Vector3d(0,-title.Height*2.1,0)));
            b.AppendEntity(lower);tr.AddNewlyCreatedDBObject(lower,true);lower.AdjustAlignment(db);
            var dict=(DBDictionary)tr.GetObject(db.MLeaderStyleDictionaryId,OpenMode.ForWrite);
            var style=new MLeaderStyle {
                ContentType=ContentType.BlockContent,BlockId=b.ObjectId,
                BlockConnectionType=BlockConnectionType.ConnectBase,
                BlockScale=new Scale3d(1),BlockRotation=0,
                LeaderLineType=LeaderType.StraightLeader,ArrowSize=2.5,
                EnableLanding=false,EnableDogleg=false,DoglegLength=0,
                Scale=1,Annotative=AnnotativeStates.False,
                LeaderLineColor=Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer,256),
                LeaderLineWeight=LineWeight.ByLayer
            };
            var sid=style.PostMLeaderStyleToDb(db,StyleName);tr.AddNewlyCreatedDBObject(style,true);
            return sid;
        }
        static AttributeDefinition CopyAttribute(AttributeReference source,Point3d center)
        {
            var a=new AttributeDefinition();a.SetPropertiesFrom(source);
            a.Tag=source.Tag;a.Prompt=source.Tag;a.TextString=source.TextString;
            a.TextStyleId=source.TextStyleId;a.Height=source.Height;a.WidthFactor=source.WidthFactor;
            a.Normal=source.Normal;a.Rotation=source.Rotation;a.Oblique=source.Oblique;
            a.IsMirroredInX=source.IsMirroredInX;a.IsMirroredInY=source.IsMirroredInY;
            a.Justify=source.Justify;a.Position=source.Position-center.GetAsVector();
            if(source.Justify!=AttachmentPoint.BaseLeft)a.AlignmentPoint=source.AlignmentPoint-center.GetAsVector();
            a.LockPositionInBlock=true;return a;
        }
        static MLeader AddLeader(Database db,Transaction tr,BlockTableRecord space,ObjectId sid,Point3d center,Point3d tip,string label,string title="DETALLE",string lower="ESC. 1:25")
        {
            var m=new MLeader();m.SetDatabaseDefaults(db);m.MLeaderStyle=sid;m.ContentType=ContentType.BlockContent;
            var style=(MLeaderStyle)tr.GetObject(sid,OpenMode.ForRead);
            m.BlockContentId=style.BlockId;m.BlockConnectionType=BlockConnectionType.ConnectBase;
            m.BlockScale=new Scale3d(1);m.BlockRotation=0;m.Scale=1;m.ArrowSize=2.5;
            m.EnableLanding=false;m.EnableDogleg=false;m.LeaderLineType=LeaderType.StraightLeader;
            m.EnableAnnotationScale=false;m.Layer="0";
            space.AppendEntity(m);tr.AddNewlyCreatedDBObject(m,true);
            m.BlockPosition=center;
            int leader=m.AddLeader();int line=m.AddLeaderLine(leader);
            m.AddFirstVertex(line,tip);m.AddLastVertex(line,center);
            m.BlockPosition=center;
            var block=(BlockTableRecord)tr.GetObject(style.BlockId,OpenMode.ForRead);
            foreach(ObjectId id in block)
            {
                var a=tr.GetObject(id,OpenMode.ForRead) as AttributeDefinition;if(a==null)continue;
                using(var value=new AttributeReference())
                {
                    value.SetAttributeFromBlock(a,Matrix3d.Displacement(center.GetAsVector()));
                    value.TextString=a.Tag=="LABEL"?label:a.Tag=="DET"?title:a.Tag=="TEXTO_INFERIOR"?lower:a.TextString;
                    m.SetBlockAttribute(id,value);
                }
            }
            return m;
        }

        static BlockTableRecord LayoutSpace(Database db,Transaction tr,string name)
        {
            var lm=LayoutManager.Current;
            var dict=(DBDictionary)tr.GetObject(db.LayoutDictionaryId,OpenMode.ForRead);
            var lid=dict.Contains(name)?dict.GetAt(name):lm.CreateLayout(name);
            var layout=(Layout)tr.GetObject(lid,OpenMode.ForRead);
            return (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId,OpenMode.ForWrite);
        }
        static void Note(Database db,Transaction tr,BlockTableRecord space,string text,double x,double y,double h=3)
        {
            var t=new DBText {TextString=text,Position=new Point3d(x,y,0),Height=h,TextStyleId=db.Textstyle,Layer="0"};space.AppendEntity(t);tr.AddNewlyCreatedDBObject(t,true);
        }

        static void Require(bool condition,string message) {if(!condition)throw new System.Exception(message);}

        [CommandMethod("DETALLEPDF")]
        public void Pdf()
        {
            try
            {
                var doc=AcApp.DocumentManager.MdiActiveDocument;var db=doc.Database;
                AcApp.SetSystemVariable("BACKGROUNDPLOT",0);AcApp.SetSystemVariable("WIPEOUTFRAME",0);
                foreach(string name in new[]{"Layout1","DETALLE LIBRE","COMPARACION"})
                {
                    LayoutManager.Current.CurrentLayout=name;
                    using(var tr=db.TransactionManager.StartTransaction())
                    {
                        var lid=LayoutManager.Current.GetLayoutId(name);var layout=(Layout)tr.GetObject(lid,OpenMode.ForRead);
                        using(var ps=new PlotSettings(layout.ModelType))
                        using(var pi=new Autodesk.AutoCAD.PlottingServices.PlotInfo())
                        using(var validator=new Autodesk.AutoCAD.PlottingServices.PlotInfoValidator())
                        {
                            ps.CopyFrom(layout);var psv=PlotSettingsValidator.Current;
                            psv.SetPlotConfigurationName(ps,"DWG To PDF.pc3","ISO_full_bleed_A4_(297.00_x_210.00_MM)");
                            psv.SetPlotPaperUnits(ps,PlotPaperUnit.Millimeters);
                            psv.SetPlotRotation(ps,PlotRotation.Degrees000);
                            var window=name=="Layout1"?new Extents2d(60,70,160,145):name=="COMPARACION"?new Extents2d(60,70,240,150):new Extents2d(25,10,265,190);
                            if(name=="Layout1")
                            {
                                var space=(BlockTableRecord)tr.GetObject(layout.BlockTableRecordId,OpenMode.ForRead);
                                var marker=space.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<MLeader>().Single();
                                var bounds=marker.GeometricExtents;window=new Extents2d(bounds.MinPoint.X-10,bounds.MinPoint.Y-10,bounds.MaxPoint.X+10,bounds.MaxPoint.Y+10);
                            }
                            psv.SetPlotWindowArea(ps,window);
                            psv.SetPlotType(ps,Autodesk.AutoCAD.DatabaseServices.PlotType.Window);
                            psv.SetUseStandardScale(ps,true);psv.SetStdScaleType(ps,StdScaleType.ScaleToFit);psv.SetPlotCentered(ps,true);
                            pi.Layout=lid;pi.OverrideSettings=ps;
                            validator.MediaMatchingPolicy=Autodesk.AutoCAD.PlottingServices.MatchingPolicy.MatchEnabled;validator.Validate(pi);
                            using(var engine=Autodesk.AutoCAD.PlottingServices.PlotFactory.CreatePublishEngine())
                            using(var page=new Autodesk.AutoCAD.PlottingServices.PlotPageInfo())
                            {
                                string output=Path.Combine(Root,name=="Layout1"?"Detalle - vista.pdf":name=="COMPARACION"?"Detalle - comparacion.pdf":"Detalle - ejemplos.pdf");
                                engine.BeginPlot(null,null);engine.BeginDocument(pi,doc.Name,null,1,true,output);
                                engine.BeginPage(page,pi,true,null);engine.BeginGenerateGraphics(null);engine.EndGenerateGraphics(null);engine.EndPage(null);engine.EndDocument(null);engine.EndPlot(null);
                            }
                        }
                        tr.Abort();
                    }
                }
                LayoutManager.Current.CurrentLayout="Layout1";
                doc.Editor.WriteMessage("\nDETALLE_PDF_OK\n");
            }
            catch(System.Exception ex) {File.WriteAllText(Path.Combine(Root,"pdf-error.txt"),ex.ToString());AcApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nDETALLE_PDF_FAILED "+ex+"\n");}
        }
    }
}
