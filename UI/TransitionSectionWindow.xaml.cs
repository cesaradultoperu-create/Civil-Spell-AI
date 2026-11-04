using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using CivilSpellAI.Geometry;

namespace CivilSpellAI.UI
{
    public partial class TransitionSectionWindow : Window
    {
        private bool isInitializing;
        private TransitionSectionType currentType;

        public TransitionSectionWindow(TransitionSectionParameters initialValues)
        {
            if (initialValues == null)
                throw new ArgumentNullException("initialValues");

            isInitializing = true;
            InitializeComponent();
            currentType = initialValues.SectionType;
            SetFields(initialValues);
            SectionTypeComboBox.SelectedIndex =
                initialValues.SectionType == TransitionSectionType.TypeB ? 1 : 0;
            UpdateTypeFields();
            isInitializing = false;
            RefreshCalculatedValues();
        }

        private void SetFields(TransitionSectionParameters values)
        {
            InitialStraightTextBox.Text = Format(values.InitialStraightLength);
            TransitionLengthTextBox.Text = Format(values.TransitionLength);
            FinalStraightTextBox.Text = Format(values.FinalStraightLength);
            InitialClearHeightTextBox.Text = Format(values.InitialClearHeight);
            FinalClearHeightTextBox.Text = Format(values.FinalClearHeight);
            InnerThicknessTextBox.Text = Format(values.WallAngleDegrees);
            OuterThicknessTextBox.Text = Format(values.OuterThickness);
            WallHeightTextBox.Text = Format(values.WallHeight);
            HatchPatternComboBox.Text = values.HatchPatternName;
            HatchScaleTextBox.Text = Format(values.HatchScale);
            HatchAngleTextBox.Text = Format(values.HatchAngleDegrees);
            HatchColorTextBox.Text = values.HatchColorIndex.ToString(
                CultureInfo.InvariantCulture);
            TransitionSectionParameters typeB = values.SectionType == TransitionSectionType.TypeB
                ? values
                : TransitionSectionParameters.TypeBDefaults;
            SecondTransitionTextBox.Text = Format(typeB.SecondTransitionLength);
            TerminalHeightTextBox.Text = Format(typeB.TerminalTotalHeight);
        }

        public TransitionSectionParameters Parameters { get; private set; }

        public bool CreateFromObjectRequested { get; private set; }

        public TransitionSectionParameters DraftParameters { get; private set; }

        private void CreateFromObjectClick(object sender, RoutedEventArgs eventArgs)
        {
            TransitionSectionParameters draft;
            string error;
            if (TryReadParameters(out draft, out error, false))
                DraftParameters = draft;
            CreateFromObjectRequested = true;
            Close();
        }

        private void AcceptClick(object sender, RoutedEventArgs eventArgs)
        {
            TransitionSectionParameters parameters;
            string error;
            if (!TryReadParameters(out parameters, out error, true))
            {
                MessageBox.Show(
                    this,
                    error,
                    "Sección paramétrica",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            Parameters = parameters;
            DialogResult = true;
        }

        private void ValueTextChanged(object sender, TextChangedEventArgs eventArgs)
        {
            if (isInitializing)
                return;
            RefreshCalculatedValues();
        }

        private void HatchSelectionChanged(
            object sender,
            SelectionChangedEventArgs eventArgs)
        {
            if (IsInitialized)
                RefreshCalculatedValues();
        }

        private void SectionTypeSelectionChanged(
            object sender,
            SelectionChangedEventArgs eventArgs)
        {
            if (isInitializing || SectionTypeComboBox.SelectedIndex < 0)
                return;

            TransitionSectionType selected = SectionTypeComboBox.SelectedIndex == 1
                ? TransitionSectionType.TypeB
                : TransitionSectionType.TypeA;
            if (selected != currentType)
            {
                currentType = selected;
                isInitializing = true;
                SetFields(selected == TransitionSectionType.TypeB
                    ? TransitionSectionParameters.TypeBDefaults
                    : TransitionSectionParameters.Defaults);
                isInitializing = false;
            }

            UpdateTypeFields();
            RefreshCalculatedValues();
        }

        private void UpdateTypeFields()
        {
            bool typeB = currentType == TransitionSectionType.TypeB;
            Visibility visibility = typeB ? Visibility.Visible : Visibility.Collapsed;
            SecondTransitionLabel.Visibility = visibility;
            SecondTransitionTextBox.Visibility = visibility;
            TerminalHeightLabel.Visibility = visibility;
            TerminalHeightTextBox.Visibility = visibility;
            FinalStraightLabelText.Text = typeB
                ? " · Longitud recta central:"
                : " · Longitud recta final:";
            FinalHeightLabelText.Text = typeB
                ? " · Altura libre intermedia:"
                : " · Altura libre final:";
        }

        private void RefreshCalculatedValues()
        {
            if (InitialTotalTextBlock == null)
                return;

            TransitionSectionParameters parameters;
            string error;
            if (!TryReadParameters(out parameters, out error, false))
            {
                SectionPreview.Parameters = null;
                InitialTotalTextBlock.Text = "Altura total inicial: —";
                FinalTotalTextBlock.Text = "Altura total final: —";
                DimensionedLengthTextBlock.Text = "Longitud transición + tramo final: —";
                return;
            }

            SectionPreview.Parameters = parameters;

            InitialTotalTextBlock.Text =
                "Altura total inicial calculada: " + Format(parameters.InitialTotalHeight);
            if (parameters.SectionType == TransitionSectionType.TypeB)
            {
                FinalTotalTextBlock.Text =
                    "Altura total intermedia calculada: " + Format(parameters.FinalTotalHeight) +
                    " · altura terminal: " + Format(parameters.TerminalTotalHeight);
                DimensionedLengthTextBlock.Text =
                    "Longitud L2 + L3 + L4: " + Format(parameters.DimensionedLength);
            }
            else
            {
                FinalTotalTextBlock.Text =
                    "Altura total final calculada: " + Format(parameters.FinalTotalHeight);
                DimensionedLengthTextBlock.Text =
                    "Longitud transición + tramo final: " + Format(parameters.DimensionedLength);
            }
        }

        private bool TryReadParameters(
            out TransitionSectionParameters parameters,
            out string error,
            bool focusInvalidField)
        {
            parameters = null;
            error = null;
            double initialStraight;
            double transition;
            double finalStraight;
            double initialClear;
            double finalClear;
            double wallAngle;
            double outer;
            double wallHeight;
            double hatchScale;
            double hatchAngle;
            int hatchColor;
            double secondTransition = 0.0;
            double terminalHeight = 0.0;

            if (!TryReadPositive(InitialStraightTextBox, "longitud recta inicial", focusInvalidField, out initialStraight, out error) ||
                !TryReadPositive(TransitionLengthTextBox, "longitud de transición", focusInvalidField, out transition, out error) ||
                !TryReadPositive(FinalStraightTextBox, "longitud recta final", focusInvalidField, out finalStraight, out error) ||
                !TryReadPositive(InitialClearHeightTextBox, "altura libre inicial", focusInvalidField, out initialClear, out error) ||
                !TryReadPositive(FinalClearHeightTextBox, "altura libre final", focusInvalidField, out finalClear, out error) ||
                !TryReadWallAngle(InnerThicknessTextBox, focusInvalidField, out wallAngle, out error) ||
                !TryReadPositive(OuterThicknessTextBox, "espesor del muro", focusInvalidField, out outer, out error) ||
                !TryReadPositive(WallHeightTextBox, "altura de muros 3D", focusInvalidField, out wallHeight, out error) ||
                !TryReadPositive(HatchScaleTextBox, "escala del hatch", focusInvalidField, out hatchScale, out error) ||
                !TryReadNumber(HatchAngleTextBox, "ángulo del hatch", focusInvalidField, out hatchAngle, out error) ||
                !TryReadColorIndex(HatchColorTextBox, focusInvalidField, out hatchColor, out error))
            {
                return false;
            }

            string hatchPattern = (HatchPatternComboBox.Text ?? string.Empty).Trim();
            if (hatchPattern.Length == 0)
            {
                error = "Seleccione o escriba un patrón de hatch.";
                if (focusInvalidField)
                    HatchPatternComboBox.Focus();
                return false;
            }

            TransitionSectionType sectionType = SectionTypeComboBox.SelectedIndex == 1
                ? TransitionSectionType.TypeB
                : TransitionSectionType.TypeA;
            if (sectionType == TransitionSectionType.TypeB &&
                (!TryReadPositive(SecondTransitionTextBox, "segunda transición", focusInvalidField, out secondTransition, out error) ||
                 !TryReadPositive(TerminalHeightTextBox, "altura total terminal", focusInvalidField, out terminalHeight, out error)))
            {
                return false;
            }

            try
            {
                double inner = wallHeight /
                    Math.Tan(wallAngle * Math.PI / 180.0);
                parameters = new TransitionSectionParameters(
                initialStraight,
                transition,
                finalStraight,
                initialClear,
                finalClear,
                inner,
                outer,
                hatchPattern,
                hatchScale,
                hatchAngle,
                hatchColor,
                sectionType,
                secondTransition,
                terminalHeight,
                wallHeight,
                wallAngle);
            }
            catch (ArgumentException exception)
            {
                error = exception.Message;
                return false;
            }
            return true;
        }

        private static bool TryReadPositive(
            TextBox textBox,
            string name,
            bool focusInvalidField,
            out double value,
            out string error)
        {
            string normalized = (textBox.Text ?? string.Empty)
                .Trim()
                .Replace(',', '.');
            if (!double.TryParse(
                    normalized,
                    NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent,
                    CultureInfo.InvariantCulture,
                    out value) ||
                double.IsNaN(value) ||
                double.IsInfinity(value) ||
                value <= 0.0)
            {
                error = "Ingrese un valor mayor que cero para " + name + ".";
                if (focusInvalidField)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }
                return false;
            }

            error = null;
            return true;
        }

        private static bool TryReadNumber(
            TextBox textBox,
            string name,
            bool focusInvalidField,
            out double value,
            out string error)
        {
            string normalized = (textBox.Text ?? string.Empty)
                .Trim()
                .Replace(',', '.');
            if (!double.TryParse(
                    normalized,
                    NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent,
                    CultureInfo.InvariantCulture,
                    out value) ||
                double.IsNaN(value) ||
                double.IsInfinity(value))
            {
                error = "Ingrese un número válido para " + name + ".";
                if (focusInvalidField)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }
                return false;
            }

            error = null;
            return true;
        }

        private static bool TryReadWallAngle(
            TextBox textBox,
            bool focusInvalidField,
            out double value,
            out string error)
        {
            if (!TryReadNumber(
                    textBox,
                    "inclinación del muro",
                    focusInvalidField,
                    out value,
                    out error) ||
                value <= 0.0 ||
                value >= 90.0)
            {
                error = "Ingrese una inclinación mayor que 0° y menor que 90°.";
                if (focusInvalidField)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }
                return false;
            }

            return true;
        }

        private static bool TryReadColorIndex(
            TextBox textBox,
            bool focusInvalidField,
            out int value,
            out string error)
        {
            if (!int.TryParse(
                    (textBox.Text ?? string.Empty).Trim(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out value) ||
                value < 0 || value > 256)
            {
                error = "Ingrese un color ACI entre 0 y 256. Ejemplos: 1 rojo, 2 amarillo, 3 verde, 5 azul, 7 blanco y 256 PorCapa.";
                if (focusInvalidField)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }
                return false;
            }

            error = null;
            return true;
        }

        private static string Format(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
