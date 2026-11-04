using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;

public class PlanListUpdate
{
    [CommandMethod("INSPECTPLANLINKAPI")]
    public void InspectApi()
    {
        var types = new[] { typeof(DataLink), typeof(DataLinkManager), typeof(Cell), typeof(CellRange), typeof(UpdateOption), typeof(UpdateDirection), typeof(DataLinkOption) };
        var lines = new List<string>();
        foreach (var type in types)
        {
            lines.Add(type.FullName);
            if (type.IsEnum) lines.Add(String.Join(",", Enum.GetNames(type)));
            else foreach (var m in type.GetMembers().Where(m => m.Name.IndexOf("DataLink", StringComparison.OrdinalIgnoreCase) >= 0 || type == typeof(DataLink) || type == typeof(DataLinkManager))) lines.Add(m.ToString());
        }
        File.WriteAllLines(Path.Combine(Root, "datalink-api.txt"), lines);
        Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nPLAN_LINK_API_OK\n");
    }
    public class Payload
    {
        public string handle { get; set; }
        public string[][] rows { get; set; }
        public int[] section_rows { get; set; }
        public int[] plan_rows { get; set; }
    }
    static string Root => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
    static Payload ReadPayload() => Json.Deserialize<Payload>(File.ReadAllText(Path.Combine(Root, "update-payload.json")));
    const string ExcelSource = @"Q:\Proyectos 2026\1107.001.00.16 - Pad SOTRAMI\05_Drawing\1107.001.00.16-17-LE-001-RB - Lista de entregables.xlsx";
    public class LinkSpec
    {
        public string name { get; set; }
        public string range { get; set; }
        public int row { get; set; }
        public int col { get; set; }
    }
    static void Guard(Database db)
    {
        if (!String.Equals(Path.GetFullPath(db.Filename), Path.Combine(Root, "1107.001.00.14-200-00-P-001 - REVISION EXCEL.dwg"), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Solo se permite modificar la copia de revision del espacio de trabajo.");
    }

    [CommandMethod("UPDATEPLANLISTCOPY", CommandFlags.Modal)]
    public void Update()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        try
        {
            var db = doc.Database;
            Guard(db);
            var data = ReadPayload();
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var table = (Table)tr.GetObject(db.GetObjectId(false, new Handle(Convert.ToInt64(data.handle, 16)), 0), OpenMode.ForWrite);
                var owner = (BlockTableRecord)tr.GetObject(table.OwnerId, OpenMode.ForRead);
                var layout = (Layout)tr.GetObject(owner.LayoutId, OpenMode.ForRead);
                if (layout.LayoutName != "A" || table.Rows.Count != 57 || table.Columns.Count != 5)
                    throw new InvalidOperationException("La tabla no coincide con el listado inspeccionado.");
                var styles = Enumerable.Range(0, 5).Select(c => table.Cells[17, c].TextStyleId).ToArray();
                var heights = Enumerable.Range(0, 5).Select(c => table.Cells[17, c].TextHeight).ToArray();
                var aligns = Enumerable.Range(0, 5).Select(c => table.Cells[17, c].Alignment).ToArray();
                var colors = Enumerable.Range(0, 5).Select(c => table.Cells[17, c].ContentColor).ToArray();
                table.UnmergeCells(CellRange.Create(table, 3, 0, table.Rows.Count - 1, 4));
                if (table.Rows.Count < data.rows.Length)
                    table.InsertRows(table.Rows.Count, 5.0, data.rows.Length - table.Rows.Count);
                else if (table.Rows.Count > data.rows.Length)
                    table.DeleteRows(data.rows.Length, table.Rows.Count - data.rows.Length);
                for (int r = 3; r < data.rows.Length; r++)
                {
                    for (int c = 0; c < 5; c++)
                    {
                        var cell = table.Cells[r, c];
                        cell.TextStyleId = styles[c];
                        cell.TextHeight = heights[c];
                        cell.Alignment = aligns[c];
                        cell.ContentColor = colors[c];
                        cell.TextString = data.rows[r][c];
                    }
                    table.Rows[r].Height = 5.0;
                }
                // A relative raster path would otherwise break after moving the DWG copy.
                foreach (ObjectId id in owner)
                {
                    var image = tr.GetObject(id, OpenMode.ForRead) as RasterImage;
                    if (image == null || image.ImageDefId.IsNull) continue;
                    var def = (RasterImageDef)tr.GetObject(image.ImageDefId, OpenMode.ForRead);
                    if (def.SourceFileName.Replace('/', '\\').Equals(@".\Xref\02_24.jpg", StringComparison.OrdinalIgnoreCase))
                    {
                        var original = @"Q:\Proyectos 2026\1107.001.00.16 - Pad SOTRAMI\05_Drawing\Xref\02_24.jpg";
                        if (File.Exists(original)) { def.UpgradeOpen(); def.SourceFileName = original; }
                    }
                }
                table.GenerateLayout();
                tr.Commit();
            }
            Verify();
            doc.Editor.WriteMessage("\nPLAN_LIST_UPDATE_OK\n");
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage("\nPLAN_LIST_UPDATE_FAILED: " + ex + "\n");
            throw;
        }
    }

    [CommandMethod("LINKPLANLISTCOPY", CommandFlags.Modal)]
    public void LinkCopy()
    {
        try { LinkCore(); File.WriteAllText(Path.Combine(Root, "link-status.json"), "{\"success\":true}"); }
        catch (System.Exception ex) { File.WriteAllText(Path.Combine(Root, "link-status.json"), Json.Serialize(new { success = false, error = ex.ToString() })); Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nPLAN_LIST_LINK_FAILED: " + ex.Message); }
    }
    void LinkCore()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        var db = doc.Database;
        Guard(db);
        File.AppendAllText(Path.Combine(Root, "link-trace.txt"), "Begin link\n");
        var specs = Json.Deserialize<LinkSpec[]>(File.ReadAllText(Path.Combine(Root, "link-specs.json")));
        foreach (var spec in specs)
        {
            ObjectId linkId;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                linkId = db.DataLinkManager.GetDataLink(spec.name);
                if (linkId.IsNull)
                {
                    var link = new DataLink { DataAdapterId = "AcExcel", Name = spec.name, Description = "Fuente oficial: hoja LE; solo lectura desde Excel", ConnectionString = ExcelSource + "!LE!" + spec.range, DataLinkOption = DataLinkOption.PersistCache, UpdateOption = (int)UpdateOption.SkipFormat };
                    File.AppendAllText(Path.Combine(Root, "link-trace.txt"), "Add link " + spec.name + "\n");
                    linkId = db.DataLinkManager.AddDataLink(link);
                    tr.AddNewlyCreatedDBObject(link, true);
                }
                tr.Commit();
            }
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var table = (Table)tr.GetObject(db.GetObjectId(false, new Handle(0xDA1B), 0), OpenMode.ForWrite);
                File.AppendAllText(Path.Combine(Root, "link-trace.txt"), "Set link\n");
                table.Cells[spec.row, spec.col].SetDataLink(linkId, true);
                File.AppendAllText(Path.Combine(Root, "link-trace.txt"), "Set link done\n");
                tr.Commit();
                File.AppendAllText(Path.Combine(Root, "link-trace.txt"), "Committed link\n");
            }
        }
        Verify();
        doc.Editor.WriteMessage("\nPLAN_LIST_LINK_OK\n");
    }

    [CommandMethod("SYNCSOTRAMICOPY", CommandFlags.Modal)]
    public void SyncSheetSet()
    {
        Guard(Application.DocumentManager.MdiActiveDocument.Database);
        var code = SheetSetSync.Execute(true);
        File.WriteAllText(Path.Combine(Root, "sheetset-status.json"), Json.Serialize(new { success = code == 0 }));
        Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(code == 0 ? "\nSOTRAMI_SHEETSET_SYNC_OK\n" : "\nSOTRAMI_SHEETSET_SYNC_FAILED\n");
    }

    [CommandMethod("ACTUALIZARSOTRAMI", CommandFlags.Modal)]
    public void RefreshLinked()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        Guard(doc.Database);
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            var table = (Table)tr.GetObject(doc.Database.GetObjectId(false, new Handle(0xDA1B), 0), OpenMode.ForWrite);
            table.UpdateDataLink(UpdateDirection.SourceToData, UpdateOption.SkipFormat);
            tr.Commit();
        }
        SyncSheetSet();
        doc.Editor.WriteMessage("\nTabla y conjunto actualizados desde Excel. Guarde el DWG para conservar los cambios.\n");
    }

    [CommandMethod("VERIFYPLANLISTCOPY", CommandFlags.Modal)]
    public void Verify()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        var db = doc.Database;
        Guard(db);
        var data = ReadPayload();
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var table = (Table)tr.GetObject(db.GetObjectId(false, new Handle(Convert.ToInt64(data.handle, 16)), 0), OpenMode.ForRead);
            if (table.Rows.Count != data.rows.Length || table.Columns.Count != 5)
                throw new InvalidOperationException("Numero de filas o columnas distinto del esperado.");
            var errors = new List<string>();
            for (int r = 3; r < data.rows.Length; r++)
                for (int c = 0; c < 5; c++)
                    if (table.Cells[r, c].TextString != data.rows[r][c]) errors.Add(r + "," + c);
            if (errors.Count != 0) throw new InvalidOperationException("Celdas diferentes: " + String.Join("; ", errors));
            var actual = new string[table.Rows.Count][];
            for (int r = 0; r < actual.Length; r++) actual[r] = Enumerable.Range(0, 5).Select(c => table.Cells[r, c].TextString).ToArray();
            var rowHeights = Enumerable.Range(0, table.Rows.Count).Select(r => table.Rows[r].Height).ToArray();
            File.WriteAllText(Path.Combine(Root, "verified-table.json"), Json.Serialize(new { rows = actual, rowHeights, width = table.Width, height = table.Height, position = new[] { table.Position.X, table.Position.Y, table.Position.Z } }));
            tr.Commit();
        }
        doc.Editor.WriteMessage("\nPLAN_LIST_VERIFY_OK: " + data.plan_rows.Length + " planos; celdas identicas al Excel.\n");
    }

    [CommandMethod("PREVIEWPLANLISTCOPY", CommandFlags.Modal)]
    public void Preview()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        Guard(doc.Database);
        LayoutManager.Current.CurrentLayout = "A";
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            var layout = (Layout)tr.GetObject(LayoutManager.Current.GetLayoutId("A"), OpenMode.ForRead);
            var table = (Table)tr.GetObject(doc.Database.GetObjectId(false, new Handle(0xDA1B), 0), OpenMode.ForRead);
            using (var settings = new PlotSettings(false))
            {
                settings.CopyFrom(layout);
                var validator = PlotSettingsValidator.Current;
                validator.SetPlotConfigurationName(settings, "DWG To PDF.pc3", "ISO_full_bleed_A3_(297.00_x_420.00_MM)");
                validator.RefreshLists(settings);
                validator.SetPlotRotation(settings, PlotRotation.Degrees000);
                validator.SetPlotWindowArea(settings, new Extents2d(table.Position.X - 2, table.Position.Y - table.Height - 2, table.Position.X + table.Width + 2, table.Position.Y + 2));
                validator.SetPlotType(settings, Autodesk.AutoCAD.DatabaseServices.PlotType.Window);
                validator.SetUseStandardScale(settings, true);
                validator.SetStdScaleType(settings, StdScaleType.ScaleToFit);
                validator.SetPlotCentered(settings, true);
                using (var info = new PlotInfo())
                {
                    info.Layout = layout.ObjectId;
                    info.OverrideSettings = settings;
                    using (var infoValidator = new PlotInfoValidator())
                    {
                        infoValidator.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
                        infoValidator.Validate(info);
                    }
                    using (var engine = PlotFactory.CreatePublishEngine())
                    {
                        engine.BeginPlot(null, null);
                        engine.BeginDocument(info, doc.Name, null, 1, true, Path.Combine(Root, "Vista previa - lista actualizada.pdf"));
                        using (var page = new PlotPageInfo())
                        {
                            engine.BeginPage(page, info, true, null);
                            engine.BeginGenerateGraphics(null);
                            engine.EndGenerateGraphics(null);
                            engine.EndPage(null);
                        }
                        engine.EndDocument(null);
                        engine.EndPlot(null);
                    }
                }
            }
            tr.Commit();
        }
        doc.Editor.WriteMessage("\nPLAN_LIST_PREVIEW_OK\n");
    }
}
