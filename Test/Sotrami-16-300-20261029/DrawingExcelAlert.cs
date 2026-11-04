using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

// Save callbacks only enqueue. CAD inspection and advisory UI run later on the
// main thread, with a quiescent active document. Never evaluate resident fields.
public class DrawingExcelAlert
{
    public class Difference
    {
        public string Source { get; set; }
        public string Field { get; set; }
        public string Actual { get; set; }
        public string Excel { get; set; }
    }
    public class Result
    {
        public string State { get; set; }
        public string File { get; set; }
        public string Code { get; set; }
        public string CheckedLima { get; set; }
        public string Reason { get; set; }
        public string LastSavedBy { get; set; }
        public string LastSavedLima { get; set; }
        public string SavedWindowsUser { get; set; }
        public string SavedComputer { get; set; }
        public string ConsultedBy { get; set; }
        public string ConsultedComputer { get; set; }
        public string WorkbookHash { get; set; }
        public string ReportFile { get; set; }
        public List<Difference> Differences { get; set; } = new List<Difference>();
        public List<string> Errors { get; set; } = new List<string>();
    }
    static readonly JsonCodec Json = new JsonCodec();
    static readonly Dictionary<Database, Document> Attached = new Dictionary<Database, Document>();
    static readonly Dictionary<Document, string> Pending = new Dictionary<Document, string>();
    static readonly Dictionary<Document, string> Announced = new Dictionary<Document, string>();
    static bool started, running;
    internal static Action<string> Notify = message => Application.ShowAlertDialog(message);
    static string Normal(string value) => Regex.Replace((value ?? "").Replace('\u00A0', ' '), @"\s+", " ").Trim().ToUpperInvariant();
    static string Title(string value) => Normal(String.Join(" ", TitleParts.Split(value)));
    static string LimaNow() => DateTime.UtcNow.AddHours(-5).ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + " (LIMA, UTC-05:00)";
    static bool SameFile(string a, string b) => !String.IsNullOrEmpty(a) && !String.IsNullOrEmpty(b) && String.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
    static bool Eligible(Document doc)
    {
        var file = doc.Database.Filename;
        if (String.IsNullOrWhiteSpace(file) || !Path.IsPathRooted(file) || !Path.GetExtension(file).Equals(".dwg", StringComparison.OrdinalIgnoreCase)) return false;
        var root = Path.GetFullPath(PackagePaths.ProjectDirectory).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(file).StartsWith(root, StringComparison.OrdinalIgnoreCase)) return false;
        var relative = Path.GetFullPath(file).Substring(root.Length);
        if (relative.StartsWith("Old\\", StringComparison.OrdinalIgnoreCase))
            return SheetSetSync.ReadForAlerts().Any(s => SameFile(s.File, file));
        return Regex.IsMatch(Path.GetFileNameWithoutExtension(file), @"^1107\.001\.00\.14-200-\d{2}-P-\d{3}$", RegexOptions.IgnoreCase);
    }
    public static void Start()
    {
        if (started) return;
        started = true;
        var manager = Application.DocumentManager;
        manager.DocumentCreated += Created; manager.DocumentActivated += Activated; manager.DocumentToBeDestroyed += Destroyed;
        Application.Idle += Idle;
        foreach (Document doc in manager) { Attach(doc); Queue(doc, "APERTURA"); }
    }
    public static void Stop()
    {
        if (!started) return;
        var manager = Application.DocumentManager;
        manager.DocumentCreated -= Created; manager.DocumentActivated -= Activated; manager.DocumentToBeDestroyed -= Destroyed;
        Application.Idle -= Idle;
        foreach (var pair in Attached) try { pair.Key.SaveComplete -= Saved; } catch (System.Exception) { }
        Attached.Clear(); Pending.Clear(); Announced.Clear(); started = false;
    }
    static void Attach(Document doc)
    {
        if (Attached.ContainsKey(doc.Database)) return;
        Attached[doc.Database] = doc; doc.Database.SaveComplete += Saved;
    }
    static void Queue(Document doc, string reason)
    {
        if (doc == null) return;
        string existing;
        if (Pending.TryGetValue(doc, out existing) && existing == "GUARDADO COMPLETADO") return;
        Pending[doc] = reason;
    }
    static void Created(object sender, DocumentCollectionEventArgs args)
    { try { Attach(args.Document); Queue(args.Document, "APERTURA"); } catch (System.Exception) { } }
    static void Activated(object sender, DocumentCollectionEventArgs args)
    { try { Queue(args.Document, "ACTIVACION"); } catch (System.Exception) { } }
    static void Destroyed(object sender, DocumentCollectionEventArgs args)
    {
        try { args.Document.Database.SaveComplete -= Saved; Attached.Remove(args.Document.Database); Pending.Remove(args.Document); Announced.Remove(args.Document); }
        catch (System.Exception) { }
    }
    static void Saved(object sender, DatabaseIOEventArgs args)
    {
        try
        {
            Document doc; var db = sender as Database;
            if (db != null && Attached.TryGetValue(db, out doc) && SameFile(args.FileName, db.Filename) && Path.GetExtension(args.FileName).Equals(".dwg", StringComparison.OrdinalIgnoreCase)) Queue(doc, "GUARDADO COMPLETADO");
        }
        catch (System.Exception) { } // Never throw into native saving.
    }
    static void Idle(object sender, EventArgs args)
    {
        if (running || Pending.Count == 0) return;
        var doc = Application.DocumentManager.MdiActiveDocument;
        try
        {
            if (doc == null || !Pending.ContainsKey(doc) || !doc.Editor.IsQuiescent || Convert.ToInt32(Application.GetSystemVariable("CMDACTIVE")) != 0) return;
            var reason = Pending[doc]; Pending.Remove(doc);
            if (Eligible(doc)) CheckAndReport(doc, reason, false);
        }
        catch (System.Exception error)
        {
            try { if (doc != null) doc.Editor.WriteMessage("\nSOTRAMI: NO SE PUDO VERIFICAR LOS DATOS. " + error.Message + "\n"); } catch (System.Exception) { }
        }
    }
    static void Compare(Result r, string source, string field, string actual, string expected, bool title)
    {
        if ((title ? Title(actual) : Normal(actual)) != (title ? Title(expected) : Normal(expected)))
            r.Differences.Add(new Difference { Source = source, Field = field, Actual = Normal(actual), Excel = Normal(expected) });
    }
    public static Result Inspect(Document doc)
    {
        var r = new Result {
            State = "NO SE PUDO VERIFICAR", File = doc.Database.Filename,
            Code = Path.GetFileNameWithoutExtension(doc.Database.Filename).ToUpperInvariant(), CheckedLima = LimaNow(),
            ConsultedBy = Environment.UserDomainName + "\\" + Environment.UserName, ConsultedComputer = Environment.MachineName
        };
        try
        {
            if (!Eligible(doc)) throw new InvalidOperationException("EL DWG NO ES UN PLANO ACTIVO DEL PROYECTO CONFIGURADO.");
            var saved = DrawingSaveAudit.Inspect(doc, false);
            r.LastSavedBy = saved.NativeUser; r.LastSavedLima = saved.SavedLima;
            if (saved.ObservedSaveMatches)
            {
                r.SavedWindowsUser = saved.WindowsUser; r.SavedComputer = saved.Computer;
                // Native Database.SaveAs may leave LastSavedBy empty. A matched
                // SaveComplete record still proves which Windows account saved.
                if (String.IsNullOrWhiteSpace(r.LastSavedBy)) r.LastSavedBy = saved.WindowsUser;
            }
            if (saved.DiskChangedSinceOpen) throw new InvalidOperationException("EL ARCHIVO EN DISCO CAMBIO DESDE SU APERTURA. REVISE UNA COPIA ACTUALIZADA.");
            using (DrawingRenumber.AcquireForUpdate())
            {
                DrawingRenumber.RequireNoPending();
                r.WorkbookHash = DrawingRenumber.Hash(PackagePaths.WorkbookFile);
                var plans = ExcelPlanSync.ReadPlans();
                var plan = plans.SingleOrDefault(p => p.Code.Equals(r.Code, StringComparison.OrdinalIgnoreCase));
                if (plan == null) throw new InvalidOperationException("EL CODIGO DEL ARCHIVO NO FIGURA EN EL EXCEL. REVISE UN POSIBLE CAMBIO DE NUMERO O RETIRO.");
                SheetSetSync.Record[] sheets = null;
                try { sheets = SheetSetSync.ReadForAlerts(); }
                catch (System.Exception error) { r.Errors.Add("NO SE PUDO VERIFICAR SHEETS: " + error.Message); }
                if (sheets != null)
                {
                    // Match by file so a manually edited sheet number cannot select
                    // another Excel row and silently change this drawing's identity.
                    var matches = sheets.Where(s => SameFile(s.File, r.File)).ToArray();
                    if (matches.Length != 1) r.Errors.Add("SHEETS: NO HAY UNA HOJA UNICA PARA ESTE ARCHIVO.");
                    foreach (var sheet in matches)
                    {
                        Compare(r, "SHEETS / SOTRAMI", "NUMERO", sheet.Number, plan.Code, false);
                        Compare(r, "SHEETS / SOTRAMI", "TITULO", sheet.Title, plan.Title, true);
                    }
                }
                int count = 0;
                using (doc.LockDocument())
                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    var layouts = (DBDictionary)tr.GetObject(doc.Database.LayoutDictionaryId, OpenMode.ForRead);
                    foreach (DBDictionaryEntry entry in layouts)
                    {
                        var layout = (Layout)tr.GetObject(entry.Value, OpenMode.ForRead);
                        if (layout.ModelType) continue;
                        foreach (ObjectId id in (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead))
                        {
                            var block = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                            if (block == null) continue;
                            var attrs = block.AttributeCollection.Cast<ObjectId>().Select(a => tr.GetObject(a, OpenMode.ForRead) as AttributeReference).Where(a => a != null).ToArray();
                            var tags = new[] { "TITTLE_DRAW1", "TITTLE_DRAW2", "DRAWING_NO" };
                            if (!tags.All(tag => attrs.Count(a => a.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase)) == 1)) continue;
                            var values = tags.Select(tag => attrs.Single(a => a.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase))).ToArray();
                            Func<AttributeReference, string> visible = a => { if (!a.IsMTextAttribute) return a.TextString; using (var mt = a.MTextAttribute) return mt.Text; };
                            var source = "MEMBRETE / " + layout.LayoutName + " / " + block.Handle;
                            Compare(r, source, "NUMERO", visible(values[2]), plan.Code, false);
                            Compare(r, source, "TITULO", visible(values[0]) + " " + visible(values[1]), String.Join(" ", TitleParts.Split(plan.Title)), false);
                            count++;
                        }
                    }
                    // ForRead only: no Evaluate, Regen, Commit, or CAD write.
                }
                if (count == 0 && !SameFile(r.File, PackagePaths.DrawingFile)) r.Errors.Add("NO SE ENCONTRO UN MEMBRETE IDENTIFICABLE PARA COMPARAR.");
                if (r.WorkbookHash != DrawingRenumber.Hash(PackagePaths.WorkbookFile))
                {
                    r.Differences.Clear(); throw new InvalidOperationException("EL EXCEL CAMBIO DURANTE LA CONSULTA. VUELVA A VERIFICAR.");
                }
                r.State = r.Errors.Count > 0 ? "NO SE PUDO VERIFICAR" : r.Differences.Count > 0 ? "DIFERENCIAS" : "COINCIDE";
            }
        }
        catch (System.Exception error) { r.Errors.Add(error.Message); r.State = "NO SE PUDO VERIFICAR"; }
        return r;
    }
    public static string Text(Result r)
    {
        var t = new StringBuilder("SOTRAMI - REVISION DE DATOS CON EL EXCEL\r\n");
        t.AppendLine("ESTADO: " + r.State).AppendLine("CONSULTA: " + r.CheckedLima).AppendLine("MOTIVO: " + r.Reason);
        t.AppendLine("PLANO: " + r.Code).AppendLine("DWG: " + r.File);
        t.AppendLine("ULTIMO GUARDADO DEL DWG POR: " + (String.IsNullOrWhiteSpace(r.LastSavedBy) ? "SIN INFORMACION DISPONIBLE" : r.LastSavedBy));
        t.AppendLine("FECHA DEL ULTIMO GUARDADO: " + r.LastSavedLima);
        t.AppendLine("CUENTA DEL GUARDADO: " + (r.SavedWindowsUser ?? "NO REGISTRADA PARA ESTE GUARDADO"));
        t.AppendLine("EQUIPO DEL GUARDADO: " + (r.SavedComputer ?? "NO REGISTRADO PARA ESTE GUARDADO"));
        t.AppendLine("CONSULTADO POR: " + r.ConsultedBy + " | EQUIPO: " + r.ConsultedComputer);
        foreach (var d in r.Differences) t.AppendLine().AppendLine(d.Source + " / " + d.Field).AppendLine("EN CIVIL 3D: " + d.Actual).AppendLine("EN EL EXCEL: " + d.Excel);
        foreach (var error in r.Errors) t.AppendLine("VERIFICACION PENDIENTE: " + error);
        if (r.Differences.Count > 0)
            t.AppendLine().AppendLine("LOS TÍTULOS DE LOS PLANOS DEBEN MODIFICARSE EN EXCEL Y NO DIRECTAMENTE EN EL PLANO.")
                .AppendLine("SI EL CAMBIO ES CORRECTO, ACTUALICE Y GUARDE EL EXCEL; DESPUES EJECUTE SOTRAMIEXCEL EN LA PORTADA.")
                .AppendLine("SI EL CAMBIO FUE UN ERROR, RESTAURE LOS DATOS DEL PLANO SEGUN EL EXCEL.");
        t.AppendLine().AppendLine("EL ULTIMO USUARIO QUE GUARDO NO DEMUESTRA QUIEN CAMBIO EL TITULO O SHEETS.")
            .AppendLine("LA CONSULTA LEE EL EXCEL GUARDADO Y LOS DATOS ABIERTOS EN ESTA SESION; NO INCLUYE CAMBIOS DE EXCEL SIN GUARDAR.")
            .AppendLine("ESTE AVISO NO MODIFICA EL EXCEL, EL DWG NI EL CONJUNTO, Y NO CANCELA EL GUARDADO.");
        return t.ToString();
    }
    static string Fingerprint(Result r) => r.State + "|" + String.Join("|", r.Differences.Select(d => d.Source + ":" + d.Field + ":" + d.Actual + ":" + d.Excel)) + "|" + String.Join("|", r.Errors);
    static string Safe(string text) => Regex.Replace(text, @"[^\w.-]", "_");
    public static string Message(Result r)
    {
        var t = new StringBuilder("SOTRAMI: " + r.State + "\nPLANO: " + r.Code + "\n");
        foreach (var d in r.Differences.Take(4))
            t.AppendLine().AppendLine(d.Source + " / " + d.Field).AppendLine("EN CIVIL 3D: " + d.Actual).AppendLine("EN EXCEL: " + d.Excel);
        if (r.Differences.Count > 4) t.AppendLine("HAY MAS DIFERENCIAS EN EL INFORME.");
        foreach (var e in r.Errors.Take(2)) t.AppendLine(e);
        t.AppendLine().AppendLine("ULTIMO GUARDADO DEL DWG POR: " + (String.IsNullOrWhiteSpace(r.LastSavedBy) ? "SIN INFORMACION DISPONIBLE" : Normal(r.LastSavedBy))).AppendLine("FECHA: " + r.LastSavedLima);
        if (r.Differences.Count > 0)
            t.AppendLine().AppendLine("LOS TÍTULOS DE LOS PLANOS DEBEN MODIFICARSE EN EXCEL Y NO DIRECTAMENTE EN EL PLANO.")
                .AppendLine("SI EL CAMBIO ES CORRECTO, GUARDELO EN EXCEL Y EJECUTE SOTRAMIEXCEL EN LA PORTADA.");
        t.AppendLine().AppendLine("EL ULTIMO GUARDADO NO DEMUESTRA QUIEN CAMBIO EL TITULO.")
            .AppendLine("ESTE AVISO NO CANCELA EL GUARDADO DEL DWG.");
        return t.ToString();
    }
    static void Persist(Result r)
    {
        var folder = Path.Combine(PackagePaths.ProjectDirectory, "Registros_DWG", "Alertas_Excel", r.Code);
        Directory.CreateDirectory(folder);
        var prefix = Path.Combine(folder, Safe(Environment.MachineName + "_" + Environment.UserName));
        r.ReportFile = prefix + ".txt";
        // Separate observers do not overwrite another computer's findings.
        using (var gate = new FileStream(prefix + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        {
            Result prior = null;
            if (File.Exists(prefix + ".json")) try { prior = Json.Deserialize<Result>(File.ReadAllText(prefix + ".json")); } catch (System.Exception) { }
            if (prior == null || Fingerprint(prior) != Fingerprint(r) || prior.LastSavedLima != r.LastSavedLima || prior.LastSavedBy != r.LastSavedBy)
                File.AppendAllText(prefix + " - HISTORIAL.txt", Text(r) + "\r\n----------------------------------------\r\n", new UTF8Encoding(true));
            Atomic(prefix + ".txt", Text(r));
            Atomic(prefix + ".json", Json.Serialize(r));
        }
    }
    static void Atomic(string file, string body)
    {
        var temp = file + "." + Guid.NewGuid().ToString("N").Substring(0, 8) + ".tmp";
        try
        {
            File.WriteAllText(temp, body, new UTF8Encoding(true));
            if (File.Exists(file)) File.Replace(temp, file, null); else File.Move(temp, file);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    public static Result CheckAndReport(Document doc, string reason, bool explicitRequest)
    {
        if (running) return null;
        running = true;
        try
        {
            var r = Inspect(doc); r.Reason = reason;
            var fingerprint = Fingerprint(r);
            string previous; bool had = Announced.TryGetValue(doc, out previous);
            bool changed = !had || previous != fingerprint;
            bool warning = r.State != "COINCIDE";
            if (warning || had || explicitRequest)
            {
                try { Persist(r); }
                catch (System.Exception error) { doc.Editor.WriteMessage("\nNO SE PUDO GUARDAR EL INFORME DE LA ALERTA: " + error.Message + ". EL GUARDADO DEL DWG NO SE CANCELA.\n"); }
            }
            if (explicitRequest || (warning && (changed || reason == "GUARDADO COMPLETADO")) || (had && changed))
                doc.Editor.WriteMessage("\n" + Text(r) + (r.ReportFile == null ? "" : "\nINFORME: " + r.ReportFile + "\n"));
            Announced[doc] = fingerprint;
            // Save has already completed. Avoid repeating an identical dialog,
            // but print a reminder on every save while a difference persists.
            if (warning && changed) Notify(Message(r));
            return r;
        }
        finally { running = false; }
    }
    [CommandMethod("SOTRAMIVERIFICARDATOS", CommandFlags.Modal)]
    public void Verify()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        if (doc == null) return;
        try { if (!Eligible(doc)) { doc.Editor.WriteMessage("\nACTIVE UN PLANO DEL PROYECTO CONFIGURADO.\n"); return; } CheckAndReport(doc, "CONSULTA MANUAL", true); }
        catch (System.Exception error) { doc.Editor.WriteMessage("\nNO SE PUDO VERIFICAR: " + error.Message + "\n"); }
    }
}
