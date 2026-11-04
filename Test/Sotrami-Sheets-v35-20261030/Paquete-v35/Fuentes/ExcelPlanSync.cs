using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;

public partial class ExcelPlanSync
{
    static string Root => PackagePaths.Root;
    static string Workbook => PackagePaths.WorkbookFile;
    static readonly JsonCodec Json = new JsonCodec();
    public static List<Plan> ReadPlans() => ReadPlans(Workbook);
    static void Guard(Database db)
    {
        if (!String.Equals(Path.GetFullPath(db.Filename), Path.GetFullPath(PackagePaths.DrawingFile), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Abra el dibujo configurado para actualizar: " + PackagePaths.DrawingFile);
    }
    static Table OpenTable(Database db, Transaction tr, OpenMode mode) => (Table)tr.GetObject(db.GetObjectId(false, new Handle(0xDA1B), 0), mode);

    [CommandMethod("SOTRAMIINFO", CommandFlags.Modal)]
    public void Info()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        if (doc == null) return;
        doc.Editor.WriteMessage("\nSOTRAMI_CARGADO: " + typeof(ExcelPlanSync).Assembly.GetName().Name + "\nPORTADA: " + PackagePaths.DrawingFile + "\nUSE SOTRAMIEXCEL EN LA PORTADA DESPUES DE GUARDAR EL EXCEL.\n");
    }

    [CommandMethod("SOTRAMIDIAGNOSTICO", CommandFlags.Modal)]
    public void Diagnose()
    {
        var plans = ReadPlans();
        var registered = SheetSetSync.RegisteredCodes();
        File.WriteAllText(Path.Combine(PackagePaths.LogDirectory, "diagnostico.json"), Json.Serialize(new {
            assembly = typeof(ExcelPlanSync).Assembly.GetName().Name,
            plans = plans.Count,
            additions = plans.Where(p => !registered.Contains(p.Code)).Select(p => p.Code).ToArray(),
            retirements = SheetSetSync.Retirements(new HashSet<string>(plans.Select(p => p.Code), StringComparer.OrdinalIgnoreCase)).Select(p => p.Code).ToArray(),
            missingDrawings = plans.Where(p => !File.Exists(Path.Combine(PackagePaths.ProjectDirectory, p.Code + ".dwg")) && !File.Exists(Path.Combine(PackagePaths.ProjectDirectory, "Old", p.Code + ".dwg"))).Select(p => p.Code).ToArray()
        }));
        Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nSOTRAMI_DIAGNOSTICO_OK: " + plans.Count + " planos del Excel.\n");
    }

    [CommandMethod("SOTRAMIEXCEL", CommandFlags.Session)]
    public void Update()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        try
        {
            Guard(doc.Database);
            using (DrawingRenumber.AcquireForUpdate())
            {
            var plans = ReadPlans();
            var retired = DrawingRetirement.Run(plans);
            using (doc.LockDocument())
            {
            var created = NewDrawingSync.Prepare(plans);
            UpdateTableFromPlans(doc, plans);
            var code = SheetSetSync.Execute(true);
            if (code != 0) throw new InvalidOperationException("La tabla se actualizo, pero uno de los conjuntos tiene pendientes. Consulte sheetset-summary.json, team-sheetset-summary.json y sheetset-error.txt. Para un DWG nuevo, importe su presentacion en SOTRAMI o abra el DWG antes de actualizar.");
            NewDrawingSync.LinkCreated(created);
            Verify();
            var titleBlocks = TitleBlockSync.RefreshOpenDrawings();
            if (titleBlocks.Any(p => !p.Success)) throw new InvalidOperationException("Hay rotulos abiertos pendientes; consulte Registros/rotulos-abiertos.json.");
            doc.Editor.Regen();
            if (Application.DocumentManager.IsApplicationContext)
                doc.SendStringToExecute("SOTRAMICONJUNTO\n", true, false, false);
            else SheetSetDisplay.Show();
            doc.Editor.WriteMessage("\nACTUALIZACION_PLANOS_OK: tabla, conjunto y rotulos abiertos sincronizados con Excel. PLANOS NUEVOS CREADOS Y VINCULADOS: " + created.Count + ". PLANOS RETIRADOS Y ARCHIVADOS: " + retired + ". Los rotulos vinculados de dibujos cerrados se actualizaran al abrir o regenerar. Guarde los DWG modificados.\n");
            File.WriteAllText(Path.Combine(PackagePaths.LogDirectory, "sync-status.json"), Json.Serialize(new { success = true, plans = plans.Count, workbook = Workbook, drawing = PackagePaths.DrawingFile, sheetSet = PackagePaths.TeamSheetSetFile, compatibilitySheetSet = PackagePaths.SheetSetFile, assembly = typeof(ExcelPlanSync).Assembly.GetName().Name }));
            }
        }
            }
        catch (System.Exception ex)
        {
            File.WriteAllText(Path.Combine(PackagePaths.LogDirectory, "sync-status.json"), Json.Serialize(new { success = false, error = ex.ToString() }));
            doc.Editor.WriteMessage("\nACTUALIZACION_PLANOS_FAILED: " + ex.Message + "\n");
        }
    }

    [CommandMethod("SOTRAMIETAPAS", CommandFlags.Modal)]
    public void OrganizeStages()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        try
        {
            using (DrawingRenumber.AcquireForUpdate())
            {
                SheetSetSync.OrganizeFinalStages();
                if (SheetSetSync.Execute(false) != 0) throw new InvalidOperationException("El conjunto tiene pendientes; consulte sheetset-summary.json.");
                SheetSetDisplay.Show();
                doc.Editor.WriteMessage("\nSOTRAMI_ETAPAS_OK: planos finales en " + SheetSetSync.FinalStageSubset + ".\n");
            }
        }
        catch (System.Exception ex)
        {
            File.WriteAllText(Path.Combine(PackagePaths.LogDirectory, "etapas-error.txt"), ex.ToString());
            doc.Editor.WriteMessage("\nSOTRAMI_ETAPAS_FAILED: " + ex.Message + "\n");
        }
    }

    internal static void UpdateTableFromPlans(Document doc, List<Plan> plans)
    {
        Guard(doc.Database);
            var rows = Rows(plans);
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var t = OpenTable(doc.Database, tr, OpenMode.ForWrite);
                if (t.Columns.Count != 5 || t.Rows.Count < 5) throw new InvalidOperationException("Formato de tabla inesperado.");
                var styles = Enumerable.Range(0, 5).Select(c => t.Cells[4, c].TextStyleId).ToArray();
                var heights = Enumerable.Range(0, 5).Select(c => t.Cells[4, c].TextHeight).ToArray();
                var alignments = Enumerable.Range(0, 5).Select(c => t.Cells[4, c].Alignment).ToArray();
                var colors = Enumerable.Range(0, 5).Select(c => t.Cells[4, c].ContentColor).ToArray();
                var owner = (BlockTableRecord)tr.GetObject(t.OwnerId, OpenMode.ForRead);
                if (((Layout)tr.GetObject(owner.LayoutId, OpenMode.ForRead)).LayoutName != "A") throw new InvalidOperationException("La tabla debe estar en A.");
                t.UnmergeCells(CellRange.Create(t, 3, 0, t.Rows.Count - 1, 4));
                if (t.Rows.Count < rows.Count) t.InsertRows(t.Rows.Count, 5, rows.Count - t.Rows.Count);
                else if (t.Rows.Count > rows.Count) t.DeleteRows(rows.Count, t.Rows.Count - rows.Count);
                for (int row = 3; row < rows.Count; row++)
                {
                    for (int col = 0; col < 5; col++)
                    {
                        var cell = t.Cells[row, col];
                        cell.TextStyleId = styles[col]; cell.TextHeight = heights[col]; cell.Alignment = alignments[col]; cell.ContentColor = colors[col];
                        cell.TextString = rows[row][col];
                    }
                    t.Rows[row].Height = 5;
                }
                if (t.ExtensionDictionary.IsNull) t.CreateExtensionDictionary();
                var dictionary = (DBDictionary)tr.GetObject(t.ExtensionDictionary, OpenMode.ForWrite);
                const string key = "SOTRAMI_EXCEL_SOURCE";
                Xrecord record;
                if (dictionary.Contains(key)) record = (Xrecord)tr.GetObject(dictionary.GetAt(key), OpenMode.ForWrite);
                else { record = new Xrecord(); dictionary.SetAt(key, record); tr.AddNewlyCreatedDBObject(record, true); }
                record.Data = new ResultBuffer(new TypedValue((int)DxfCode.Text, Workbook), new TypedValue((int)DxfCode.Text, "LE"), new TypedValue((int)DxfCode.Text, PackagePaths.SheetSetFile), new TypedValue((int)DxfCode.Text, "REVISION VACIA=A; NUMERACION POR GRUPO DESDE 1; TEXTOS EN MAYUSCULAS"));
                for (int r = 0; r < t.Rows.Count; r++) for (int c = 0; c < t.Columns.Count; c++)
                {
                    var borders = t.Cells[r, c].Borders;
                    foreach (var border in new[] { borders.Top, borders.Bottom, borders.Left, borders.Right })
                    {
                        border.IsVisible = true;
                        border.Color = Color.FromRgb(140, 140, 140);
                        border.LineWeight = LineWeight.LineWeight013;
                    }
                }
                t.GenerateLayout();
                for (int r = 3; r < t.Rows.Count; r++) t.Rows[r].Height = Math.Max(5, t.Rows[r].MinimumHeight + 0.6);
                t.GenerateLayout();
                t.RecomputeTableBlock(true);
                tr.Commit();
            }
    }

    [CommandMethod("VERIFICARSOTRAMIEXCEL", CommandFlags.Modal)]
    public void Verify()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        Guard(doc.Database);
        var plans = ReadPlans();
        var rows = Rows(plans);
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            var table = OpenTable(doc.Database, tr, OpenMode.ForRead);
            if (table.Rows.Count != rows.Count || table.Columns.Count != 5) throw new InvalidOperationException("Dimensiones incorrectas.");
            for (int r = 3; r < rows.Count; r++) for (int c = 0; c < 5; c++) if (table.Cells[r, c].TextString != rows[r][c]) throw new InvalidOperationException("Diferencia Excel/tabla en " + r + "," + c);
            if (table.ExtensionDictionary.IsNull) throw new InvalidOperationException("Falta la referencia al Excel.");
            var dictionary = (DBDictionary)tr.GetObject(table.ExtensionDictionary, OpenMode.ForRead);
            var record = (Xrecord)tr.GetObject(dictionary.GetAt("SOTRAMI_EXCEL_SOURCE"), OpenMode.ForRead);
            if ((string)record.Data.AsArray()[0].Value != Workbook) throw new InvalidOperationException("La ruta de la fuente es diferente.");
            File.WriteAllText(Path.Combine(PackagePaths.LogDirectory, "verification.json"), Json.Serialize(new { success = true, plans = plans.Count, rows = rows.Count, width = table.Width, height = table.Height, rowHeights = Enumerable.Range(0, table.Rows.Count).Select(r => table.Rows[r].Height).ToArray(), minimumHeights = Enumerable.Range(0, table.Rows.Count).Select(r => table.Rows[r].MinimumHeight).ToArray(), gridVisible = table.Cells[4, 2].Borders.Top.IsVisible, source = Workbook, table = rows }));
            tr.Commit();
        }
        doc.Editor.WriteMessage("\nVINCULO_PLANOS_VERIFICADO: " + plans.Count + " entradas identicas al Excel.\n");
        if (SheetSetSync.Execute(false) != 0) throw new InvalidOperationException("El conjunto no coincide con el Excel.");
    }

    [CommandMethod("VISTASOTRAMIEXCEL", CommandFlags.Modal)]
    public void Preview()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        Guard(doc.Database);
        LayoutManager.Current.CurrentLayout = "A";
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        using (var settings = new PlotSettings(false))
        {
            var layout = (Layout)tr.GetObject(LayoutManager.Current.GetLayoutId("A"), OpenMode.ForRead);
            var table = OpenTable(doc.Database, tr, OpenMode.ForRead);
            settings.CopyFrom(layout);
            var v = PlotSettingsValidator.Current;
            v.SetPlotConfigurationName(settings, "DWG To PDF.pc3", "ISO_full_bleed_A3_(297.00_x_420.00_MM)");
            v.RefreshLists(settings);
            v.SetPlotRotation(settings, PlotRotation.Degrees000);
            v.SetPlotWindowArea(settings, new Extents2d(table.Position.X - 2, table.Position.Y - table.Height - 2, table.Position.X + table.Width + 2, table.Position.Y + 2));
            v.SetPlotType(settings, Autodesk.AutoCAD.DatabaseServices.PlotType.Window);
            v.SetUseStandardScale(settings, true);
            v.SetStdScaleType(settings, StdScaleType.ScaleToFit);
            v.SetPlotCentered(settings, true);
            using (var info = new PlotInfo())
            {
                info.Layout = layout.ObjectId;
                info.OverrideSettings = settings;
                using (var iv = new PlotInfoValidator()) { iv.MediaMatchingPolicy = MatchingPolicy.MatchEnabled; iv.Validate(info); }
                using (var engine = PlotFactory.CreatePublishEngine())
                {
                    engine.BeginPlot(null, null);
                    engine.BeginDocument(info, doc.Name, null, 1, true, Path.Combine(Root, "Vista previa - lista actualizada.pdf"));
                    using (var page = new PlotPageInfo()) { engine.BeginPage(page, info, true, null); engine.BeginGenerateGraphics(null); engine.EndGenerateGraphics(null); engine.EndPage(null); }
                    engine.EndDocument(null); engine.EndPlot(null);
                }
            }
            tr.Commit();
        }
        doc.Editor.WriteMessage("\nVISTA_SOTRAMI_EXCEL_OK\n");
    }
}
