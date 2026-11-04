using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using CivilSpellAI.Geometry;

namespace CivilSpellAI.Autodesk
{
    internal sealed class ParametricSectionObjectAnalyzer
    {
        private const double RelativeTolerance = 1e-6;

        public bool TryAnalyze(
            IEnumerable<ObjectId> selectedIds,
            Transaction transaction,
            out TransitionSectionParameters parameters,
            out string error)
        {
            ObjectId referenceId;
            return TryAnalyze(selectedIds, transaction, out parameters, out referenceId, out error);
        }

        public bool TryAnalyze(
            IEnumerable<ObjectId> selectedIds,
            Transaction transaction,
            out TransitionSectionParameters parameters,
            out ObjectId referenceId,
            out string error)
        {
            parameters = null;
            referenceId = ObjectId.Null;
            error = null;
            if (selectedIds == null || transaction == null)
            {
                error = "No se recibió una selección válida.";
                return false;
            }

            ObjectId[] ids = selectedIds.Where(id => !id.IsNull && !id.IsErased).Distinct().ToArray();
            var service = new ParametricSectionDrawingService();
            var references = new HashSet<ObjectId>();
            foreach (ObjectId objectId in ids)
            {
                ObjectId resolved;
                if (service.TryResolveSectionReference(objectId, transaction, out resolved))
                    references.Add(resolved);
            }
            if (references.Count > 1)
            {
                error = "Se seleccionaron varias secciones paramétricas. Seleccione solo una sección o sus cotas asociadas.";
                return false;
            }
            if (references.Count == 1)
            {
                foreach (ObjectId objectId in ids)
                {
                    ObjectId ignored;
                    if (service.TryResolveSectionReference(objectId, transaction, out ignored))
                        continue;
                    Entity entity = transaction.GetObject(objectId, OpenMode.ForRead, false) as Entity;
                    if (entity is BlockReference || entity is Polyline || entity is Hatch || entity is Solid3d)
                    {
                        error = "La selección mezcla una sección paramétrica con otros objetos geométricos. Seleccione un solo bloque o sus cotas asociadas.";
                        return false;
                    }
                }
                referenceId = references.Single();
                return service.TryReadParameters(referenceId, transaction, out parameters);
            }

            var polylines = new List<Polyline>();
            var temporaryObjects = new List<DBObject>();
            Hatch sourceHatch = null;
            try
            {
                var visited = new HashSet<ObjectId>();
                foreach (ObjectId objectId in ids)
                {
                    CollectGeometry(objectId, transaction, visited, polylines,
                        temporaryObjects, ref sourceHatch);
                }
                if (TryAnalyzeGeometry(polylines, sourceHatch, out parameters, out error))
                    return true;

                // Analyze vertical profiles on temporary copies, leaving the drawing intact.
                string horizontalError = error;
                foreach (double angle in new[] { Math.PI / 2.0, -Math.PI / 2.0 })
                {
                    var rotated = new List<Polyline>();
                    foreach (Polyline polyline in polylines)
                    {
                        var copy = (Polyline)polyline.Clone();
                        temporaryObjects.Add(copy);
                        copy.TransformBy(global::Autodesk.AutoCAD.Geometry.Matrix3d.Rotation(
                            angle, global::Autodesk.AutoCAD.Geometry.Vector3d.ZAxis,
                            global::Autodesk.AutoCAD.Geometry.Point3d.Origin));
                        rotated.Add(copy);
                    }
                    TransitionSectionParameters candidate;
                    string candidateError;
                    if (TryAnalyzeGeometry(rotated, sourceHatch, out candidate, out candidateError) &&
                        (parameters == null || candidate.InitialClearHeight < parameters.InitialClearHeight))
                        parameters = candidate;
                }
                error = parameters == null ? horizontalError : null;
                return parameters != null;
            }
            catch (global::Autodesk.AutoCAD.Runtime.Exception exception)
            {
                error = "No se pudo leer la geometría del objeto seleccionado: " + exception.Message;
                return false;
            }
            catch (InvalidOperationException exception)
            {
                error = exception.Message;
                return false;
            }
            finally
            {
                for (int index = temporaryObjects.Count - 1; index >= 0; index--)
                    temporaryObjects[index].Dispose();
            }
        }

        private static void CollectGeometry(
            ObjectId id, Transaction transaction, ISet<ObjectId> visited,
            IList<Polyline> polylines, IList<DBObject> temporaryObjects, ref Hatch sourceHatch)
        {
            if (id.IsNull || id.IsErased || !visited.Add(id))
                return;
            Entity entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
            Hatch hatch = entity as Hatch;
            if (hatch != null && hatch.Associative)
            {
                foreach (ObjectId boundaryId in hatch.GetAssociatedObjectIds())
                    CollectGeometry(boundaryId, transaction, visited, polylines,
                        temporaryObjects, ref sourceHatch);
            }
            CollectEntity(entity, polylines, temporaryObjects, ref sourceHatch, 0);
        }

        private static void CollectEntity(
            Entity entity, IList<Polyline> polylines, IList<DBObject> temporaryObjects,
            ref Hatch sourceHatch, int depth)
        {
            Polyline polyline = entity as Polyline;
            if (polyline != null && polyline.Closed && polyline.NumberOfVertices >= 6)
            {
                for (int index = 0; index < polyline.NumberOfVertices; index++)
                {
                    if (Math.Abs(polyline.GetBulgeAt(index)) > 1e-9)
                        return;
                }
                polylines.Add(polyline);
            }
            else if (entity is Hatch && sourceHatch == null)
                sourceHatch = (Hatch)entity;
            else if (entity is BlockReference)
            {
                if (depth >= 16)
                    throw new InvalidOperationException("El bloque contiene demasiados niveles anidados.");
                using (var objects = new DBObjectCollection())
                {
                    try
                    {
                        entity.Explode(objects);
                    }
                    finally
                    {
                        foreach (DBObject item in objects)
                            temporaryObjects.Add(item);
                    }
                    foreach (DBObject item in objects)
                        CollectEntity(item as Entity, polylines, temporaryObjects,
                            ref sourceHatch, depth + 1);
                }
            }
        }

        private static bool TryAnalyzeGeometry(
            List<Polyline> polylines, Hatch sourceHatch,
            out TransitionSectionParameters parameters, out string error)
        {
            parameters = null;
            error = null;

            if (polylines.Count == 0)
            {
                error = "No se encontraron contornos compatibles. Seleccione un bloque de sección, polilíneas cerradas rectas o hatch con contornos asociados. Un sólido, línea o texto aislado no define los parámetros de una sección.";
                return false;
            }

            double minY = polylines.Min(item => item.GeometricExtents.MinPoint.Y);
            double maxY = polylines.Max(item => item.GeometricExtents.MaxPoint.Y);
            double minX = polylines.Min(item => item.GeometricExtents.MinPoint.X);
            double maxX = polylines.Max(item => item.GeometricExtents.MaxPoint.X);
            double scale = Math.Max(maxX - minX, maxY - minY);
            double tolerance = Math.Max(1e-8, scale * RelativeTolerance);
            double centerY = (minY + maxY) / 2.0;

            Polyline core = polylines
                .Where(item =>
                    item.GeometricExtents.MinPoint.Y < centerY - tolerance &&
                    item.GeometricExtents.MaxPoint.Y > centerY + tolerance)
                .OrderByDescending(item => Math.Abs(item.Area))
                .FirstOrDefault();
            if (core == null)
            {
                error = "No se encontró el contorno central que cruza el eje horizontal de la sección.";
                return false;
            }

            List<Station> stations = ExtractStations(core, centerY, tolerance);
            RemoveCollinearStations(stations, tolerance);
            if (stations.Count != 4 && stations.Count != 5)
            {
                error = string.Format(
                    "Se detectaron {0} estaciones en el contorno central. Esta versión reconoce secciones simétricas de 4 o 5 estaciones formadas por segmentos rectos.",
                    stations.Count);
                return false;
            }

            for (int index = 0; index < core.NumberOfVertices; index++)
            {
                global::Autodesk.AutoCAD.Geometry.Point2d point = core.GetPoint2dAt(index);
                bool hasMirror = false;
                for (int other = 0; other < core.NumberOfVertices; other++)
                {
                    global::Autodesk.AutoCAD.Geometry.Point2d mirror = core.GetPoint2dAt(other);
                    if (Math.Abs(point.X - mirror.X) <= tolerance &&
                        Math.Abs(point.Y + mirror.Y - (2.0 * centerY)) <= tolerance)
                    {
                        hasMirror = true;
                        break;
                    }
                }
                if (!hasMirror)
                {
                    error = "El contorno no es simétrico respecto de su eje. Seleccione una sola sección tipo A o B horizontal o vertical, o su bloque paramétrico.";
                    return false;
                }
            }
            if (Math.Abs(stations[0].HalfHeight - stations[1].HalfHeight) > tolerance ||
                Math.Abs(stations[2].HalfHeight - stations[3].HalfHeight) > tolerance)
            {
                error = "Los tramos rectos del contorno no corresponden a una sección tipo A o B.";
                return false;
            }

            for (int index = 0; index < stations.Count; index++)
            {
                if (stations[index].HalfHeight <= tolerance ||
                    (index > 0 && stations[index].X - stations[index - 1].X <= tolerance))
                {
                    error = "El contorno seleccionado contiene alturas o tramos que no pueden convertirse en parámetros positivos.";
                    return false;
                }
            }

            List<double> offsets = ExtractOffsetsAtStart(
                polylines,
                stations[0].X,
                centerY,
                tolerance);
            double innerThickness;
            double outerThickness;
            InferThicknesses(
                offsets,
                stations[0].HalfHeight,
                tolerance,
                out innerThickness,
                out outerThickness);

            string hatchPattern = sourceHatch == null
                ? "GRAVEL"
                : sourceHatch.PatternName;
            double hatchScale = sourceHatch == null || sourceHatch.PatternScale <= 0.0
                ? 0.20
                : sourceHatch.PatternScale;
            double hatchAngle = sourceHatch == null
                ? 0.0
                : sourceHatch.PatternAngle * 180.0 / Math.PI;
            int hatchColor = sourceHatch == null
                ? 256
                : sourceHatch.ColorIndex;
            if (hatchColor < 0 || hatchColor > 256)
                hatchColor = 256;

            double[] segmentLengths = new double[stations.Count - 1];
            for (int index = 0; index < segmentLengths.Length; index++)
                segmentLengths[index] = stations[index + 1].X - stations[index].X;

            try
            {
                if (stations.Count == 4)
                {
                    parameters = new TransitionSectionParameters(
                        segmentLengths[0],
                        segmentLengths[1],
                        segmentLengths[2],
                        stations[0].HalfHeight * 2.0,
                        stations[3].HalfHeight * 2.0,
                        innerThickness,
                        outerThickness,
                        hatchPattern,
                        hatchScale,
                        hatchAngle,
                        hatchColor);
                }
                else
                {
                    parameters = new TransitionSectionParameters(
                        segmentLengths[0],
                        segmentLengths[1],
                        segmentLengths[2],
                        stations[0].HalfHeight * 2.0,
                        ((stations[2].HalfHeight + stations[3].HalfHeight) / 2.0) * 2.0,
                        innerThickness,
                        outerThickness,
                        hatchPattern,
                        hatchScale,
                        hatchAngle,
                        hatchColor,
                        TransitionSectionType.TypeB,
                        segmentLengths[3],
                        (stations[4].HalfHeight * 2.0) +
                            (2.0 * outerThickness));
                }
            }
            catch (ArgumentException exception)
            {
                error = "No se pudo convertir la geometría seleccionada: " + exception.Message;
                parameters = null;
                return false;
            }

            return true;
        }

        private static List<Station> ExtractStations(
            Polyline core,
            double centerY,
            double tolerance)
        {
            var result = new List<Station>();
            for (int index = 0; index < core.NumberOfVertices; index++)
            {
                global::Autodesk.AutoCAD.Geometry.Point2d point = core.GetPoint2dAt(index);
                double halfHeight = point.Y - centerY;
                if (halfHeight <= tolerance)
                    continue;

                Station existing = result.FirstOrDefault(
                    item => Math.Abs(item.X - point.X) <= tolerance);
                if (existing == null)
                {
                    result.Add(new Station(point.X, halfHeight));
                }
                else if (halfHeight < existing.HalfHeight)
                {
                    existing.HalfHeight = halfHeight;
                }
            }

            result.Sort((left, right) => left.X.CompareTo(right.X));
            return result;
        }

        private static void RemoveCollinearStations(
            IList<Station> stations,
            double tolerance)
        {
            for (int index = stations.Count - 2; index > 0; index--)
            {
                Station previous = stations[index - 1];
                Station current = stations[index];
                Station next = stations[index + 1];
                double cross =
                    ((current.X - previous.X) * (next.HalfHeight - previous.HalfHeight)) -
                    ((current.HalfHeight - previous.HalfHeight) * (next.X - previous.X));
                if (Math.Abs(cross) <= tolerance * tolerance)
                    stations.RemoveAt(index);
            }
        }

        private static List<double> ExtractOffsetsAtStart(
            IEnumerable<Polyline> polylines,
            double startX,
            double centerY,
            double tolerance)
        {
            var offsets = new List<double>();
            foreach (Polyline polyline in polylines)
            {
                for (int index = 0; index < polyline.NumberOfVertices; index++)
                {
                    global::Autodesk.AutoCAD.Geometry.Point2d point =
                        polyline.GetPoint2dAt(index);
                    if (Math.Abs(point.X - startX) > tolerance || point.Y <= centerY)
                        continue;

                    double offset = point.Y - centerY;
                    if (!offsets.Any(value => Math.Abs(value - offset) <= tolerance))
                        offsets.Add(offset);
                }
            }

            offsets.Sort();
            return offsets;
        }

        private static void InferThicknesses(
            IList<double> offsets,
            double clearHalfHeight,
            double tolerance,
            out double innerThickness,
            out double outerThickness)
        {
            List<double> aboveClear = offsets
                .Where(value => value > clearHalfHeight + tolerance)
                .ToList();
            if (aboveClear.Count >= 2)
            {
                innerThickness = aboveClear[0] - clearHalfHeight;
                outerThickness = aboveClear[1] - aboveClear[0];
                return;
            }

            if (aboveClear.Count == 1)
            {
                double combined = aboveClear[0] - clearHalfHeight;
                innerThickness = combined * 0.75;
                outerThickness = combined * 0.25;
                return;
            }

            double fallback = Math.Max(0.01, clearHalfHeight * 0.50);
            innerThickness = fallback * 0.75;
            outerThickness = fallback * 0.25;
        }

        private sealed class Station
        {
            public Station(double x, double halfHeight)
            {
                X = x;
                HalfHeight = halfHeight;
            }

            public double X { get; private set; }

            public double HalfHeight { get; set; }
        }
    }
}
