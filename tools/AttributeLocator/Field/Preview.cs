using System;
using System.Collections.Generic;
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
    public sealed class Scene
    {
        public struct Edge {public Point3d A,B;public Edge(Point3d a,Point3d b){A=a;B=b;}}
        public sealed class TextItem
        {
            public string Text,Family="Arial";public Slot Slot;public Extents3d Bounds;
            public Point3d TopLeft;public double Rotation,Width,Height,Horizontal,Vertical=1;
            public FontStyle Style;
            public TextItem WithSlot(Slot slot){var copy=(TextItem)MemberwiseClone();copy.Slot=slot;return copy;}
        }
        public readonly List<Edge> Edges=new List<Edge>();
        public readonly List<TextItem> Texts=new List<TextItem>();
        public static Scene Read(Database db,ObjectId root,IEnumerable<Slot> attributes)
        {
            var scene=new Scene();
            using(var tr=db.TransactionManager.StartTransaction())
            {
                Collect((BlockReference)tr.GetObject(root,OpenMode.ForRead),Matrix3d.Identity,0,new HashSet<ObjectId>(),scene,tr);
                foreach(var slot in attributes.Where(s=>s.BlockId==root))
                    AddText((DBText)tr.GetObject(slot.AttributeId,OpenMode.ForRead),slot.ParentTransform,scene,tr,slot);
                tr.Commit();
            }
            return scene;
        }
        internal static void AddText(DBText original,Matrix3d transform,Scene scene,Transaction tr,Slot slot=null)
        {
            using(var world=(DBText)original.GetTransformedCopy(transform))
            using(var flat=(DBText)world.GetTransformedCopy(Matrix3d.Rotation(-world.Rotation,Vector3d.ZAxis,Point3d.Origin)))
            {
                Extents3d box;
                try{box=flat.GeometricExtents;}
                catch(Autodesk.AutoCAD.Runtime.Exception)
                {box=new Extents3d(flat.Position,flat.Position+new Vector3d(Math.Max(flat.Height,0.1),Math.Max(flat.Height,0.1),0));}
                if(String.IsNullOrWhiteSpace(slot?.Value??original.TextString))
                {
                    double h=Math.Max(flat.Height,0.1),w=h*0.65;
                    double horizontal=flat.HorizontalMode==TextHorizontalMode.TextCenter || flat.HorizontalMode==TextHorizontalMode.TextMid?0.5:flat.HorizontalMode==TextHorizontalMode.TextRight?1:0;
                    double vertical=flat.VerticalMode==TextVerticalMode.TextVerticalMid?0.5:flat.VerticalMode==TextVerticalMode.TextTop?0:1;
                    var anchor=flat.IsDefaultAlignment?flat.Position:flat.AlignmentPoint;
                    box=new Extents3d(new Point3d(anchor.X-w*horizontal,anchor.Y+h*vertical-h,0),new Point3d(anchor.X-w*horizontal+w,anchor.Y+h*vertical,0));
                }
                var worldBounds=box;worldBounds.TransformBy(Matrix3d.Rotation(world.Rotation,Vector3d.ZAxis,Point3d.Origin));
                var item=new TextItem {Text=original.TextString,Slot=slot,Bounds=worldBounds,
                    Rotation=world.Rotation,Width=box.MaxPoint.X-box.MinPoint.X,Height=box.MaxPoint.Y-box.MinPoint.Y,
                    TopLeft=new Point3d(box.MinPoint.X,box.MaxPoint.Y,0).RotateBy(world.Rotation,Vector3d.ZAxis,Point3d.Origin)};
                item.Horizontal=world.HorizontalMode==TextHorizontalMode.TextCenter || world.HorizontalMode==TextHorizontalMode.TextMid?0.5:world.HorizontalMode==TextHorizontalMode.TextRight?1:0;
                item.Vertical=world.VerticalMode==TextVerticalMode.TextVerticalMid?0.5:world.VerticalMode==TextVerticalMode.TextTop?0:1;
                if(!original.TextStyleId.IsNull)
                {
                    var font=((TextStyleTableRecord)tr.GetObject(original.TextStyleId,OpenMode.ForRead)).Font;
                    if(!String.IsNullOrWhiteSpace(font.TypeFace))item.Family=font.TypeFace;
                    item.Style=(font.Bold?FontStyle.Bold:FontStyle.Regular)|(font.Italic?FontStyle.Italic:FontStyle.Regular);
                }
                scene.Texts.Add(item);
            }
        }
        static void AddMText(MText text,Matrix3d transform,Scene scene,Transaction tr)
        {
            var parts=new DBObjectCollection();
            try
            {
                // Native explosion supplies the real wrapped lines and character formatting.
                text.Explode(parts);
                foreach(DBObject part in parts){var line=part as DBText;if(line!=null)AddText(line,transform,scene,tr);}
            }
            finally{foreach(DBObject part in parts)part.Dispose();}
        }
        static void Collect(BlockReference block,Matrix3d parent,int depth,HashSet<ObjectId> visited,Scene scene,Transaction tr)
        {
            if(depth>12 || visited.Contains(block.BlockTableRecord))return;
            var definition=(BlockTableRecord)tr.GetObject(block.BlockTableRecord,OpenMode.ForRead);
            if(definition.IsFromExternalReference || definition.IsFromOverlayReference)return;
            var transform=parent*block.BlockTransform;var next=new HashSet<ObjectId>(visited){block.BlockTableRecord};
            foreach(ObjectId id in definition)
            {
                var entity=tr.GetObject(id,OpenMode.ForRead) as Entity;if(entity==null)continue;
                var line=entity as Line;
                if(line!=null)scene.Edges.Add(new Edge(line.StartPoint.TransformBy(transform),line.EndPoint.TransformBy(transform)));
                var poly=entity as Polyline;
                if(poly!=null)for(int i=0;i<poly.NumberOfVertices-(poly.Closed?0:1);i++)
                    if(poly.GetBulgeAt(i)==0)scene.Edges.Add(new Edge(poly.GetPoint3dAt(i).TransformBy(transform),poly.GetPoint3dAt((i+1)%poly.NumberOfVertices).TransformBy(transform)));
                var text=entity as DBText;var mtext=entity as MText;
                if((text!=null && !(text is AttributeDefinition)) || mtext!=null)
                {
                    try
                    {
                        if(text!=null)AddText(text,transform,scene,tr);else AddMText(mtext,transform,scene,tr);
                    }
                    catch(Autodesk.AutoCAD.Runtime.Exception) { }
                }
                var child=entity as BlockReference;if(child!=null)Collect(child,transform,depth+1,next,scene,tr);
            }
        }
    }

    public sealed class GuideForm : Form
    {
        readonly Label destination=new Label(),selection=new Label(),summary=new Label();
        readonly TextBox links=new TextBox();
        readonly CellPreview targetPreview=new CellPreview(),usagePreview=new CellPreview();
        readonly ComboBox usagePicker=new ComboBox {Name="UsagePicker",DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill};
        Slot[] useSlots=new Slot[0];Dictionary<ObjectId,Scene> sceneMap;
        bool changingUses;
        FieldIdentity previousProperty;
        readonly CellMap cellMap=new CellMap();
        bool positioned;
        public event Action<Slot> EditRequested;
        protected override bool ShowWithoutActivation=>true;
        protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x08000000;return p;}}
        public GuideForm()
        {
            Text="Field · celdas y vínculos";Font=new Font("Segoe UI",9);Size=new Size(860,890);
            MinimumSize=new Size(740,760);FormBorderStyle=FormBorderStyle.SizableToolWindow;ShowInTaskbar=false;
            var root=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=2,RowCount=6,Padding=new Padding(10),BackColor=Color.FromArgb(245,247,250)};
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute,58));root.RowStyles.Add(new RowStyle(SizeType.Absolute,240));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute,50));root.RowStyles.Add(new RowStyle(SizeType.Absolute,45));
            root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,34));
            Controls.Add(root);
            destination.Dock=DockStyle.Fill;destination.ForeColor=Color.Firebrick;destination.Font=new Font(Font,FontStyle.Bold);
            selection.Dock=DockStyle.Fill;selection.ForeColor=Color.RoyalBlue;selection.Font=new Font(Font,FontStyle.Bold);
            root.Controls.Add(destination,0,0);root.Controls.Add(selection,1,0);
            targetPreview.Name=targetPreview.Text="TargetPreview";usagePreview.Name=usagePreview.Text="UsagePreview";
            targetPreview.Dock=DockStyle.Fill;usagePreview.Dock=DockStyle.Fill;root.Controls.Add(targetPreview,0,1);
            var usageHost=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=Padding.Empty};
            usageHost.RowStyles.Add(new RowStyle(SizeType.Absolute,28));usageHost.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            usageHost.Controls.Add(usagePicker,0,0);usageHost.Controls.Add(usagePreview,0,1);root.Controls.Add(usageHost,1,1);
            usagePicker.SelectedIndexChanged+=(s,e)=>{if(!changingUses)RenderUsage(true);};
            summary.Dock=DockStyle.Fill;root.Controls.Add(summary,0,2);root.SetColumnSpan(summary,2);
            links.Dock=DockStyle.Fill;links.Multiline=true;links.ReadOnly=true;links.ScrollBars=ScrollBars.Vertical;root.Controls.Add(links,0,3);root.SetColumnSpan(links,2);
            cellMap.Dock=DockStyle.Fill;root.Controls.Add(cellMap,0,4);root.SetColumnSpan(cellMap,2);
            cellMap.EditRequested+=slot=>EditRequested?.Invoke(slot);
            root.Controls.Add(new Label {Text="Editar casilla cambia su valor. El vínculo se elige con los botones del Field nativo.",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,5);root.SetColumnSpan(root.GetControlFromPosition(0,5),2);
        }
        public void SetCatalog(IEnumerable<Slot> slots,Dictionary<ObjectId,Scene> scenes)=>cellMap.SetCatalog(slots,scenes);
        public void FocusSlot(Slot slot)=>cellMap.FocusSlot(slot);
        void RenderUsage(bool focusMap)
        {
            var slot=usagePicker.SelectedIndex<0?null:useSlots[usagePicker.SelectedIndex];Scene scene=null;
            if(slot!=null && sceneMap!=null)sceneMap.TryGetValue(slot.BlockId,out scene);
            usagePreview.Update(scene,slot,Color.RoyalBlue,useSlots);
            if(focusMap && slot!=null)cellMap.FocusSlot(slot);
        }
        public void UpdateInfo(Slot target,string selectedValue,FieldIdentity selected,Slot[] uses,Dictionary<Autodesk.AutoCAD.DatabaseServices.ObjectId,Scene> scenes)
        {
            bool propertyChanged=selected!=null && !selected.SameProperty(previousProperty);
            previousProperty=selected;
            cellMap.FollowProperty(selected);
            destination.Text=target==null?"CELDA EN EDICIÓN\nNo identificada de forma única":"EDITANDO: "+target.Tag+" ["+target.Layout+"]\nValor guardado: "+target.Value+"\nVínculo: "+(target.Property.Length==0?"Sin campo":target.Property);
            selection.Text=selected==null?"PROPERTY\nSelecciona una propiedad Sheet Set en Field":"PROPERTY: "+selected.Property+"\nValor: "+selectedValue+"\n"+selected.Scope+" · "+uses.Length+" usos en este DWG";
            Scene targetScene=null;
            if(target!=null)scenes.TryGetValue(target.BlockId,out targetScene);
            targetPreview.Update(targetScene,target,Color.Firebrick);
            var previous=usagePicker.SelectedIndex<0?null:useSlots[usagePicker.SelectedIndex];
            changingUses=true;useSlots=uses;sceneMap=scenes;usagePicker.Items.Clear();
            for(int i=0;i<uses.Length;i++)usagePicker.Items.Add((i+1)+" / "+uses.Length+" · "+CellStatus.Row(uses[i])+" · "+uses[i].Tag+" ["+uses[i].Layout+"]");
            usagePicker.Enabled=uses.Length>0;
            if(uses.Length>0)
            {
                int index=Array.FindIndex(uses,s=>previous!=null && s.AttributeId==previous.AttributeId);
                if(index<0 && target!=null)index=Array.FindIndex(uses,s=>s.BlockId==target.BlockId);
                usagePicker.SelectedIndex=Math.Max(0,index);
            }
            changingUses=false;RenderUsage(propertyChanged && cellMap.FollowsProperty);
            if(selected==null)summary.Text="Al cambiar Property se mostrarán sus usos existentes.\nRojo: celda en edición. Azul: una celda que ya usa la propiedad elegida.";
            else
            {
                string relation=target==null?"Destino no identificado; no se compara su vínculo.":selected.SameProperty(FieldIdentity.Parse(target.Code))?"Es el vínculo actual de la celda.":"La propiedad elegida difiere del vínculo guardado de la celda.";
                summary.Text=relation+"\n"+(uses.Length==0?"Sin coincidencias en este DWG; no se han revisado los demás planos.":"Ya se usa en este DWG: "+uses.Length+" · "+String.Join(", ",uses.Select(s=>s.Tag+" ["+s.Layout+"]")));
            }
            var original=target==null?null:FieldIdentity.Parse(target.Code);
            links.Text=(original==null?"":"DST ACTUAL: "+original.DatabasePath+Environment.NewLine)+
                (selected==null?"":"DST ELEGIDO: "+selected.DatabasePath);
        }
        public void Attach(IntPtr owner,IntPtr main)
        {
            bool first=!Visible;
            if(first)
            {
                NativeDialogs.Rect app;NativeDialogs.GetWindowRect(main,out app);
                var area=Screen.FromHandle(main).WorkingArea;
                if(!positioned || !area.Contains(Location))
                {
                    Height=Math.Min(Height,area.Height-45);
                    NativeDialogs.Rect field;NativeDialogs.GetWindowRect(owner,out field);
                    bool left=field.Left-area.Left>=area.Right-field.Right;
                    Location=new Point(left?Math.Max(area.Left+10,app.Left+10):Math.Min(app.Right-Width-10,area.Right-Width),
                        Math.Max(area.Top+10,Math.Min(app.Top+80,area.Bottom-Height-10)));
                    positioned=true;
                }
                Show();
            }
            // The native Field window is destroyed on close. Own the helper by the application
            // so the helper survives and can reappear for the next Field dialog.
            NativeDialogs.SetOwner(Handle,-8,main);
            if(!CellPreview.ExpandedOpen)NativeDialogs.SetWindowPos(Handle,IntPtr.Zero,0,0,0,0,0x13);
        }
    }
}
