using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Autodesk.AutoCAD.DatabaseServices;
using CivilSpellAI.Autodesk;
using CivilSpellAI.Geometry;
using Material=System.Windows.Media.Media3D.Material;

namespace CivilSpellAI.UI
{
    internal sealed class ReferenceAssembly3DWindow : Window
    {
        private readonly ReferenceAssembly3D draft;
        private readonly Figure3DPreview preview=new Figure3DPreview();
        private readonly TextBlock error=new TextBlock {Foreground=Brushes.Firebrick,TextWrapping=TextWrapping.Wrap};
        private readonly Button accept=new Button{Content="Crear 3D",Width=110,Margin=new Thickness(8)};
        private readonly List<Action> readers=new List<Action>();
        private readonly List<GeometryModel3D> componentMeshes=new List<GeometryModel3D>();
        private readonly List<Material> componentMaterials=new List<Material>();
        private readonly List<Button> componentButtons=new List<Button>();
        private readonly TextBlock selection=new TextBlock {Text="Seleccione un campo o un componente para resaltarlo en verde.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(8)};
        private readonly Material selectedMaterial=CreateSelectedMaterial();
        private int selectedComponent=-1;
        public ReferenceAssembly3D Model {get;private set;}
        public ReferenceAssembly3DWindow(ReferenceAssembly3D model,SelectedFigureSnapshot top=null,SelectedFigureSnapshot front=null,bool editing=false)
        {
            draft=model.Copy();Title="Reconstruir sólido desde TOP y FRONT";Width=1140;Height=780;MinWidth=940;MinHeight=600;
            WindowStartupLocation=WindowStartupLocation.CenterOwner;ShowInTaskbar=false;
            var root=new DockPanel{Margin=new Thickness(16),Background=Brushes.White};Content=root;
            var title=new TextBlock{Text=Title,FontSize=22,Margin=new Thickness(0,0,0,12)};
            DockPanel.SetDock(title,Dock.Top);root.Children.Add(title);
            var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};
            accept.Content=editing?"Aplicar":"Crear 3D";
            accept.Click+=(s,e)=>{if(Refresh()){Model=draft.Copy();DialogResult=true;}};
            actions.Children.Add(accept);actions.Children.Add(new Button{Content="Cancelar",IsCancel=true,Width=95,Margin=new Thickness(8)});
            DockPanel.SetDock(actions,Dock.Bottom);root.Children.Add(actions);
            var grid=new Grid();grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(365)});
            grid.ColumnDefinitions.Add(new ColumnDefinition());root.Children.Add(grid);
            var fields=new StackPanel{Margin=new Thickness(0,0,15,0)};
            grid.Children.Add(new ScrollViewer{Content=fields,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
            fields.Children.Add(new TextBlock{Text="Componentes relacionados con ambas referencias",FontWeight=FontWeights.Bold,TextWrapping=TextWrapping.Wrap});
            for(int index=0;index<draft.Parts.Count;index++)
            {
                var part=draft.Parts[index];
                Component(fields,part.Name,index);
                // Paired sections constrain the shape and cannot be replaced by a height slider.
                if(part.Front.Count==0)
                {
                    Field(fields,"Cota inferior",part.Bottom,v=>part.Bottom=v,index);
                    Field(fields,"Altura",part.Height,v=>part.Height=v,index);
                }
            }
            if(draft.Pipe!=null)
            {
                var p=draft.Pipe;
                int index=draft.Parts.Count;
                Component(fields,"Tubería",index);
                Field(fields,"Diámetro exterior",p.Diameter,v=>p.Diameter=v,index);
                Field(fields,"Espesor (0: superficie)",p.Wall,v=>p.Wall=v,index);
                Field(fields,"X inicial",p.X1,v=>p.X1=v,index);Field(fields,"X final",p.X2,v=>p.X2=v,index);
                Field(fields,"Y inicial",p.Y1,v=>p.Y1=v,index);Field(fields,"Y final",p.Y2,v=>p.Y2=v,index);
                var startElevation=Field(fields,"Cota inicial",p.Z1,v=>p.Z1=v,index);
                var endElevation=Field(fields,"Cota final",p.Z2,v=>p.Z2=v,index);
                Inclination(fields,p,startElevation,endElevation,index);
            }
            for(int i=0;i<draft.PipeRoutes.Count;i++)
            {
                var route=draft.PipeRoutes[i];int index=draft.Parts.Count+(draft.Pipe==null?0:1)+i;
                Component(fields,route.Name,index);
                Field(fields,"Diámetro exterior",route.Diameter,v=>route.Diameter=v,index);
                if(!route.Cap) Field(fields,route.CorrugationProfile==null?"Espesor":"Espesor en el valle",route.Wall,v=>route.Wall=v,index);
                if(route.CorrugationProfile!=null)Field(fields,"Paso del corrugado",route.CorrugationPitch,v=>route.CorrugationPitch=v,index);
                if(route.X.Length==3)Field(fields,"Radio del codo",route.BendRadius,v=>route.BendRadius=v,index);
                for(int j=0;j<route.Z.Length;j++)
                {
                    int point=j;
                    Field(fields,"Cota del eje "+(j+1),route.Z[j],v=>route.Z[point]=v,index);
                }
            }
            foreach(string note in draft.Notes) fields.Children.Add(new TextBlock{Text=note,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,0)});
            fields.Children.Add(error);
            var tabs=new TabControl();Grid.SetColumn(tabs,1);grid.Children.Add(tabs);
            var panel=new DockPanel();var fit=new Button{Content="Encuadrar · Arrastrar para girar · Rueda para zoom",Padding=new Thickness(8)};
            fit.Click+=(s,e)=>preview.ResetView();DockPanel.SetDock(fit,Dock.Top);panel.Children.Add(fit);
            var selectionBar=new DockPanel{Background=new SolidColorBrush(Color.FromRgb(237,248,237))};
            var clear=new Button{Content="Quitar resaltado",Margin=new Thickness(4),Padding=new Thickness(6)};
            AutomationProperties.SetName(clear,"Quitar resaltado");clear.Click+=(s,e)=>SelectComponent(-1);
            DockPanel.SetDock(clear,Dock.Right);selectionBar.Children.Add(clear);selectionBar.Children.Add(selection);
            DockPanel.SetDock(selectionBar,Dock.Top);panel.Children.Add(selectionBar);panel.Children.Add(preview);
            tabs.Items.Add(new TabItem{Header="Sólido 3D",Content=panel});
            if(top!=null) tabs.Items.Add(new TabItem{Header="TOP seleccionado",Content=new SelectedFigurePreview{Snapshot=top}});
            if(front!=null) tabs.Items.Add(new TabItem{Header="FRONT seleccionado",Content=new SelectedFigurePreview{Snapshot=front}});
            Refresh();
        }
        private string ComponentName(int index)
        {
            if(index<draft.Parts.Count)return draft.Parts[index].Name;
            index-=draft.Parts.Count;
            if(draft.Pipe!=null){if(index==0)return "Tubería";index--;}
            return index<draft.PipeRoutes.Count?draft.PipeRoutes[index].Name:"Tubería";
        }
        private void Component(StackPanel panel,string name,int index)
        {
            var button=new Button{Content=new TextBlock{Text=name,FontWeight=FontWeights.SemiBold,TextWrapping=TextWrapping.Wrap},
                HorizontalContentAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,12,0,4),Padding=new Thickness(4),Background=Brushes.Transparent};
            AutomationProperties.SetName(button,name);
            button.Click+=(s,e)=>SelectComponent(index);
            button.GotKeyboardFocus+=(s,e)=>SelectComponent(index);
            componentButtons.Add(button);panel.Children.Add(button);
        }
        private static Material CreateSelectedMaterial()
        {
            var material=new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(65,220,85))));
            material.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromRgb(15,65,20))));
            return material;
        }
        private void SelectComponent(int index,string field=null)
        {
            selectedComponent=index;
            selection.Text=index<0?"Seleccione un campo o un componente para resaltarlo en verde.":
                "Resaltado: "+ComponentName(index)+(field==null?"":" · "+field);
            ApplyHighlight();
        }
        private void ApplyHighlight()
        {
            for(int i=0;i<componentMeshes.Count;i++)
            {
                var material=i==selectedComponent?selectedMaterial:componentMaterials[i];
                componentMeshes[i].Material=material;componentMeshes[i].BackMaterial=material;
            }
            for(int i=0;i<componentButtons.Count;i++)
                componentButtons[i].Background=i==selectedComponent?new SolidColorBrush(Color.FromRgb(214,246,217)):Brushes.Transparent;
        }
        private TextBox Field(StackPanel panel,string label,double value,Action<double> write,int component)
        {
            var row=new DockPanel{Margin=new Thickness(0,4,0,0)};
            var input=new TextBox{Text=value.ToString("0.######",CultureInfo.InvariantCulture),Width=100,Padding=new Thickness(4)};
            string originalText=input.Text;
            AutomationProperties.SetName(input,ComponentName(component)+" · "+label);
            input.GotKeyboardFocus+=(s,e)=>SelectComponent(component,label);
            DockPanel.SetDock(input,Dock.Right);row.Children.Add(input);row.Children.Add(new TextBlock{Text=label,VerticalAlignment=VerticalAlignment.Center});panel.Children.Add(row);
            readers.Add(()=>{if(input.Text==originalText){write(value);return;}double next;if(!double.TryParse(input.Text.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out next))
                throw new ArgumentException("Revise " + label + ".");write(next);});
            input.LostFocus+=(s,e)=>Refresh();
            return input;
        }
        private void Inclination(StackPanel panel,ReferencePipe3D pipe,TextBox startElevation,TextBox endElevation,int component)
        {
            var anchor=new ComboBox{Margin=new Thickness(0,6,0,4),SelectedIndex=0};
            anchor.Items.Add("Mantener cota inicial");anchor.Items.Add("Mantener cota final");
            AutomationProperties.SetName(anchor,"Tubería · Extremo fijo para inclinar");
            panel.Children.Add(anchor);
            var row=new DockPanel{Margin=new Thickness(0,4,0,0)};
            var angle=new TextBox{Text=pipe.InclinationDegrees.ToString("0.######",CultureInfo.InvariantCulture),Width=100,Padding=new Thickness(4)};
            string synchronizedText=angle.Text;
            AutomationProperties.SetName(angle,"Tubería · Inclinación (°)");
            angle.GotKeyboardFocus+=(s,e)=>SelectComponent(component,"Inclinación (°)");
            DockPanel.SetDock(angle,Dock.Right);row.Children.Add(angle);
            row.Children.Add(new TextBlock{Text="Inclinación (°)",VerticalAlignment=VerticalAlignment.Center});panel.Children.Add(row);
            panel.Children.Add(new TextBlock{Text="Desde el extremo inicial al final: + sube, − baja. 0° es horizontal. Se conserva la longitud en planta; cambia la longitud 3D.",
                TextWrapping=TextWrapping.Wrap,FontSize=11,Margin=new Thickness(0,4,0,8)});
            // Endpoint readers run first. An untouched angle follows coordinate edits without
            // rounding the original slope; an edited angle updates the opposite elevation.
            readers.Add(()=>{
                if(angle.Text!=synchronizedText)
                {
                    double degrees;
                    if(!double.TryParse(angle.Text.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out degrees))
                        throw new ArgumentException("Revise Inclinación (°).");
                    pipe.SetInclination(degrees,anchor.SelectedIndex==1);
                    if(anchor.SelectedIndex==1) startElevation.Text=pipe.Z1.ToString("R",CultureInfo.InvariantCulture);
                    else endElevation.Text=pipe.Z2.ToString("R",CultureInfo.InvariantCulture);
                }
                synchronizedText=pipe.InclinationDegrees.ToString("0.######",CultureInfo.InvariantCulture);
                angle.Text=synchronizedText;
            });
            angle.LostFocus+=(s,e)=>Refresh();
        }
        private bool Refresh()
        {
            List<Entity> entities=null;
            try
            {
                foreach(var read in readers) read();
                entities=new ReferenceAssemblyDrawingService().Build(draft);
                var group=new Model3DGroup();
                group.Children.Add(new AmbientLight(Color.FromRgb(110,110,110)));
                group.Children.Add(new DirectionalLight(Colors.White,new Vector3D(-1,1,-2)));
                Rect3D bounds=Rect3D.Empty;
                var meshes=new List<GeometryModel3D>();var materials=new List<Material>();
                foreach(var entity in entities)
                {
                    var extents=entity.GeometricExtents;
                    double size=Math.Max(extents.MaxPoint.X-extents.MinPoint.X,Math.Max(extents.MaxPoint.Y-extents.MinPoint.Y,extents.MaxPoint.Z-extents.MinPoint.Z));
                    var data=SubDMesh.GetObjectMesh(entity,new MeshFaceterData {FaceterMeshType=2,FaceterDevSurface=size*.0005,FaceterDevNormal=10});
                    var mesh=new MeshGeometry3D();
                    var faces=data.FaceArray;
                    for(int i=0;i<faces.Count;)
                    {
                        int count=faces[i++];if(count<3 || i+count>faces.Count) throw new InvalidOperationException("Malla de vista previa inválida.");
                        int first=mesh.Positions.Count;
                        var a=data.VertexArray[faces[i]];var b=data.VertexArray[faces[i+1]];var c=data.VertexArray[faces[i+2]];
                        var normal=Vector3D.CrossProduct(new Vector3D(b.X-a.X,b.Y-a.Y,b.Z-a.Z),new Vector3D(c.X-a.X,c.Y-a.Y,c.Z-a.Z));
                        if(normal.LengthSquared<1e-20){i+=count;continue;}normal.Normalize();
                        for(int j=0;j<count;j++){var p=data.VertexArray[faces[i+j]];mesh.Positions.Add(new Point3D(p.X,p.Y,p.Z));mesh.Normals.Add(normal);}
                        for(int j=1;j<count-1;j++){mesh.TriangleIndices.Add(first);mesh.TriangleIndices.Add(first+j);mesh.TriangleIndices.Add(first+j+1);}i+=count;
                    }
                    Color color=entity.ColorIndex==2?Color.FromRgb(199,169,86):entity.ColorIndex==30?Color.FromRgb(179,158,127):entity.ColorIndex==4?Color.FromRgb(86,151,183):entity.ColorIndex==9?Color.FromRgb(188,186,167):Color.FromRgb(150,171,182);
                    var material=new DiffuseMaterial(new SolidColorBrush(color));
                    var geometry=new GeometryModel3D(mesh,material){BackMaterial=material};
                    group.Children.Add(geometry);meshes.Add(geometry);materials.Add(material);bounds.Union(mesh.Bounds);
                }
                componentMeshes.Clear();componentMeshes.AddRange(meshes);componentMaterials.Clear();componentMaterials.AddRange(materials);
                ApplyHighlight();preview.SetNativeModel(group,bounds);error.Text="";accept.IsEnabled=true;return true;
            }
            catch(Exception ex){error.Text="No se puede crear el modelo: "+ex.Message;accept.IsEnabled=false;return false;}
            finally {if(entities!=null) foreach(var entity in entities) entity.Dispose();}
        }
    }
}
