using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly:ExtensionApplication(typeof(DetalleAutoLado.AutoSideApplication))]
namespace DetalleAutoLado
{
    public static class SideSwitch
    {
        public const string BaseName="DETALLE_LAYOUT_REFERENCIA_V2",OldRight="DETALLE_LAYOUT_REFERENCIA_V3_DERECHA",Prefix="DETALLE_LAYOUT_V6_",PreviousPrefix="DETALLE_LAYOUT_V4_",MaskPrefix="DETALLE_LAYOUT_V5_";
        public const string WidthRecord="DETALLE_ANCHO_MINIMO";
        public const string MaskRecord="DETALLE_MASCARA_FONDO";
        public static bool KnownName(string n){return n==BaseName||n==OldRight||n.StartsWith(Prefix,StringComparison.Ordinal)||n.StartsWith(PreviousPrefix,StringComparison.Ordinal)||n.StartsWith(MaskPrefix,StringComparison.Ordinal);}
        public static bool IsRight(string n){return n==OldRight||n.StartsWith(Prefix+"D_",StringComparison.Ordinal)||n.StartsWith(PreviousPrefix+"D_",StringComparison.Ordinal)||n.StartsWith(MaskPrefix+"D_",StringComparison.Ordinal);}
        public static bool IsDetail(MLeader m,Transaction tr)
        {return m.ContentType==ContentType.BlockContent&&!m.BlockContentId.IsNull&&KnownName(((BlockTableRecord)tr.GetObject(m.BlockContentId,OpenMode.ForRead)).Name);}
        public static double Width(BlockTableRecord b,Transaction tr)
        {var l=b.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<Line>().Single();return Math.Max(Math.Abs(l.StartPoint.X),Math.Abs(l.EndPoint.X));}
        public static Matrix3d Transform(MLeader m)
        {
            var r=Matrix3d.Rotation(m.BlockRotation,Vector3d.ZAxis,Point3d.Origin);
            return Matrix3d.AlignCoordinateSystem(Point3d.Origin,Vector3d.XAxis,Vector3d.YAxis,Vector3d.ZAxis,m.BlockPosition,
                (Vector3d.XAxis*m.BlockScale.X*m.Scale).TransformBy(r),(Vector3d.YAxis*m.BlockScale.Y*m.Scale).TransformBy(r),(Vector3d.ZAxis*m.BlockScale.Z*m.Scale).TransformBy(r));
        }
        public static Point3d Endpoint(MLeader m,Transaction tr)
        {var b=(BlockTableRecord)tr.GetObject(m.BlockContentId,OpenMode.ForRead);return new Point3d(IsRight(b.Name)?Width(b,tr):-Width(b,tr),0,0).TransformBy(Transform(m));}
        public static double Minimum(MLeader m,Transaction tr)
        {
            if(m.ExtensionDictionary.IsNull)return 35;
            var d=(DBDictionary)tr.GetObject(m.ExtensionDictionary,OpenMode.ForRead);if(!d.Contains(WidthRecord))return 35;
            using(var data=((Xrecord)tr.GetObject(d.GetAt(WidthRecord),OpenMode.ForRead)).Data)return data==null?35:Convert.ToDouble(data.AsArray()[0].Value,CultureInfo.InvariantCulture);
        }
        public static void SetMinimum(MLeader m,Transaction tr,double width)
        {
            if(double.IsNaN(width)||double.IsInfinity(width))throw new ArgumentOutOfRangeException(nameof(width));
            if(!m.IsWriteEnabled)m.UpgradeOpen();if(m.ExtensionDictionary.IsNull)m.CreateExtensionDictionary();
            var d=(DBDictionary)tr.GetObject(m.ExtensionDictionary,OpenMode.ForWrite);Xrecord record;
            if(d.Contains(WidthRecord))record=(Xrecord)tr.GetObject(d.GetAt(WidthRecord),OpenMode.ForWrite);
            else{record=new Xrecord();d.SetAt(WidthRecord,record);tr.AddNewlyCreatedDBObject(record,true);}
            record.Data=new ResultBuffer(new TypedValue((int)DxfCode.Real,Math.Max(12,width)));
        }
        public static bool MaskEnabled(MLeader m,Transaction tr)
        {
            if(m.ExtensionDictionary.IsNull)return true;
            var d=(DBDictionary)tr.GetObject(m.ExtensionDictionary,OpenMode.ForRead);if(!d.Contains(MaskRecord))return true;
            using(var data=((Xrecord)tr.GetObject(d.GetAt(MaskRecord),OpenMode.ForRead)).Data)return data==null||Convert.ToInt32(data.AsArray()[0].Value)!=0;
        }
        public static void SetMaskEnabled(MLeader m,Transaction tr,bool enabled)
        {
            if(!m.IsWriteEnabled)m.UpgradeOpen();if(m.ExtensionDictionary.IsNull)m.CreateExtensionDictionary();
            var d=(DBDictionary)tr.GetObject(m.ExtensionDictionary,OpenMode.ForWrite);Xrecord record;
            if(d.Contains(MaskRecord))record=(Xrecord)tr.GetObject(d.GetAt(MaskRecord),OpenMode.ForWrite);
            else{record=new Xrecord();d.SetAt(MaskRecord,record);tr.AddNewlyCreatedDBObject(record,true);}
            record.Data=new ResultBuffer(new TypedValue((int)DxfCode.Int16,(short)(enabled?1:0)));
        }
        public static double TextWidth(AttributeDefinition a,string text)
        {
            if(string.IsNullOrEmpty(text))return 0;
            
            using(var t=new DBText())
            {t.SetDatabaseDefaults(a.Database);t.TextStyleId=a.TextStyleId;t.Height=a.Height;t.WidthFactor=a.WidthFactor;t.Oblique=a.Oblique;t.Position=Point3d.Origin;t.TextString=text;var e=t.GeometricExtents;return e.MaxPoint.X-e.MinPoint.X;}
        }
        static void PlaceText(DBText text,string tag,bool right,double width,double height,Matrix3d transform)
        {
            // Attribute MText setters retain DBText alignment. Move the whole
            // attribute using its actual native text bounds, including accents.
            using(var mt=text is AttributeDefinition a?a.MTextAttributeDefinition:((AttributeReference)text).MTextAttribute)
            {
                var inverse=transform.Inverse();var p=mt.GetBoundingPoints().Cast<Point3d>().Select(v=>v.TransformBy(inverse)).ToArray();
                double dx=(right?width:-width)-(right?p.Max(v=>v.X):p.Min(v=>v.X));
                double dy=tag=="DET"?height*.5-p.Min(v=>v.Y):-height*.5-p.Max(v=>v.Y);
                text.TransformBy(Matrix3d.Displacement(new Vector3d(dx,dy,0).TransformBy(transform)));
            }
        }
        static void ConfigureMask(MText mt,bool mask)
        {
            mt.Width=0;mt.BackgroundScaleFactor=1.1;mt.UseBackgroundColor=mask;mt.BackgroundFill=mask;
        }
        static ObjectId Variant(Database db,Transaction tr,BlockTableRecord source,bool right,double width,bool mask,Dictionary<string,string> texts)
        {
            string textKey;
            using(var sha=SHA256.Create())textKey=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(texts["DET"].Length+":"+texts["DET"]+texts["TEXTO_INFERIOR"]))).Replace("-","").Substring(0,24);
            string name=Prefix+(right?"D_":"I_")+((long)Math.Round(width*10)).ToString("D6",CultureInfo.InvariantCulture)+(mask?"_M1":"_M0")+"_T"+textKey;
            var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead);if(bt.Has(name))return bt[name];
            bt.UpgradeOpen();var b=new BlockTableRecord {Name=name,Units=source.Units};bt.Add(b);tr.AddNewlyCreatedDBObject(b,true);
            var c=source.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<Circle>().Single();
            foreach(ObjectId id in source)
            {
                var copy=(Entity)tr.GetObject(id,OpenMode.ForRead).Clone();var a=copy as AttributeDefinition;
                if(a!=null&&(a.Tag=="DET"||a.Tag=="TEXTO_INFERIOR"))
                {
                    if(a.IsMTextAttributeDefinition)a.IsMTextAttributeDefinition=false;
                    a.TextString=texts[a.Tag];
                    var p=new Point3d(right?width:-width,0,0);
                    a.Justify=right?AttachmentPoint.BaseRight:AttachmentPoint.BaseLeft;a.Position=p;if(right)a.AlignmentPoint=p;a.IsMirroredInX=false;a.IsMirroredInY=false;
                }
                else if(copy is Line l){l.StartPoint=new Point3d(right?-c.Radius:-width,0,0);l.EndPoint=new Point3d(right?width:c.Radius,0,0);}
                b.AppendEntity(copy);tr.AddNewlyCreatedDBObject(copy,true);
                if(a!=null)
                {
                    a.AdjustAlignment(db);
                    if(a.Tag=="DET"||a.Tag=="TEXTO_INFERIOR")
                    {
                        a.IsMTextAttributeDefinition=true;a.UpdateMTextAttributeDefinition();
                        using(var mt=a.MTextAttributeDefinition)
                        {ConfigureMask(mt,mask);a.MTextAttributeDefinition=mt;}
                        PlaceText(a,a.Tag,right,width,a.Height,Matrix3d.Identity);
                    }
                }
            }
            return b.ObjectId;
        }
        public static bool Update(MLeader m,Transaction tr)
        {
            if(!IsDetail(m,tr)||m.LeaderLineCount==0)return false;
            
            var before=(BlockTableRecord)tr.GetObject(m.BlockContentId,OpenMode.ForRead);
            int line=(int)m.GetLeaderLineIndexes((int)m.GetLeaderIndexes()[0])[0];var arrow=m.GetFirstVertex(line).TransformBy(Transform(m).Inverse());
            bool right=Math.Abs(arrow.X)<0.05?IsRight(before.Name):arrow.X<0;
            var texts=new Dictionary<string,string>();bool mask=MaskEnabled(m,tr);double longest=0;
            foreach(ObjectId id in before)
            {
                var a=tr.GetObject(id,OpenMode.ForRead) as AttributeDefinition;if(a==null)continue;
                using(var v=m.GetBlockAttribute(id))texts[a.Tag]=v.TextString;
                if(a.Tag=="DET"||a.Tag=="TEXTO_INFERIOR")longest=Math.Max(longest,TextWidth(a,texts[a.Tag]));
            }
            double radius=before.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<Circle>().Single().Radius;
            if(m.ExtensionDictionary.IsNull||!((DBDictionary)tr.GetObject(m.ExtensionDictionary,OpenMode.ForRead)).Contains(WidthRecord))SetMinimum(m,tr,35);
            double width=Math.Ceiling(Math.Max(Minimum(m,tr),radius+2+longest)*10-1e-8)/10;
            var target=Variant(m.Database,tr,before,right,width,mask,texts);if(target==m.BlockContentId)return false;
            if(!m.IsWriteEnabled)m.UpgradeOpen();var center=m.BlockPosition;var transform=Transform(m);m.BlockContentId=target;
            var after=(BlockTableRecord)tr.GetObject(target,OpenMode.ForRead);
            foreach(ObjectId id in after)
            {
                var a=tr.GetObject(id,OpenMode.ForRead) as AttributeDefinition;if(a==null)continue;
                using(var v=new AttributeReference())
                {
                    v.SetAttributeFromBlock(a,transform);v.TextString=texts.ContainsKey(a.Tag)?texts[a.Tag]:a.TextString;v.AdjustAlignment(m.Database);
                    if(v.IsMTextAttribute)
                    {
                        v.UpdateMTextAttribute();
                        using(var mt=v.MTextAttribute){ConfigureMask(mt,mask);v.MTextAttribute=mt;}
                        PlaceText(v,a.Tag,right,width,a.Height,transform);
                    }
                    m.SetBlockAttribute(id,v);
                }
            }
            m.BlockPosition=center;m.RecordGraphicsModified(true);return true;
        }
        public static int UpdateDatabase(Database db,IEnumerable<ObjectId> ids=null)
        {
            int count=0;using(var tr=db.TransactionManager.StartTransaction())
            {
                var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead);
                if(!bt.Has(BaseName)&&!bt.Has(OldRight)&&!bt.Cast<ObjectId>().Any(id=>KnownName(((BlockTableRecord)tr.GetObject(id,OpenMode.ForRead)).Name)))return 0;
                if(ids==null)ids=bt.Cast<ObjectId>().Select(id=>(BlockTableRecord)tr.GetObject(id,OpenMode.ForRead)).Where(b=>b.IsLayout).SelectMany(b=>b.Cast<ObjectId>()).ToArray();
                foreach(var id in ids){if(id.IsNull||id.IsErased||!id.IsValid)continue;var m=tr.GetObject(id,OpenMode.ForRead,false) as MLeader;if(m!=null&&Update(m,tr))count++;}
                tr.Commit();
            }
            return count;
        }
    }
    public sealed class AutoSideApplication:IExtensionApplication
    {
        sealed class State {public readonly HashSet<ObjectId> Pending=new HashSet<ObjectId>();public bool Scan=true,Busy;}
        static readonly Dictionary<Document,State> States=new Dictionary<Document,State>();static DetailGripOverrule Grip;
        public void Initialize()
        {
            
            // The old loaded assembly cannot be unloaded; detach its static handlers.
            foreach(var old in AppDomain.CurrentDomain.GetAssemblies().Where(a=>a!=typeof(AutoSideApplication).Assembly&&(a.GetName().Name=="DetalleAutoLado.2024"||a.GetName().Name.StartsWith("DetalleAdaptable",StringComparison.Ordinal))))
            {var t=old.GetType("DetalleAutoLado.AutoSideApplication");if(t!=null)t.GetMethod("Terminate").Invoke(Activator.CreateInstance(t),null);}
            App.DocumentManager.DocumentCreated+=Created;App.DocumentManager.DocumentToBeDestroyed+=Destroyed;App.Idle+=Idle;foreach(Document d in App.DocumentManager)Attach(d);
            
            Grip=new DetailGripOverrule();Grip.SetExtensionDictionaryEntryFilter(SideSwitch.WidthRecord);Overrule.AddOverrule(RXObject.GetClass(typeof(MLeader)),Grip,false);Overrule.Overruling=true;
            
        }
        public void Terminate()
        {
            App.Idle-=Idle;App.DocumentManager.DocumentCreated-=Created;App.DocumentManager.DocumentToBeDestroyed-=Destroyed;foreach(var d in States.Keys.ToArray())Detach(d);
            if(Grip!=null){Overrule.RemoveOverrule(RXObject.GetClass(typeof(MLeader)),Grip);Grip.Dispose();Grip=null;}
        }
        static void Created(object s,DocumentCollectionEventArgs e){Attach(e.Document);}static void Destroyed(object s,DocumentCollectionEventArgs e){Detach(e.Document);}
        static void Attach(Document d)
        {if(States.ContainsKey(d))return;States.Add(d,new State());d.Database.ObjectModified+=Modified;d.Database.ObjectAppended+=Modified;d.CommandEnded+=Finished;d.CommandCancelled+=Finished;d.CommandFailed+=Finished;}
        static void Detach(Document d)
        {if(!States.Remove(d))return;d.Database.ObjectModified-=Modified;d.Database.ObjectAppended-=Modified;d.CommandEnded-=Finished;d.CommandCancelled-=Finished;d.CommandFailed-=Finished;}
        static void Modified(object sender,ObjectEventArgs e)
        {if(!(e.DBObject is MLeader))return;foreach(var p in States)if(p.Key.Database==(sender as Database)&&!p.Value.Busy&&!e.DBObject.ObjectId.IsNull)p.Value.Pending.Add(e.DBObject.ObjectId);}
        static void Finished(object sender,CommandEventArgs e)
        {
            var d=sender as Document;if(d==null||!States.ContainsKey(d))return;
            if(new[]{"U","UNDO","REDO","MREDO"}.Contains(e.GlobalCommandName)){States[d].Pending.Clear();States[d].Scan=false;return;}Flush(d,false);
        }
        static void Idle(object s,EventArgs e){var d=App.DocumentManager.MdiActiveDocument;if(d!=null&&string.IsNullOrEmpty(d.CommandInProgress))Flush(d,true);}
        static int Flush(Document d,bool useLock)
        {
            State s;if(!States.TryGetValue(d,out s)||s.Busy||(!s.Scan&&s.Pending.Count==0))return 0;s.Busy=true;
            try
            {
                int n;if(useLock)using(d.LockDocument())n=SideSwitch.UpdateDatabase(d.Database,s.Scan?null:s.Pending.ToArray());else n=SideSwitch.UpdateDatabase(d.Database,s.Scan?null:s.Pending.ToArray());
                s.Scan=false;s.Pending.Clear();if(n>0)d.Editor.Regen();return n;
            }
            catch(System.Exception ex)
            {var a=ex as Autodesk.AutoCAD.Runtime.Exception;if(a==null||a.ErrorStatus!=ErrorStatus.LockViolation){s.Pending.Clear();s.Scan=false;d.Editor.WriteMessage("\nDetalle adaptable: "+ex.Message);}return 0;}
            finally{s.Busy=false;}
        }
        [CommandMethod("DETALLEAJUSTAR6")]
        public void Refresh(){var d=App.DocumentManager.MdiActiveDocument;States[d].Scan=true;Flush(d,false);d.Editor.WriteMessage("\nDETALLE_ADAPTABLE_ACTIVO\n");}
        [CommandMethod("DETALLEACTUALIZARTODOS6",CommandFlags.Session)]
        public void RefreshAll()
        {
            int n=0;var report=new List<string>();
            foreach(var d in States.Keys.ToArray()){States[d].Scan=true;int changed=Flush(d,true);n+=changed;report.Add(d.Name+" updated="+changed);}
            report.Add("DETALLES_ACTUALIZADOS: "+n);
            System.IO.File.WriteAllLines(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"DetalleAdaptable-updates.txt"),report);
            App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nDETALLES_ACTUALIZADOS: "+n+"\n");
        }
        [CommandMethod("DETALLEADAPTABLEESTADO6")]
        public void Status()
        {
            string status="DETALLE_ADAPTABLE_ACTIVO documents="+States.Count+" grip="+(Grip!=null)+" assembly="+typeof(AutoSideApplication).Assembly.Location;
            System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"DetalleAdaptable-status.txt"),status);App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\n"+status+"\n");
        }
        [CommandMethod("DETALLECOMPROBAR6",CommandFlags.Session)]
        public void CheckTextPlacement()
        {
            var report=new List<string>();int count=0,failed=0;
            foreach(Document doc in App.DocumentManager)
            using(doc.LockDocument())using(var tr=doc.Database.TransactionManager.StartTransaction())
            {
                var bt=(BlockTable)tr.GetObject(doc.Database.BlockTableId,OpenMode.ForRead);
                foreach(var b in bt.Cast<ObjectId>().Select(id=>(BlockTableRecord)tr.GetObject(id,OpenMode.ForRead)).Where(b=>b.IsLayout))
                foreach(var m in b.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<MLeader>().Where(m=>SideSwitch.IsDetail(m,tr)))
                {
                    var def=(BlockTableRecord)tr.GetObject(m.BlockContentId,OpenMode.ForRead);var gaps=new Dictionary<string,double>();var values=new Dictionary<string,string>();
                    foreach(var a in def.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<AttributeDefinition>().Where(a=>a.Tag=="DET"||a.Tag=="TEXTO_INFERIOR"))
                    using(var v=m.GetBlockAttribute(a.ObjectId))using(var mt=v.MTextAttribute)
                    {var ys=mt.GetBoundingPoints().Cast<Point3d>().Select(p=>p.TransformBy(SideSwitch.Transform(m).Inverse()).Y).ToArray();gaps[a.Tag]=a.Tag=="DET"?ys.Min():-ys.Max();values[a.Tag]=v.TextString;}
                    bool ok=gaps.Count==2&&gaps.Values.Min()>0&&Math.Abs(gaps["DET"]-gaps["TEXTO_INFERIOR"])<0.05;
                    count++;if(!ok)failed++;
                    report.Add(doc.Name+" | "+m.Handle+" | "+(ok?"CENTERED":"FAILED")+" | "+string.Join("; ",gaps.Select(p=>p.Key+" gap="+p.Value.ToString("F6",CultureInfo.InvariantCulture)))+" | "+string.Join(" | ",values.Values));
                }
                tr.Abort();
            }
            report.Add("DETAIL_TEXT_CHECK count="+count+" failed="+failed);
            System.IO.File.WriteAllLines(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"DetalleTexto-centrado.txt"),report);
            App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\n"+report.Last()+"\n");
        }
    }
}


