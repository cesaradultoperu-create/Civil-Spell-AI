using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CivilSpellAI.Autodesk;
using CadPoint = Autodesk.AutoCAD.Geometry.Point2d;

namespace CivilSpellAI.UI
{
    internal sealed class SelectedFigureWindow : Window
    {
        private readonly SelectedFigureSnapshot snapshot;
        private readonly TextBox widthInput;
        private readonly TextBox heightInput;
        private readonly TextBlock status;
        private readonly TextBlock selectedTitle;
        private readonly SelectedFigurePreview preview;
        private readonly CheckBox copyOption;
        private readonly Dictionary<FigureMeasure, TextBox> measureInputs = new Dictionary<FigureMeasure, TextBox>();
        private bool refreshing;
        private FigureMeasure invalidMeasure;
        public FigureMeasureEdit MeasureEdit { get; private set; }
        public bool Accepted { get; private set; }
        public bool SelectAgainRequested { get; private set; }
        public bool Create3DRequested { get; private set; }
        public double FigureWidth { get; private set; }
        public double FigureHeight { get; private set; }
        public bool CreateCopy { get { return copyOption.IsChecked == true; } }

        public SelectedFigureWindow(SelectedFigureSnapshot snapshot, bool createCopy)
        {
            this.snapshot = snapshot;
            if (snapshot.Measures == null) snapshot.Measures = FigureMeasureLayout.Detect(snapshot);
            MeasureEdit = new FigureMeasureEdit(snapshot.Measures);
            bool hasMeasures = snapshot.Measures.Measures.Count > 0;
            Title = "Editar figura seleccionada";
            Width = 1120; Height = 740; MinWidth = 950; MinHeight = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowInTaskbar = false;
            var root = new Grid { Margin = new Thickness(20), Background = Brushes.White };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Content = root;
            var heading = new StackPanel();
            heading.Children.Add(new TextBlock { Text = "Medidas de la figura seleccionada", FontSize = 20, FontWeight = FontWeights.SemiBold });
            heading.Children.Add(new TextBlock { Text = snapshot.Description + " · " + snapshot.Measures.Measures.Count + " medidas detectadas",
                Margin = new Thickness(0, 6, 0, 12), TextWrapping = TextWrapping.Wrap });
            root.Children.Add(heading);
            var body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(330) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(body, 1); root.Children.Add(body);
            var panel = new Grid { Margin = new Thickness(0, 0, 16, 0) };
            panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            body.Children.Add(panel);
            var instructions = new TextBlock { Text = hasMeasures
                ? "Elija una letra para localizar la distancia. Cambie su valor y pulse Tab o Enter. Las medidas relacionadas se actualizan juntas."
                : "No se encontraron bordes rectos paralelos. Puede ajustar el tamaño total del conjunto.",
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) };
            panel.Children.Add(instructions);
            var fields = new StackPanel();
            var scroll = new ScrollViewer { Content = fields, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Grid.SetRow(scroll, 1); panel.Children.Add(scroll);
            foreach (FigureMeasure measure in snapshot.Measures.Measures)
            {
                if (measure == snapshot.Measures.Measures[0])
                    fields.Children.Add(new TextBlock { Text = "Medidas detectadas (unidades del dibujo)",
                        FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 5, 0, 10) });
                if (measure.Name == null && snapshot.Measures.Measures.TakeWhile(m => m != measure).LastOrDefault() != null &&
                    snapshot.Measures.Measures.TakeWhile(m => m != measure).Last().Name != null)
                    fields.Children.Add(new TextBlock { Text = "Otras distancias", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 10) });
                var row = new Grid { Margin = new Thickness(0, 0, 0, 7) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(43) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
                var locate = new Button { Content = measure.Letter, Foreground = Brushes.ForestGreen, FontWeight = FontWeights.Bold,
                    ToolTip = "Localizar " + measure.Letter + ": " + measure.Description, Margin = new Thickness(0, 0, 7, 0) };
                locate.Click += (s, e) => { Highlight(measure); measureInputs[measure].Focus(); measureInputs[measure].SelectAll(); };
                row.Children.Add(locate);
                var input = new TextBox { Text = Format(measure.Original), Padding = new Thickness(5, 4, 5, 4), ToolTip = measure.Description };
                measureInputs.Add(measure, input);
                input.GotKeyboardFocus += (s, e) => Highlight(measure);
                input.LostKeyboardFocus += (s, e) => CommitMeasure(measure);
                input.KeyDown += (s, e) => { if (e.Key == Key.Enter) { CommitMeasure(measure); e.Handled = true; } };
                Grid.SetColumn(input, 1); row.Children.Add(input);
                var axis = new TextBlock { Text = measure.Horizontal ? "↔" : "↕", VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center, ToolTip = measure.Description };
                Grid.SetColumn(axis, 2); row.Children.Add(axis); fields.Children.Add(row);
            }
            fields.Children.Add(new TextBlock { Text = hasMeasures ? "Tamaño resultante (unidades del dibujo):" : "Tamaño del conjunto (unidades del dibujo):",
                FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5), TextWrapping = TextWrapping.Wrap });
            fields.Children.Add(new TextBlock { Text = "Ancho:" });
            widthInput = new TextBox { Text = Format(snapshot.Width), IsReadOnly = hasMeasures, IsEnabled = snapshot.Width > 1e-10, Margin = new Thickness(0, 4, 0, 8) };
            fields.Children.Add(widthInput);
            fields.Children.Add(new TextBlock { Text = "Alto:" });
            heightInput = new TextBox { Text = Format(snapshot.Height), IsReadOnly = hasMeasures, IsEnabled = snapshot.Height > 1e-10, Margin = new Thickness(0, 4, 0, 8) };
            fields.Children.Add(heightInput);
            var reset = new Button { Content = "Restablecer medidas", Margin = new Thickness(0, 8, 0, 8), Padding = new Thickness(6) };
            reset.Click += (s, e) => { MeasureEdit = new FigureMeasureEdit(snapshot.Measures); invalidMeasure = null; RefreshFields(); status.Text = ""; };
            fields.Children.Add(reset);
            var footer = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
            Grid.SetRow(footer, 2); panel.Children.Add(footer);
            selectedTitle = new TextBlock { Foreground = Brushes.ForestGreen, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10) };
            footer.Children.Add(selectedTitle);
            copyOption = new CheckBox { Content = "Crear una copia editable", IsChecked = createCopy, Margin = new Thickness(0, 0, 0, 8) };
            footer.Children.Add(copyOption);
            footer.Children.Add(new TextBlock { Text = "Se ajustan los bordes de la distancia elegida; los tramos fuera de ella conservan su longitud. La vista previa muestra cómo se relacionan las medidas.",
                TextWrapping = TextWrapping.Wrap, FontSize = 11 });
            status = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.Firebrick, Margin = new Thickness(0, 8, 0, 0) };
            footer.Children.Add(status);
            preview = new SelectedFigurePreview { Snapshot = snapshot, MeasureEdit = MeasureEdit };
            preview.MeasureSelected += measure => { measureInputs[measure].BringIntoView(); measureInputs[measure].Focus(); measureInputs[measure].SelectAll(); Highlight(measure); };
            var previewPanel = new DockPanel();
            var viewTools = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6) };
            var zoomOut = new Button { Content = "−", Width = 30, ToolTip = "Alejar", Margin = new Thickness(0, 0, 5, 0) };
            zoomOut.Click += (s, e) => preview.ZoomAt(new Point(preview.ActualWidth / 2, preview.ActualHeight / 2), 1 / 1.25);
            viewTools.Children.Add(zoomOut);
            var zoomIn = new Button { Content = "+", Width = 30, ToolTip = "Acercar", Margin = new Thickness(0, 0, 5, 0) };
            zoomIn.Click += (s, e) => preview.ZoomAt(new Point(preview.ActualWidth / 2, preview.ActualHeight / 2), 1.25);
            viewTools.Children.Add(zoomIn);
            var fit = new Button { Content = "Encuadrar", Padding = new Thickness(8, 3, 8, 3) };
            fit.Click += (s, e) => preview.ResetView();
            viewTools.Children.Add(fit);
            viewTools.Children.Add(new TextBlock { Text = "Rueda: zoom · Botón central: desplazar", FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) });
            DockPanel.SetDock(viewTools, Dock.Top); previewPanel.Children.Add(viewTools);
            previewPanel.Children.Add(preview);
            var border = new Border { BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(1), Child = previewPanel };
            Grid.SetColumn(border, 1); body.Children.Add(border);
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
            Grid.SetRow(buttons, 2); root.Children.Add(buttons);
            var reselect = new Button { Content = "Seleccionar otra figura…", Padding = new Thickness(14, 6, 14, 6), Margin = new Thickness(0, 0, 12, 0) };
            reselect.Click += (s, e) => { SelectAgainRequested = true; Close(); }; buttons.Children.Add(reselect);
            var create3D = new Button { Content = "Crear 3D con FRONT…", Padding = new Thickness(14, 6, 14, 6), Margin = new Thickness(0, 0, 12, 0),
                IsEnabled = snapshot.Measures.FrameBounds != null,
                ToolTip = "Usar esta planta TOP y seleccionar la elevación FRONT para crear una figura 3D editable." };
            create3D.Click += (s, e) => {
                if (!CommitPendingMeasure() || !ReadSizes()) return;
                Create3DRequested = true; Close();
            };
            buttons.Children.Add(create3D);
            var accept = new Button { Content = "Aceptar", Width = 90, Margin = new Thickness(0, 0, 8, 0) };
            accept.Click += Accept; buttons.Children.Add(accept);
            buttons.Children.Add(new Button { Content = "Cancelar", IsCancel = true, Width = 90 });
            if (!hasMeasures)
            {
                widthInput.TextChanged += (s, e) => RefreshPreview();
                heightInput.TextChanged += (s, e) => RefreshPreview();
            }
            RefreshFields();
            if (hasMeasures) Highlight(snapshot.Measures.Measures[0]);
        }

        private void Highlight(FigureMeasure measure)
        {
            preview.SelectedMeasure = measure;
            selectedTitle.Text = measure.Letter + " · " + measure.Description;
            foreach (var pair in measureInputs) pair.Value.Background = pair.Key == measure ? Brushes.Honeydew : Brushes.White;
            preview.InvalidateVisual();
        }
        private bool CommitMeasure(FigureMeasure measure)
        {
            if (refreshing) return true;
            double value;
            if (!ReadSize(measureInputs[measure].Text, measure.Original, out value))
            { invalidMeasure = measure; status.Text = measure.Letter + ": ingrese una medida positiva válida."; return false; }
            try
            {
                if (Math.Abs(value - MeasureEdit.Value(measure)) > snapshot.Measures.Tolerance) MeasureEdit.Set(measure, value);
                invalidMeasure = null; status.Text = ""; RefreshFields(); return true;
            }
            catch (ArgumentException exception) { invalidMeasure = measure; status.Text = measure.Letter + ": " + exception.Message; return false; }
        }
        private void RefreshFields()
        {
            refreshing = true;
            foreach (var pair in measureInputs) pair.Value.Text = Format(MeasureEdit.Value(pair.Key));
            if (measureInputs.Count > 0)
            {
                var local = snapshot.Outlines.SelectMany(line => line).Select(p => MeasureEdit.Map(FigureMeasureLayout.ToLocal(snapshot, p))).ToArray();
                widthInput.Text = Format(MeasureEdit.Changed ? local.Max(p => p.X) - local.Min(p => p.X) : snapshot.Width);
                heightInput.Text = Format(MeasureEdit.Changed ? local.Max(p => p.Y) - local.Min(p => p.Y) : snapshot.Height);
            }
            refreshing = false; RefreshPreview();
        }
        private void Accept(object sender, RoutedEventArgs args)
        {
            if (!CommitPendingMeasure() || !ReadSizes()) return;
            Accepted = true; DialogResult = true;
        }
        private bool CommitPendingMeasure()
        {
            // Only the focused field can contain a pending edit; other fields are derived values.
            var pending = measureInputs.FirstOrDefault(pair => pair.Value.IsKeyboardFocusWithin);
            if (pending.Key != null && !CommitMeasure(pending.Key)) return false;
            if (invalidMeasure != null && !CommitMeasure(invalidMeasure)) return false;
            if (measureInputs.Any(pair => { double value; return !ReadSize(pair.Value.Text, pair.Key.Original, out value); }))
            { status.Text = "Corrija la medida inválida antes de aceptar."; return false; }
            return true;
        }
        private bool ReadSizes()
        {
            double width, height;
            if (!ReadSize(widthInput.Text, snapshot.Width, out width) || !ReadSize(heightInput.Text, snapshot.Height, out height))
            { status.Text = "Ingrese medidas positivas válidas."; return false; }
            FigureWidth = width; FigureHeight = height; return true;
        }
        private void RefreshPreview()
        {
            if (!ReadSizes()) return;
            preview.MeasureEdit = MeasureEdit;
            preview.WidthFactor = measureInputs.Count == 0 && snapshot.Width > 1e-10 ? FigureWidth / snapshot.Width : 1;
            preview.HeightFactor = measureInputs.Count == 0 && snapshot.Height > 1e-10 ? FigureHeight / snapshot.Height : 1;
            preview.InvalidateVisual();
        }
        private static bool ReadSize(string text, double original, out double value)
        {
            return double.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                !double.IsNaN(value) && !double.IsInfinity(value) && (original > 1e-10 ? value > 0 : Math.Abs(value) <= 1e-10);
        }
        private static string Format(double value) { return value.ToString("0.######", CultureInfo.InvariantCulture); }
    }

    internal sealed class FigurePreviewViewport
    {
        public double Zoom { get; private set; } = 1;
        public Vector Offset { get; private set; }

        public Point Transform(Point point, Size size)
        {
            var center = new Point(size.Width / 2, size.Height / 2);
            return center + (point - center) * Zoom + Offset;
        }
        public void ZoomAt(Point anchor, Size size, double factor)
        {
            double next = Math.Max(0.5, Math.Min(16, Zoom * factor));
            double ratio = next / Zoom;
            var center = new Point(size.Width / 2, size.Height / 2);
            Offset = (anchor - center) * (1 - ratio) + Offset * ratio;
            Zoom = next;
        }
        public void Pan(Vector delta) { Offset += delta; }
        public void Reset() { Zoom = 1; Offset = new Vector(); }
    }

    internal sealed class SelectedFigurePreview : FrameworkElement
    {
        public SelectedFigureSnapshot Snapshot;
        public FigureMeasureEdit MeasureEdit;
        public FigureMeasure SelectedMeasure;
        public double WidthFactor = 1;
        public double HeightFactor = 1;
        public event Action<FigureMeasure> MeasureSelected;
        private readonly List<Tuple<Rect, FigureMeasure>> hits = new List<Tuple<Rect, FigureMeasure>>();
        private readonly FigurePreviewViewport viewport = new FigurePreviewViewport();
        private Point lastPanPoint;

        public SelectedFigurePreview() { ClipToBounds = true; }
        internal void ZoomAt(Point anchor, double factor)
        {
            viewport.ZoomAt(anchor, RenderSize, factor);
            InvalidateVisual();
        }
        internal void ResetView() { viewport.Reset(); InvalidateVisual(); }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            ZoomAt(e.GetPosition(this), Math.Pow(1.25, e.Delta / 120.0));
            e.Handled = true;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.ChangedButton == MouseButton.Middle)
            {
                lastPanPoint = e.GetPosition(this);
                if (CaptureMouse()) Cursor = Cursors.SizeAll;
                e.Handled = true;
                return;
            }
            if (e.ChangedButton != MouseButton.Left) return;
            Point point = e.GetPosition(this);
            var hit = hits.LastOrDefault(item => item.Item1.Contains(point));
            if (hit != null && MeasureSelected != null) { MeasureSelected(hit.Item2); e.Handled = true; }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!IsMouseCaptured) return;
            if (e.MiddleButton != MouseButtonState.Pressed) { ReleaseMouseCapture(); return; }
            Point point = e.GetPosition(this);
            viewport.Pan(point - lastPanPoint);
            lastPanPoint = point;
            InvalidateVisual();
            e.Handled = true;
        }
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.ChangedButton == MouseButton.Middle && IsMouseCaptured)
            { ReleaseMouseCapture(); e.Handled = true; }
        }
        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);
            Cursor = null;
        }
        protected override void OnRender(DrawingContext context)
        {
            base.OnRender(context);
            context.DrawRectangle(Brushes.White, null, new Rect(RenderSize)); hits.Clear();
            double viewWidth = RenderSize.Width, viewHeight = RenderSize.Height;
            if (Snapshot == null || Snapshot.Outlines.Count == 0 || viewWidth <= 130 || viewHeight <= 130) return;
            Func<CadPoint, CadPoint> resize = point => {
                CadPoint local = FigureMeasureLayout.ToLocal(Snapshot, point);
                if (MeasureEdit != null) local = MeasureEdit.Map(local);
                return new CadPoint(local.X * WidthFactor, local.Y * HeightFactor);
            };
            var lines = Snapshot.Outlines.Select(line => line.Select(resize).ToArray()).ToArray();
            var points = lines.SelectMany(line => line).ToArray();
            double minX = points.Min(p => p.X), minY = points.Min(p => p.Y);
            double width = points.Max(p => p.X) - minX, height = points.Max(p => p.Y) - minY;
            double scale = Math.Min((viewWidth - 130) / Math.Max(width, 1e-8), (viewHeight - 130) / Math.Max(height, 1e-8));
            Func<CadPoint, Point> screen = p => viewport.Transform(new Point((viewWidth - width * scale) / 2 + (p.X - minX) * scale,
                (viewHeight + height * scale) / 2 - (p.Y - minY) * scale), RenderSize);
            var pen = new Pen(Brushes.DarkSlateGray, 1.1);
            foreach (var line in lines)
            {
                var geometry = new StreamGeometry();
                using (StreamGeometryContext path = geometry.Open())
                    for (int i = 0; i < line.Length; i++)
                        if (i == 0) path.BeginFigure(screen(line[i]), false, false); else path.LineTo(screen(line[i]), true, false);
                context.DrawGeometry(null, pen, geometry);
            }
            if (Snapshot.Measures == null) return;
            // Small letter markers locate every detected span; selecting one displays its full arrows.
            foreach (FigureMeasure measure in Snapshot.Measures.Measures.Where(m => m != SelectedMeasure))
                DrawMeasure(context, measure, screen, false);
            if (SelectedMeasure != null) DrawMeasure(context, SelectedMeasure, screen, true);
        }

        private void DrawMeasure(DrawingContext context, FigureMeasure measure, Func<CadPoint, Point> screen, bool selected)
        {
            double[] stations = measure.Horizontal ? Snapshot.Measures.X : Snapshot.Measures.Y;
            CadPoint a = measure.Horizontal ? new CadPoint(stations[measure.First], measure.Anchor) : new CadPoint(measure.Anchor, stations[measure.First]);
            CadPoint b = measure.Horizontal ? new CadPoint(stations[measure.Last], measure.Anchor) : new CadPoint(measure.Anchor, stations[measure.Last]);
            if (MeasureEdit != null) { a = MeasureEdit.Map(a); b = MeasureEdit.Map(b); }
            Point start = screen(a), end = screen(b);
            var midpoint = new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2);
            Brush color = selected ? Brushes.ForestGreen : Brushes.SeaGreen;
            var text = new FormattedText(measure.Letter + (selected ? " = " + (MeasureEdit == null ? measure.Original : MeasureEdit.Value(measure)).ToString("0.####", CultureInfo.InvariantCulture) : ""),
                CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), selected ? 14 : 10, color, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            // A deterministic lane offset makes coincident horizontal/vertical labels distinguishable.
            int lane = Snapshot.Measures.Measures.IndexOf(measure) % 3;
            Point label = measure.Horizontal ? new Point(midpoint.X - text.Width / 2, midpoint.Y - 17 - lane * 12)
                : new Point(midpoint.X + 7 + lane * 17, midpoint.Y - text.Height / 2);
            Rect labelBounds = new Rect(label.X - 2, label.Y - 1, text.Width + 4, text.Height + 2);
            context.DrawRectangle(selected ? Brushes.Honeydew : new SolidColorBrush(Color.FromArgb(205, 255, 255, 255)), null, labelBounds);
            if (selected)
            {
                var dimensionPen = new Pen(color, 2);
                context.DrawLine(dimensionPen, start, end);
                Arrow(context, dimensionPen, start, end); Arrow(context, dimensionPen, end, start);
                context.DrawEllipse(color, null, start, 3, 3); context.DrawEllipse(color, null, end, 3, 3);
            }
            context.DrawText(text, label); hits.Add(Tuple.Create(labelBounds, measure));
        }
        private static void Arrow(DrawingContext context, Pen pen, Point tip, Point other)
        {
            Vector direction = other - tip;
            if (direction.Length < 1) return;
            direction.Normalize(); Vector normal = new Vector(-direction.Y, direction.X);
            context.DrawLine(pen, tip, tip + direction * 8 + normal * 4);
            context.DrawLine(pen, tip, tip + direction * 8 - normal * 4);
        }
    }
}
