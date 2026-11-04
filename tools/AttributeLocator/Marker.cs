using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using AcApp=Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace LocalizarCasillas
{
    public sealed class Marker : IDisposable
    {
        readonly List<Entity> graphics=new List<Entity>();
        Document document;
        public int Count => graphics.Count;

        public void Show(Document doc,Slot slot,bool zoom)
        {
            Clear();
            if (AcApp.DocumentManager.MdiActiveDocument != doc) return;
            using(doc.LockDocument())
            {
                if (!slot.AttributeId.IsValid || slot.AttributeId.IsErased)
                    throw new InvalidOperationException("El atributo ya no existe. Actualiza la lista.");
                if(LayoutManager.Current.CurrentLayout != slot.Layout) LayoutManager.Current.CurrentLayout=slot.Layout;
                if(slot.Layout!="Model" && Convert.ToInt32(AcApp.GetSystemVariable("CVPORT"))!=1)
                    doc.Editor.SwitchToPaperSpace();
                var bounds=Catalog.MarkerBounds(slot);
                if(zoom) Zoom(doc.Editor,bounds,Math.Max(slot.Height*26,Math.Min(slot.BlockBounds.MaxPoint.X-slot.BlockBounds.MinPoint.X,slot.Height*65)));
                document=doc;
                foreach(var entity in CreateGraphics(slot))
                {
                    try
                    {
                        if(!TransientManager.CurrentTransientManager.AddTransient(entity,TransientDrawingMode.DirectTopmost,128,new IntegerCollection()))
                            throw new InvalidOperationException("Civil 3D no pudo dibujar la marca temporal.");
                        graphics.Add(entity);
                    }
                    catch { entity.Dispose(); Clear(); throw; }
                }
                doc.Editor.UpdateScreen();
            }
        }

        public static List<Entity> CreateGraphics(Slot slot)
        {
            var b=Catalog.MarkerBounds(slot); double z=slot.Position.Z,h=slot.Height;
            var box=new Autodesk.AutoCAD.DatabaseServices.Polyline(4);
            box.AddVertexAt(0,new Point2d(b.MinPoint.X,b.MinPoint.Y),0,0,0);
            box.AddVertexAt(1,new Point2d(b.MaxPoint.X,b.MinPoint.Y),0,0,0);
            box.AddVertexAt(2,new Point2d(b.MaxPoint.X,b.MaxPoint.Y),0,0,0);
            box.AddVertexAt(3,new Point2d(b.MinPoint.X,b.MaxPoint.Y),0,0,0);
            box.Closed=true; box.Elevation=z; box.ColorIndex=1; box.ConstantWidth=h*0.09;
            var start=new Point3d(b.MaxPoint.X,b.MaxPoint.Y,z);
            var end=start+new Vector3d(h*5,h*6,0);
            var line=new Line(start,end) {ColorIndex=1,LineWeight=LineWeight.LineWeight050};
            var arrow1=new Line(start,start+new Vector3d(h*0.15,h*1.5,0)){ColorIndex=1};
            var arrow2=new Line(start,start+new Vector3d(h*1.5,h*0.4,0)){ColorIndex=1};
            var label=new MText {Location=end+new Vector3d(h*0.4,h,0),TextHeight=h*1.15,
                Attachment=AttachmentPoint.BottomLeft,Contents="AQUI: "+Escape(slot.Tag),ColorIndex=1};
            label.BackgroundFill=true; label.UseBackgroundColor=true; label.BackgroundScaleFactor=1.15;
            return new List<Entity>{box,line,arrow1,arrow2,label};
        }
        static string Escape(string s) => s.Replace("\\","\\\\").Replace("{","\\{").Replace("}","\\}");

        public static void Zoom(Editor editor,Extents3d bounds,double minHeight)
        {
            using(var view=editor.GetCurrentView())
            {
                // Title block layouts are viewed from above. Set both direction and target explicitly.
                view.ViewDirection=Vector3d.ZAxis; view.Target=Point3d.Origin; view.ViewTwist=0;
                double aspect=view.Width/Math.Max(view.Height,0.001);
                view.Height=Math.Max(minHeight,Math.Max((bounds.MaxPoint.Y-bounds.MinPoint.Y)*1.7,
                    (bounds.MaxPoint.X-bounds.MinPoint.X)*1.7/Math.Max(aspect,0.1)));
                view.Width=view.Height*aspect;
                view.CenterPoint=new Point2d((bounds.MinPoint.X+bounds.MaxPoint.X)/2,(bounds.MinPoint.Y+bounds.MaxPoint.Y)/2);
                editor.SetCurrentView(view);
            }
        }
        public void Clear()
        {
            foreach(var entity in graphics)
            {
                try {TransientManager.CurrentTransientManager.EraseTransient(entity,new IntegerCollection());}
                catch(Autodesk.AutoCAD.Runtime.Exception) { }
                finally {entity.Dispose();}
            }
            graphics.Clear();
            if(document!=null && AcApp.DocumentManager.MdiActiveDocument==document)
                try {document.Editor.UpdateScreen();}catch(Autodesk.AutoCAD.Runtime.Exception) { }
            document=null;
        }
        public void Dispose() => Clear();
    }
}
