using System;

namespace CivilSpellAI.Geometry
{
    internal sealed class Figure3DPassage
    {
        public const double InitialWidth = 0.10;
        public bool Enabled;
        public double Width, Height, Center, Bottom;
        public Figure3DPassage Copy() { return (Figure3DPassage)MemberwiseClone(); }
    }

    internal sealed class Figure3DParameters
    {
        public double Width, Depth, Height;
        public double LeftWall, RightWall, FrontWall, BackWall, BaseThickness;
        public Figure3DPassage Left = new Figure3DPassage();
        public Figure3DPassage Right = new Figure3DPassage();
        public double InnerWidth { get { return Width - LeftWall - RightWall; } }
        public double InnerDepth { get { return Depth - FrontWall - BackWall; } }
        public double ClearHeight { get { return Height - BaseThickness; } }

        public Figure3DParameters Copy()
        {
            var result = (Figure3DParameters)MemberwiseClone();
            result.Left = Left.Copy(); result.Right = Right.Copy();
            return result;
        }
        public void Validate()
        {
            Positive(Width, "Ancho"); Positive(Depth, "Profundidad"); Positive(Height, "Altura");
            Positive(LeftWall, "Muro izquierdo"); Positive(RightWall, "Muro derecho");
            Positive(FrontWall, "Muro frontal"); Positive(BackWall, "Muro posterior");
            Positive(BaseThickness, "Base");
            if (InnerWidth <= 1e-6 || InnerDepth <= 1e-6 || ClearHeight <= 1e-6)
                throw new ArgumentException("Los espesores deben dejar un espacio interior positivo.");
            ValidatePassage(Left, "izquierdo"); ValidatePassage(Right, "derecho");
        }
        private void ValidatePassage(Figure3DPassage passage, string side)
        {
            if (!passage.Enabled) return;
            Positive(passage.Width, "Ancho del paso " + side);
            Positive(passage.Height, "Alto del paso " + side);
            Positive(passage.Center, "Centro del paso " + side);
            Positive(passage.Bottom, "Cota inferior del paso " + side);
            if (passage.Center - passage.Width / 2 < FrontWall - 1e-7 ||
                passage.Center + passage.Width / 2 > Depth - BackWall + 1e-7 ||
                passage.Bottom < BaseThickness - 1e-7 || passage.Bottom + passage.Height >= Height - 1e-7)
                throw new ArgumentException("El paso " + side + " debe quedar dentro del muro, sobre la base y bajo la coronación.");
        }
        private static void Positive(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 1e-6)
                throw new ArgumentException(name + ": ingrese una medida positiva válida.");
        }
    }
}
