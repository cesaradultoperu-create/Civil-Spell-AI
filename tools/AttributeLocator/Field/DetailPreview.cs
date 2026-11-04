using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using LocalizarCasillas;
using Font=System.Drawing.Font;

namespace CasillasField
{
    public sealed class CellPreview : Control
    {
        Scene scene;Slot slot;Color accent;Slot[] highlights=new Slot[0];
        readonly FlowLayoutPanel toolbar=new FlowLayoutPanel {Dock=DockStyle.Bottom,Height=28,WrapContents=false,BackColor=Color.White};
        double zoom=1;PointF pan;Point drag;bool dragging;readonly bool enlarged;
        public static bool ExpandedOpen {get;private set;}
        Rectangle Viewport=>new Rectangle(1,26,Math.Max(1,Width-2),Math.Max(1,Height-toolbar.Height-27));
        public CellPreview():this(false){}
        CellPreview(bool large)
        {
            enlarged=large;DoubleBuffered=true;BackColor=Color.White;Cursor=Cursors.Hand;
            Controls.Add(toolbar);AddButton("+",()=>Zoom(1.35));AddButton("−",()=>Zoom(1/1.35));AddButton("Ajustar",ResetView);
            if(!enlarged)AddButton("Ampliar",Expand);
        }
        void AddButton(string text,Action action)
        {
            var button=new Button {Text=text,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Height=23,Margin=new Padding(1),Padding=new Padding(2,0,2,0),TabStop=false};
            button.Click+=(s,e)=>action();toolbar.Controls.Add(button);
        }
        public void Update(Scene value,Slot selected,Color color,System.Collections.Generic.IEnumerable<Slot> matches=null)
        {
            bool changed=slot?.AttributeId!=selected?.AttributeId || slot?.Tag!=selected?.Tag;
            scene=value;slot=selected;accent=color;
            highlights=selected==null?new Slot[0]:(matches??new[]{selected}).Where(s=>s.BlockId==selected.BlockId).ToArray();
            if(changed){zoom=1;pan=PointF.Empty;}
            toolbar.Enabled=slot!=null;Invalidate();
        }
        internal static Extents3d FocusBounds(Slot slot)
        {
            var cell=Catalog.MarkerBounds(slot);double h=Math.Max(slot.Height,0.1);
            double width=Math.Max(h*24,Math.Min(cell.MaxPoint.X-cell.MinPoint.X+h*8,h*36));
            double height=Math.Max(h*16,Math.Min(cell.MaxPoint.Y-cell.MinPoint.Y+h*8,h*24));
            double cx=(cell.MinPoint.X+cell.MaxPoint.X)/2,cy=(cell.MinPoint.Y+cell.MaxPoint.Y)/2;
            if(cell.MaxPoint.X-cell.MinPoint.X>width)cx=(slot.Bounds.MinPoint.X+slot.Bounds.MaxPoint.X)/2;
            return new Extents3d(new Point3d(cx-width/2,cy-height/2,0),new Point3d(cx+width/2,cy+height/2,0));
        }
        void ResetView(){zoom=1;pan=PointF.Empty;Invalidate();}
        void Zoom(double factor)=>ZoomAt(factor,new Point(Viewport.Left+Viewport.Width/2,Viewport.Top+Viewport.Height/2));
        void ZoomAt(double factor,Point anchor)
        {
            double previous=zoom;zoom=Math.Max(0.15,Math.Min(zoom*factor,12));double ratio=zoom/previous;
            var view=Viewport;float x=anchor.X-view.Left-view.Width/2f,y=anchor.Y-view.Top-view.Height/2f;
            pan=new PointF((float)(x-(x-pan.X)*ratio),(float)(y-(y-pan.Y)*ratio));Invalidate();
        }
        protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);ZoomAt(e.Delta>0?1.2:1/1.2,e.Location);}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){drag=e.Location;dragging=true;Capture=true;Cursor=Cursors.SizeAll;}}
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging){pan=new PointF(pan.X+e.X-drag.X,pan.Y+e.Y-drag.Y);drag=e.Location;Invalidate();}}
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;Cursor=Cursors.Hand;}
        protected override void OnDoubleClick(EventArgs e){base.OnDoubleClick(e);if(!enlarged)Expand();}
        void Expand()
        {
            if(scene==null || slot==null || ExpandedOpen)return;
            using(var window=new Form {Text="Vista ampliada · "+slot.Tag,Size=new Size(1050,650),MinimumSize=new Size(620,400),StartPosition=FormStartPosition.CenterParent,ShowInTaskbar=false})
            {
                var preview=new CellPreview(true) {Dock=DockStyle.Fill};preview.Update(scene,slot,accent,highlights);window.Controls.Add(preview);
                var footer=new Label {Dock=DockStyle.Bottom,Height=38,Padding=new Padding(8),Text=CellStatus.Row(slot)+" / "+CellStatus.Column(slot)+" · Valor: "+slot.Value+" · Rueda: zoom. Arrastrar: desplazar."};window.Controls.Add(footer);
                var native=NativeDialogs.Read();ExpandedOpen=true;
                try{window.ShowDialog(native.Field==IntPtr.Zero?(IWin32Window)FindForm():new Owner(native.Field));}
                finally{ExpandedOpen=false;}
            }
        }
        sealed class Owner : IWin32Window {public IntPtr Handle{get;}public Owner(IntPtr h){Handle=h;}}
        static GraphicsPath Glyphs(string value,Scene.TextItem item)
        {
            var path=new GraphicsPath();FontFamily family;
            try{family=new FontFamily(item.Family);}catch(ArgumentException){family=new FontFamily("Arial");}
            using(family)path.AddString(value.Replace("%%d","°"),family,(int)item.Style,100,PointF.Empty,StringFormat.GenericTypographic);
            return path;
        }
        static void DrawText(Graphics g,Scene.TextItem item,Func<Point3d,PointF> point,double scale)
        {
            string value=item.Slot?.Value??item.Text;if(String.IsNullOrEmpty(value)||item.Height<=0||item.Width<=0)return;
            using(var path=Glyphs(value,item))
            using(var original=Glyphs(String.IsNullOrEmpty(item.Text)?"M":item.Text,item))
            {
                var ink=path.GetBounds();var basis=original.GetBounds();if(ink.Width<=0||ink.Height<=0||basis.Width<=0||basis.Height<=0)return;
                double sx=item.Width/basis.Width,sy=item.Height/basis.Height;
                double dw=(ink.Width-basis.Width)*sx,dh=(ink.Height-basis.Height)*sy;
                double cos=Math.Cos(item.Rotation),sin=Math.Sin(item.Rotation);
                var origin=item.TopLeft+new Vector3d(-dw*item.Horizontal*cos-dh*item.Vertical*sin,-dw*item.Horizontal*sin+dh*item.Vertical*cos,0);
                var anchor=point(origin);
                using(var normalize=new Matrix(1,0,0,1,-ink.Left,-ink.Top))path.Transform(normalize);
                using(var frame=new Matrix((float)(sx*scale*cos),(float)(-sx*scale*sin),(float)(sy*scale*sin),(float)(sy*scale*cos),anchor.X,anchor.Y))path.Transform(frame);
                if(path.GetBounds().IntersectsWith(g.ClipBounds))g.FillPath(Brushes.Black,path);
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);var g=e.Graphics;g.Clear(Color.White);g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
            var view=Viewport;
            if(scene==null || slot==null){TextRenderer.DrawText(g,"Sin coincidencia identificada",Font,view,Color.DimGray,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);return;}
            var focus=FocusBounds(slot);double width=focus.MaxPoint.X-focus.MinPoint.X,height=focus.MaxPoint.Y-focus.MinPoint.Y;
            double cx=(focus.MinPoint.X+focus.MaxPoint.X)/2,cy=(focus.MinPoint.Y+focus.MaxPoint.Y)/2;
            double scale=Math.Min(view.Width/width,view.Height/height)*zoom;
            Func<Point3d,PointF> point=p=>new PointF((float)(view.Left+view.Width/2+(p.X-cx)*scale+pan.X),(float)(view.Top+view.Height/2-(p.Y-cy)*scale+pan.Y));
            var state=g.Save();g.SetClip(view);
            using(var pen=new Pen(Color.FromArgb(80,80,80),1))foreach(var line in scene.Edges)g.DrawLine(pen,point(line.A),point(line.B));
            foreach(var text in scene.Texts)
            {
                var a=point(new Point3d(text.Bounds.MinPoint.X,text.Bounds.MaxPoint.Y,0));var b=point(new Point3d(text.Bounds.MaxPoint.X,text.Bounds.MinPoint.Y,0));
                if((text.Slot==null || text.Slot.Value==text.Text) && !RectangleF.FromLTRB(a.X,a.Y,b.X,b.Y).IntersectsWith(view))continue;
                DrawText(g,text,point,scale);
            }
            foreach(var highlight in highlights)
            {
                var cell=Catalog.MarkerBounds(highlight);var a=point(new Point3d(cell.MinPoint.X,cell.MaxPoint.Y,0));var b=point(new Point3d(cell.MaxPoint.X,cell.MinPoint.Y,0));
                using(var fill=new SolidBrush(Color.FromArgb(24,accent)))g.FillRectangle(fill,a.X,a.Y,b.X-a.X,b.Y-a.Y);
                using(var pen=new Pen(accent,highlight.AttributeId==slot.AttributeId?2.5f:1.5f))g.DrawRectangle(pen,a.X,a.Y,b.X-a.X,b.Y-a.Y);
            }
            g.Restore(state);
            using(var font=new Font("Segoe UI",9,FontStyle.Bold))TextRenderer.DrawText(g,slot.Tag+" · "+CellStatus.Column(slot),font,new Rectangle(5,3,Width-10,22),accent,TextFormatFlags.EndEllipsis|TextFormatFlags.VerticalCenter);
            using(var pen=new Pen(Color.LightGray))g.DrawRectangle(pen,0,0,Width-1,Height-1);
        }
    }
}
