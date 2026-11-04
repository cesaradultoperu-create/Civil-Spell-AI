using System;
using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Operation.Linemerge;
using NetTopologySuite.Operation.OverlayNG;
using G = NetTopologySuite.Geometries.Geometry;

namespace DrenBordes
{
    internal sealed class BorderResult
    {
        internal readonly List<Coordinate[]> Paths = new List<Coordinate[]>();
        internal G Footprint;
        internal int Junctions, Terminals, LimitedCorners;
    }

    internal static class NetworkBorders
    {
        internal const double Grid = .000001;
        internal const double MitreLimit = 10;
        private sealed class Node
        {
            internal Coordinate Point;
            internal readonly List<double> Angles = new List<double>();
        }

        internal static BorderResult Build(IList<Coordinate[]> paths, double offset)
        {
            if (paths.Count == 0 || double.IsNaN(offset) || double.IsInfinity(offset) || offset < .0001)
                throw new ArgumentException("No hay ejes utilizables o la distancia es menor que 0.0001.");
            var precision = new PrecisionModel(1 / Grid);
            var factory = new GeometryFactory(precision);
            var lines = paths.Select(p => (G)factory.CreateLineString(p)).ToArray();
            // Node at every intersection, remove duplicate segments, then join degree-two vertices.
            var network = UnaryUnionNG.Union(lines, factory, precision);
            var noded = Lines(network).ToList();
            var nodes = new Dictionary<string, Node>();
            foreach (var line in noded)
            {
                var points = line.Coordinates;
                for (int i = 1; i < points.Length; i++)
                {
                    AddRay(nodes, points[i - 1], points[i]);
                    AddRay(nodes, points[i], points[i - 1]);
                }
            }
            var merger = new LineMerger();
            merger.Add(network);
            var areas = merger.GetMergedLineStrings().Select(l => BufferOp.Buffer(l, offset,
                new BufferParameters { EndCapStyle = EndCapStyle.Flat, JoinStyle = JoinStyle.Mitre,
                    MitreLimit = MitreLimit, SimplifyFactor = 0 })).ToList();
            var result = new BorderResult();
            var caps = new STRtree<LineSegment>();
            foreach (var node in nodes.Values)
            {
                node.Angles.Sort();
                if (node.Angles.Count == 1)
                {
                    result.Terminals++;
                    var cap = new LineSegment(Side(node.Point, node.Angles[0], offset),
                        Side(node.Point, node.Angles[0], -offset));
                    var envelope = new Envelope(cap.P0, cap.P1); envelope.ExpandBy(Grid * 8);
                    caps.Insert(envelope, cap);
                }
                if (node.Angles.Count < 3) continue;
                result.Junctions++;
                // Buffers already meet on the inside of each sector. Fill an outside
                // corner when three or more rays all point into one half-plane.
                for (int i = 0; i < node.Angles.Count; i++)
                {
                    double a = node.Angles[i], b = node.Angles[(i + 1) % node.Angles.Count];
                    if (i == node.Angles.Count - 1) b += 2 * Math.PI;
                    double gap = b - a;
                    if (gap <= Math.PI + 1e-10) continue;
                    var p = Side(node.Point, a, offset);
                    var q = Side(node.Point, b, -offset);
                    double distance = offset / Math.Sin(gap / 2);
                    var wedge = new List<Coordinate> { node.Point, p };
                    if (distance <= offset * MitreLimit)
                        wedge.Add(new Coordinate(node.Point.X + distance * Math.Cos((a + b) / 2),
                            node.Point.Y + distance * Math.Sin((a + b) / 2)));
                    else result.LimitedCorners++;
                    wedge.Add(q); wedge.Add(node.Point);
                    areas.Add(factory.CreatePolygon(wedge.ToArray()));
                }
            }
            caps.Build();
            result.Footprint = UnaryUnionNG.Union(areas, factory, precision);
            if (result.Footprint.IsEmpty || !result.Footprint.IsValid)
                throw new InvalidOperationException("No se pudo formar una red de bordes valida.");
            var borderMerger = new LineMerger();
            foreach (var ring in Lines(result.Footprint.Boundary))
            {
                var points = ring.Coordinates;
                for (int i = 1; i < points.Length; i++)
                {
                    var a = points[i - 1]; var b = points[i];
                    if (a.Distance(b) < Grid) continue;
                    var env = new Envelope(a, b); env.ExpandBy(Grid * 8);
                    // Keep terminal ends open: remove only the transverse cap, including
                    // partial caps exposed where two nearby ribbons overlap.
                    if (caps.Query(env).Any(c => c.Distance(a) <= Grid * 8 && c.Distance(b) <= Grid * 8)) continue;
                    borderMerger.Add(factory.CreateLineString(new[] { a, b }));
                }
            }
            result.Paths.AddRange(borderMerger.GetMergedLineStrings().Select(l => l.Coordinates));
            if (result.Paths.Count == 0) throw new InvalidOperationException("La red no produjo bordes.");
            return result;
        }

        private static void AddRay(Dictionary<string, Node> nodes, Coordinate a, Coordinate b)
        {
            if (a.Distance(b) < Grid) return;
            string key = Math.Round(a.X / Grid).ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" +
                Math.Round(a.Y / Grid).ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            Node node;
            if (!nodes.TryGetValue(key, out node)) nodes.Add(key, node = new Node { Point = a });
            double angle = Math.Atan2(b.Y - a.Y, b.X - a.X);
            if (angle < 0) angle += 2 * Math.PI;
            if (!node.Angles.Any(x => Math.Abs(Math.Atan2(Math.Sin(x - angle), Math.Cos(x - angle))) < 1e-10))
                node.Angles.Add(angle);
        }
        private static Coordinate Side(Coordinate p, double angle, double d)
        { return new Coordinate(p.X - d * Math.Sin(angle), p.Y + d * Math.Cos(angle)); }
        internal static IEnumerable<LineString> Lines(G geometry)
        {
            var line = geometry as LineString;
            if (line != null) { yield return line; yield break; }
            var collection = geometry as GeometryCollection;
            if (collection != null)
                for (int i = 0; i < collection.NumGeometries; i++)
                    foreach (var part in Lines(collection.GetGeometryN(i))) yield return part;
        }
    }
}
