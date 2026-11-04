using System;
using System.Collections.Generic;

namespace CivilSpellAI.Geometry
{
    public enum TransitionSectionType
    {
        TypeA = 1,
        TypeB = 2
    }

    public sealed class TransitionSectionParameters
    {
        public TransitionSectionParameters(
            double initialStraightLength,
            double transitionLength,
            double finalStraightLength,
            double initialClearHeight,
            double finalClearHeight,
            double innerThickness,
            double outerThickness)
            : this(
                initialStraightLength,
                transitionLength,
                finalStraightLength,
                initialClearHeight,
                finalClearHeight,
                innerThickness,
                outerThickness,
                "GRAVEL",
                0.20,
                0.0,
                256)
        {
        }

        public TransitionSectionParameters(
            double initialStraightLength,
            double transitionLength,
            double finalStraightLength,
            double initialClearHeight,
            double finalClearHeight,
            double innerThickness,
            double outerThickness,
            string hatchPatternName,
            double hatchScale,
            double hatchAngleDegrees)
            : this(
                initialStraightLength,
                transitionLength,
                finalStraightLength,
                initialClearHeight,
                finalClearHeight,
                innerThickness,
                outerThickness,
                hatchPatternName,
                hatchScale,
                hatchAngleDegrees,
                256,
                TransitionSectionType.TypeA,
                0.0,
                0.0)
        {
        }

        public TransitionSectionParameters(
            double initialStraightLength,
            double transitionLength,
            double finalStraightLength,
            double initialClearHeight,
            double finalClearHeight,
            double innerThickness,
            double outerThickness,
            string hatchPatternName,
            double hatchScale,
            double hatchAngleDegrees,
            int hatchColorIndex)
            : this(
                initialStraightLength,
                transitionLength,
                finalStraightLength,
                initialClearHeight,
                finalClearHeight,
                innerThickness,
                outerThickness,
                hatchPatternName,
                hatchScale,
                hatchAngleDegrees,
                hatchColorIndex,
                TransitionSectionType.TypeA,
                0.0,
                0.0)
        {
        }

        public TransitionSectionParameters(
            double initialStraightLength,
            double transitionLength,
            double finalStraightLength,
            double initialClearHeight,
            double finalClearHeight,
            double innerThickness,
            double outerThickness,
            string hatchPatternName,
            double hatchScale,
            double hatchAngleDegrees,
            int hatchColorIndex,
            TransitionSectionType sectionType,
            double secondTransitionLength,
            double terminalTotalHeight,
            double wallHeight = 0.30,
            double wallAngleDegrees = double.NaN)
        {
            InitialStraightLength = RequirePositive(
                initialStraightLength,
                "La longitud recta inicial");
            TransitionLength = RequirePositive(
                transitionLength,
                "La longitud de transición");
            FinalStraightLength = RequirePositive(
                finalStraightLength,
                "La longitud recta final");
            InitialClearHeight = RequirePositive(
                initialClearHeight,
                "La altura libre inicial");
            FinalClearHeight = RequirePositive(
                finalClearHeight,
                "La altura libre final");
            OuterThickness = RequirePositive(
                outerThickness,
                "El espesor exterior");
            HatchPatternName = RequirePatternName(hatchPatternName);
            HatchScale = RequirePositive(hatchScale, "La escala del hatch");
            HatchAngleDegrees = RequireFinite(
                hatchAngleDegrees,
                "El ángulo del hatch");
            HatchColorIndex = RequireColorIndex(hatchColorIndex);
            WallHeight = RequirePositive(
                wallHeight,
                "La altura de los muros 3D");
            if (double.IsNaN(wallAngleDegrees))
            {
                InnerThickness = RequirePositive(
                    innerThickness,
                    "El desplazamiento horizontal del muro");
                WallAngleDegrees = Math.Atan2(
                    WallHeight,
                    InnerThickness) * 180.0 / Math.PI;
            }
            else
            {
                WallAngleDegrees = RequireWallAngle(wallAngleDegrees);
                InnerThickness = WallHeight /
                    Math.Tan(WallAngleDegrees * Math.PI / 180.0);
            }
            if (sectionType != TransitionSectionType.TypeA &&
                sectionType != TransitionSectionType.TypeB)
            {
                throw new ArgumentOutOfRangeException("sectionType");
            }

            SectionType = sectionType;
            if (sectionType == TransitionSectionType.TypeB)
            {
                SecondTransitionLength = RequirePositive(
                    secondTransitionLength,
                    "La segunda longitud de transición");
                TerminalTotalHeight = RequirePositive(
                    terminalTotalHeight,
                    "La altura total terminal");
                if (TerminalClearHeight <= 0.0)
                {
                    throw new ArgumentOutOfRangeException(
                        "terminalTotalHeight",
                        "La altura total terminal debe ser mayor que la suma de las capas superiores e inferiores.");
                }
            }
            else
            {
                SecondTransitionLength = 0.0;
                TerminalTotalHeight = 0.0;
            }
        }

        public static TransitionSectionParameters Defaults
        {
            get
            {
                return new TransitionSectionParameters(
                    1.20,
                    1.20,
                    1.20,
                    0.30,
                    1.20,
                    0.15,
                    0.10);
            }
        }

        public static TransitionSectionParameters TypeBDefaults
        {
            get
            {
                return new TransitionSectionParameters(
                    0.75,
                    1.50,
                    1.50,
                    0.40,
                    5.00,
                    0.15,
                    0.05,
                    "GRAVEL",
                    0.20,
                    0.0,
                    256,
                    TransitionSectionType.TypeB,
                    1.03,
                    7.00);
            }
        }

        public double InitialStraightLength { get; private set; }

        public double TransitionLength { get; private set; }

        public double FinalStraightLength { get; private set; }

        public double InitialClearHeight { get; private set; }

        public double FinalClearHeight { get; private set; }

        public double InnerThickness { get; private set; }

        public double OuterThickness { get; private set; }

        public string HatchPatternName { get; private set; }

        public double HatchScale { get; private set; }

        public double HatchAngleDegrees { get; private set; }

        public int HatchColorIndex { get; private set; }

        public double WallHeight { get; private set; }

        public double WallAngleDegrees { get; private set; }

        public TransitionSectionType SectionType { get; private set; }

        public double SecondTransitionLength { get; private set; }

        public double TerminalTotalHeight { get; private set; }

        public double TerminalClearHeight
        {
            get
            {
                // En el extremo de la sección B ya terminó E1. Solo E2
                // permanece entre el núcleo y la altura total terminal.
                return TerminalTotalHeight - (2.0 * OuterThickness);
            }
        }

        public double InitialTotalHeight
        {
            get
            {
                return InitialClearHeight +
                    (2.0 * InnerThickness) +
                    (2.0 * OuterThickness);
            }
        }

        public double FinalTotalHeight
        {
            get
            {
                return FinalClearHeight +
                    (2.0 * InnerThickness) +
                    (2.0 * OuterThickness);
            }
        }

        public double DimensionedLength
        {
            get
            {
                return TransitionLength + FinalStraightLength +
                    (SectionType == TransitionSectionType.TypeB
                        ? SecondTransitionLength
                        : 0.0);
            }
        }

        private static double RequirePositive(double value, string description)
        {
            if (double.IsNaN(value) ||
                double.IsInfinity(value) ||
                value <= 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    "value",
                    description + " debe ser mayor que cero.");
            }

            return value;
        }

        private static double RequireFinite(double value, string description)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    "value",
                    description + " debe ser un número válido.");
            }

            return value;
        }

        private static double RequireWallAngle(double value)
        {
            if (double.IsNaN(value) ||
                double.IsInfinity(value) ||
                value <= 0.0 ||
                value >= 90.0)
            {
                throw new ArgumentOutOfRangeException(
                    "wallAngleDegrees",
                    "La inclinación del muro debe ser mayor que 0° y menor que 90°.");
            }

            return value;
        }

        private static string RequirePatternName(string value)
        {
            string normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0 || normalized.Length > 64)
            {
                throw new ArgumentException(
                    "Seleccione o escriba un nombre de patrón de hatch válido.",
                    "hatchPatternName");
            }

            return normalized.ToUpperInvariant();
        }

        private static int RequireColorIndex(int value)
        {
            if (value < 0 || value > 256)
            {
                throw new ArgumentOutOfRangeException(
                    "hatchColorIndex",
                    "El color ACI del hatch debe estar entre 0 y 256.");
            }

            return value;
        }
    }

    public struct SectionPoint
    {
        public SectionPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; private set; }

        public double Y { get; private set; }
    }

    public sealed class TransitionSectionGeometry
    {
        public TransitionSectionGeometry(TransitionSectionParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException("parameters");

            Parameters = parameters;
            Polygons = CreatePolygons(parameters);
        }

        public TransitionSectionParameters Parameters { get; private set; }

        public IReadOnlyList<IReadOnlyList<SectionPoint>> Polygons { get; private set; }

        private static IReadOnlyList<IReadOnlyList<SectionPoint>> CreatePolygons(
            TransitionSectionParameters parameters)
        {
            if (parameters.SectionType == TransitionSectionType.TypeB)
                return CreateTypeBPolygons(parameters);

            double x0 = 0.0;
            double x1 = parameters.InitialStraightLength;
            double x2 = x1 + parameters.TransitionLength;
            double x3 = x2 + parameters.FinalStraightLength;
            double initialClear = parameters.InitialClearHeight / 2.0;
            double finalClear = parameters.FinalClearHeight / 2.0;
            double inner = parameters.InnerThickness;
            double outer = parameters.OuterThickness;

            var polygons = new List<IReadOnlyList<SectionPoint>>();
            polygons.Add(CreateSymmetricCore(
                x0,
                x1,
                x2,
                x3,
                initialClear,
                finalClear));
            polygons.Add(CreateBand(
                x0,
                x1,
                x2,
                x3,
                initialClear,
                finalClear,
                inner,
                1.0));
            polygons.Add(CreateBand(
                x0,
                x1,
                x2,
                x3,
                initialClear,
                finalClear,
                inner,
                -1.0));
            polygons.Add(CreateBand(
                x0,
                x1,
                x2,
                x3,
                initialClear + inner,
                finalClear + inner,
                outer,
                1.0));
            polygons.Add(CreateBand(
                x0,
                x1,
                x2,
                x3,
                initialClear + inner,
                finalClear + inner,
                outer,
                -1.0));
            return polygons;
        }

        private static IReadOnlyList<IReadOnlyList<SectionPoint>> CreateTypeBPolygons(
            TransitionSectionParameters parameters)
        {
            double[] x =
            {
                0.0,
                parameters.InitialStraightLength,
                parameters.InitialStraightLength + parameters.TransitionLength,
                parameters.InitialStraightLength + parameters.TransitionLength +
                    parameters.FinalStraightLength,
                parameters.InitialStraightLength + parameters.TransitionLength +
                    parameters.FinalStraightLength + parameters.SecondTransitionLength
            };
            double[] clear =
            {
                parameters.InitialClearHeight / 2.0,
                parameters.InitialClearHeight / 2.0,
                parameters.FinalClearHeight / 2.0,
                parameters.FinalClearHeight / 2.0,
                parameters.TerminalClearHeight / 2.0
            };

            // E1 termina en A (fin de L3), mientras E2 continúa hasta H3.
            // El núcleo incorpora el escalón vertical de E1 en A para unirse
            // directamente con E2 en el tramo terminal.
            double[] wallX =
            {
                x[0],
                x[1],
                x[2],
                x[3]
            };
            double[] wallClear =
            {
                clear[0],
                clear[1],
                clear[2],
                clear[3]
            };

            var polygons = new List<IReadOnlyList<SectionPoint>>
            {
                CreateTypeBCore(x, clear, parameters.InnerThickness),
                CreateVariableBand(wallX, wallClear, parameters.InnerThickness, 1.0),
                CreateVariableBand(wallX, wallClear, parameters.InnerThickness, -1.0)
            };
            double[] outerBase = new double[clear.Length];
            for (int index = 0; index < clear.Length - 1; index++)
                outerBase[index] = clear[index] + parameters.InnerThickness;
            outerBase[outerBase.Length - 1] = clear[clear.Length - 1];
            polygons.Add(CreateVariableBand(x, outerBase, parameters.OuterThickness, 1.0));
            polygons.Add(CreateVariableBand(x, outerBase, parameters.OuterThickness, -1.0));
            return polygons;
        }

        private static IReadOnlyList<SectionPoint> CreateTypeBCore(
            IReadOnlyList<double> x,
            IReadOnlyList<double> halfHeight,
            double innerThickness)
        {
            int wallEnd = x.Count - 2;
            int terminal = x.Count - 1;
            var points = new List<SectionPoint>((x.Count * 2) + 2);
            for (int index = 0; index <= wallEnd; index++)
                points.Add(new SectionPoint(x[index], halfHeight[index]));
            points.Add(new SectionPoint(
                x[wallEnd],
                halfHeight[wallEnd] + innerThickness));
            points.Add(new SectionPoint(x[terminal], halfHeight[terminal]));
            points.Add(new SectionPoint(x[terminal], -halfHeight[terminal]));
            points.Add(new SectionPoint(
                x[wallEnd],
                -(halfHeight[wallEnd] + innerThickness)));
            for (int index = wallEnd; index >= 0; index--)
                points.Add(new SectionPoint(x[index], -halfHeight[index]));
            return points;
        }

        private static IReadOnlyList<SectionPoint> CreateVariableCore(
            IReadOnlyList<double> x,
            IReadOnlyList<double> halfHeight)
        {
            var points = new List<SectionPoint>(x.Count * 2);
            for (int index = 0; index < x.Count; index++)
                points.Add(new SectionPoint(x[index], halfHeight[index]));
            for (int index = x.Count - 1; index >= 0; index--)
                points.Add(new SectionPoint(x[index], -halfHeight[index]));
            return points;
        }

        private static IReadOnlyList<SectionPoint> CreateVariableBand(
            IReadOnlyList<double> x,
            IReadOnlyList<double> innerOffset,
            double thickness,
            double side)
        {
            var points = new List<SectionPoint>(x.Count * 2);
            for (int index = 0; index < x.Count; index++)
                points.Add(new SectionPoint(x[index], side * innerOffset[index]));
            for (int index = x.Count - 1; index >= 0; index--)
                points.Add(new SectionPoint(
                    x[index],
                    side * (innerOffset[index] + thickness)));
            return points;
        }

        private static IReadOnlyList<SectionPoint> CreateSymmetricCore(
            double x0,
            double x1,
            double x2,
            double x3,
            double initialHalfHeight,
            double finalHalfHeight)
        {
            return new[]
            {
                new SectionPoint(x0, initialHalfHeight),
                new SectionPoint(x1, initialHalfHeight),
                new SectionPoint(x2, finalHalfHeight),
                new SectionPoint(x3, finalHalfHeight),
                new SectionPoint(x3, -finalHalfHeight),
                new SectionPoint(x2, -finalHalfHeight),
                new SectionPoint(x1, -initialHalfHeight),
                new SectionPoint(x0, -initialHalfHeight)
            };
        }

        private static IReadOnlyList<SectionPoint> CreateBand(
            double x0,
            double x1,
            double x2,
            double x3,
            double initialInnerOffset,
            double finalInnerOffset,
            double thickness,
            double side)
        {
            double initialOuterOffset = initialInnerOffset + thickness;
            double finalOuterOffset = finalInnerOffset + thickness;
            return new[]
            {
                new SectionPoint(x0, side * initialInnerOffset),
                new SectionPoint(x1, side * initialInnerOffset),
                new SectionPoint(x2, side * finalInnerOffset),
                new SectionPoint(x3, side * finalInnerOffset),
                new SectionPoint(x3, side * finalOuterOffset),
                new SectionPoint(x2, side * finalOuterOffset),
                new SectionPoint(x1, side * initialOuterOffset),
                new SectionPoint(x0, side * initialOuterOffset)
            };
        }
    }
}
