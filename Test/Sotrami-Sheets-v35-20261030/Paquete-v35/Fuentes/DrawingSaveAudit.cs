using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

public class DrawingSaveAudit
{
    static readonly JsonCodec Json = new JsonCodec();
    static readonly Dictionary<Database, Document> Documents = new Dictionary<Database, Document>();
    static readonly Dictionary<Database, Stamp> Seen = new Dictionary<Database, Stamp>();
    static readonly Queue<SaveRecord> Pending = new Queue<SaveRecord>();
    static readonly Queue<Document> Opened = new Queue<Document>();
    static readonly Dictionary<string, SaveRecord> Observed = new Dictionary<string, SaveRecord>(StringComparer.OrdinalIgnoreCase);
    static bool started;
    static readonly TimeZoneInfo Lima = FindLima();
    public class Stamp { public long Ticks { get; set; } public long Length { get; set; } }
    public class SaveRecord
    {
        public string File { get; set; }
        public string NativeUser { get; set; }
        public string SavedUtc { get; set; }
        public string ObservedUtc { get; set; }
        public string WindowsUser { get; set; }
        public string Computer { get; set; }
        public string Assembly { get; set; }
        public Stamp Disk { get; set; }
    }
    public class Report
    {
        public string File { get; set; }
        public string NativeUser { get; set; }
        public string SavedUtc { get; set; }
        public string SavedLima { get; set; }
        public bool DiskChangedSinceOpen { get; set; }
        public bool ObservedSaveMatches { get; set; }
        public string WindowsUser { get; set; }
        public string Computer { get; set; }
        public string TextFile { get; set; }
        public string HistoryFile { get; set; }
        public string LogError { get; set; }
    }
    static TimeZoneInfo FindLima()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.CreateCustomTimeZone("LIMA", TimeSpan.FromHours(-5), "LIMA", "LIMA"); }
    }
    static string Normal(string file) => Path.GetFullPath(file);
    static bool InProject(string file)
    {
        if (String.IsNullOrWhiteSpace(file) || !Path.IsPathRooted(file) || !Path.GetExtension(file).Equals(".dwg", StringComparison.OrdinalIgnoreCase)) return false;
        return Normal(file).StartsWith(Normal(PackagePaths.ProjectDirectory).TrimEnd('\\', '/') + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
    static Stamp Disk(string file)
    {
        var info = new FileInfo(file);
        return info.Exists ? new Stamp { Ticks = info.LastWriteTimeUtc.Ticks, Length = info.Length } : null;
    }
    static bool Same(Stamp first, Stamp second) => first != null && second != null && first.Ticks == second.Ticks && first.Length == second.Length;
    static string NativeUtc(Database db)
    {
        var value = db.Tduupdate;
        return value.Year < 1980 ? "" : DateTime.SpecifyKind(value, DateTimeKind.Utc).ToString("o", CultureInfo.InvariantCulture);
    }
    static string LocalDate(string utc)
    {
        DateTime value;
        return DateTime.TryParse(utc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value)
            ? TimeZoneInfo.ConvertTimeFromUtc(value.ToUniversalTime(), Lima).ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + " (LIMA, UTC-05:00)" : "SIN FECHA DISPONIBLE";
    }
    static string BaseFile(string file)
    {
        if (!InProject(file)) return null;
        var relative = Normal(file).Substring(Normal(PackagePaths.ProjectDirectory).TrimEnd('\\', '/').Length + 1);
        var result = Normal(Path.Combine(PackagePaths.ProjectDirectory, "Registros_DWG", Path.ChangeExtension(relative, null)));
        var allowed = Normal(Path.Combine(PackagePaths.ProjectDirectory, "Registros_DWG")) + Path.DirectorySeparatorChar;
        if (!result.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Ruta de registro no valida.");
        return result;
    }
    public static void Start()
    {
        if (started) return;
        started = true;
        var manager = Application.DocumentManager;
        manager.DocumentCreated += Created;
        manager.DocumentToBeDestroyed += Destroyed;
        Application.Idle += Idle;
        foreach (Document doc in manager) Attach(doc);
        if (manager.MdiActiveDocument != null) Opened.Enqueue(manager.MdiActiveDocument);
    }
    public static void Stop()
    {
        if (!started) return;
        Flush();
        Application.DocumentManager.DocumentCreated -= Created;
        Application.DocumentManager.DocumentToBeDestroyed -= Destroyed;
        Application.Idle -= Idle;
        foreach (var pair in Documents) { try { pair.Key.SaveComplete -= Saved; } catch (System.Exception) { } }
        Documents.Clear(); Seen.Clear(); Opened.Clear(); Pending.Clear(); Observed.Clear(); started = false;
    }
    static void Attach(Document doc)
    {
        if (Documents.ContainsKey(doc.Database)) return;
        Documents[doc.Database] = doc;
        Seen[doc.Database] = String.IsNullOrEmpty(doc.Database.Filename) ? null : Disk(doc.Database.Filename);
        doc.Database.SaveComplete += Saved;
    }
    static void Created(object sender, DocumentCollectionEventArgs args)
    {
        try { Attach(args.Document); Opened.Enqueue(args.Document); }
        catch (System.Exception) { } // Logging must never cancel document opening.
    }
    static void Destroyed(object sender, DocumentCollectionEventArgs args)
    {
        try { args.Document.Database.SaveComplete -= Saved; Documents.Remove(args.Document.Database); Seen.Remove(args.Document.Database); }
        catch (System.Exception) { }
    }
    static void Saved(object sender, DatabaseIOEventArgs args)
    {
        try
        {
            var db = sender as Database;
            if (db == null || !InProject(args.FileName)) return; // Ignore autosaves, exports and other projects.
            Pending.Enqueue(new SaveRecord {
                File = Normal(args.FileName), NativeUser = db.SummaryInfo.LastSavedBy ?? "", SavedUtc = NativeUtc(db),
                ObservedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                WindowsUser = Environment.UserDomainName + "\\" + Environment.UserName, Computer = Environment.MachineName,
                Assembly = typeof(DrawingSaveAudit).Assembly.GetName().Name
            });
        }
        catch (System.Exception) { } // Never throw back into the native save operation.
    }
    static void WriteAtomic(string file, string text)
    {
        var temp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, text, new UTF8Encoding(true));
            if (File.Exists(file)) File.Replace(temp, file, null);
            else File.Move(temp, file);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    static void Append(string file, string text)
    {
        using (var stream = new FileStream(file, FileMode.Append, FileAccess.Write, FileShare.Read))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(true))) writer.Write(text);
    }
    static void Record(SaveRecord record)
    {
        if (!File.Exists(record.File)) return;
        record.Disk = Disk(record.File);
        // A successful native save remains observed even if a TXT/JSON is locked.
        // Do this before best-effort logging so a logging failure cannot make our
        // own save look like an external file change or lose its Windows account.
        Observed[record.File] = record;
        foreach (var pair in Documents)
            if (String.Equals(pair.Key.Filename, record.File, StringComparison.OrdinalIgnoreCase)) Seen[pair.Key] = record.Disk;
        var prefix = BaseFile(record.File);
        Directory.CreateDirectory(Path.GetDirectoryName(prefix));
        var history = prefix + " - HISTORIAL.txt";
        if (!File.Exists(history)) Append(history, "HISTORIAL DE GUARDADOS OBSERVADOS POR SOTRAMI\r\nDWG: " + record.File + "\r\nSE REGISTRA EL USUARIO DE LA CUENTA. REQUIERE LA HERRAMIENTA CARGADA AL GUARDAR.\r\nCONSULTE QUIENGUARDO PARA COMPROBAR EL ULTIMO DATO DEL DWG.\r\n\r\n");
        Append(history, LocalDate(record.SavedUtc) + " | WINDOWS: " + record.WindowsUser + " | EQUIPO: " + record.Computer + " | USUARIO DWG: " + record.NativeUser + "\r\n");
        WriteAtomic(prefix + ".json", Json.Serialize(record));
        var report = new Report { File = record.File, NativeUser = record.NativeUser, SavedUtc = record.SavedUtc, SavedLima = LocalDate(record.SavedUtc), ObservedSaveMatches = true, WindowsUser = record.WindowsUser, Computer = record.Computer };
        WriteAtomic(prefix + " - ULTIMO GUARDADO.txt", Text(report));
    }
    public static void Flush()
    {
        while (Pending.Count > 0)
        {
            var record = Pending.Dequeue();
            try { Record(record); }
            catch (System.Exception error)
            {
                var active = Application.DocumentManager.MdiActiveDocument;
                if (active != null && String.Equals(active.Database.Filename, record.File, StringComparison.OrdinalIgnoreCase))
                    active.Editor.WriteMessage("\nDWG GUARDADO; NO SE PUDO ESCRIBIR SU REGISTRO: " + error.Message + "\n");
            }
        }
    }
    static void Idle(object sender, EventArgs args)
    {
        try
        {
            Flush();
            while (Opened.Count > 0)
            {
                var doc = Opened.Dequeue();
                if (doc == null || !Documents.ContainsKey(doc.Database) || !InProject(doc.Database.Filename)) continue;
                // DocumentCreated can fire before its filename and metadata are
                // ready; establish its disk snapshot only after opening finishes.
                Seen[doc.Database] = Disk(doc.Database.Filename);
                if (doc == Application.DocumentManager.MdiActiveDocument) Show(doc, false);
            }
        }
        catch (System.Exception) { }
    }
    public static Report Inspect(Document doc, bool writeText)
    {
        Flush();
        var file = doc.Database.Filename;
        if (String.IsNullOrEmpty(file) || !Path.IsPathRooted(file) || !File.Exists(file)) throw new InvalidOperationException("EL DIBUJO TODAVIA NO TIENE UN DWG GUARDADO.");
        var report = new Report { File = Normal(file), NativeUser = doc.Database.SummaryInfo.LastSavedBy ?? "", SavedUtc = NativeUtc(doc.Database) };
        report.SavedLima = LocalDate(report.SavedUtc);
        Stamp loaded;
        report.DiskChangedSinceOpen = Seen.TryGetValue(doc.Database, out loaded) && loaded != null && !Same(loaded, Disk(file));
        var prefix = BaseFile(file);
        if (prefix != null)
        {
            report.HistoryFile = prefix + " - HISTORIAL.txt";
            try
            {
                SaveRecord memory;
                if (Observed.TryGetValue(report.File, out memory) && Matches(memory, report, file))
                {
                    report.ObservedSaveMatches = true; report.WindowsUser = memory.WindowsUser; report.Computer = memory.Computer;
                }
                else if (File.Exists(prefix + ".json"))
                {
                    var record = Json.Deserialize<SaveRecord>(File.ReadAllText(prefix + ".json"));
                    report.ObservedSaveMatches = Matches(record, report, file);
                    if (report.ObservedSaveMatches) { report.WindowsUser = record.WindowsUser; report.Computer = record.Computer; }
                }
                if (writeText)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(prefix));
                    report.TextFile = prefix + " - ULTIMO GUARDADO.txt";
                    WriteAtomic(report.TextFile, Text(report));
                }
            }
            catch (System.Exception error) { report.LogError = error.Message; }
        }
        return report;
    }
    static bool Matches(SaveRecord record, Report report, string file) => record != null && !report.DiskChangedSinceOpen && Same(record.Disk, Disk(file)) && record.NativeUser == report.NativeUser && record.SavedUtc == report.SavedUtc && String.Equals(record.File, report.File, StringComparison.OrdinalIgnoreCase);
    public static string Text(Report report)
    {
        return "CONSULTA DEL ULTIMO GUARDADO DEL DWG\r\nCONSULTA: " + LocalDate(DateTime.UtcNow.ToString("o")) +
            "\r\nDWG: " + report.File + "\r\nUSUARIO GUARDADO EN EL DWG: " + (String.IsNullOrWhiteSpace(report.NativeUser) ? "SIN INFORMACION DISPONIBLE" : report.NativeUser) +
            "\r\nFECHA DEL GUARDADO: " + report.SavedLima +
            "\r\nUSUARIO WINDOWS: " + (report.ObservedSaveMatches ? report.WindowsUser : "NO REGISTRADO PARA ESTE GUARDADO") +
            "\r\nEQUIPO: " + (report.ObservedSaveMatches ? report.Computer : "NO REGISTRADO PARA ESTE GUARDADO") +
            "\r\n\r\n" + (report.DiskChangedSinceOpen ? "EL ARCHIVO EN DISCO CAMBIO DESDE SU APERTURA. EL DATO DEL DIBUJO ABIERTO PUEDE ESTAR DESACTUALIZADO. GUARDE SU TRABAJO Y REABRA CUANDO PUEDA PARA COMPROBARLO.\r\n" : "") +
            "ESTE INFORME ES UNA CONSULTA, NO SE REFRESCA SOLO AL ESTAR ABIERTO EN BLOC DE NOTAS.\r\nNO INCLUYE EDICIONES QUE TODAVIA NO SE HAN GUARDADO.\r\nEL HISTORIAL DETALLADO REQUIERE SOTRAMI CARGADO EN CADA EQUIPO.\r\nUNA CUENTA COMPARTIDA NO IDENTIFICA A UNA PERSONA DISTINTA.\r\n";
    }
    public static Report Show(Document doc, bool writeText)
    {
        var report = Inspect(doc, writeText);
        doc.Editor.WriteMessage("\nULTIMO GUARDADO: " + (String.IsNullOrWhiteSpace(report.NativeUser) ? "SIN USUARIO DISPONIBLE" : report.NativeUser) + " | " + report.SavedLima + "\n");
        if (report.ObservedSaveMatches) doc.Editor.WriteMessage("WINDOWS: " + report.WindowsUser + " | EQUIPO: " + report.Computer + "\n");
        if (report.DiskChangedSinceOpen) doc.Editor.WriteMessage("EL ARCHIVO EN DISCO CAMBIO DESDE SU APERTURA. REABRA CUANDO PUEDA PARA VER EL ULTIMO GUARDADO.\n");
        if (writeText && report.TextFile != null && report.LogError == null) doc.Editor.WriteMessage("INFORME: " + report.TextFile + "\n");
        if (report.LogError != null) doc.Editor.WriteMessage("NO SE PUDO ACTUALIZAR EL INFORME: " + report.LogError + "\n");
        return report;
    }
    [CommandMethod("QUIENGUARDO", CommandFlags.Modal)]
    [CommandMethod("SOTRAMIULTIMOGUARDADO", CommandFlags.Modal)]
    public void Query()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        try { Show(doc, true); }
        catch (System.Exception error) { doc.Editor.WriteMessage("\nCONSULTA DE GUARDADO: " + error.Message + "\n"); }
    }
    [CommandMethod("REGISTRODWG", CommandFlags.Modal)]
    public void Notepad()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        try
        {
            var report = Show(doc, true);
            if (report.TextFile == null || report.LogError != null) throw new InvalidOperationException("NO HAY INFORME DISPONIBLE EN LA CARPETA DEL PROYECTO.");
            Process.Start(new ProcessStartInfo("notepad.exe", "\"" + report.TextFile + "\"") { UseShellExecute = false });
        }
        catch (System.Exception error) { doc.Editor.WriteMessage("\nREGISTRO DWG: " + error.Message + "\n"); }
    }
}
