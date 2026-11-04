using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
using CivilSpellAI.Geometry;

namespace CivilSpellAI.UI
{
    internal static class Figure3DPreviewMesh
    {
        // Partition at every wall and opening boundary, then emit only exposed faces.
        // Each face owns its vertices and normal: coplanar cells have identical shading.
        internal static MeshGeometry3D Build(Figure3DParameters p)
        {
            p.Validate();
            var xs = new[] { 0, p.LeftWall, p.Width - p.RightWall, p.Width };
            var ys = new List<double> { 0, p.FrontWall, p.Depth - p.BackWall, p.Depth };
            var zs = new List<double> { 0, p.BaseThickness, p.Height };
            foreach (var passage in new[] { p.Left, p.Right }.Where(item => item.Enabled))
            {
                ys.Add(passage.Center - passage.Width / 2); ys.Add(passage.Center + passage.Width / 2);
                zs.Add(passage.Bottom); zs.Add(passage.Bottom + passage.Height);
            }
            double[] y = ys.Distinct().OrderBy(v => v).ToArray(), z = zs.Distinct().OrderBy(v => v).ToArray();
            var cells = new bool[xs.Length - 1, y.Length - 1, z.Length - 1];
            for (int i = 0; i < xs.Length - 1; i++)
                for (int j = 0; j < y.Length - 1; j++)
                    for (int k = 0; k < z.Length - 1; k++)
                    {
                        double mx = (xs[i] + xs[i + 1]) / 2, my = (y[j] + y[j + 1]) / 2, mz = (z[k] + z[k + 1]) / 2;
                        cells[i, j, k] = mz < p.BaseThickness ||
                            ((mx < p.LeftWall || mx > p.Width - p.RightWall || my < p.FrontWall || my > p.Depth - p.BackWall) &&
                            !(mx < p.LeftWall && Inside(p.Left, my, mz)) &&
                            !(mx > p.Width - p.RightWall && Inside(p.Right, my, mz)));
                    }
            var mesh = new MeshGeometry3D();
            for (int i = 0; i < xs.Length - 1; i++)
                for (int j = 0; j < y.Length - 1; j++)
                    for (int k = 0; k < z.Length - 1; k++)
                    {
                        if (!cells[i, j, k]) continue;
                        var a = new Point3D(xs[i], y[j], z[k]); var b = new Point3D(xs[i + 1], y[j], z[k]);
                        var c = new Point3D(xs[i + 1], y[j + 1], z[k]); var d = new Point3D(xs[i], y[j + 1], z[k]);
                        var e = new Point3D(xs[i], y[j], z[k + 1]); var f = new Point3D(xs[i + 1], y[j], z[k + 1]);
                        var g = new Point3D(xs[i + 1], y[j + 1], z[k + 1]); var h = new Point3D(xs[i], y[j + 1], z[k + 1]);
                        if (!Filled(cells, i - 1, j, k)) Face(mesh, a, e, h, d);
                        if (!Filled(cells, i + 1, j, k)) Face(mesh, b, c, g, f);
                        if (!Filled(cells, i, j - 1, k)) Face(mesh, a, b, f, e);
                        if (!Filled(cells, i, j + 1, k)) Face(mesh, d, h, g, c);
                        if (!Filled(cells, i, j, k - 1)) Face(mesh, a, d, c, b);
                        if (!Filled(cells, i, j, k + 1)) Face(mesh, e, f, g, h);
                    }
            mesh.Freeze();
            return mesh;
        }

        private static bool Inside(Figure3DPassage passage, double y, double z)
        {
            return passage.Enabled && y > passage.Center - passage.Width / 2 && y < passage.Center + passage.Width / 2 &&
                z > passage.Bottom && z < passage.Bottom + passage.Height;
        }

        internal static MeshGeometry3D BuildEdges(MeshGeometry3D surface, double thickness)
        {
            var edges = new Dictionary<Tuple<Point3D, Point3D>, List<Vector3D>>();
            for (int first = 0; first < surface.Positions.Count; first += 4)
                for (int corner = 0; corner < 4; corner++)
                {
                    Point3D a = surface.Positions[first + corner], b = surface.Positions[first + (corner + 1) % 4];
                    if (a.X > b.X || (a.X == b.X && (a.Y > b.Y || (a.Y == b.Y && a.Z > b.Z))))
                    { Point3D swap = a; a = b; b = swap; }
                    var key = Tuple.Create(a, b);
                    List<Vector3D> normals;
                    if (!edges.TryGetValue(key, out normals)) edges[key] = normals = new List<Vector3D>();
                    normals.Add(surface.Normals[first]);
                }
            var mesh = new MeshGeometry3D();
            double radius = thickness / 2;
            foreach (var edge in edges)
            {
                // Omit subdivision seams; show only actual corners and opening rims.
                if (edge.Value.All(n => Vector3D.DotProduct(n, edge.Value[0]) > 0.99)) continue;
                Point3D low = edge.Key.Item1, high = edge.Key.Item2;
                var a = new Point3D(low.X - radius, low.Y - radius, low.Z - radius);
                var b = new Point3D(high.X + radius, low.Y - radius, low.Z - radius);
                var c = new Point3D(high.X + radius, high.Y + radius, low.Z - radius);
                var d = new Point3D(low.X - radius, high.Y + radius, low.Z - radius);
                var e = new Point3D(low.X - radius, low.Y - radius, high.Z + radius);
                var f = new Point3D(high.X + radius, low.Y - radius, high.Z + radius);
                var g = new Point3D(high.X + radius, high.Y + radius, high.Z + radius);
                var h = new Point3D(low.X - radius, high.Y + radius, high.Z + radius);
                Face(mesh, a, e, h, d); Face(mesh, b, c, g, f);
                Face(mesh, a, b, f, e); Face(mesh, d, h, g, c);
                Face(mesh, a, d, c, b); Face(mesh, e, f, g, h);
            }
            mesh.Freeze();
            return mesh;
        }

        private static bool Filled(bool[,,] cells, int x, int y, int z)
        {
            return x >= 0 && y >= 0 && z >= 0 && x < cells.GetLength(0) && y < cells.GetLength(1) && z < cells.GetLength(2) && cells[x, y, z];
        }

        private static void Face(MeshGeometry3D mesh, Point3D a, Point3D b, Point3D c, Point3D d)
        {
            int first = mesh.Positions.Count;
            Vector3D normal = Vector3D.CrossProduct(b - a, c - a); normal.Normalize();
            foreach (var point in new[] { a, b, c, d }) { mesh.Positions.Add(point); mesh.Normals.Add(normal); }
            foreach (int index in new[] { 0, 1, 2, 0, 2, 3 }) mesh.TriangleIndices.Add(first + index);
        }
    }
}
