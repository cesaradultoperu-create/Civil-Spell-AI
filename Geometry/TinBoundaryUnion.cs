using System;
using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.OverlayNG;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;

namespace CivilSpellAI.Geometry
{
    // Robust snap-rounded polygon overlay. Z is lifted afterwards from the
    // original border segments, never from the two-dimensional overlay engine.
    internal static class TinBoundaryUnion
    {
        private const double Tolerance = TinBoundaryGeometry.TopologyTolerance;
        private sealed class SourceEdge
        {
            public TinBoundaryPoint A, B;
            public int Id;
        }
        private sealed class LiftedEdge
        {
            public TinBoundaryPoint A, B;
            public double CoincidentDifference;
        }

        internal static List<TinBoundaryLoop> Build(IList<TinFootprint> surfaces)
        {
            var precision = new PrecisionModel(1 / Tolerance);
            var factory = new GeometryFactory(precision);
            var polygons = new List<NtsGeometry>();
            var index = new STRtree<SourceEdge>();
            int edgeCount = 0;
            foreach (var surface in surfaces)
            {
                NtsGeometry footprint = null;
                foreach (var ring in surface.Rings)
                {
                    if (ring.Count < 3) throw new InvalidOperationException("Un borde TIN tiene menos de tres vertices.");
                    var coordinates = new List<Coordinate>();
                    for (int i = 0; i < ring.Count; i++)
                    {
                        var a = ring[i]; var b = ring[(i + 1) % ring.Count];
                        var coordinate = new Coordinate(a.X, a.Y); precision.MakePrecise(coordinate);
                        if (coordinates.Count == 0 || !coordinates[coordinates.Count - 1].Equals2D(coordinate)) coordinates.Add(coordinate);
                        if (TinBoundaryGeometry.Distance(a, b) > Tolerance)
                            index.Insert(Envelope(a, b), new SourceEdge { A = a, B = b, Id = edgeCount++ });
                        else if (Math.Abs(a.Z - b.Z) > TinBoundaryGeometry.ElevationTolerance)
                            throw new InvalidOperationException("Un borde contiene un salto vertical en el mismo XY.");
                    }
                    if (coordinates.Count > 1 && coordinates[0].Equals2D(coordinates[coordinates.Count - 1])) coordinates.RemoveAt(coordinates.Count - 1);
                    if (coordinates.Count < 3) continue;
                    coordinates.Add(coordinates[0].Copy());
                    NtsGeometry polygon = GeometryFixer.Fix(factory.CreatePolygon(coordinates.ToArray()));
                    // Even/odd membership: independent of ring order/orientation,
                    // including holes and multiple disconnected TIN components.
                    footprint = footprint == null ? polygon : OverlayNG.Overlay(footprint, polygon, SpatialFunction.SymDifference, precision);
                }
                if (footprint != null && !footprint.IsEmpty) polygons.Add(footprint);
            }
            if (edgeCount > 100000) throw new InvalidOperationException("Demasiados segmentos; seleccione menos superficies.");
            if (polygons.Count == 0) throw new InvalidOperationException("No hay bordes TIN utilizables.");
            index.Build();
            var union = UnaryUnionNG.Union(polygons, factory, precision);
            var loops = new List<TinBoundaryLoop>();
            AddPolygons(union, index, loops);
            if (loops.Count == 0) throw new InvalidOperationException("No se encontro un contorno cerrado de los TIN.");
            return loops;
        }

        private static void AddPolygons(NtsGeometry geometry, STRtree<SourceEdge> index, List<TinBoundaryLoop> loops)
        {
            var polygon = geometry as Polygon;
            if (polygon != null)
            {
                loops.Add(Lift(polygon.ExteriorRing, true, index));
                for (int i = 0; i < polygon.NumInteriorRings; i++) loops.Add(Lift(polygon.GetInteriorRingN(i), false, index));
                return;
            }
            var collection = geometry as GeometryCollection;
            if (collection != null)
                for (int i = 0; i < collection.NumGeometries; i++) AddPolygons(collection.GetGeometryN(i), index, loops);
        }

        private static TinBoundaryLoop Lift(LineString ring, bool exterior, STRtree<SourceEdge> index)
        {
            var xy = ring.Coordinates.Take(ring.NumPoints - 1).Select(p => new TinBoundaryPoint(p.X, p.Y, 0)).ToList();
            if ((TinBoundaryGeometry.SignedArea(xy) > 0) != exterior) xy.Reverse();
            var lifted = new List<LiftedEdge>();
            for (int i = 0; i < xy.Count; i++)
            {
                var a = xy[i]; var b = xy[(i + 1) % xy.Count];
                double length = TinBoundaryGeometry.Distance(a, b);
                if (length <= Tolerance * .1) continue;
                var envelope = Envelope(a, b); envelope.ExpandBy(Tolerance * 4);
                var candidates = index.Query(envelope).OrderBy(e => e.Id).ToList();
                var cuts = new List<double> { 0, 1 };
                foreach (var edge in candidates)
                {
                    AddCut(cuts, edge.A, a, b); AddCut(cuts, edge.B, a, b);
                }
                cuts.Sort();
                var distinct = new List<double> { 0 };
                foreach (double cut in cuts)
                    if ((cut - distinct[distinct.Count - 1]) * length > Tolerance) distinct.Add(cut);
                if (distinct[distinct.Count - 1] != 1) distinct.Add(1);
                for (int j = 1; j < distinct.Count; j++)
                {
                    var start = TinBoundaryPoint.Lerp(a, b, distinct[j - 1]);
                    var end = TinBoundaryPoint.Lerp(a, b, distinct[j]);
                    var mid = TinBoundaryPoint.Lerp(start, end, .5);
                    var sources = candidates.Where(e => DistanceTo(mid, e.A, e.B) <= Tolerance * 4 &&
                        DistanceTo(start, e.A, e.B) <= Tolerance * 4 && DistanceTo(end, e.A, e.B) <= Tolerance * 4).ToList();
                    if (sources.Count == 0)
                        throw new InvalidOperationException("No se encontro un borde de referencia para elevar un tramo del contorno.");
                    var source = sources[0];
                    double z0 = Elevation(start, source), z1 = Elevation(end, source);
                    double difference = 0;
                    foreach (var other in sources.Skip(1)) difference = Math.Max(difference,
                        Math.Max(Math.Abs(z0 - Elevation(start, other)), Math.Abs(z1 - Elevation(end, other))));
                    lifted.Add(new LiftedEdge { A = new TinBoundaryPoint(start.X, start.Y, z0),
                        B = new TinBoundaryPoint(end.X, end.Y, z1), CoincidentDifference = difference });
                }
            }
            var loop = new TinBoundaryLoop { Points = new List<TinBoundaryPoint>(), ReferencePoints = new List<TinBoundaryPoint>() };
            for (int i = 0; i < lifted.Count; i++)
            {
                var current = lifted[i]; var next = lifted[(i + 1) % lifted.Count];
                loop.Points.Add(current.A); loop.ReferencePoints.Add(current.A);
                double delta = Math.Abs(current.B.Z - next.A.Z);
                if (delta > TinBoundaryGeometry.ElevationTolerance)
                {
                    loop.ReferencePoints.Add(new TinBoundaryPoint(next.A.X, next.A.Y, current.B.Z));
                    loop.ElevationConflictCount++;
                    loop.ElevationError = "Hay encuentros de bordes con distintas cotas; conserve ambos extremos de referencia mediante tramos verticales.";
                }
                if (current.CoincidentDifference > TinBoundaryGeometry.ElevationTolerance)
                {
                    loop.CoincidentEdgeConflictCount++;
                    loop.ElevationError = "Hay bordes coincidentes con distintas cotas de referencia; no se puede elegir una de esas cotas automaticamente.";
                }
                loop.MaxElevationDifference = Math.Max(loop.MaxElevationDifference, Math.Max(delta, current.CoincidentDifference));
            }
            loop.Area = TinBoundaryGeometry.SignedArea(loop.Points);
            return loop;
        }

        private static void AddCut(List<double> cuts, TinBoundaryPoint p, TinBoundaryPoint a, TinBoundaryPoint b)
        {
            double t = Project(p, a, b);
            if (t > 0 && t < 1 && DistanceTo(p, a, b) <= Tolerance * 4) cuts.Add(t);
        }
        private static double Project(TinBoundaryPoint p, TinBoundaryPoint a, TinBoundaryPoint b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y, square = dx * dx + dy * dy;
            return square == 0 ? 0 : Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / square));
        }
        private static double DistanceTo(TinBoundaryPoint p, TinBoundaryPoint a, TinBoundaryPoint b)
        { return TinBoundaryGeometry.Distance(p, TinBoundaryPoint.Lerp(a, b, Project(p, a, b))); }
        private static double Elevation(TinBoundaryPoint p, SourceEdge edge)
        { return TinBoundaryPoint.Lerp(edge.A, edge.B, Project(p, edge.A, edge.B)).Z; }
        private static Envelope Envelope(TinBoundaryPoint a, TinBoundaryPoint b)
        { return new Envelope(Math.Min(a.X, b.X), Math.Max(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.Y, b.Y)); }
    }
}
