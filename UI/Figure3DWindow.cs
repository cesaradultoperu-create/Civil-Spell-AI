using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using CivilSpellAI.Autodesk;
using CivilSpellAI.Geometry;

namespace CivilSpellAI.UI
{
    internal sealed class Figure3DWindow : Window
    {
        private readonly Figure3DParameters draft;
        private readonly List<Action<Figure3DParameters>> readers = new List<Action<Figure3DParameters>>();
        private readonly Figure3DPreview preview = new Figure3DPreview();
        private readonly TextBlock status = new TextBlock { Foreground = Brushes.Firebrick, TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock summary = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 8) };
        private readonly Button accept = new Button { Content = "Crear 3D", Width = 100, Padding = new Thickness(8), Margin = new Thickness(0, 0, 8, 0) };
        public Figure3DParameters Parameters { get; private set; }

        public Figure3DWindow(Figure3DParameters parameters, SelectedFigureSnapshot top = null,
            SelectedFigureSnapshot front = null, bool editing = false)
        {
            draft = parameters.Copy();
            Title = editing ? "Editar figura 3D" : "Crear figura 3D desde TOP y FRONT";
            Width = 1100; Height = 760; MinWidth = 900; MinHeight = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner; ShowInTaskbar = false;
            var root = new DockPanel { Margin = new Thickness(18), Background = Brushes.White }; Content = root;
            var heading = new StackPanel();
            heading.Children.Add(new TextBlock { Text = Title, FontSize = 21, FontWeight = FontWeights.SemiBold });
            heading.Children.Add(new TextBlock { Text = "Caja rectangular abierta, base y pasos laterales. Medidas en unidades del dibujo.",
                Margin = new Thickness(0, 8, 0, 12), TextWrapping = TextWrapping.Wrap });
            DockPanel.SetDock(heading, Dock.Top); root.Children.Add(heading);
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0) };
            if (editing) accept.Content = "Aplicar";
            accept.Click += (s, e) => { if (ReadDraft()) { Parameters = draft.Copy(); DialogResult = true; } };
            buttons.Children.Add(accept); buttons.Children.Add(new Button { Content = "Cancelar", IsCancel = true, Width = 90 });
            DockPanel.SetDock(buttons, Dock.Bottom); root.Children.Add(buttons);
            var body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(330) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); root.Children.Add(body);
            var fields = new StackPanel { Margin = new Thickness(0, 0, 15, 0) };
            var scroll = new ScrollViewer { Content = fields, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; body.Children.Add(scroll);
            Group(fields, "Planta · TOP");
            Field(fields, "Ancho exterior", draft.Width, (p, v) => p.Width = v);
            Field(fields, "Profundidad exterior", draft.Depth, (p, v) => p.Depth = v);
            Field(fields, "Muro izquierdo", draft.LeftWall, (p, v) => p.LeftWall = v);
            Field(fields, "Muro derecho", draft.RightWall, (p, v) => p.RightWall = v);
            Field(fields, "Muro frontal", draft.FrontWall, (p, v) => p.FrontWall = v);
            Field(fields, "Muro posterior", draft.BackWall, (p, v) => p.BackWall = v);
            Group(fields, "Elevación · FRONT");
            Field(fields, "Altura exterior", draft.Height, (p, v) => p.Height = v);
            Field(fields, "Espesor de base", draft.BaseThickness, (p, v) => p.BaseThickness = v);
            Passage(fields, "Paso izquierdo", draft.Left, p => p.Left);
            Passage(fields, "Paso derecho", draft.Right, p => p.Right);
            fields.Children.Add(summary); fields.Children.Add(status);
            var tabs = new TabControl(); Grid.SetColumn(tabs, 1); body.Children.Add(tabs);
            var modelPanel = new DockPanel();
            var tools = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6) };
            var fit = new Button { Content = "Encuadrar", Padding = new Thickness(8, 4, 8, 4) };
            fit.Click += (s, e) => preview.ResetView(); tools.Children.Add(fit);
            tools.Children.Add(new TextBlock { Text = "Arrastrar: girar · Rueda: zoom", Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
            DockPanel.SetDock(tools, Dock.Top); modelPanel.Children.Add(tools); modelPanel.Children.Add(preview);
            tabs.Items.Add(new TabItem { Header = "Modelo 3D", Content = modelPanel });
            if (top != null) tabs.Items.Add(new TabItem { Header = "Referencia TOP", Content = new SelectedFigurePreview { Snapshot = top } });
            if (front != null) tabs.Items.Add(new TabItem { Header = "Referencia FRONT", Content = new SelectedFigurePreview { Snapshot = front } });
            ReadDraft();
        }
        private static void Group(StackPanel panel, string title)
        {
            panel.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 6) });
        }
        private void Field(StackPanel panel, string label, double initial, Action<Figure3DParameters, double> write, CheckBox enabled = null)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
            row.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap });
            var input = new TextBox { Text = initial.ToString("0.######", CultureInfo.InvariantCulture), Padding = new Thickness(5),
                IsEnabled = enabled == null || enabled.IsChecked == true };
            Grid.SetColumn(input, 1); row.Children.Add(input); panel.Children.Add(row);
            readers.Add(p => {
                if (enabled != null && enabled.IsChecked != true) return;
                double value;
                if (!double.TryParse(input.Text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                    double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentException(label + ": ingrese una medida válida.");
                write(p, value);
            });
            input.TextChanged += (s, e) => ReadDraft();
            if (enabled != null) {
                enabled.Checked += (s, e) => { input.IsEnabled = true; ReadDraft(); };
                enabled.Unchecked += (s, e) => { input.IsEnabled = false; ReadDraft(); };
            }
        }
        private void Passage(StackPanel panel, string title, Figure3DPassage initial, Func<Figure3DParameters, Figure3DPassage> passage)
        {
            var enabled = new CheckBox { Content = title + " rectangular", IsChecked = initial.Enabled, Margin = new Thickness(0, 12, 0, 8) };
            readers.Add(p => passage(p).Enabled = enabled.IsChecked == true); panel.Children.Add(enabled);
            panel.Children.Add(new TextBlock { Text = "Ancho, alto, centro y cota inferior son editables.",
                TextWrapping = TextWrapping.Wrap, FontSize = 11, Margin = new Thickness(0, 0, 0, 8) });
            if (!initial.Enabled && initial.Height > 0 && initial.Width == 0)
                panel.Children.Add(new TextBlock { Text = "FRONT define la altura del paso. Active el paso y complete su ancho en TOP; el centro inicial está a mitad de la profundidad.",
                    TextWrapping = TextWrapping.Wrap, FontSize = 11, Margin = new Thickness(0, 0, 0, 8) });
            Field(panel, "Ancho en TOP", initial.Width, (p, v) => passage(p).Width = v, enabled);
            Field(panel, "Alto en FRONT", initial.Height, (p, v) => passage(p).Height = v, enabled);
            Field(panel, "Centro desde cara frontal", initial.Center, (p, v) => passage(p).Center = v, enabled);
            Field(panel, "Cota inferior desde base", initial.Bottom, (p, v) => passage(p).Bottom = v, enabled);
        }
        private bool ReadDraft()
        {
            try
            {
                var next = draft.Copy(); foreach (var read in readers) read(next); next.Validate();
                draft.Width = next.Width; draft.Depth = next.Depth; draft.Height = next.Height;
                draft.LeftWall = next.LeftWall; draft.RightWall = next.RightWall;
                draft.FrontWall = next.FrontWall; draft.BackWall = next.BackWall; draft.BaseThickness = next.BaseThickness;
                draft.Left = next.Left; draft.Right = next.Right;
                preview.SetParameters(next);
                summary.Text = string.Format(CultureInfo.InvariantCulture, "Interior: {0:0.###} × {1:0.###}\nAltura libre: {2:0.###}", next.InnerWidth, next.InnerDepth, next.ClearHeight);
                status.Text = ""; accept.IsEnabled = true; return true;
            }
            catch (ArgumentException exception) { status.Text = exception.Message; accept.IsEnabled = false; return false; }
        }
    }

    internal sealed class Figure3DPreview : Grid
    {
        private readonly Viewport3D viewport = new Viewport3D();
        private Figure3DParameters parameters;
        private Rect3D nativeBounds = Rect3D.Empty;
        private double yaw = -0.8, pitch = 0.55, zoom = 1;
        private Point lastPoint;
        public Figure3DPreview() { Background = Brushes.WhiteSmoke; ClipToBounds = true; Children.Add(viewport); }
        public void SetParameters(Figure3DParameters p)
        {
            nativeBounds = Rect3D.Empty;
            parameters = p;
            var group = new Model3DGroup();
            group.Children.Add(new AmbientLight(Color.FromRgb(110, 110, 110)));
            group.Children.Add(new DirectionalLight(Colors.White, new Vector3D(-1, 1, -2)));
            var material = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(150, 171, 182)));
            var surface = Figure3DPreviewMesh.Build(p);
            group.Children.Add(new GeometryModel3D(surface, material) { BackMaterial = material });
            var outline = new EmissiveMaterial(new SolidColorBrush(Color.FromRgb(55, 75, 85)));
            group.Children.Add(new GeometryModel3D(Figure3DPreviewMesh.BuildEdges(surface,
                Math.Max(p.Width, Math.Max(p.Depth, p.Height)) * 0.0015), outline) { BackMaterial = outline });
            viewport.Children.Clear(); viewport.Children.Add(new ModelVisual3D { Content = group });
            UpdateCamera();
        }
        public void SetNativeModel(Model3DGroup model, Rect3D bounds)
        {
            parameters = null; nativeBounds = bounds;
            viewport.Children.Clear(); viewport.Children.Add(new ModelVisual3D { Content = model });
            UpdateCamera();
        }
        public void ResetView() { yaw = -0.8; pitch = 0.55; zoom = 1; UpdateCamera(); }
        private void UpdateCamera()
        {
            if (parameters == null && nativeBounds.IsEmpty) return;
            var center = parameters != null ? new Point3D(parameters.Width / 2, parameters.Depth / 2, parameters.Height / 2) :
                new Point3D(nativeBounds.X + nativeBounds.SizeX / 2, nativeBounds.Y + nativeBounds.SizeY / 2, nativeBounds.Z + nativeBounds.SizeZ / 2);
            double size = parameters != null ? Math.Max(parameters.Width, Math.Max(parameters.Depth, parameters.Height)) :
                Math.Max(nativeBounds.SizeX, Math.Max(nativeBounds.SizeY, nativeBounds.SizeZ));
            var direction = new Vector3D(Math.Cos(yaw) * Math.Cos(pitch), Math.Sin(yaw) * Math.Cos(pitch), Math.Sin(pitch)) * size * 4;
            viewport.Camera = new OrthographicCamera(center + direction, -direction, new Vector3D(0, 0, 1), size * 2.2 / zoom);
        }
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.ChangedButton != MouseButton.Left) return;
            lastPoint = e.GetPosition(this); CaptureMouse(); Cursor = Cursors.SizeAll; e.Handled = true;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e); if (!IsMouseCaptured) return;
            if (e.LeftButton != MouseButtonState.Pressed) { ReleaseMouseCapture(); return; }
            Point point = e.GetPosition(this); yaw -= (point.X - lastPoint.X) * 0.01;
            pitch = Math.Max(0.1, Math.Min(1.45, pitch + (point.Y - lastPoint.Y) * 0.01)); lastPoint = point;
            UpdateCamera(); e.Handled = true;
        }
        protected override void OnMouseUp(MouseButtonEventArgs e) { base.OnMouseUp(e); if (IsMouseCaptured) ReleaseMouseCapture(); }
        protected override void OnLostMouseCapture(MouseEventArgs e) { base.OnLostMouseCapture(e); Cursor = null; }
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e); zoom = Math.Max(0.5, Math.Min(8, zoom * Math.Pow(1.2, e.Delta / 120.0))); UpdateCamera(); e.Handled = true;
        }
    }
}
