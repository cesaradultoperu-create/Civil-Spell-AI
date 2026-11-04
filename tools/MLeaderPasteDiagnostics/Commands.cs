using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace MLeaderPasteDiagnostics
{
    // One-shot read-only command; no timers, reactors, overrules or startup entry.
    public class Commands
    {
        static string TestRoot=>Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(Commands).Assembly.Location),"../../Test/MLeaderPaste-20261022"));
        static string Get(object obj,string name)
        {try{return Convert.ToString(obj.GetType().GetProperty(name)?.GetValue(obj,null));}catch(System.Exception ex){return ex.GetType().Name;}}
        static void Write(XmlWriter w,string name,object value)=>w.WriteAttributeString(name,Convert.ToString(value));
        [CommandMethod("MLPEGARINFO",CommandFlags.Session)]
        public void Inspect()
        {
            string folder=Path.Combine(Path.GetDirectoryName(typeof(Commands).Assembly.Location),"Reports");Directory.CreateDirectory(folder);
            string output=Path.Combine(folder,"inspeccion-"+DateTime.Now.ToString("HHmmss")+".xml");
            using(var w=XmlWriter.Create(output,new XmlWriterSettings{Indent=true}))
            {
                w.WriteStartElement("ReadOnlyInspection");
                foreach(Document doc in App.DocumentManager)
                {
                    if(!doc.Name.Contains("200-15-P-005")&&!doc.Name.Contains("200-15-P-009")&&!doc.Name.Contains("P-005-copia")&&!doc.Name.Contains("P-009-copia"))continue;
                    w.WriteStartElement("Drawing");Write(w,"path",doc.Name);Write(w,"readonly",doc.IsReadOnly);
                    var clock=Stopwatch.StartNew();
                    using(doc.LockDocument())using(var tr=doc.Database.TransactionManager.StartTransaction())
                    {
                        var db=doc.Database;var layouts=(DBDictionary)tr.GetObject(db.LayoutDictionaryId,OpenMode.ForRead);
                        var collection=db.ObjectContextManager.GetContextCollection("ACDB_ANNOTATIONSCALES");
                        var contexts=collection.Cast<ObjectContext>().ToArray();Write(w,"scaleCount",contexts.Length);
                        Write(w,"currentScale",db.Cannoscale.Name);
                        var ctxt=typeof(DBObject).Assembly.GetType("Autodesk.AutoCAD.DatabaseServices.ObjectContexts");
                        var has=ctxt?.GetMethod("HasContext",BindingFlags.Public|BindingFlags.Static);
                        foreach(DBDictionaryEntry entry in layouts)
                        {
                            var layout=(Layout)tr.GetObject(entry.Value,OpenMode.ForRead);
                            var space=(BlockTableRecord)tr.GetObject(layout.BlockTableRecordId,OpenMode.ForRead);
                            w.WriteStartElement("Layout");Write(w,"name",layout.LayoutName);int mlCount=0,labelCount=0;
                            foreach(ObjectId id in space)
                            {
                                string kind=id.ObjectClass.DxfName;
                                if(kind=="VIEWPORT")
                                {
                                    var v=(Viewport)tr.GetObject(id,OpenMode.ForRead);w.WriteStartElement("Viewport");Write(w,"number",v.Number);
                                    Write(w,"locked",v.Locked);Write(w,"customScale",v.CustomScale);
                                    Write(w,"annotationScale",Get(v.AnnotationScale,"Name"));
                                    Write(w,"frozenLayers",String.Join(",",v.GetFrozenLayers().Cast<ObjectId>().Select(k=>((LayerTableRecord)tr.GetObject(k,OpenMode.ForRead)).Name)));
                                    w.WriteEndElement();continue;
                                }
                                bool isML=kind=="MULTILEADER",isLabel=kind.IndexOf("LABEL",StringComparison.OrdinalIgnoreCase)>=0;
                                if(!isML&&!isLabel)continue;
                                var entity=(Entity)tr.GetObject(id,OpenMode.ForRead);
                                if(isML)mlCount++;else labelCount++;
                                var layer=(LayerTableRecord)tr.GetObject(entity.LayerId,OpenMode.ForRead);
                                w.WriteStartElement(isML?"MLeader":"Label");Write(w,"handle",id.Handle);Write(w,"type",kind);Write(w,"layer",layer.Name);
                                Write(w,"visible",entity.Visible);Write(w,"off",layer.IsOff);Write(w,"frozen",layer.IsFrozen);
                                Write(w,"annotative",Get(entity,"Annotative"));
                                try{Write(w,"bounds",entity.GeometricExtents);}catch(Autodesk.AutoCAD.Runtime.Exception){}
                                if(isML)
                                {
                                    var m=(MLeader)entity;var style=(MLeaderStyle)tr.GetObject(m.MLeaderStyle,OpenMode.ForRead);
                                    Write(w,"style",style.Name);Write(w,"scale",m.Scale);Write(w,"annotationEnabled",m.EnableAnnotationScale);Write(w,"contentType",m.ContentType);
                                    if(has!=null)Write(w,"contexts",String.Join(",",contexts.Take(2000).Where(c=>(bool)has.Invoke(null,new object[]{m,c})).Select(c=>c.Name)));
                                    using(var text=m.MText)
                                    {
                                        if(text!=null)
                                        {
                                            Write(w,"text",text.Contents);Write(w,"height",text.TextHeight);
                                            var ts=(TextStyleTableRecord)tr.GetObject(text.TextStyleId,OpenMode.ForRead);
                                            Write(w,"textStyle",ts.Name);Write(w,"font",ts.FileName);Write(w,"fontWidth",ts.XScale);
                                        }
                                    }
                                }
                                else
                                {
                                    Write(w,"labelStyleId",Get(entity,"StyleId"));Write(w,"labelStyleName",Get(entity,"StyleName"));
                                    Write(w,"labelLocation",Get(entity,"LabelLocation"));
                                }
                                w.WriteEndElement();
                            }
                            w.WriteElementString("MLeaderCount",mlCount.ToString());w.WriteElementString("LabelCount",labelCount.ToString());w.WriteEndElement();
                        }
                        tr.Commit();
                    }
                    w.WriteElementString("ElapsedMs",clock.ElapsedMilliseconds.ToString());w.WriteEndElement();
                }
                w.WriteEndElement();
            }
            App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nInspección sin cambios: "+output+"\n");
        }
        [CommandMethod("MLPEGARPROBAR")]
        public void CloneTest()
        {
            string root=TestRoot;var report=new List<string>();
            try
            {
                using(var source=new Database(false,true))using(var destination=new Database(false,true))
                {
                    var watch=Stopwatch.StartNew();
                    source.ReadDwgFile(Path.Combine(root,"P-005-copia.dwg"),FileOpenMode.OpenForReadAndAllShare,true,null);source.CloseInput(true);
                    destination.ReadDwgFile(Path.Combine(root,"P-009-copia.dwg"),FileOpenMode.OpenForReadAndAllShare,true,null);destination.CloseInput(true);
                    report.Add("READ_COPIES_MS "+watch.ElapsedMilliseconds);var ids=new ObjectIdCollection();ObjectId to=ObjectId.Null;
                    using(var tr=source.TransactionManager.StartTransaction())
                    {
                        var layouts=(DBDictionary)tr.GetObject(source.LayoutDictionaryId,OpenMode.ForRead);
                        foreach(DBDictionaryEntry entry in layouts)
                        {
                            var l=(Layout)tr.GetObject(entry.Value,OpenMode.ForRead);if(l.LayoutName!="A 1")continue;
                            foreach(ObjectId id in (BlockTableRecord)tr.GetObject(l.BlockTableRecordId,OpenMode.ForRead))
                            {
                                if(id.ObjectClass.DxfName!="MULTILEADER")continue;
                                var m=(MLeader)tr.GetObject(id,OpenMode.ForRead);if(m.Layer=="_llamados")ids.Add(id);
                            }
                        }
                        tr.Commit();
                    }
                    using(var tr=destination.TransactionManager.StartTransaction())
                    {
                        var layouts=(DBDictionary)tr.GetObject(destination.LayoutDictionaryId,OpenMode.ForRead);
                        foreach(DBDictionaryEntry entry in layouts){var l=(Layout)tr.GetObject(entry.Value,OpenMode.ForRead);if(l.LayoutName=="Layout1")to=l.BlockTableRecordId;}
                        tr.Commit();
                    }
                    if(ids.Count==0||to.IsNull)throw new InvalidOperationException("Faltan llamados o presentación de destino.");
                    report.Add("SOURCE_MLEADERS "+ids.Count);watch.Restart();var map=new IdMapping();
                    source.WblockCloneObjects(ids,to,map,DuplicateRecordCloning.Ignore,false);
                    report.Add("CLONE_MS "+watch.ElapsedMilliseconds);
                    using(var tr=destination.TransactionManager.StartTransaction())
                    {
                        foreach(ObjectId id in ids)
                        {
                            var m=(MLeader)tr.GetObject(map[id].Value,OpenMode.ForRead);var l=(LayerTableRecord)tr.GetObject(m.LayerId,OpenMode.ForRead);
                            using(var text=m.MText)report.Add("COPIED "+m.Handle+" layer="+l.Name+" off="+l.IsOff+" frozen="+l.IsFrozen+" visible="+m.Visible+" annotative="+Get(m,"Annotative")+" scale="+m.Scale+" textHeight="+text.TextHeight+" bounds="+m.GeometricExtents+" text="+text.Contents);
                        }
                        tr.Commit();
                    }
                    destination.SaveAs(Path.Combine(root,"P-009-prueba-llamados.dwg"),DwgVersion.Current);
                    report.Add("LOCAL_CLONE_OK");
                }
            }
            catch(System.Exception ex){report.Add("LOCAL_CLONE_FAILED "+ex);}
            File.WriteAllLines(Path.Combine(root,"prueba-copia.txt"),report);
            App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\n"+String.Join("\n",report)+"\n");
        }
    }
}
