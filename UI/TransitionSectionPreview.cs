using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using CivilSpellAI.Geometry;

namespace CivilSpellAI.UI
{
    public sealed class TransitionSectionPreview : FrameworkElement
    {
        private static readonly Pen OutlinePen =
            CreatePen(Color.FromRgb(45, 55, 65), 1.25);
        private static readonly Pen DimensionPen =
            CreatePen(Color.FromRgb(38, 92, 145), 1.0);
        private static readonly Brush CoreBrush =
            CreateBrush(Color.FromRgb(219, 226, 232));
        private static readonly Brush InnerBrush =
            CreateBrush(Color.FromRgb(157, 184, 207));
        private static readonly Brush OuterBrush =
            CreateBrush(Color.FromRgb(93, 139, 177));
        private static readonly Brush TextBrush =
            CreateBrush(Color.FromRgb(25, 35, 45));
        private static readonly Brush MarkerBrush =
            CreateBrush(Color.FromRgb(215, 25, 32));

        private TransitionSectionParameters parameters;

        public TransitionSectionParameters Parameters
        {
            get { return parameters; }
            set
            {
                parameters = value;
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.DrawRectangle(Brushes.White, null, new Rect(RenderSize));

            if (parameters == null || ActualWidth < 100.0 || ActualHeight < 100.0)
            {
                DrawCenteredText(drawingContext, "Ingrese medidas válidas para ver la figura.");
                return;
            }

            TransitionSectionGeometry geometry =
                new TransitionSectionGeometry(parameters);
            double totalLength = parameters.InitialStraightLength +
                parameters.TransitionLength +
                parameters.FinalStraightLength +
                (parameters.SectionType == TransitionSectionType.TypeB
                    ? parameters.SecondTransitionLength
                    : 0.0);
            double totalHeight = Math.Max(
                parameters.InitialTotalHeight,
                parameters.SectionType == TransitionSectionType.TypeB
                    ? parameters.TerminalTotalHeight
                    : parameters.FinalTotalHeight);
            double left = 42.0;
            double right = 54.0;
            double top = 42.0;
            double bottom = 70.0;
            double scale = Math.Min(
                (ActualWidth - left - right) / totalLength,
                (ActualHeight - top - bottom) / totalHeight);
            if (scale <= 0.0 || double.IsNaN(scale) || double.IsInfinity(scale))
                return;

            double centerY = top + ((ActualHeight - top - bottom) / 2.0);
            Func<SectionPoint, Point> convert = point => new Point(
                left + (point.X * scale),
                centerY - (point.Y * scale));

            for (int index = 0; index < geometry.Polygons.Count; index++)
            {
                Brush fill = index == 0
                    ? CoreBrush
                    : (index <= 2 ? InnerBrush : OuterBrush);
                DrawPolygon(drawingContext, geometry.Polygons[index], convert, fill);
            }

            double x0 = left;
            double x1 = left + (parameters.InitialStraightLength * scale);
            double x2 = x1 + (parameters.TransitionLength * scale);
            double x3 = x2 + (parameters.FinalStraightLength * scale);
            double x4 = x3 + (parameters.SecondTransitionLength * scale);
            double dimensionY = ActualHeight - 31.0;
            DrawHorizontalDimension(
                drawingContext,
                x0,
                x1,
                dimensionY,
                Format(parameters.InitialStraightLength));
            DrawMarker(drawingContext, "L1", new Point((x0 + x1) / 2.0, dimensionY - 24.0));
            DrawHorizontalDimension(
                drawingContext,
                x1,
                x2,
                dimensionY,
                Format(parameters.TransitionLength));
            DrawMarker(drawingContext, "L2", new Point((x1 + x2) / 2.0, dimensionY - 24.0));
            DrawHorizontalDimension(
                drawingContext,
                x2,
                x3,
                dimensionY,
                Format(parameters.FinalStraightLength));
            DrawMarker(drawingContext, "L3", new Point((x2 + x3) / 2.0, dimensionY - 24.0));
            if (parameters.SectionType == TransitionSectionType.TypeB)
            {
                DrawHorizontalDimension(
                    drawingContext,
                    x3,
                    x4,
                    dimensionY,
                    Format(parameters.SecondTransitionLength));
                DrawMarker(drawingContext, "L4", new Point((x3 + x4) / 2.0, dimensionY - 24.0));
            }

            DrawVerticalDimension(
                drawingContext,
                left - 18.0,
                centerY - ((parameters.InitialClearHeight / 2.0) * scale),
                centerY + ((parameters.InitialClearHeight / 2.0) * scale),
                "Libre " + Format(parameters.InitialClearHeight),
                false);
            DrawMarker(
                drawingContext,
                "H1",
                new Point(left - 27.0, centerY - 22.0));
            if (parameters.SectionType == TransitionSectionType.TypeB)
            {
                DrawVerticalDimension(
                    drawingContext,
                    x3 + 14.0,
                    centerY - ((parameters.FinalClearHeight / 2.0) * scale),
                    centerY + ((parameters.FinalClearHeight / 2.0) * scale),
                    "Libre " + Format(parameters.FinalClearHeight),
                    true);
                DrawMarker(drawingContext, "H2", new Point(x3 + 23.0, centerY - 22.0));
                DrawVerticalDimension(
                    drawingContext,
                    x4 + 18.0,
                    centerY - ((parameters.TerminalTotalHeight / 2.0) * scale),
                    centerY + ((parameters.TerminalTotalHeight / 2.0) * scale),
                    "Total " + Format(parameters.TerminalTotalHeight),
                    true);
                DrawMarker(drawingContext, "H3", new Point(x4 + 27.0, centerY - 22.0));
            }
            else
            {
                DrawVerticalDimension(
                    drawingContext,
                    x3 + 18.0,
                    centerY - ((parameters.FinalClearHeight / 2.0) * scale),
                    centerY + ((parameters.FinalClearHeight / 2.0) * scale),
                    "Libre " + Format(parameters.FinalClearHeight),
                    true);
                DrawMarker(drawingContext, "H2", new Point(x3 + 27.0, centerY - 22.0));
            }

            double transitionRatio = 0.55;
            double markerModelX = parameters.InitialStraightLength +
                (parameters.TransitionLength * transitionRatio);
            double clearOffset = (parameters.InitialClearHeight / 2.0) +
                (((parameters.FinalClearHeight - parameters.InitialClearHeight) / 2.0) * transitionRatio);
            Point innerMarker = convert(new SectionPoint(
                markerModelX,
                clearOffset + (parameters.InnerThickness / 2.0)));
            Point outerMarker = convert(new SectionPoint(
                markerModelX,
                clearOffset + parameters.InnerThickness + (parameters.OuterThickness / 2.0)));
            DrawMarker(drawingContext, "E1", new Point(innerMarker.X, innerMarker.Y - 4.0));
            DrawMarker(drawingContext, "E2", new Point(outerMarker.X, outerMarker.Y - 4.0));

            string thicknessText = string.Format(
                CultureInfo.InvariantCulture,
                "Espesores: interior {0} · exterior {1}",
                Format(parameters.InnerThickness),
                Format(parameters.OuterThickness));
            DrawText(
                drawingContext,
                thicknessText,
                new Point(left, 12.0),
                TextAlignment.Left);
            DrawText(
                drawingContext,
                "Hatch: " + parameters.HatchPatternName +
                    " · escala " + Format(parameters.HatchScale) +
                    " · ángulo " + Format(parameters.HatchAngleDegrees) + "°" +
                    " · color ACI " + parameters.HatchColorIndex.ToString(CultureInfo.InvariantCulture),
                new Point(left, 27.0),
                TextAlignment.Left);
        }

        private static void DrawPolygon(
            DrawingContext drawingContext,
            IReadOnlyList<SectionPoint> polygon,
            Func<SectionPoint, Point> convert,
            Brush fill)
        {
            var geometry = new StreamGeometry();
            using (StreamGeometryContext context = geometry.Open())
            {
                context.BeginFigure(convert(polygon[0]), true, true);
                var points = new List<Point>();
                for (int index = 1; index < polygon.Count; index++)
                    points.Add(convert(polygon[index]));
                context.PolyLineTo(points, true, false);
            }

            geometry.Freeze();
            drawingContext.DrawGeometry(fill, OutlinePen, geometry);
        }

        private static void DrawHorizontalDimension(
            DrawingContext drawingContext,
            double startX,
            double endX,
            double y,
            string text)
        {
            drawingContext.DrawLine(DimensionPen, new Point(startX, y), new Point(endX, y));
            drawingContext.DrawLine(DimensionPen, new Point(startX, y - 5.0), new Point(startX, y + 5.0));
            drawingContext.DrawLine(DimensionPen, new Point(endX, y - 5.0), new Point(endX, y + 5.0));
            DrawText(
                drawingContext,
                text,
                new Point((startX + endX) / 2.0, y + 7.0),
                TextAlignment.Center);
        }

        private static void DrawVerticalDimension(
            DrawingContext drawingContext,
            double x,
            double top,
            double bottom,
            string text,
            bool textOnRight)
        {
            drawingContext.DrawLine(DimensionPen, new Point(x, top), new Point(x, bottom));
            drawingContext.DrawLine(DimensionPen, new Point(x - 5.0, top), new Point(x + 5.0, top));
            drawingContext.DrawLine(DimensionPen, new Point(x - 5.0, bottom), new Point(x + 5.0, bottom));
            DrawText(
                drawingContext,
                text,
                new Point(textOnRight ? x + 8.0 : x - 8.0, ((top + bottom) / 2.0) - 7.0),
                textOnRight ? TextAlignment.Left : TextAlignment.Right);
        }

        private static void DrawText(
            DrawingContext drawingContext,
            string text,
            Point origin,
            TextAlignment alignment)
        {
            var formatted = new FormattedText(
                text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                11.0,
                TextBrush,
                1.0)
            {
                TextAlignment = alignment
            };
            drawingContext.DrawText(formatted, origin);
        }

        private static void DrawMarker(
            DrawingContext drawingContext,
            string text,
            Point origin)
        {
            var formatted = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(
                    new FontFamily("Segoe UI"),
                    FontStyles.Normal,
                    FontWeights.Bold,
                    FontStretches.Normal),
                14.0,
                MarkerBrush,
                1.0)
            {
                TextAlignment = TextAlignment.Center
            };
            drawingContext.DrawText(formatted, origin);
        }

        private void DrawCenteredText(DrawingContext drawingContext, string text)
        {
            DrawText(
                drawingContext,
                text,
                new Point(ActualWidth / 2.0, ActualHeight / 2.0),
                TextAlignment.Center);
        }

        private static Pen CreatePen(Color color, double thickness)
        {
            var pen = new Pen(CreateBrush(color), thickness);
            pen.Freeze();
            return pen;
        }

        private static Brush CreateBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static string Format(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
