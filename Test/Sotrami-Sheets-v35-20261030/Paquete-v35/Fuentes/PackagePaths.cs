using System;
using System.IO;

static class PackagePaths
{
    public class Settings
    {
        public string ProjectDirectory { get; set; }
        public string DrawingName { get; set; }
        public string SheetSetName { get; set; }
        public string WorkbookName { get; set; }
    }
    public static string Root => Path.GetDirectoryName(typeof(PackagePaths).Assembly.Location);
    static Settings ReadSettings()
    {
        var file = Path.Combine(Root, "SOTRAMI.config.json");
        if (!File.Exists(file)) return null;
        var settings = new JsonCodec().Deserialize<Settings>(File.ReadAllText(file));
        if (settings == null || String.IsNullOrWhiteSpace(settings.ProjectDirectory)) throw new InvalidOperationException("Falta la carpeta del proyecto en SOTRAMI.config.json.");
        foreach (var name in new[] { settings.DrawingName, settings.SheetSetName, settings.WorkbookName })
            if (String.IsNullOrWhiteSpace(name) || Path.GetFileName(name) != name) throw new InvalidOperationException("Nombre de archivo invalido en SOTRAMI.config.json.");
        return settings;
    }
    static readonly Settings Config = ReadSettings();
    public static string LogDirectory => Config == null ? Root : Directory.CreateDirectory(Path.Combine(Root, "Registros")).FullName;
    public static string ProjectDirectory => Config == null ? @"Q:\Proyectos 2026\1107.001.00.16 - Pad SOTRAMI\05_Drawing" : Config.ProjectDirectory;
    public static string WorkbookFile => Path.Combine(ProjectDirectory, Config == null ? "1107.001.00.16-17-LE-001-RB - Lista de entregables.xlsx" : Config.WorkbookName);
    public static string DrawingFile => Config == null ? Path.Combine(Root, "1107.001.00.14-200-00-P-001 - REVISION EXCEL.dwg") : Path.Combine(ProjectDirectory, Config.DrawingName);
    public static string SheetSetFile => Config == null ? Path.Combine(Root, "SOTRAMI - REVISION EXCEL.dst") : Path.Combine(ProjectDirectory, "Xref", Config.SheetSetName);
    public static string TeamSheetSetFile => SheetSetFile;
}
