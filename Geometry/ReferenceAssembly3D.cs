using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace CivilSpellAI.Geometry
{
    [DataContract]
    internal sealed class ReferenceContour3D
    {
        // XY polygon coordinates and polyline bulges. Front contours use X/elevation.
        [DataMember] public double[] X;
        [DataMember] public double[] Y;
        [DataMember] public double[] Bulges;
        [DataMember] public bool Hole;
    }
    [DataContract]
    internal sealed class ReferencePart3D
    {
        [DataMember] public string Name;
        [DataMember] public short Color = 8;
        [DataMember] public List<ReferenceContour3D> Top = new List<ReferenceContour3D>();
        [DataMember] public List<ReferenceContour3D> Front = new List<ReferenceContour3D>();
        [DataMember] public double Bottom, Height;
        // A paired FRONT material section limits a TOP extrusion, rather than being
        // interpreted as the exterior silhouette of the entire assembly.
    }
    [DataContract]
    internal sealed class ReferencePipe3D
    {
        [DataMember] public double X1, Y1, Z1, X2, Y2, Z2, Diameter, Wall;
        [DataMember] public string Name;

        public double InclinationDegrees
        {
            get { return Math.Atan2(Z2 - Z1, HorizontalLength) * 180 / Math.PI; }
        }
        private double HorizontalLength
        {
            get { return Math.Sqrt(Math.Pow(X2 - X1, 2) + Math.Pow(Y2 - Y1, 2)); }
        }
        public void SetInclination(double degrees, bool keepFinalElevation)
        {
            if (double.IsNaN(degrees) || double.IsInfinity(degrees) || degrees <= -90 || degrees >= 90)
                throw new ArgumentException("La inclinación debe ser mayor que -90° y menor que 90°.");
            double length = HorizontalLength;
            if (double.IsNaN(length) || double.IsInfinity(length) || length <= 1e-7)
                throw new ArgumentException("La tubería necesita un recorrido horizontal para definir su inclinación.");
            double rise = length * Math.Tan(degrees * Math.PI / 180);
            double elevation = keepFinalElevation ? Z2 - rise : Z1 + rise;
            if (double.IsNaN(elevation) || double.IsInfinity(elevation))
                throw new ArgumentException("La inclinación produce una cota inválida.");
            // Keep TOP coordinates and the selected endpoint; only the other elevation changes.
            if (keepFinalElevation) Z1 = elevation;
            else Z2 = elevation;
        }
    }
    [DataContract]
    internal sealed class ReferencePipeRoute3D
    {
        [DataMember] public string Name;
        [DataMember] public double[] X, Y, Z;
        [DataMember] public double Diameter, Wall, BendRadius;
        [DataMember] public bool Cap;
        [DataMember] public ReferenceContour3D CorrugationProfile, CapProfile;
        [DataMember] public double CorrugationPitch;
    }
    [DataContract]
    internal sealed class ReferenceAssembly3D
    {
        [DataMember] public List<ReferencePart3D> Parts = new List<ReferencePart3D>();
        [DataMember] public ReferencePipe3D Pipe;
        [DataMember] public List<ReferencePipeRoute3D> PipeRoutes = new List<ReferencePipeRoute3D>();
        [DataMember] public List<string> Notes = new List<string>();
        [DataMember] public string TopReference, FrontReference;
        [DataMember] public List<ReferencePart3D> Cuts = new List<ReferencePart3D>();
        [DataMember] public bool CutPipeOpening;

        public void Validate()
        {
            if (PipeRoutes == null) PipeRoutes = new List<ReferencePipeRoute3D>();
            if (Parts == null || (Parts.Count == 0 && PipeRoutes.Count == 0)) throw new ArgumentException("Faltan componentes relacionados con TOP y FRONT.");
            foreach (var route in PipeRoutes)
            {
                if (route.X == null || route.Y == null || route.Z == null || route.X.Length < 2 || route.X.Length > 3 ||
                    route.Y.Length != route.X.Length || route.Z.Length != route.X.Length ||
                    !Finite(route.Diameter) || route.Diameter <= 1e-7 || !Finite(route.Wall) ||
                    (!route.Cap && (route.Wall <= 0 || route.Wall * 2 >= route.Diameter)) ||
                    !Finite(route.BendRadius) || route.BendRadius < 0)
                    throw new ArgumentException("Revise el recorrido, diámetro y espesor de " + route.Name + ".");
                if (route.CorrugationProfile != null)
                {
                    ValidateContour(route.CorrugationProfile);
                    double min = double.MaxValue, max = 0;
                    foreach(double radius in route.CorrugationProfile.Y) { min=Math.Min(min,radius);max=Math.Max(max,radius); }
                    if (!Finite(route.CorrugationPitch) || route.CorrugationPitch <= 1e-6 || min <= 0 ||
                        max <= min || route.Wall >= route.Diameter*min/(2*max))
                        throw new ArgumentException("Revise el paso, relieve y espesor en el valle del corrugado.");
                }
                if (route.CapProfile != null) ValidateContour(route.CapProfile);
                for (int i = 0; i < route.X.Length; i++)
                {
                    if (!Finite(route.X[i]) || !Finite(route.Y[i]) || !Finite(route.Z[i]))
                        throw new ArgumentException("Coordenadas inválidas en " + route.Name + ".");
                    if (i > 0 && Math.Sqrt(Math.Pow(route.X[i]-route.X[i-1],2)+Math.Pow(route.Y[i]-route.Y[i-1],2)+Math.Pow(route.Z[i]-route.Z[i-1],2)) < 1e-7)
                        throw new ArgumentException("El recorrido contiene puntos repetidos.");
                }
            }
            foreach (var part in Parts)
            {
                if (!Finite(part.Bottom) || !Finite(part.Height) || part.Height <= 1e-7 || part.Top.Count == 0)
                    throw new ArgumentException("Revise el contorno y la altura de " + part.Name + ".");
                foreach (var contour in part.Top) ValidateContour(contour);
                foreach (var contour in part.Front) ValidateContour(contour);
            }
            foreach (var cut in Cuts)
            {
                if (!Finite(cut.Bottom) || !Finite(cut.Height) || cut.Height <= 1e-7 || cut.Top.Count == 0)
                    throw new ArgumentException("Revise el vacío: " + cut.Name + ".");
                foreach (var contour in cut.Top) ValidateContour(contour);
                foreach (var contour in cut.Front) ValidateContour(contour);
            }
            if (Pipe != null)
            {
                var p = Pipe;
                if (!Finite(p.Diameter) || !Finite(p.Wall) || p.Diameter <= 1e-7 || p.Wall < 0 || p.Wall * 2 >= p.Diameter ||
                    !Finite(p.X1) || !Finite(p.Y1) || !Finite(p.Z1) || !Finite(p.X2) || !Finite(p.Y2) || !Finite(p.Z2) ||
                    Math.Sqrt(Math.Pow(p.X2-p.X1,2)+Math.Pow(p.Y2-p.Y1,2)+Math.Pow(p.Z2-p.Z1,2)) < 1e-7)
                    throw new ArgumentException("Revise el diámetro, espesor y recorrido de la tubería.");
            }
        }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        private static void ValidateContour(ReferenceContour3D contour)
        {
            if (contour.X == null || contour.Y == null || contour.X.Length < 3 || contour.X.Length != contour.Y.Length ||
                (contour.Bulges != null && contour.Bulges.Length != contour.X.Length))
                throw new ArgumentException("Contorno incompleto. Seleccione perfiles cerrados del componente.");
            for (int i=0; i<contour.X.Length; i++)
                if (!Finite(contour.X[i]) || !Finite(contour.Y[i]) || (contour.Bulges != null && !Finite(contour.Bulges[i])))
                    throw new ArgumentException("El contorno contiene coordenadas inválidas.");
        }
        public string Serialize()
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(ReferenceAssembly3D)).WriteObject(stream, this);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
        public static ReferenceAssembly3D Deserialize(string json)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                return (ReferenceAssembly3D)new DataContractJsonSerializer(typeof(ReferenceAssembly3D)).ReadObject(stream);
        }
        public ReferenceAssembly3D Copy() { return Deserialize(Serialize()); }
    }
}
