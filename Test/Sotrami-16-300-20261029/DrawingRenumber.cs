using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

public class DrawingRenumber
{
    static readonly JsonCodec Json = new JsonCodec();
    static string Root => Path.GetFullPath(PackagePaths.ProjectDirectory).TrimEnd('\\');
    static string Folder => Path.Combine(Root, "Old", "Renumeraciones_SOTRAMI");
    static string Pending => Path.Combine(Folder, "operaciones.jsonl");
    public class Preview
    {
        public string OldCode { get; set; }
        public string NewCode { get; set; }
        public string Title { get; set; }
        public string Source { get; set; }
        public string Destination { get; set; }
        public string SheetId { get; set; }
        public string SourceHash { get; set; }
        public string WorkbookHash { get; set; }
        public string SetHash { get; set; }
        public string Layout { get; set; }
        public string LayoutHandle { get; set; }
        public Dictionary<string, string> InspectedFiles { get; set; }
    }
    public class Operation
    {
        public Preview Plan { get; set; }
        public string Directory { get; set; }
        public string State { get; set; }
        public string PreparedDwg { get; set; }
        public string PreparedWorkbook { get; set; }
        public string PreparedDwgHash { get; set; }
        public string PreparedWorkbookHash { get; set; }
        public string Cell { get; set; }
        public string Utc { get; set; }
        public string User { get; set; }
        public string Error { get; set; }
    }
    static bool Same(string a, string b) => String.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
    static string Safe(string path)
    {
        var full = Path.GetFullPath(path);
        if (!full.StartsWith(Root + "\\", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("RUTA FUERA DEL PROYECTO.");
        return full;
    }
    public static string Hash(string path)
    {
        using (var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        using (var h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(s)).Replace("-", "");
    }
    static void Append(string file, Operation value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file));
        // Append-only durable states avoid replacing a journal that a reader or
        // antivirus has open. The project gate serializes all state writers.
        using (var stream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
        {
            if (stream.Length > 0)
            {
                stream.Position = stream.Length - 1;
                var last = stream.ReadByte(); stream.Position = stream.Length;
                if (last != '\n') stream.WriteByte((byte)'\n');
            }
            var bytes = new UTF8Encoding(false).GetBytes(Json.Serialize(value) + "\n");
            stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
        }
    }
    static Operation ReadLast()
    {
        Operation last = null;
        using (var input = new FileStream(Pending, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(input, Encoding.UTF8, true))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (String.IsNullOrWhiteSpace(line)) continue;
                try { last = Json.Deserialize<Operation>(line); }
                catch (System.Exception) { throw new InvalidOperationException("EL REGISTRO DE RENUMERACION ESTA INCOMPLETO; CONSERVE LOS RESPALDOS Y SOLICITE SU REVISION."); }
            }
        }
        if (last == null) throw new InvalidOperationException("EL REGISTRO DE RENUMERACION ESTA VACIO; REQUIERE REVISION.");
        return last;
    }
    static void Save(Operation op) { Append(Pending, op); Append(Path.Combine(op.Directory, "eventos.jsonl"), op); }
    public static void RequireNoPending()
    {
        if (!System.IO.File.Exists(Pending)) return;
        var op = ReadLast();
        if (op.State != "COMPLETO" && op.State != "REVERTIDO")
            throw new InvalidOperationException("HAY UNA RENUMERACION PENDIENTE. USE SOTRAMIRENUMERARRECUPERAR ANTES DE ACTUALIZAR EL EXCEL.");
    }
    static FileStream Gate()
    {
        Directory.CreateDirectory(Folder);
        try { return new FileStream(Path.Combine(Folder, "proyecto.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
        catch (IOException e) { throw new InvalidOperationException("OTRO EQUIPO ESTA ACTUALIZANDO O RENUMERANDO EL PROYECTO. INTENTE CUANDO TERMINE.", e); }
    }
    public static IDisposable AcquireForUpdate()
    {
        var gate = Gate();
        try { RequireNoPending(); return gate; } catch { gate.Dispose(); throw; }
    }
    static void Closed(string file)
    {
        foreach (Document d in Application.DocumentManager)
            if (!String.IsNullOrEmpty(d.Database.Filename) && Same(d.Database.Filename, file))
                throw new InvalidOperationException("GUARDE Y CIERRE EL PLANO ANTES DE RENUMERAR: " + Path.GetFileName(file));
        try { using (System.IO.File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { } }
        catch (IOException e) { throw new InvalidOperationException("ARCHIVO EN USO. GUARDE Y CIERRE: " + Path.GetFileName(file), e); }
    }
    static void Cover()
    {
        var d = Application.DocumentManager.MdiActiveDocument;
        if (d == null || !Same(d.Database.Filename, PackagePaths.DrawingFile))
            throw new InvalidOperationException("EJECUTE LA OPCION EN LA PORTADA.");
        if (d.IsReadOnly) throw new InvalidOperationException("LA PORTADA ESTA EN SOLO LECTURA.");
    }
    public static Preview Prepare(string oldCode, string newCode)
    {
        Cover(); RequireNoPending();
        oldCode = (oldCode ?? "").Trim().ToUpperInvariant(); newCode = (newCode ?? "").Trim().ToUpperInvariant();
        const string pattern = @"^1107\.001\.00\.14-200-\d{2}-P-\d{3}$";
        if (!Regex.IsMatch(oldCode, pattern) || !Regex.IsMatch(newCode, pattern) || oldCode == newCode)
            throw new InvalidOperationException("INDIQUE DOS CODIGOS COMPLETOS Y DIFERENTES.");
        if (oldCode.Substring(0, oldCode.Length - 3) != newCode.Substring(0, newCode.Length - 3))
            throw new InvalidOperationException("LA RENUMERACION CONSERVA LA DISCIPLINA. PARA CAMBIAR DE DISCIPLINA SE REQUIERE OTRA REVISION.");
        var source = Safe(Path.Combine(Root, oldCode + ".dwg")); var dest = Safe(Path.Combine(Root, newCode + ".dwg"));
        if (Same(source, PackagePaths.DrawingFile)) throw new InvalidOperationException("LA PORTADA NO SE RENUMERA CON ESTA OPCION.");
        if (!System.IO.File.Exists(source)) throw new FileNotFoundException("NO EXISTE EL PLANO ACTIVO A RENUMERAR.", source);
        if (System.IO.File.Exists(dest) || System.IO.File.Exists(Path.Combine(Root, "Old", newCode + ".dwg")))
            throw new InvalidOperationException("EL DESTINO YA EXISTE, ACTIVO O ARCHIVADO. NO SE REEMPLAZA.");
        if (System.IO.File.Exists(Path.Combine(Path.GetDirectoryName(PackagePaths.WorkbookFile), "~$" + Path.GetFileName(PackagePaths.WorkbookFile))))
            throw new InvalidOperationException("GUARDE Y CIERRE EL EXCEL ANTES DE RENUMERAR.");
        Closed(source); Closed(PackagePaths.WorkbookFile);
        new ExcelPlanSync().Verify(); // Requires the original code still in LE.
        var plans = ExcelPlanSync.ReadPlans(); var plan = plans.SingleOrDefault(p => p.Code == oldCode);
        if (plan == null) throw new InvalidOperationException("EL NUMERO ACTUAL DEBE SEGUIR EN EL EXCEL. NO CAMBIE SU CELDA MANUALMENTE ANTES DE USAR ESTA OPCION.");
        if (plans.Any(p => p.Code == newCode) || SheetSetSync.RegisteredCodes().Contains(newCode)) throw new InvalidOperationException("EL CODIGO DESTINO YA ESTA REGISTRADO.");
        var retired = Path.Combine(Root, "Old", "Retiros_Excel", "retiros.json");
        if (System.IO.File.Exists(retired) && System.IO.File.ReadAllText(retired).Contains(newCode)) throw new InvalidOperationException("EL CODIGO DESTINO FUE RETIRADO; NO SE REUTILIZA AUTOMATICAMENTE.");
        var b = TitleBlockSync.ReadBindings().Single(p => p.Code == oldCode);
        if (!Same(b.File, source)) throw new InvalidOperationException("EL CONJUNTO NO APUNTA AL DWG ACTIVO CORRESPONDIENTE.");
        var preview = new Preview { OldCode = oldCode, NewCode = newCode, Title = plan.Title, Source = source, Destination = dest,
            SheetId = SheetSetSync.RenumberSheetId(oldCode), SourceHash = Hash(source), WorkbookHash = Hash(PackagePaths.WorkbookFile),
            SetHash = Hash(PackagePaths.SheetSetFile), Layout = b.Layout, LayoutHandle = b.LayoutHandle };
        preview.InspectedFiles = InspectReferences(preview);
        if (Hash(source) != preview.SourceHash || Hash(PackagePaths.WorkbookFile) != preview.WorkbookHash || Hash(PackagePaths.SheetSetFile) != preview.SetHash)
            throw new InvalidOperationException("LOS ARCHIVOS CAMBIARON DURANTE LA REVISION. REPITA LA VISTA PREVIA.");
        return preview;
    }
    static Dictionary<string, string> InspectReferences(Preview plan)
    {
        var files = new HashSet<string>(TitleBlockSync.ReadBindings().Select(b => Safe(b.File)), StringComparer.OrdinalIgnoreCase);
        foreach (var f in Directory.GetFiles(Root, "*.dwg")) files.Add(Safe(f));
        var xref = Path.Combine(Root, "Xref");
        if (Directory.Exists(xref)) foreach (var f in Directory.GetFiles(xref, "*.dwg", SearchOption.AllDirectories)) files.Add(Safe(f));
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var manager = Application.DocumentManager; var original = manager.MdiActiveDocument;
        try
        {
            foreach (var f in files)
            {
                if (!System.IO.File.Exists(f)) throw new FileNotFoundException("FALTA UNA REFERENCIA DEL PROYECTO.", f);
                var before = Hash(f); Document doc = null; bool owned = false;
                try
                {
                    foreach (Document open in manager) if (!String.IsNullOrEmpty(open.Database.Filename) && Same(open.Database.Filename, f)) { doc = open; break; }
                    if (doc == null) { doc = manager.Open(f, true); owned = true; }
                    manager.MdiActiveDocument = doc;
                    using (doc.LockDocument())
                    using (var tr = doc.Database.TransactionManager.StartTransaction())
                    {
                        foreach (ObjectId id in (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead))
                        {
                            var block = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
                            if (!block.IsFromExternalReference && !block.IsFromOverlayReference) continue;
                            var path = block.PathName;
                            if (String.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("REFERENCIA DWG SIN RUTA EN " + Path.GetFileName(f));
                            var full = Path.IsPathRooted(path) ? path : Path.Combine(Path.GetDirectoryName(f), path);
                            if (Same(full, plan.Source) || Path.GetFileName(full).Equals(Path.GetFileName(plan.Source), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("OTRO DIBUJO USA O PODRIA USAR ESTE PLANO COMO REFERENCIA: " + f + ". REQUIERE COORDINAR ESA REFERENCIA ANTES DE RENUMERAR.");
                        }
                    }
                }
                finally { if (owned && doc != null) doc.CloseAndDiscard(); }
                if (Hash(f) != before) throw new InvalidOperationException("UN DWG CAMBIO DURANTE LA REVISION: " + f);
                result[f] = before;
            }
            // Data shortcuts / publish lists are not rewritten speculatively.
            var projectParent = Path.GetDirectoryName(Root);
            foreach (var f in TextReferences(projectParent))
            {
                string text;
                if (Path.GetExtension(f).Equals(".xml", StringComparison.OrdinalIgnoreCase)) text = System.Xml.Linq.XDocument.Load(f).ToString();
                else using (var reader = new StreamReader(f, Encoding.UTF8, true)) text = reader.ReadToEnd();
                if (text.IndexOf(Path.GetFileName(plan.Source), StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new InvalidOperationException("HAY UNA REFERENCIA DE DATOS O PUBLICACION A ESTE DWG: " + f + ". REVISE ESA DEPENDENCIA ANTES DE RENUMERAR.");
            }
            return result;
        }
        finally { if (original != null) manager.MdiActiveDocument = original; }
    }
    static IEnumerable<string> TextReferences(string directory)
    {
        foreach (var file in Directory.GetFiles(directory)) if (new[] { ".xml", ".dsd" }.Contains(Path.GetExtension(file).ToLowerInvariant())) yield return file;
        foreach (var sub in Directory.GetDirectories(directory))
        {
            var name = Path.GetFileName(sub);
            if (new[] { "Old", "Registros_DWG", "Renumeraciones_SOTRAMI" }.Any(n => n.Equals(name, StringComparison.OrdinalIgnoreCase))) continue;
            if ((System.IO.File.GetAttributes(sub) & FileAttributes.ReparsePoint) != 0) throw new InvalidOperationException("NO SE REVISA UN ENLACE DE CARPETA AUTOMATICAMENTE: " + sub);
            foreach (var f in TextReferences(sub)) yield return f;
        }
    }
    static void Copy(FileStream source, string destination)
    {
        source.Position = 0;
        using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None)) source.CopyTo(output);
    }
    static void CheckPreview(Preview p)
    {
        Closed(p.Source); Closed(PackagePaths.WorkbookFile);
        if (System.IO.File.Exists(p.Destination) || Hash(p.Source) != p.SourceHash || Hash(PackagePaths.WorkbookFile) != p.WorkbookHash || Hash(PackagePaths.SheetSetFile) != p.SetHash)
            throw new InvalidOperationException("CAMBIO EL ORIGEN, EL EXCEL, SOTRAMI O EL DESTINO DESDE LA VISTA PREVIA. REPITA LA OPERACION.");
        foreach (var pair in p.InspectedFiles) if (Hash(pair.Key) != pair.Value) throw new InvalidOperationException("UN DWG CAMBIO DESDE LA REVISION: " + pair.Key);
    }
    public static Operation Apply(Preview plan)
    {
        Cover();
        using (Gate())
        {
            RequireNoPending(); CheckPreview(plan);
            var manager = Application.DocumentManager; var original = manager.MdiActiveDocument;
            var token = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            var dir = Safe(Path.Combine(Folder, token)); Directory.CreateDirectory(dir);
            var op = new Operation { Plan = plan, Directory = dir, State = "PREPARANDO", Utc = DateTime.UtcNow.ToString("o"), User = Environment.UserDomainName + "\\" + Environment.UserName,
                PreparedDwg = Safe(Path.Combine(Root, "SOTRAMI_RENUM_" + token + ".dwg")), PreparedWorkbook = Safe(Path.Combine(Root, "SOTRAMI_RENUM_" + token + ".xlsx")) };
            Document copy = null; bool durable = false;
            try
            {
                using (var dwg = new FileStream(plan.Source, FileMode.Open, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete))
                using (var book = new FileStream(PackagePaths.WorkbookFile, FileMode.Open, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete))
                {
                    Copy(dwg, Path.Combine(dir, "ORIGINAL.dwg")); Copy(book, Path.Combine(dir, "ORIGINAL.xlsx"));
                    System.IO.File.Copy(PackagePaths.SheetSetFile, Path.Combine(dir, "ORIGINAL.dst"));
                    if (Hash(Path.Combine(dir, "ORIGINAL.dwg")) != plan.SourceHash || Hash(Path.Combine(dir, "ORIGINAL.xlsx")) != plan.WorkbookHash || Hash(Path.Combine(dir, "ORIGINAL.dst")) != plan.SetHash)
                        throw new InvalidOperationException("EL RESPALDO NO COINCIDE CON LA VISTA PREVIA.");
                    Copy(dwg, op.PreparedDwg); Copy(book, op.PreparedWorkbook);
                    op.Cell = RenumberWorkbook.Prepare(op.PreparedWorkbook, plan.OldCode, plan.NewCode); op.PreparedWorkbookHash = Hash(op.PreparedWorkbook);
                    Save(op); durable = true; // Durable before changing sheet metadata or originals.
                    copy = manager.Open(op.PreparedDwg, false); manager.MdiActiveDocument = copy;
                    if (copy.IsReadOnly) throw new InvalidOperationException("NO SE PUEDE EDITAR LA COPIA PREPARADA.");
                    var oldBinding = TitleBlockSync.ReadBindings().Single(b => b.Code == plan.OldCode);
                    TitleBlockAudit.Check before;
                    using (copy.LockDocument()) { copy.Editor.Regen(); before = TitleBlockAudit.Inspect(copy, oldBinding); }
                    if (!before.Success) throw new InvalidOperationException("EL MEMBRETE ORIGINAL NO COINCIDE CON SU CODIGO: " + before.Error);
                    var geometry = Fingerprint(copy);
                    SheetSetSync.Renumber(plan.SheetId, plan.OldCode, plan.NewCode, plan.Source, plan.Destination);
                    op.State = "CONJUNTO_CAMBIADO"; Save(op);
                    var binding = new TitleBlockBinding { Code = plan.NewCode, File = plan.Destination, Title = oldBinding.Title, Layout = oldBinding.Layout, LayoutHandle = oldBinding.LayoutHandle,
                        Line1 = oldBinding.Line1, Line2 = oldBinding.Line2, Field1 = oldBinding.Field1, Field2 = oldBinding.Field2, FieldNumber = oldBinding.FieldNumber };
                    var applied = TitleBlockSync.ApplyExplicit(copy, binding, plan.OldCode);
                    if (applied == null || !applied.Success || Fingerprint(copy) != geometry) throw new InvalidOperationException("NO SE VERIFICO EL MEMBRETE O LA CONSERVACION DEL CONTENIDO: " + (applied == null ? "" : applied.Error));
                    using (copy.LockDocument()) copy.Database.SaveAs(op.PreparedDwg, true, DwgVersion.Current, copy.Database.SecurityParameters);
                    copy.CloseAndDiscard(); copy = null; op.PreparedDwgHash = Hash(op.PreparedDwg); op.State = "PREPARADO"; Save(op);
                    // File.Move never replaces an existing destination. Originals
                    // remain leased against concurrent writes until publication.
                    System.IO.File.Move(plan.Source, Path.Combine(dir, "RETENIDO.dwg"));
                    System.IO.File.Move(op.PreparedDwg, plan.Destination); op.State = "DWG_PUBLICADO"; Save(op);
                    System.IO.File.Move(PackagePaths.WorkbookFile, Path.Combine(dir, "RETENIDO.xlsx"));
                    System.IO.File.Move(op.PreparedWorkbook, PackagePaths.WorkbookFile); op.State = "EXCEL_PUBLICADO"; Save(op);
                    if (SheetSetSync.Execute(false) != 0) throw new InvalidOperationException("EXCEL Y CONJUNTO NO COINCIDEN TRAS EL CAMBIO.");
                    copy = manager.Open(plan.Destination, true); manager.MdiActiveDocument = copy;
                    using (copy.LockDocument()) { copy.Editor.Regen(); var persisted = TitleBlockAudit.Inspect(copy, TitleBlockSync.ReadBindings().Single(b => b.Code == plan.NewCode)); if (!persisted.Success || Fingerprint(copy) != geometry) throw new InvalidOperationException("FALLO AL REABRIR EL PLANO NUEVO: " + persisted.Error); }
                    copy.CloseAndDiscard(); copy = null;
                    op.State = "ARCHIVOS_VERIFICADOS"; Save(op);
                }
                manager.MdiActiveDocument = original;
                using (original.LockDocument()) ExcelPlanSync.UpdateTableFromPlans(original, ExcelPlanSync.ReadPlans());
                new ExcelPlanSync().Verify();
                op.State = "COMPLETO"; Save(op);
                System.IO.File.WriteAllText(Path.Combine(dir, "RENUMERACION.txt"), "RENUMERACION CON CONTENIDO CONSERVADO\r\n" + plan.OldCode + " -> " + plan.NewCode + "\r\nTITULO: " + plan.Title + "\r\nUTC: " + op.Utc + "\r\nUSUARIO: " + op.User + "\r\nIDENTIDAD DE HOJA CONSERVADA: " + plan.SheetId + "\r\nRESPALDOS: ORIGINAL.dwg / ORIGINAL.xlsx / ORIGINAL.dst\r\n", Encoding.UTF8);
                return op;
            }
            catch (System.Exception e)
            {
                if (copy != null) { copy.CloseAndDiscard(); copy = null; }
                op.Error = e.Message;
                // Once the prepared DWG has passed its read-back verification,
                // a table refresh failure must not undo already committed files.
                if (op.State == "ARCHIVOS_VERIFICADOS") { Save(op); throw new InvalidOperationException("ARCHIVOS RENUMERADOS Y VERIFICADOS. FALTA REFRESCAR LA PORTADA; USE SOTRAMIRENUMERARRECUPERAR. " + e.Message, e); }
                if (durable)
                    try { Rollback(op); } catch (System.Exception recovery) { op.State = "REQUIERE_RECUPERACION"; op.Error += " | " + recovery.Message; Save(op); }
                throw new InvalidOperationException("RENUMERACION NO COMPLETADA: " + e.Message + ". ESTADO: " + op.State, e);
            }
            finally
            {
                if (copy != null) copy.CloseAndDiscard();
                if (original != null) manager.MdiActiveDocument = original;
                QuarantineWork(op);
            }
        }
    }
    static string Fingerprint(Document doc)
    {
        // Entity identities and spatial extents, including all model/paper
        // entities. Only title/number attributes and sheet hints may change.
        var parts = new List<string>();
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        foreach (ObjectId b in (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead))
        {
            var block = (BlockTableRecord)tr.GetObject(b, OpenMode.ForRead); if (block.IsFromExternalReference) continue;
            foreach (ObjectId id in block)
            {
                var e = tr.GetObject(id, OpenMode.ForRead) as Entity; if (e == null || e is AttributeDefinition || e is AttributeReference) continue;
                string box = ""; try { box = e.GeometricExtents.ToString(); } catch (Autodesk.AutoCAD.Runtime.Exception) { }
                parts.Add(b.Handle + ":" + id.Handle + ":" + id.ObjectClass.DxfName + ":" + box);
            }
        }
        return String.Join("|", parts.OrderBy(s => s, StringComparer.Ordinal));
    }
    static void Restore(string original, string backup, string originalHash, string publishedHash, string dir, string suffix)
    {
        if (Hash(backup) != originalHash) throw new InvalidOperationException("RESPALDO ALTERADO; NO SE RESTAURA: " + backup);
        if (System.IO.File.Exists(original))
        {
            if (Hash(original) == originalHash) return;
            if (String.IsNullOrEmpty(publishedHash) || Hash(original) != publishedHash) throw new InvalidOperationException("EL ARCHIVO FUE EDITADO DESPUES; NO SE REEMPLAZA: " + original);
            System.IO.File.Move(original, Path.Combine(dir, "REVERTIDO_" + Guid.NewGuid().ToString("N").Substring(0, 12) + suffix));
        }
        System.IO.File.Copy(backup, original, false);
    }
    static void ValidateOperation(Operation op)
    {
        if (op == null || op.Plan == null || !Path.GetFullPath(op.Directory).StartsWith(Folder + "\\", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("DIARIO DE RENUMERACION INVALIDO.");
        Safe(op.Directory); Safe(op.Plan.Source); Safe(op.Plan.Destination); Safe(op.PreparedDwg); Safe(op.PreparedWorkbook);
        if (!Same(op.Plan.Source, Path.Combine(Root, op.Plan.OldCode + ".dwg")) || !Same(op.Plan.Destination, Path.Combine(Root, op.Plan.NewCode + ".dwg"))) throw new InvalidOperationException("RUTAS DEL DIARIO INVALIDAS.");
    }
    static void QuarantineWork(Operation op)
    {
        foreach (var file in new[] { op.PreparedDwg, op.PreparedWorkbook, Path.ChangeExtension(op.PreparedDwg, ".bak") })
            if (System.IO.File.Exists(Safe(file)))
                try { System.IO.File.Move(file, Path.Combine(op.Directory, "TRABAJO_" + Guid.NewGuid().ToString("N").Substring(0, 12) + Path.GetExtension(file))); }
                catch (IOException) { /* Retain a busy work file; never force its removal. */ }
    }
    static void Rollback(Operation op)
    {
        ValidateOperation(op);
        foreach (Document d in Application.DocumentManager)
            if (!String.IsNullOrEmpty(d.Database.Filename) && (Same(d.Database.Filename, op.Plan.Source) || Same(d.Database.Filename, op.Plan.Destination))) throw new InvalidOperationException("CIERRE LOS PLANOS DEL CAMBIO ANTES DE RECUPERAR.");
        var current = SheetSetSync.RenumberCurrent(op.Plan.SheetId);
        if (current != op.Plan.OldCode && current != op.Plan.NewCode) throw new InvalidOperationException("LA HOJA FUE CAMBIADA POR OTRA OPERACION.");
        if (Hash(Path.Combine(op.Directory, "ORIGINAL.dwg")) != op.Plan.SourceHash || Hash(Path.Combine(op.Directory, "ORIGINAL.xlsx")) != op.Plan.WorkbookHash)
            throw new InvalidOperationException("RESPALDO ALTERADO; NO SE RESTAURA.");
        if (System.IO.File.Exists(op.Plan.Source) && Hash(op.Plan.Source) != op.Plan.SourceHash)
            throw new InvalidOperationException("EL ORIGEN FUE EDITADO DESPUES; NO SE REEMPLAZA.");
        if (System.IO.File.Exists(PackagePaths.WorkbookFile))
        {
            var hash = Hash(PackagePaths.WorkbookFile);
            if (hash != op.Plan.WorkbookHash && hash != op.PreparedWorkbookHash) throw new InvalidOperationException("EL EXCEL FUE EDITADO DESPUES; NO SE REEMPLAZA.");
            Closed(PackagePaths.WorkbookFile);
        }
        if (System.IO.File.Exists(op.Plan.Destination))
        {
            if (String.IsNullOrEmpty(op.PreparedDwgHash) || Hash(op.Plan.Destination) != op.PreparedDwgHash) throw new InvalidOperationException("EL DESTINO FUE EDITADO; NO SE RETIRA AUTOMATICAMENTE.");
            System.IO.File.Move(op.Plan.Destination, Path.Combine(op.Directory, "DESTINO_REVERTIDO_" + Guid.NewGuid().ToString("N").Substring(0, 12) + ".dwg"));
        }
        Restore(op.Plan.Source, Path.Combine(op.Directory, "ORIGINAL.dwg"), op.Plan.SourceHash, null, op.Directory, ".dwg");
        Restore(PackagePaths.WorkbookFile, Path.Combine(op.Directory, "ORIGINAL.xlsx"), op.Plan.WorkbookHash, op.PreparedWorkbookHash, op.Directory, ".xlsx");
        if (current == op.Plan.NewCode) SheetSetSync.Renumber(op.Plan.SheetId, op.Plan.NewCode, op.Plan.OldCode, op.Plan.Destination, op.Plan.Source);
        if (SheetSetSync.Execute(false) != 0) throw new InvalidOperationException("EL CONJUNTO RESTAURADO REQUIERE REVISION.");
        op.State = "REVERTIDO"; Save(op); QuarantineWork(op);
    }
    public static void Recover()
    {
        Cover(); using (Gate())
        {
            if (!System.IO.File.Exists(Pending)) throw new InvalidOperationException("NO HAY UNA RENUMERACION PENDIENTE.");
            var op = ReadLast(); ValidateOperation(op);
            if (op.State == "COMPLETO" || op.State == "REVERTIDO") return;
            if (op.State == "ARCHIVOS_VERIFICADOS")
            {
                if (Hash(op.Plan.Destination) != op.PreparedDwgHash || Hash(PackagePaths.WorkbookFile) != op.PreparedWorkbookHash || SheetSetSync.Execute(false) != 0)
                    throw new InvalidOperationException("LOS ARCHIVOS CAMBIARON TRAS LA INTERRUPCION; NO SE SOBRESCRIBEN.");
                using (Application.DocumentManager.MdiActiveDocument.LockDocument()) ExcelPlanSync.UpdateTableFromPlans(Application.DocumentManager.MdiActiveDocument, ExcelPlanSync.ReadPlans());
                new ExcelPlanSync().Verify(); op.State = "COMPLETO"; Save(op);
            }
            else
            {
                Rollback(op);
                using (Application.DocumentManager.MdiActiveDocument.LockDocument()) ExcelPlanSync.UpdateTableFromPlans(Application.DocumentManager.MdiActiveDocument, ExcelPlanSync.ReadPlans());
                new ExcelPlanSync().Verify();
            }
        }
    }
    [CommandMethod("SOTRAMIRENUMERAR", CommandFlags.Session)]
    [CommandMethod("SOTRAMICAMBIARNUMERO", CommandFlags.Session)]
    public void Command()
    {
        var doc = Application.DocumentManager.MdiActiveDocument; if (doc == null) return;
        try
        {
            Cover(); var old = doc.Editor.GetString(new PromptStringOptions("\nCODIGO COMPLETO ACTUAL DEL PLANO: ") { AllowSpaces = false }); if (old.Status != PromptStatus.OK) return;
            var next = doc.Editor.GetString(new PromptStringOptions("\nCODIGO COMPLETO NUEVO: ") { AllowSpaces = false }); if (next.Status != PromptStatus.OK) return;
            doc.Editor.WriteMessage("\nREVISANDO EL EXCEL, LOS ARCHIVOS Y SUS REFERENCIAS.\n");
            var preview = Prepare(old.StringResult, next.StringResult);
            doc.Editor.WriteMessage("\nVISTA PREVIA: " + preview.OldCode + " -> " + preview.NewCode + "\nTITULO: " + preview.Title + "\nCONSERVA EL CONTENIDO Y LA MISMA HOJA SOTRAMI. ACTUALIZA LA CELDA DEL EXCEL, EL DWG Y LA TABLA.\nDESTINO: " + preview.Destination + "\nRESPALDO: " + Folder + "\n");
            var options = new PromptKeywordOptions("\nAPLICAR EL CAMBIO [Aplicar/Cancelar] <Cancelar>: "); options.Keywords.Add("Aplicar"); options.Keywords.Add("Cancelar"); options.Keywords.Default = "Cancelar"; options.AllowNone = true;
            var answer = doc.Editor.GetKeywords(options); if (answer.Status != PromptStatus.OK || answer.StringResult != "Aplicar") { doc.Editor.WriteMessage("\nRENUMERACION CANCELADA. ARCHIVOS CONSERVADOS.\n"); return; }
            var done = Apply(preview); doc.Editor.WriteMessage("\nRENUMERACION_OK: " + done.Plan.OldCode + " -> " + done.Plan.NewCode + ". CONTENIDO Y VINCULO VERIFICADOS AL REABRIR. GUARDE LA PORTADA CON CTRL+S.\nRESPALDO: " + done.Directory + "\n");
        }
        catch (System.Exception e) { doc.Editor.WriteMessage("\nRENUMERACION_PENDIENTE: " + e.Message + "\n"); }
    }
    [CommandMethod("SOTRAMIRENUMERARRECUPERAR", CommandFlags.Session)]
    public void RecoverCommand()
    {
        var doc = Application.DocumentManager.MdiActiveDocument; if (doc == null) return;
        try { Recover(); doc.Editor.WriteMessage("\nRENUMERACION_RECUPERADA. REVISE EL INFORME Y GUARDE LA PORTADA.\n"); }
        catch (System.Exception e) { doc.Editor.WriteMessage("\nRENUMERACION_RECUPERACION_PENDIENTE: " + e.Message + "\n"); }
    }
}
