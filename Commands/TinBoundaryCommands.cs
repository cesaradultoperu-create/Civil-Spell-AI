using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CivilSpellAI.Autodesk;
using CivilSpellAI.Geometry;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CivilSpellAI.Commands
{
    public sealed class TinBoundaryCommands
    {
        [CommandMethod("BORDETIN3D", CommandFlags.Modal)]
        public void Create()
        {
            var doc = AcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;
            bool written = false;
            if (doc.Database.TileMode == false)
            { ed.WriteMessage("\nEjecute BORDETIN3D en la ficha Modelo."); return; }
            try
            {
                ed.WriteMessage("\nBORDETIN3D 0.1.3");
                var mode = new PromptKeywordOptions("\nCrear borde TIN: ") { AllowNone = true };
                mode.Keywords.Add("Seleccion"); mode.Keywords.Add("PickPoints", "PickPoints", "Pick Points");
                mode.Keywords.Default = "Seleccion";
                var choice = ed.GetKeywords(mode);
                if (choice.Status != PromptStatus.OK && choice.Status != PromptStatus.None) return;
                bool pick = choice.StringResult == "PickPoints";
                ObjectId[] ids;
                if (pick)
                {
                    var scope = new PromptKeywordOptions("\nTIN que delimitan el espacio: ") { AllowNone = true };
                    scope.Keywords.Add("Visibles"); scope.Keywords.Add("Seleccion"); scope.Keywords.Default = "Visibles";
                    var scopeResult = ed.GetKeywords(scope);
                    if (scopeResult.Status != PromptStatus.OK && scopeResult.Status != PromptStatus.None) return;
                    ids = scopeResult.StringResult == "Seleccion" ? Select(ed) : TinBoundaryDrawingService.VisibleTinIds(doc.Database);
                }
                else ids = Select(ed);
                if (ids == null) return;
                var toleranceResult = ed.GetDouble(new PromptDoubleOptions("\nDesviacion maxima de simplificacion en XY y Z (unidades del dibujo; 0 conserva todos los vertices): ")
                    { DefaultValue = .001, UseDefaultValue = true, AllowNegative = false, AllowZero = true });
                if (toleranceResult.Status != PromptStatus.OK) return;
                double tolerance = toleranceResult.Value;
                if (double.IsNaN(tolerance) || double.IsInfinity(tolerance)) return;
                ed.WriteMessage("\nLeyendo los bordes de {0} superficies TIN...", ids.Length);
                var calculationTime = System.Diagnostics.Stopwatch.StartNew();
                var surfaces = TinBoundaryDrawingService.Read(doc.Database, ids, true,
                    name => ed.WriteMessage("\nTIN '{0}': referencia desactualizada; se usa su geometria almacenada en este dibujo.", name));
                var loops = TinBoundaryGeometry.Build(surfaces);
                var contours = new List<List<TinBoundaryPoint>>();
                int originalCount = 0;
                if (!pick)
                {
                    var loop = TinBoundaryGeometry.Exterior(loops);
                    contours.Add(TinBoundaryGeometry.Simplify(loop, tolerance, true)); originalCount = loop.ReferencePoints.Count;
                    ReportReferenceSteps(ed, loop);
                }
                else
                {
                    var picked = new HashSet<TinBoundaryLoop>();
                    ed.WriteMessage("\nPick Points: indique espacios vacios cerrados entre TIN. Enter genera los bordes; Esc cancela todo.");
                    while (true)
                    {
                        calculationTime.Stop();
                        var point = ed.GetPoint(new PromptPointOptions("\nPunto interior del espacio vacio <Generar>: ") { AllowNone = true });
                        calculationTime.Start();
                        if (point.Status == PromptStatus.None) break;
                        if (point.Status != PromptStatus.OK) return;
                        Point3d wcs = point.Value.TransformBy(ed.CurrentUserCoordinateSystem);
                        try
                        {
                            var loop = TinBoundaryGeometry.PickEmpty(surfaces, loops, new TinBoundaryPoint(wcs.X, wcs.Y, 0));
                            if (picked.Contains(loop)) { ed.WriteMessage("\nEste recinto ya esta incluido."); continue; }
                            var contour = TinBoundaryGeometry.Simplify(loop, tolerance, true);
                            picked.Add(loop); contours.Add(contour); originalCount += loop.ReferencePoints.Count;
                            ReportReferenceSteps(ed, loop);
                            ed.WriteMessage("\nRecinto incluido: {0} vertices, area en planta {1:G8}.", contour.Count, Math.Abs(loop.Area));
                        }
                        catch (InvalidOperationException ex) { ed.WriteMessage("\n{0}", ex.Message); }
                    }
                }
                if (contours.Count == 0) { ed.WriteMessage("\nNo se seleccionaron recintos."); return; }
                var created = TinBoundaryDrawingService.Write(doc.Database, contours);
                calculationTime.Stop();
                written = true;
                ed.SetImpliedSelection(created);
                ed.WriteMessage("\nCREACION CORRECTA. Polilineas 3D cerradas: {0}. Color: MAGENTA. Capa: {1}. Vertices: {2} -> {3}. Desviacion maxima XY/Z: {4:G}.",
                    created.Length, TinBoundaryDrawingService.LayerName, originalCount, contours.Sum(c => c.Count), tolerance);
                ed.WriteMessage("\nTiempo de calculo y creacion: {0:F2} s.", calculationTime.Elapsed.TotalSeconds);
            }
            catch (System.Exception ex)
            { ed.WriteMessage(written ? "\nBORDETIN3D: {0}" : "\nNO SE CREO NINGUNA POLILINEA 3D. BORDETIN3D: {0}", ex.Message); }
        }

        private static ObjectId[] Select(Editor ed)
        {
            var filter = new SelectionFilter(new[] { new TypedValue(0, "AECC_TIN_SURFACE"), new TypedValue(410, "Model") });
            var result = ed.GetSelection(new PromptSelectionOptions { MessageForAdding = "\nSeleccione las superficies TIN: " }, filter);
            return result.Status == PromptStatus.OK ? result.Value.GetObjectIds() : null;
        }

        private static void ReportReferenceSteps(Editor ed, TinBoundaryLoop loop)
        {
            if (loop.CoincidentEdgeConflictCount > 0)
                ed.WriteMessage("\nEn los tramos coincidentes se conserva la cota del primer TIN seleccionado que aporta ese borde.");
            if (loop.ElevationConflictCount > 0)
                ed.WriteMessage("\nCotas de referencia conservadas en {0} encuentros mediante tramos verticales. Diferencia maxima Z: {1:G8} unidades.",
                    loop.ElevationConflictCount, loop.MaxElevationDifference);
        }
    }
}
