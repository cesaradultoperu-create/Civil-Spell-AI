using System;
using System.Collections.Generic;
using System.Linq;
using CivilSpellAI.Geometry;

namespace CivilSpellAI.Tests
{
    internal static class TinBoundaryGeometryTests
    {
        internal static int ProbeFile(string path, double tolerance = .001)
        {
            try
            {
                var surfaces = new SortedDictionary<int, TinFootprint>();
                foreach (var line in System.IO.File.ReadLines(path).Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var cells = line.Split('\t');
                    int s = int.Parse(cells[0]), r = int.Parse(cells[1]);
                    TinFootprint footprint;
                    if (!surfaces.TryGetValue(s, out footprint)) { footprint = new TinFootprint { Name = "TIN " + s }; surfaces.Add(s, footprint); }
                    while (footprint.Rings.Count <= r) footprint.Rings.Add(new List<TinBoundaryPoint>());
                    footprint.Rings[r].Add(new TinBoundaryPoint(double.Parse(cells[2], System.Globalization.CultureInfo.InvariantCulture),
                        double.Parse(cells[3], System.Globalization.CultureInfo.InvariantCulture), double.Parse(cells[4], System.Globalization.CultureInfo.InvariantCulture)));
                }
                var loops = TinBoundaryGeometry.Build(surfaces.Values.ToList());
                foreach (var loop in loops) Console.WriteLine("LOOP area={0:R}, vertices={1}, Z={2}, junctions={3}, coincident={4}, maxDeltaZ={5:R}",
                    loop.Area, loop.Points.Count, loop.ElevationError ?? "OK", loop.ElevationConflictCount, loop.CoincidentEdgeConflictCount, loop.MaxElevationDifference);
                var exterior = TinBoundaryGeometry.Exterior(loops);
                var simplified = TinBoundaryGeometry.Simplify(exterior, tolerance, true);
                Console.WriteLine("EXTERIOR_OK vertices={0}, area={1:R}", simplified.Count, exterior.Area);
                using (var writer = new System.IO.StreamWriter(path + ".exterior.tsv"))
                {
                    writer.WriteLine("x\ty\tz");
                    foreach (var p in simplified) writer.WriteLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:R}\t{1:R}\t{2:R}", p.X, p.Y, p.Z));
                }
                return 0;
            }
            catch (Exception ex) { Console.WriteLine(ex); return 1; }
        }

        internal static void RunAll()
        {
            var single = new[] { Rectangle(0, 0, 10, 10) };
            var loops = TinBoundaryGeometry.Build(single);
            Require(loops.Count == 1 && Near(loops[0].Area, 100), "Area de un TIN.");
            CheckPlane(loops[0]);
            var adjacent = new[] { Rectangle(0, 0, 10, 10), Rectangle(10, 0, 20, 10) };
            loops = TinBoundaryGeometry.Build(adjacent);
            var simplified = TinBoundaryGeometry.Simplify(TinBoundaryGeometry.Exterior(loops), 1e-6);
            Require(loops.Count == 1 && Near(loops[0].Area, 200) && simplified.Count == 4, "Union y vertices colineales.");
            loops = TinBoundaryGeometry.Build(new[] { Rectangle(0, 0, 10, 10), Rectangle(5, 5, 15, 15) });
            Require(loops.Count == 1 && Near(loops[0].Area, 175), "Intersecciones entre bordes."); CheckPlane(loops[0]);
            loops = TinBoundaryGeometry.Build(new[] { Rectangle(0, 0, 10, 10), Rectangle(10, 3, 15, 7) });
            Require(loops.Count == 1 && Near(loops[0].Area, 120), "Solape colineal parcial en T.");
            loops = TinBoundaryGeometry.Build(new[] { Rectangle(0, 0, 10, 10), Rectangle(2, 2, 5, 5) });
            Require(loops.Count == 1 && Near(loops[0].Area, 100), "Superficie contenida no genera contorno interior.");
            loops = TinBoundaryGeometry.Build(new[] { Rectangle(0, 0, 10, 10), Rectangle(0, 0, 10, 10) });
            Require(loops.Count == 1 && Near(loops[0].Area, 100), "Bordes duplicados.");

            // Sampling both sides at a fixed offset must not jump a nearby edge.
            // This previously turned two closed polygons into an open union graph.
            loops = TinBoundaryGeometry.Build(new[] { Rectangle(0, 0, 1, 1), Rectangle(1.0000002, 0, 2, 1) });
            Require(loops.Count == 2, "Separacion pequena no debe producir un borde abierto artificial.");
            var closeUtm = new[] { Rectangle(579000, 8302000, 579001, 8302001), Rectangle(579001.0000002, 8302000, 579002, 8302001) };
            loops = TinBoundaryGeometry.Build(closeUtm);
            Require(loops.Count == 2, "Bordes proximos en coordenadas UTM.");
            var noisyHole = Rectangle(0, 0, 10, 10);
            var noisyFill = new TinFootprint { Name = "TIN que rellena hueco casi coincidente" };
            var holeRing = new List<TinBoundaryPoint>();
            var fillRing = new List<TinBoundaryPoint>();
            var seamRandom = new Random(739);
            for (int i = 0; i < 128; i++)
            {
                double angle = 2 * Math.PI * i / 128;
                var p = P(5 + 2 * Math.Cos(angle), 5 + 2 * Math.Sin(angle));
                holeRing.Add(p);
                fillRing.Add(new TinBoundaryPoint(p.X + (seamRandom.NextDouble() - .5) * 2e-9,
                    p.Y + (seamRandom.NextDouble() - .5) * 2e-9, p.Z));
            }
            noisyHole.Rings.Add(holeRing); noisyFill.Rings.Add(fillRing);
            loops = TinBoundaryGeometry.Build(new[] { noisyHole, noisyFill });
            Require(loops.Count == 1 && Near(loops[0].Area, 100), "Un hueco y su relleno casi coincidentes no deben abrir artificialmente la union.");

            var frame = new[] { Rectangle(0, 0, 10, 2), Rectangle(0, 8, 10, 10), Rectangle(0, 2, 2, 8), Rectangle(8, 2, 10, 8) };
            loops = TinBoundaryGeometry.Build(frame);
            Require(loops.Count == 2 && Near(loops.Sum(l => l.Area), 64), "Recinto vacio entre cuatro TIN.");
            var hole = TinBoundaryGeometry.PickEmpty(frame, loops, P(5, 5));
            Require(Near(hole.Area, -36), "Pick Points devuelve el espacio vacio."); CheckPlane(hole);
            Throws(() => TinBoundaryGeometry.PickEmpty(frame, loops, P(1, 1)), "Punto dentro de TIN.");
            Throws(() => TinBoundaryGeometry.PickEmpty(frame, loops, P(2, 5)), "Punto sobre borde.");
            Throws(() => TinBoundaryGeometry.PickEmpty(frame, loops, P(15, 5)), "Espacio no cerrado.");
            var opened = frame.Take(3).ToArray();
            Throws(() => TinBoundaryGeometry.PickEmpty(opened, TinBoundaryGeometry.Build(opened), P(5, 5)), "Recinto abierto.");

            var withHole = Rectangle(0, 0, 10, 10);
            withHole.Rings.Add(Rectangle(2, 2, 8, 8).Rings[0]);
            loops = TinBoundaryGeometry.Build(new[] { withHole });
            Require(loops.Count == 2 && Near(loops.Sum(l => l.Area), 64), "Hueco definido en un TIN.");
            loops = TinBoundaryGeometry.Build(new[] { withHole, Rectangle(2, 2, 8, 8) });
            Require(loops.Count == 1 && Near(loops[0].Area, 100), "Otro TIN rellena un hueco.");

            loops = TinBoundaryGeometry.Build(new[] { Rectangle(0, 0, 1, 1), Rectangle(2, 0, 3, 1) });
            Throws(() => TinBoundaryGeometry.Exterior(loops), "No conectar superficies separadas.");
            loops = TinBoundaryGeometry.Build(new[] { Rectangle(0, 0, 1, 1), Rectangle(1, 1, 2, 2) });
            Require(loops.Count == 2, "Contacto en un solo punto conserva dos contornos.");
            Throws(() => TinBoundaryGeometry.Exterior(loops), "No crear polilineas en forma de ocho.");

            var raised = Rectangle(10, 0, 20, 10);
            raised.Rings[0] = raised.Rings[0].Select(p => new TinBoundaryPoint(p.X, p.Y, p.Z + 1)).ToList();
            loops = TinBoundaryGeometry.Build(new[] { Rectangle(0, 0, 10, 10), raised });
            Throws(() => TinBoundaryGeometry.Simplify(TinBoundaryGeometry.Exterior(loops), .001), "Detectar salto Z al cambiar de TIN.");
            var stepped = TinBoundaryGeometry.Simplify(TinBoundaryGeometry.Exterior(loops), .001, true);
            Require(stepped.Count == 8 && Near(TinBoundaryGeometry.SignedArea(stepped), 200), "Conservar ambas cotas y los dos escalones de referencia.");
            int verticalEdges = 0;
            for (int i = 0; i < stepped.Count; i++)
            {
                var a = stepped[i]; var b = stepped[(i + 1) % stepped.Count];
                if (TinBoundaryGeometry.Distance(a, b) < 1e-8 && Math.Abs(a.Z - b.Z) > .99) verticalEdges++;
            }
            Require(verticalEdges == 2, "Los escalones deben ser verticales y conservar su altura.");
            var almostLevel = Rectangle(10, 0, 20, 10);
            almostLevel.Rings[0] = almostLevel.Rings[0].Select(p => new TinBoundaryPoint(p.X, p.Y, p.Z + .000768)).ToList();
            var smallStepLoop = TinBoundaryGeometry.Exterior(TinBoundaryGeometry.Build(new[] { Rectangle(0, 0, 10, 10), almostLevel }));
            var smallStepped = TinBoundaryGeometry.Simplify(smallStepLoop, .05, true);
            Require(smallStepped.Count == 8, "Conservar cotas de referencia incluso si el escalon es menor que la tolerancia de simplificacion.");
            var duplicateRaised = Rectangle(0, 0, 10, 10);
            duplicateRaised.Rings[0] = duplicateRaised.Rings[0].Select(p => new TinBoundaryPoint(p.X, p.Y, p.Z + 1)).ToList();
            loops = TinBoundaryGeometry.Build(new[] { Rectangle(0, 0, 10, 10), duplicateRaised });
            Throws(() => TinBoundaryGeometry.Simplify(loops[0], .001), "Bordes coincidentes con dos cotas posibles.");

            var kink = Rectangle(0, 0, 10, 10);
            kink.Rings[0].Insert(1, new TinBoundaryPoint(5, 0, 110));
            loops = TinBoundaryGeometry.Build(new[] { kink });
            Require(TinBoundaryGeometry.Simplify(loops[0], .001).Count == 5, "Conservar quiebre de pendiente Z en tramo recto XY.");
            Require(TinBoundaryGeometry.Simplify(loops[0], 0).Count == loops[0].Points.Count, "Tolerancia cero.");

            var utm = frame.Select(f => new TinFootprint { Name = f.Name }).ToArray();
            for (int i = 0; i < frame.Length; i++)
                utm[i].Rings.Add(frame[i].Rings[0].Select(p => new TinBoundaryPoint(p.X + 579000, p.Y + 8302000, p.Z)).ToList());
            loops = TinBoundaryGeometry.Build(utm);
            Require(loops.Count == 2 && Near(loops.Sum(l => l.Area), 64), "Precision en coordenadas UTM.");

            var reversed = frame.Select(f => new TinFootprint { Name = f.Name }).ToArray();
            for (int i = 0; i < frame.Length; i++) reversed[i].Rings.Add(frame[i].Rings[0].AsEnumerable().Reverse().ToList());
            loops = TinBoundaryGeometry.Build(reversed);
            Require(loops.Count == 2 && Near(loops.Sum(l => l.Area), 64), "Independiente de orientacion de entrada.");

            var detailed = new TinFootprint { Name = "TIN con 4000 vertices" };
            var sampled = new List<TinBoundaryPoint>();
            var corners = Rectangle(0, 0, 10, 10).Rings[0];
            for (int edge = 0; edge < 4; edge++)
            for (int i = 0; i < 1000; i++)
                sampled.Add(TinBoundaryPoint.Lerp(corners[edge], corners[(edge + 1) % 4], i / 1000.0));
            detailed.Rings.Add(sampled);
            loops = TinBoundaryGeometry.Build(new[] { detailed });
            Require(Near(loops[0].Area, 100) && TinBoundaryGeometry.Simplify(loops[0], .001).Count == 4,
                "Reducir 4000 vertices colineales dentro del limite de calculo.");

            var noisy = Rectangle(0, 0, 10, 10);
            noisy.Rings[0].Insert(1, new TinBoundaryPoint(5, .01, 105.01));
            loops = TinBoundaryGeometry.Build(new[] { noisy });
            simplified = TinBoundaryGeometry.Simplify(loops[0], .05);
            Require(simplified.Count == 4, "Simplificacion controlada de ruido pequeno.");
            foreach (var p in loops[0].Points)
            {
                bool bounded = false;
                for (int i = 0; i < simplified.Count; i++)
                {
                    var a = simplified[i]; var b = simplified[(i + 1) % simplified.Count];
                    double dx = b.X - a.X, dy = b.Y - a.Y;
                    double t = Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy)));
                    var q = TinBoundaryPoint.Lerp(a, b, t);
                    bounded |= TinBoundaryGeometry.Distance(p, q) <= .05 && Math.Abs(p.Z - q.Z) <= .05;
                }
                Require(bounded, "La simplificacion excede la desviacion XY/Z especificada.");
            }

            // Independent area oracle: integer rectangles, occupancy on unit cells.
            var random = new Random(1107);
            for (int test = 0; test < 40; test++)
            {
                var rectangles = new List<TinFootprint>();
                for (int i = 0; i < 5; i++)
                {
                    int x = random.Next(0, 10), y = random.Next(0, 10);
                    rectangles.Add(Rectangle(x, y, x + random.Next(1, 6), y + random.Next(1, 6)));
                }
                int area = 0;
                for (int x = 0; x < 15; x++)
                for (int y = 0; y < 15; y++)
                    if (rectangles.Any(f => TinBoundaryGeometry.Contains(f.Rings[0], P(x + .5, y + .5)))) area++;
                loops = TinBoundaryGeometry.Build(rectangles);
                Require(Near(loops.Sum(l => l.Area), area), "Area union aleatoria " + test);
                foreach (var loop in loops) CheckPlane(loop);
            }
        }

        private static TinBoundaryPoint P(double x, double y) { return new TinBoundaryPoint(x, y, 100 + x + 2 * y); }
        private static TinFootprint Rectangle(double x0, double y0, double x1, double y1)
        {
            var footprint = new TinFootprint { Name = "TIN prueba" };
            footprint.Rings.Add(new List<TinBoundaryPoint> { P(x0, y0), P(x1, y0), P(x1, y1), P(x0, y1) });
            return footprint;
        }
        private static void CheckPlane(TinBoundaryLoop loop)
        { foreach (var p in loop.Points) Require(Near(p.Z, 100 + p.X + 2 * p.Y), "Interpolacion Z en vertices de interseccion."); }
        private static bool Near(double a, double b) { return Math.Abs(a - b) < 1e-6; }
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
        private static void Throws(Action action, string message)
        {
            try { action(); }
            catch (InvalidOperationException) { return; }
            throw new InvalidOperationException("No se rechazo: " + message);
        }
    }
}
