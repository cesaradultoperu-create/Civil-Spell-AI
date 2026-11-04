using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

public class TitleBlockAudit
{
    static readonly JsonCodec Json = new JsonCodec();
    public class Check
    {
        public bool Success { get; set; }
        public string Code { get; set; }
        public string File { get; set; }
        public string Error { get; set; }
        public object[] Attributes { get; set; }
        public bool Repaired { get; set; }
        public bool Saved { get; set; }
        public bool OpenUserDrawing { get; set; }
        public string Backup { get; set; }
        public string RegisteredLayout { get; set; }
        public string ActualLayout { get; set; }
        public string ActualHandle { get; set; }
        public bool LayoutMismatch { get; set; }
    }
    static string Plain(string value) => (value ?? "").Trim();
    static string Bare(string value) => (value ?? "").Replace("%<", "").Replace(">%", "").Trim();
    public static Check Inspect(Document doc, TitleBlockBinding binding)
    {
        var result = new Check { Code = binding.Code, File = binding.File };
        var rows = new List<object>();
        try
        {
            var actual = SheetSetSync.ActualLayout(doc.Database, binding.Layout, binding.Code);
            result.RegisteredLayout = binding.Layout; result.ActualLayout = actual.Name; result.ActualHandle = actual.Handle;
            result.LayoutMismatch = !actual.Name.Equals(binding.Layout, StringComparison.OrdinalIgnoreCase) || (!String.IsNullOrEmpty(binding.LayoutHandle) && !actual.Handle.Equals(binding.LayoutHandle, StringComparison.OrdinalIgnoreCase));
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var layouts = (DBDictionary)tr.GetObject(doc.Database.LayoutDictionaryId, OpenMode.ForRead);
                var l = (Layout)tr.GetObject(layouts.GetAt(actual.Name), OpenMode.ForRead);
                var candidates = new List<AttributeReference[]>();
                foreach (ObjectId id in (BlockTableRecord)tr.GetObject(l.BlockTableRecordId, OpenMode.ForRead))
                {
                    var block = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                    if (block == null) continue;
                    var attrs = block.AttributeCollection.Cast<ObjectId>().Select(a => tr.GetObject(a, OpenMode.ForRead) as AttributeReference).Where(a => a != null).ToArray();
                    if (new[] { "TITTLE_DRAW1", "TITTLE_DRAW2", "DRAWING_NO" }.All(tag => attrs.Count(a => a.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase)) == 1)) candidates.Add(attrs);
                }
                var matching = candidates.Where(a => Plain(a.Single(x => x.Tag.Equals("DRAWING_NO", StringComparison.OrdinalIgnoreCase)).TextString) == binding.Code).ToArray();
                if (matching.Length == 1) candidates = matching.ToList();
                if (candidates.Count != 1) throw new InvalidOperationException("MEMBRETE AMBIGUO: " + binding.Layout);
                var errors = new List<string>();
                if (result.LayoutMismatch) errors.Add("REFERENCIA DE PRESENTACION: " + binding.Layout + " -> " + actual.Name + " (HANDLE " + actual.Handle + ")");
                foreach (var item in new[] { Tuple.Create("TITTLE_DRAW1", binding.Line1, binding.Field1), Tuple.Create("TITTLE_DRAW2", binding.Line2, binding.Field2), Tuple.Create("DRAWING_NO", binding.Code, binding.FieldNumber) })
                {
                    var a = candidates[0].Single(x => x.Tag.Equals(item.Item1, StringComparison.OrdinalIgnoreCase));
                    var f = a.HasFields ? (Field)tr.GetObject(a.GetField(), OpenMode.ForRead) : null;
                    var text = a.TextString;
                    var value = f == null ? "" : Convert.ToString(f.Value);
                    string multiline = null;
                    if (a.IsMTextAttribute) using (var mt = a.MTextAttribute) multiline = mt.Text;
                    bool ok = f != null && f.EvaluationStatus.Status == FieldEvaluationStatus.Success && Bare(f.GetFieldCode()) == Bare(item.Item3)
                        && Plain(value) == Plain(item.Item2) && Plain(text) == Plain(item.Item2) && (multiline == null || Plain(multiline) == Plain(item.Item2));
                    rows.Add(new { layout = actual.Name, handle = a.Handle.ToString(), tag = a.Tag, expected = item.Item2, visible = text, multiline, value, field = f == null ? null : f.GetFieldCode(), success = ok });
                    if (!ok) errors.Add(item.Item1 + ": VISIBLE=" + text + "; ESPERADO=" + item.Item2);
                }
                result.Success = errors.Count == 0;
                result.Error = result.Success ? null : String.Join(" | ", errors);
            }
        }
        catch (System.Exception error) { result.Error = error.Message; }
        result.Attributes = rows.ToArray();
        return result;
    }
    [CommandMethod("SOTRAMIREVISARTODOS", CommandFlags.Session)]
    public void RepairAll() { Run(true); }
    [CommandMethod("SOTRAMIAUDITARTODOS", CommandFlags.Session)]
    public void AuditAll() { Run(false); }
    static string Geometry(Document doc)
    {
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            var parts = new List<string>();
            foreach (ObjectId blockId in (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead))
            {
                var block = (BlockTableRecord)tr.GetObject(blockId, OpenMode.ForRead);
                if (block.IsFromExternalReference) continue;
                foreach (ObjectId id in block)
                {
                    var obj = tr.GetObject(id, OpenMode.ForRead);
                    if (obj is AttributeDefinition || obj is AttributeReference) continue;
                    var entity = obj as Entity;
                    if (entity != null) parts.Add(id.Handle + ":" + id.ObjectClass.DxfName + ":" + blockId.Handle);
                }
            }
            return String.Join("|", parts.OrderBy(s => s, StringComparer.Ordinal));
        }
    }
    public static void Run(bool repair)
    {
        var manager = Application.DocumentManager;
        var original = manager.MdiActiveDocument;
        var results = new List<Check>();
        var report = Path.Combine(PackagePaths.LogDirectory, "verificacion-real-todos.json");
        var backupRoot = Path.Combine(PackagePaths.ProjectDirectory, "Old", "Respaldo_vinculos_v33_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        try
        {
            DrawingRenumber.RequireNoPending();
            System.IO.File.WriteAllText(report, Json.Serialize(new { finished = false, repair, stage = "LEYENDO CONJUNTO CANONICO", results }));
            // Refresh bindings from the live canonical set, never from a stale
            // per-computer list. This operation does not modify the sheet set.
            if (SheetSetSync.Execute(false) != 0) throw new InvalidOperationException("ACTUALICE EL EXCEL EN LA PORTADA ANTES DE REVISAR LOS MEMBRETES.");
            var bindings = TitleBlockSync.ReadBindings();
            foreach (var binding in bindings)
            {
                Document doc = null;
                bool owned = false;
                var row = new Check { Code = binding.Code, File = binding.File };
                try
                {
                    if (!Path.GetFileNameWithoutExtension(binding.File).Equals(binding.Code, StringComparison.OrdinalIgnoreCase) || !Path.GetFullPath(binding.File).StartsWith(Path.GetFullPath(PackagePaths.ProjectDirectory).TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("ARCHIVO Y CODIGO NO CORRESPONDEN; NO SE MODIFICA.");
                    foreach (Document open in manager)
                        if (!String.IsNullOrEmpty(open.Database.Filename) && String.Equals(Path.GetFullPath(open.Database.Filename), Path.GetFullPath(binding.File), StringComparison.OrdinalIgnoreCase)) { doc = open; break; }
                    row.OpenUserDrawing = doc != null;
                    if (doc == null)
                    {
                        if (repair)
                        {
                            try { using (System.IO.File.Open(binding.File, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { } }
                            catch (IOException ex) { throw new InvalidOperationException("PENDIENTE: EL ARCHIVO ESTA ABIERTO EN OTRO EQUIPO.", ex); }
                        }
                        doc = manager.Open(binding.File, !repair); owned = true;
                    }
                    manager.MdiActiveDocument = doc;
                    using (doc.LockDocument()) { doc.Editor.Regen(); row = Inspect(doc, binding); }
                    row.OpenUserDrawing = !owned;
                    if (!row.Success && repair)
                    {
                        if (doc.IsReadOnly) throw new InvalidOperationException("PENDIENTE: ARCHIVO SOLO PARA LECTURA.");
                        Directory.CreateDirectory(backupRoot);
                        var backup = Path.Combine(backupRoot, binding.Code + ".dwg");
                        using (var input = System.IO.File.Open(binding.File, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var output = new FileStream(backup, FileMode.CreateNew, FileAccess.Write, FileShare.None)) input.CopyTo(output);
                        if (row.LayoutMismatch)
                        {
                            var actual = SheetSetSync.RepairOpenLayout(doc, binding.Code, backupRoot);
                            binding.Layout = actual.Name; binding.LayoutHandle = actual.Handle;
                        }
                        var geometry = Geometry(doc);
                        var applied = TitleBlockSync.Apply(doc);
                        if (applied == null || !applied.Success) throw new InvalidOperationException(applied == null ? "SIN VINCULO" : applied.Error);
                        using (doc.LockDocument()) { doc.Editor.Regen(); row = Inspect(doc, binding); }
                        row.OpenUserDrawing = !owned; row.Repaired = true; row.Backup = backup;
                        if (!row.Success) throw new InvalidOperationException("MEMBRETE VISIBLE NO COINCIDE: " + row.Error);
                        if (geometry != Geometry(doc)) throw new InvalidOperationException("CAMBIO INESPERADO EN ENTIDADES; NO SE GUARDA.");
                        if (owned)
                        {
                            using (doc.LockDocument()) doc.Database.SaveAs(binding.File, true, DwgVersion.Current, doc.Database.SecurityParameters);
                            doc.CloseAndDiscard(); doc = null;
                            doc = manager.Open(binding.File, true); manager.MdiActiveDocument = doc;
                            using (doc.LockDocument()) { doc.Editor.Regen(); var persisted = Inspect(doc, binding); if (!persisted.Success) throw new InvalidOperationException("FALLO AL REABRIR: " + persisted.Error); }
                            row.Saved = true;
                        }
                    }
                }
                catch (System.Exception error) { row.Success = false; row.Error = error.Message; }
                finally { if (owned && doc != null) doc.CloseAndDiscard(); }
                results.Add(row);
                System.IO.File.WriteAllText(report, Json.Serialize(new { finished = false, repair, results }));
            }
        }
        catch (System.Exception error) { results.Add(new Check { Success = false, Error = error.ToString() }); }
        finally { if (original != null) manager.MdiActiveDocument = original; }
        System.IO.File.WriteAllText(report, Json.Serialize(new { finished = true, repair, success = results.Count > 0 && results.All(r => r.Success), total = results.Count, correct = results.Count(r => r.Success), pending = results.Count(r => !r.Success), userDrawingsToSave = results.Where(r => r.OpenUserDrawing && r.Success).Select(r => r.Code).ToArray(), results }));
        if (original != null) original.Editor.WriteMessage("\nMEMBRETES REVISADOS: " + results.Count(r => r.Success) + " CORRECTOS; " + results.Count(r => !r.Success) + " PENDIENTES. GUARDE LOS DIBUJOS QUE YA TENIA ABIERTOS.\n");
    }
}
