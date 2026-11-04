using System;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CivilSpellAI.Commands
{
    public sealed partial class PolylineOverlapCommands
    {
        [CommandMethod(ReviewCommandName, CommandFlags.Modal)]
        public void ReviewPolylines()
        {
            var doc = AcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;
            ReviewSession session = null;
#if CIVILSPELLAI_OVERLAP2023
            ed.WriteMessage("\nCIVILSPELLAI TRAMOS DWG 2023/2024 - revision 1.3.3.2 (SDK 24.2). Analizar marca; Limpiar/Corregir requieren Si para modificar.");
#endif
            ed.WriteMessage("\nREVISION DE POLILINEAS: analizar, revisar circulos, limpieza controlada con confirmacion y comprobacion automatica.");
            while (true)
            {
                var options = new PromptKeywordOptions("\nQue desea hacer: ") { AllowNone = true };
                foreach (string keyword in new[] { "Analizar", "Revisar", "Ver", "Limpiar", "Corregir", "Comprobar", "Tramos", "Elegir", "Quitarmarcas", "Restaurar", "Restaurarsolapes", "Finalizar", "Salir" }) options.Keywords.Add(keyword);
                options.Keywords.Default = session == null ? "Analizar" : "Salir";
                var answer = ed.GetKeywords(options);
                if ((answer.Status != PromptStatus.OK && answer.Status != PromptStatus.None) || answer.StringResult == "Salir" ||
                    (session != null && string.IsNullOrEmpty(answer.StringResult))) return;
                if (answer.StringResult == "Ver") { ShowMarks(); continue; }
                if (answer.StringResult == "Finalizar")
                {
                    SetMarkerVisibility(doc.Database, "Ocultar");
                    ed.Regen();
                    ed.WriteMessage("\nFinalizado: circulos y textos de diagnostico apagados. Dibujo original visible. Para mostrarlos: " + ShowCommandName + " -> Todas.");
                    return;
                }
                if (answer.StringResult == "Quitarmarcas") { ClearMarkers(); continue; }
                if (answer.StringResult == "Restaurarsolapes")
                {
                    RestoreOverlaps();
                    if (session != null) Analyze(doc, session, false);
                    continue;
                }
                if (answer.StringResult == "Restaurar")
                {
                    RestoreCleanup();
                    if (session != null) Analyze(doc, session, false);
                    continue;
                }
                if (answer.StringResult == "Elegir" || session == null)
                {
                    var requested = PromptReview(ed);
                    if (requested == null) continue;
                    session = requested;
                    Analyze(doc, session, false);
                    if (answer.StringResult != "Limpiar" && answer.StringResult != "Revisar" && answer.StringResult != "Corregir") continue;
                }
                if (answer.StringResult == "Corregir")
                {
                    try { CorrectOverlaps(doc, session); }
                    catch (System.Exception exception)
                    {
                        ed.WriteMessage("\nCorreccion/comprobacion no terminada: {0}. No se certifica el resultado. Salga y use U para deshacer la sesion.", exception.Message);
                        session.Hits.Clear();
                    }
                    continue;
                }
                if (answer.StringResult == "Revisar")
                {
                    if (session.Hits.Count == 0) ed.WriteMessage("\nNo hay hallazgos para recorrer. Use Analizar o Comprobar.");
                    else Navigate(ed, session.Hits, session.Radius);
                    continue;
                }
                if (answer.StringResult == "Tramos")
                {
                    var threshold = ed.GetDouble(new PromptDoubleOptions("\nLongitud maxima del tramo diminuto, en unidades del dibujo: ")
                    { DefaultValue = session.ShortTolerance, UseDefaultValue = true, AllowZero = false, AllowNegative = false });
                    if (threshold.Status != PromptStatus.OK || !Finite(threshold.Value)) continue;
                    if (threshold.Value < session.Tolerance)
                    { ed.WriteMessage("\nEl limite debe ser al menos la tolerancia de solapes ({0:G}).", session.Tolerance); continue; }
                    session.ShortTolerance = threshold.Value;
                    Analyze(doc, session, false);
                    continue;
                }
                if (answer.StringResult != "Limpiar") { Analyze(doc, session, false); continue; }

                var ids = session.Ids.Where(id => id.IsValid && !id.IsErased).ToArray();
                if (ids.Length == 0) { ed.WriteMessage("\nNo quedan polilineas en el conjunto. Use Elegir."); continue; }
                // Refresh the baseline immediately before any cleanup, on exactly this set.
                Analyze(doc, session, false);
                ed.WriteMessage("\nLIMPIEZA CONTROLADA de {0} polilineas. Solapes/colineales: {1:G}; longitud de tramos diminutos: {2:G} unidades, incluido el cierre.", ids.Length, session.Tolerance, session.ShortTolerance);
                ed.WriteMessage("\nLos tramos diminutos rectos se colapsan con un desvio maximo comprobado de {0:G} unidades. Esto puede cambiar ligeramente la forma.", session.ShortTolerance);
                ed.WriteMessage("\nUn quiebre real no demuestra superposicion: se conservan las esquinas fuera de tolerancia y los arcos.");
                ed.WriteMessage("\nConserva entidades, elevacion y extremos. No ejecuta OVERKILL. Use Corregir para solapes entre entidades compatibles.");
                if (session.Incomplete) ed.WriteMessage("\nATENCION: el diagnostico actual es incompleto o contiene geometria invalida.");
                var confirm = new PromptKeywordOptions("\nEjecutar limpieza controlada: ") { AllowNone = true };
                confirm.Keywords.Add("Si"); confirm.Keywords.Add("No"); confirm.Keywords.Default = "No";
                var approval = ed.GetKeywords(confirm);
                if (approval.Status != PromptStatus.OK || approval.StringResult != "Si")
                { ed.WriteMessage("\nLimpieza cancelada. Geometria intacta."); continue; }

                // AutoCAD records this modal command as one undoable operation.
                try
                {
                    var result = SafeCleanup(doc.Database, ids, session.Tolerance, session.ShortTolerance);
                    ed.WriteMessage("\nLIMPIEZA CONTROLADA: {0} polilineas ajustadas; {1} vertices retirados; {2} omitidas por proteccion o validacion.", result.Changed, result.Removed, result.Skipped);
                    ed.WriteMessage("\nDesvio maximo de los vertices retirados respecto al tramo conservado: {0:G} unidades.", result.MaxDisplacement);
                    ed.WriteMessage("\nCOMPROBACION AUTOMATICA DESPUES DE LA LIMPIEZA:");
                    Analyze(doc, session, false);
                    ed.WriteMessage("\nLimpieza y comprobacion terminadas. Use Revisar para pendientes o Restaurar para revertir la ultima limpieza, incluso despues de PAN/ZOOM.");
                    ed.WriteMessage("\nFuera del menu: " + RestoreCommandName + ". Respaldo disponible mientras este dibujo siga abierto.");
                }
                catch (System.Exception exception)
                {
                    ed.WriteMessage("\nLa limpieza/comprobacion no termino: {0}. No se certifica el resultado. Salga y use U para deshacer el grupo.", exception.Message);
                    session.Hits.Clear();
                }
            }
        }
    }
}
