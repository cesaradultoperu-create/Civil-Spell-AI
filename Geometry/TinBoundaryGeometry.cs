using System;
using System.Collections.Generic;
using System.Linq;

namespace CivilSpellAI.Geometry
{
    // Independent of Autodesk so that topology and elevation handling can be tested.
    internal struct TinBoundaryPoint
    {
        public readonly double X, Y, Z;
        public TinBoundaryPoint(double x, double y, double z) { X = x; Y = y; Z = z; }
        public static TinBoundaryPoint Lerp(TinBoundaryPoint a, TinBoundaryPoint b, double t)
        { return new TinBoundaryPoint(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t); }
    }

    internal sealed class TinFootprint
    {
        public string Name;
        // Even/odd membership supports holes and disconnected pieces of a TIN.
        public readonly List<List<TinBoundaryPoint>> Rings = new List<List<TinBoundaryPoint>>();
    }

    internal sealed class TinBoundaryLoop
    {
        public List<TinBoundaryPoint> Points;
        public List<TinBoundaryPoint> ReferencePoints;
        public double Area;
        public string ElevationError;
        public int ElevationConflictCount, CoincidentEdgeConflictCount;
        public double MaxElevationDifference;
    }

    internal static class TinBoundaryGeometry
    {
        internal const double TopologyTolerance = 0.0000001;
        internal const double ElevationTolerance = 0.00001;
        private const long MaxWork = 20000000;

        private sealed class Segment
        {
            public TinBoundaryPoint A, B;
            public int Index;
            public readonly List<double> Cuts = new List<double> { 0, 1 };
            public double MinX { get { return Math.Min(A.X, B.X); } }
            public double MaxX { get { return Math.Max(A.X, B.X); } }
        }
        private sealed class Node
        {
            public int Id;
            public TinBoundaryPoint Point;
        }
        private sealed class PreparedRing
        {
            public List<TinBoundaryPoint> Points;
            public double MinX, MaxX, MinY, MaxY;
            public List<int>[] Rows;
            public PreparedRing(List<TinBoundaryPoint> points)
            {
                Points = points; MinX = points.Min(p => p.X); MaxX = points.Max(p => p.X);
                MinY = points.Min(p => p.Y); MaxY = points.Max(p => p.Y);
                int count = Math.Min(256, Math.Max(1, (int)Math.Sqrt(points.Count)));
                Rows = Enumerable.Range(0, count).Select(i => new List<int>()).ToArray();
                for (int i = 0; i < points.Count; i++)
                {
                    var a = points[i]; var b = points[(i + 1) % points.Count];
                    for (int row = Row(Math.Min(a.Y, b.Y)); row <= Row(Math.Max(a.Y, b.Y)); row++) Rows[row].Add(i);
                }
            }
            private int Row(double y)
            { return MaxY == MinY ? 0 : Math.Min(Rows.Length - 1, Math.Max(0, (int)((y - MinY) / (MaxY - MinY) * Rows.Length))); }
            public bool Contains(TinBoundaryPoint p, ref long work)
            {
                CheckWork(ref work);
                if (p.X < MinX || p.X > MaxX || p.Y < MinY || p.Y > MaxY) return false;
                bool inside = false;
                foreach (var i in Rows[Row(p.Y)])
                {
                    CheckWork(ref work);
                    var a = Points[i]; var b = Points[(i + 1) % Points.Count];
                    if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X)
                        inside = !inside;
                }
                return inside;
            }
        }

        public static List<TinBoundaryLoop> Build(IList<TinFootprint> surfaces)
        {
            TinBoundaryDependencies.EnsureLoaded();
            var first = surfaces.SelectMany(s => s.Rings).FirstOrDefault(r => r.Count != 0);
            if (first == null) throw new InvalidOperationException("No hay bordes TIN utilizables.");
            var origin = first[0];
            // Perform predicates near the local origin. UTM northings otherwise
            // consume precision when testing extremely close sides of an edge.
            var local = new List<TinFootprint>();
            var sharedVertices = new Dictionary<Tuple<long, long>, List<Node>>();
            int sharedVertexCount = 0;
            foreach (var surface in surfaces)
            {
                var shifted = new TinFootprint { Name = surface.Name };
                foreach (var ring in surface.Rings)
                {
                    var normalized = new List<TinBoundaryPoint>();
                    foreach (var p in ring)
                    {
                        var point = new TinBoundaryPoint(p.X - origin.X, p.Y - origin.Y, p.Z);
                        if (!Finite(point.X) || !Finite(point.Y) || !Finite(point.Z))
                            throw new InvalidOperationException("El TIN contiene coordenadas no finitas.");
                        // Use identical XY for reference vertices already within
                        // numeric tolerance BEFORE splitting and side membership.
                        // Welding only the final graph left almost coincident
                        // surface holes/fills with inconsistent occupancy tests.
                        var canonical = GetNode(sharedVertices, point, ref sharedVertexCount).Point;
                        normalized.Add(new TinBoundaryPoint(canonical.X, canonical.Y, point.Z));
                    }
                    shifted.Rings.Add(normalized);
                }
                local.Add(shifted);
            }
            var loops = TinBoundaryUnion.Build(local);
            foreach (var loop in loops)
            {
                loop.Points = loop.Points.Select(p => new TinBoundaryPoint(p.X + origin.X, p.Y + origin.Y, p.Z)).ToList();
                loop.ReferencePoints = loop.ReferencePoints.Select(p => new TinBoundaryPoint(p.X + origin.X, p.Y + origin.Y, p.Z)).ToList();
            }
            return loops;
        }

        public static TinBoundaryLoop Exterior(IList<TinBoundaryLoop> loops)
        {
            var outer = loops.Where(l => l.Area > 0).ToList();
            if (outer.Count != 1)
                throw new InvalidOperationException("El conjunto tiene " + outer.Count +
                    " contornos exteriores separados. Una sola polilinea exigiria inventar conexiones. Seleccione un conjunto conectado.");
            return outer[0];
        }

        public static TinBoundaryLoop PickEmpty(IList<TinFootprint> surfaces, IList<TinBoundaryLoop> loops, TinBoundaryPoint point)
        {
            foreach (var surface in surfaces)
            foreach (var ring in surface.Rings)
            for (int i = 0; i < ring.Count; i++)
                if (PointSegmentDistance(point, ring[i], ring[(i + 1) % ring.Count]) <= TopologyTolerance * 4)
                    throw new InvalidOperationException("El punto esta sobre un borde. Indique un punto interior del espacio vacio.");
            long work = 0;
            if (Covered(Prepare(surfaces), point, ref work))
                throw new InvalidOperationException("El punto esta dentro de una superficie TIN. Indique el espacio vacio entre superficies.");
            var hole = loops.Where(l => l.Area < 0 && Contains(l.Points, point))
                .OrderBy(l => Math.Abs(l.Area)).FirstOrDefault();
            if (hole == null)
                throw new InvalidOperationException("El espacio no esta cerrado por los bordes TIN. Revise los huecos o el conjunto seleccionado.");
            return hole;
        }

        public static List<TinBoundaryPoint> Simplify(TinBoundaryLoop loop, double tolerance, bool preserveReferenceElevations = false)
        {
            if (!Finite(tolerance) || tolerance < 0) throw new ArgumentOutOfRangeException("tolerance");
            if (loop.ElevationError != null && !preserveReferenceElevations)
                throw new InvalidOperationException(loop.ElevationError);
            var source = preserveReferenceElevations && loop.ReferencePoints != null ? loop.ReferencePoints : loop.Points;
            if (tolerance == 0 || source.Count <= 3) return new List<TinBoundaryPoint>(source);
            // A polygon overlay may start a closed ring halfway along a straight
            // edge. Anchor the cyclic simplification at a real corner instead.
            int start = 0; double bestTurn = -1;
            for (int i = 0; i < source.Count; i++)
            {
                var a = source[(i + source.Count - 1) % source.Count]; var p = source[i]; var b = source[(i + 1) % source.Count];
                double before = Distance(a, p), after = Distance(p, b);
                if (before <= TopologyTolerance || after <= TopologyTolerance) continue;
                double turn = Math.Abs(Cross(p.X - a.X, p.Y - a.Y, b.X - p.X, b.Y - p.Y)) / (before * after);
                if (turn > bestTurn) { bestTurn = turn; start = i; }
            }
            if (start != 0) source = source.Skip(start).Concat(source.Take(start)).ToList();
            int opposite = Enumerable.Range(1, source.Count - 1).OrderByDescending(i => Distance(source[0], source[i])).First();
            var open = new List<TinBoundaryPoint>(source); open.Add(source[0]);
            var keep = new SortedSet<int> { 0, opposite, source.Count };
            var stack = new Stack<Tuple<int, int>>();
            // Keep both endpoints of vertical reference steps, even when their
            // height is smaller than the requested simplification tolerance.
            for (int i = 0; i < source.Count; i++)
            {
                int next = (i + 1) % source.Count;
                if (Distance(source[i], source[next]) <= TopologyTolerance && Math.Abs(source[i].Z - source[next].Z) > ElevationTolerance)
                { keep.Add(i); keep.Add(next); }
            }
            var anchors = keep.ToList();
            for (int i = 1; i < anchors.Count; i++) stack.Push(Tuple.Create(anchors[i - 1], anchors[i]));
            while (stack.Count != 0)
            {
                var range = stack.Pop(); double worst = 1; int index = -1;
                var a = open[range.Item1]; var b = open[range.Item2];
                for (int i = range.Item1 + 1; i < range.Item2; i++)
                {
                    double t = Projection(open[i], a, b);
                    var q = TinBoundaryPoint.Lerp(a, b, t);
                    double error = Math.Max(Distance(open[i], q), Math.Abs(open[i].Z - q.Z)) / tolerance;
                    if (error > worst) { worst = error; index = i; }
                }
                if (index < 0) continue;
                keep.Add(index); stack.Push(Tuple.Create(range.Item1, index)); stack.Push(Tuple.Create(index, range.Item2));
            }
            var result = keep.Where(i => i < source.Count).Select(i => source[i]).ToList();
            if (result.Count < 3 || SignedArea(result) * loop.Area <= 0)
                return new List<TinBoundaryPoint>(source);
            // Restore only chords that cross another chord. Returning the entire
            // original ring discarded all reduction for a single tiny hairpin.
            for (int pass = 0; pass < 32; pass++)
            {
                var indices = keep.Where(i => i < source.Count).ToList();
                var crossings = CrossingSegments(result, indices, source.Count);
                if (crossings.Count == 0) return result;
                bool restored = false;
                foreach (int segment in crossings)
                {
                    int begin = indices[segment], end = indices[(segment + 1) % indices.Count];
                    if (end <= begin) end += source.Count;
                    for (int i = begin + 1; i < end; i++) restored |= keep.Add(i % source.Count);
                }
                if (!restored) break;
                result = keep.Where(i => i < source.Count).Select(i => source[i]).ToList();
            }
            return new List<TinBoundaryPoint>(source);
        }

        private static HashSet<int> CrossingSegments(List<TinBoundaryPoint> points, List<int> sourceIndices, int sourceCount)
        {
            // A vertical edge projects to a point, not a crossing in the XY ring.
            var projected = new List<TinBoundaryPoint>();
            var mapping = new List<int>();
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                if (projected.Count == 0 || Distance(projected[projected.Count - 1], p) > TopologyTolerance)
                { projected.Add(p); mapping.Add(i); }
                else mapping[mapping.Count - 1] = i;
            }
            if (projected.Count > 1 && Distance(projected[0], projected[projected.Count - 1]) <= TopologyTolerance)
                projected.RemoveAt(projected.Count - 1);
            points = projected;
            var crossing = new HashSet<int>();
            long work = 0;
            var segments = Enumerable.Range(0, points.Count).Select(i => new Segment
                { A = points[i], B = points[(i + 1) % points.Count], Index = i }).OrderBy(s => s.MinX).ToList();
            for (int i = 0; i < segments.Count; i++)
            for (int j = i + 1; j < segments.Count && segments[j].MinX <= segments[i].MaxX + TopologyTolerance; j++)
            {
                CheckWork(ref work);
                var a = segments[i]; var b = segments[j];
                int distance = Math.Abs(a.Index - b.Index);
                if (distance == 1 || distance == points.Count - 1) continue;
                if (Math.Max(a.A.Y, a.B.Y) + TopologyTolerance < Math.Min(b.A.Y, b.B.Y) ||
                    Math.Max(b.A.Y, b.B.Y) + TopologyTolerance < Math.Min(a.A.Y, a.B.Y)) continue;
                if (Split(a, b))
                {
                    int sa = mapping[a.Index], sb = mapping[b.Index];
                    // Preserve contacts already present in the reference. Only
                    // shortened chords can introduce a new crossing to restore.
                    bool originalA = (sourceIndices[(sa + 1) % sourceIndices.Count] - sourceIndices[sa] + sourceCount) % sourceCount == 1;
                    bool originalB = (sourceIndices[(sb + 1) % sourceIndices.Count] - sourceIndices[sb] + sourceCount) % sourceCount == 1;
                    if (!originalA) crossing.Add(sa);
                    if (!originalB) crossing.Add(sb);
                }
            }
            return crossing;
        }

        private static Node GetNode(Dictionary<Tuple<long, long>, List<Node>> nodes, TinBoundaryPoint p, ref int count)
        {
            long x = (long)Math.Floor(p.X / TopologyTolerance), y = (long)Math.Floor(p.Y / TopologyTolerance);
            for (long i = x - 1; i <= x + 1; i++)
            for (long j = y - 1; j <= y + 1; j++)
            {
                List<Node> bucket;
                if (nodes.TryGetValue(Tuple.Create(i, j), out bucket))
                    foreach (var node in bucket) if (Distance(p, node.Point) <= TopologyTolerance) return node;
            }
            var key = Tuple.Create(x, y); List<Node> target;
            if (!nodes.TryGetValue(key, out target)) { target = new List<Node>(); nodes.Add(key, target); }
            var added = new Node { Id = count++, Point = p }; target.Add(added); return added;
        }

        private static bool Split(Segment a, Segment b)
        {
            double rx = a.B.X - a.A.X, ry = a.B.Y - a.A.Y;
            double sx = b.B.X - b.A.X, sy = b.B.Y - b.A.Y;
            double qx = b.A.X - a.A.X, qy = b.A.Y - a.A.Y;
            double la = Distance(a.A, a.B), lb = Distance(b.A, b.B);
            double denominator = Cross(rx, ry, sx, sy);
            if (Math.Abs(denominator) > 1e-12 * la * lb)
            {
                double t = Cross(qx, qy, sx, sy) / denominator;
                double u = Cross(qx, qy, rx, ry) / denominator;
                if (t < -TopologyTolerance / la || t > 1 + TopologyTolerance / la ||
                    u < -TopologyTolerance / lb || u > 1 + TopologyTolerance / lb) return false;
                a.Cuts.Add(Clamp(t)); b.Cuts.Add(Clamp(u)); return true;
            }
            if (Math.Abs(Cross(qx, qy, rx, ry)) / la > TopologyTolerance) return false;
            bool hit = false;
            hit |= CutAt(a, b.A); hit |= CutAt(a, b.B); hit |= CutAt(b, a.A); hit |= CutAt(b, a.B);
            return hit;
        }
        private static bool CutAt(Segment segment, TinBoundaryPoint p)
        {
            double dx = segment.B.X - segment.A.X, dy = segment.B.Y - segment.A.Y;
            double t = ((p.X - segment.A.X) * dx + (p.Y - segment.A.Y) * dy) / (dx * dx + dy * dy);
            if (t < 0 || t > 1) return false;
            segment.Cuts.Add(t); return true;
        }
        private static List<List<PreparedRing>> Prepare(IList<TinFootprint> surfaces)
        { return surfaces.Select(s => s.Rings.Select(r => new PreparedRing(r)).ToList()).ToList(); }
        private static bool Covered(List<List<PreparedRing>> surfaces, TinBoundaryPoint point, ref long work)
        {
            foreach (var surface in surfaces)
            {
                bool inside = false;
                foreach (var ring in surface)
                {
                    if (ring.Contains(point, ref work)) inside = !inside;
                }
                if (inside) return true;
            }
            return false;
        }
        internal static bool Contains(IList<TinBoundaryPoint> ring, TinBoundaryPoint p)
        {
            bool inside = false;
            for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
            {
                var a = ring[i]; var b = ring[j];
                if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X)
                    inside = !inside;
            }
            return inside;
        }
        internal static double SignedArea(IList<TinBoundaryPoint> points)
        {
            double area = 0; var origin = points[0]; // Avoid cancellation at UTM coordinates.
            for (int i = 0; i < points.Count; i++)
            {
                var a = points[i]; var b = points[(i + 1) % points.Count];
                area += Cross(a.X - origin.X, a.Y - origin.Y, b.X - origin.X, b.Y - origin.Y);
            }
            return area / 2;
        }
        private static double Projection(TinBoundaryPoint p, TinBoundaryPoint a, TinBoundaryPoint b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y, square = dx * dx + dy * dy;
            return square == 0 ? 0 : Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / square);
        }
        private static double PointSegmentDistance(TinBoundaryPoint p, TinBoundaryPoint a, TinBoundaryPoint b)
        { return Distance(p, TinBoundaryPoint.Lerp(a, b, Projection(p, a, b))); }
        internal static double Distance(TinBoundaryPoint a, TinBoundaryPoint b)
        { double dx = b.X - a.X, dy = b.Y - a.Y; return Math.Sqrt(dx * dx + dy * dy); }
        private static double Cross(double ax, double ay, double bx, double by) { return ax * by - ay * bx; }
        private static double Clamp(double value) { return Math.Max(0, Math.Min(1, value)); }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        private static void CheckWork(ref long work)
        {
            if (++work > MaxWork) throw new InvalidOperationException("El conjunto supera el limite de calculo. Seleccione menos superficies TIN.");
        }
    }
}
