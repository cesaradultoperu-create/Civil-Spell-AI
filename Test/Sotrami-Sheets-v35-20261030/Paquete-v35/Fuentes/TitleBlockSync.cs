using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

public class TitleBlockSync
{
    static readonly JsonCodec Json = new JsonCodec();
    class Candidate
    {
        public string Layout;
        public ObjectId Block;
        public ObjectId First;
        public ObjectId Second;
        public ObjectId Number;
        public string Code;
    }
    public class Result
    {
        public bool Success { get; set; }
        public string Code { get; set; }
        public string File { get; set; }
        public string Title { get; set; }
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public int Blocks { get; set; }
        public string[] ExistingDrawingCodes { get; set; }
        public bool DrawingCodeMismatch { get; set; }
        public object[] Attributes { get; set; }
        public string Error { get; set; }
    }
    public static TitleBlockBinding[] ReadBindings() => Json.Deserialize<TitleBlockBinding[]>(File.ReadAllText(Path.Combine(PackagePaths.LogDirectory, "titleblock-bindings.json")));
    [CommandMethod("VERIFICARROTULOSSOTRAMI", CommandFlags.Session)]
    public void VerifyFields() { TitleBlockAudit.Run(false); }
    public static Result Apply(Document doc)
    {
        try { DrawingRenumber.RequireNoPending(); }
        catch (System.Exception e) { return new Result { Success = false, File = doc.Database.Filename, Error = e.Message }; }
        var binding = ReadBindings().SingleOrDefault(p => String.Equals(Path.GetFullPath(p.File), Path.GetFullPath(doc.Database.Filename), StringComparison.OrdinalIgnoreCase));
        if (binding == null) return null;
        return ApplyExplicit(doc, binding, null);
    }
    internal static Result ApplyExplicit(Document doc, TitleBlockBinding binding, string previousCode)
    {
        var result = new Result { Code = binding.Code, File = doc.Database.Filename, Title = binding.Title, Line1 = binding.Line1, Line2 = binding.Line2 };
        var folder = Directory.CreateDirectory(Path.Combine(PackagePaths.LogDirectory, "Rotulos")).FullName;
        var before = new List<object>();
        var changed = new List<object>();
        var previousDatabase = HostApplicationServices.WorkingDatabase;
        string previousLayout = null;
        try
        {
            if (Application.DocumentManager.MdiActiveDocument != doc) throw new InvalidOperationException("ACTIVE EL DIBUJO ANTES DE REPARAR SU MEMBRETE.");
            using (doc.LockDocument())
            {
                HostApplicationServices.WorkingDatabase = doc.Database;
                previousLayout = LayoutManager.Current.CurrentLayout;
                LayoutManager.Current.CurrentLayout = binding.Layout;
                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    var candidates = new List<Candidate>();
                    var layouts = (DBDictionary)tr.GetObject(doc.Database.LayoutDictionaryId, OpenMode.ForRead);
                    foreach (DBDictionaryEntry entry in layouts)
                    {
                        var layout = (Layout)tr.GetObject(entry.Value, OpenMode.ForRead);
                        // The registered layout is authoritative. Other layouts are
                        // included only when their drawing number matches this sheet.
                        if (layout.ModelType && binding.Layout != "Model") continue;
                        var space = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead);
                        foreach (ObjectId id in space)
                        {
                            var insert = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                            if (insert == null) continue;
                            var candidate = new Candidate { Layout = layout.LayoutName, Block = id, Code = "" };
                            foreach (ObjectId attributeId in insert.AttributeCollection)
                            {
                                var attribute = tr.GetObject(attributeId, OpenMode.ForRead) as AttributeReference;
                                if (attribute == null) continue;
                                if (attribute.Tag.Equals("TITTLE_DRAW1", StringComparison.OrdinalIgnoreCase)) candidate.First = attributeId;
                                else if (attribute.Tag.Equals("TITTLE_DRAW2", StringComparison.OrdinalIgnoreCase)) candidate.Second = attributeId;
                                else if (attribute.Tag.Equals("DRAWING_NO", StringComparison.OrdinalIgnoreCase)) { candidate.Code = attribute.TextString.Trim(); candidate.Number = attributeId; }
                            }
                            if (!candidate.First.IsNull && !candidate.Second.IsNull) candidates.Add(candidate);
                        }
                    }
                    var primary = candidates.Where(p => p.Layout.Equals(binding.Layout, StringComparison.OrdinalIgnoreCase)).ToArray();
                    var matching = primary.Where(p => p.Code.Equals(binding.Code, StringComparison.OrdinalIgnoreCase)).ToArray();
                    if (matching.Length == 1) primary = matching;
                    if (primary.Length != 1) throw new InvalidOperationException("No hay un rotulo unico en la presentacion " + binding.Layout + " de " + binding.Code);
                    var selected = new List<Candidate> { primary[0] };
                    selected.AddRange(candidates.Where(p => p.Block != primary[0].Block && (p.Code.Equals(binding.Code, StringComparison.OrdinalIgnoreCase) || (!String.IsNullOrEmpty(previousCode) && p.Code.Equals(previousCode, StringComparison.OrdinalIgnoreCase)))));
                    result.ExistingDrawingCodes = selected.Select(p => p.Code).ToArray();
                    result.DrawingCodeMismatch = selected.Any(p => !p.Code.Equals(binding.Code, StringComparison.OrdinalIgnoreCase));
                    foreach (var candidate in selected)
                    foreach (var tuple in new[] { Tuple.Create(candidate.First, binding.Field1, binding.Line1), Tuple.Create(candidate.Second, binding.Field2, binding.Line2), Tuple.Create(candidate.Number, binding.FieldNumber, binding.Code) })
                    {
                        if (tuple.Item1.IsNull) continue;
                        var attribute = (AttributeReference)tr.GetObject(tuple.Item1, OpenMode.ForWrite);
                        var oldField = attribute.HasFields ? (Field)tr.GetObject(attribute.GetField(), OpenMode.ForRead) : null;
                        before.Add(new { layout = candidate.Layout, handle = attribute.Handle.ToString(), tag = attribute.Tag, text = attribute.TextString, field = oldField == null ? null : oldField.GetFieldCode(), isTextField = oldField != null && oldField.IsTextField });
                        var marked = "%<" + tuple.Item2 + ">%";
                        if (oldField != null)
                        {
                            attribute.RemoveField();
                            oldField.UpgradeOpen();
                            oldField.Erase();
                        }
                        // A legacy text field can retain its former inline MTEXT
                        // contents even after its owner field has been replaced.
                        // Clear that cached content before installing the new field;
                        // retain the existing attribute and MTEXT geometry/styles.
                        attribute.TextString = tuple.Item3.Length == 0 ? "\u00A0" : tuple.Item3;
                        if (attribute.IsMTextAttribute)
                        {
                            using (var mt = attribute.MTextAttribute)
                            {
                                mt.Contents = attribute.TextString;
                                attribute.MTextAttribute = mt;
                            }
                            attribute.UpdateMTextAttribute();
                        }
                        var field = new Field(marked);
                        attribute.SetField(field);
                        tr.AddNewlyCreatedDBObject(field, true);
                        field.EvaluationOption = FieldEvaluationOptions.Automatic;
                        field.Evaluate((int)FieldEvaluationContext.Demand, doc.Database);
                        var evaluated = Convert.ToString(field.Value) ?? "";
                        if (field.EvaluationStatus.Status != FieldEvaluationStatus.Success || evaluated.Trim() != tuple.Item3.Trim())
                            throw new InvalidOperationException("El campo " + attribute.Tag + " no coincide con Excel: " + evaluated);
                        if (attribute.IsMTextAttribute) attribute.UpdateMTextAttribute();
                        attribute.AdjustAlignment(doc.Database);
                        attribute.RecordGraphicsModified(true);
                        changed.Add(new { layout = candidate.Layout, handle = attribute.Handle.ToString(), tag = attribute.Tag, expected = tuple.Item3, value = evaluated, field = field.GetFieldCode(), displayed = attribute.TextString });
                    }
                    result.Blocks = selected.Count;
                    result.Attributes = changed.ToArray();
                    var beforePath = Path.Combine(folder, binding.Code + "-antes.json");
                    if (!File.Exists(beforePath)) File.WriteAllText(beforePath, Json.Serialize(before));
                    var numberBeforePath = Path.Combine(folder, binding.Code + "-antes-numero.json");
                    if (!File.Exists(numberBeforePath)) File.WriteAllText(numberBeforePath, Json.Serialize(before));
                    tr.Commit();
                    result.Success = true;
                }
                doc.Editor.Regen();
                var visible = TitleBlockAudit.Inspect(doc, binding);
                result.Success = visible.Success; result.Error = visible.Error; result.Attributes = visible.Attributes;
            }
        }
        catch (System.Exception error) { result.Error = error.ToString(); }
        finally
        {
            if (previousLayout != null)
                try { using (doc.LockDocument()) { HostApplicationServices.WorkingDatabase = doc.Database; LayoutManager.Current.CurrentLayout = previousLayout; } }
                catch (System.Exception) { }
            HostApplicationServices.WorkingDatabase = previousDatabase;
        }
        File.WriteAllText(Path.Combine(folder, binding.Code + ".json"), Json.Serialize(result));
        return result;
    }
    public static Result[] RefreshOpenDrawings()
    {
        var results = new List<Result>();
        var manager = Application.DocumentManager;
        var original = manager.MdiActiveDocument;
        try
        {
            foreach (Document doc in manager)
            {
                if (String.IsNullOrEmpty(doc.Database.Filename)) continue;
                var binding = ReadBindings().SingleOrDefault(b => String.Equals(Path.GetFullPath(b.File), Path.GetFullPath(doc.Database.Filename), StringComparison.OrdinalIgnoreCase));
                if (binding == null) continue;
                // The native field owner must be the active document as well as
                // WorkingDatabase. A different active document can retain the old
                // attribute cache and its old sheet component after regeneration.
                manager.MdiActiveDocument = doc;
                var result = Apply(doc);
                if (result != null)
                {
                    if (result.Success)
                    using (doc.LockDocument())
                    {
                        doc.Editor.Regen();
                        var verified = TitleBlockAudit.Inspect(doc, binding);
                        result.Success = verified.Success;
                        result.Error = verified.Error;
                        result.Attributes = verified.Attributes;
                    }
                    results.Add(result);
                }
            }
        }
        finally { if (original != null) manager.MdiActiveDocument = original; }
        File.WriteAllText(Path.Combine(PackagePaths.LogDirectory, "rotulos-abiertos.json"), Json.Serialize(results));
        return results.ToArray();
    }

    public class Installation
    {
        public string Code { get; set; }
        public string File { get; set; }
        public bool Success { get; set; }
        public bool Saved { get; set; }
        public string Error { get; set; }
        public object[] Displayed { get; set; }
    }
    [CommandMethod("SOTRAMIVINCULARTODOS", CommandFlags.Session)]
    public void BindAll() { TitleBlockAudit.Run(true); }
    [CommandMethod("SOTRAMIREINTENTAR", CommandFlags.Session)]
    public void RetryPending() { TitleBlockAudit.Run(true); }
    void Install(bool retry)
    {
        var manager = Application.DocumentManager;
        var original = manager.MdiActiveDocument;
        var results = new List<Installation>();
        var progress = Path.Combine(PackagePaths.LogDirectory, "instalacion-rotulos-native.json");
        var opening = Path.Combine(PackagePaths.LogDirectory, "rotulo-en-proceso.json");
        var previous = retry && File.Exists(progress) ? Json.Deserialize<Installation[]>(File.ReadAllText(progress)) : new Installation[0];
        foreach (var binding in ReadBindings())
        {
            var done = previous.SingleOrDefault(p => p.Success && p.File.Equals(binding.File, StringComparison.OrdinalIgnoreCase));
            if (done != null) { results.Add(done); continue; }
            var record = new Installation { Code = binding.Code, File = binding.File };
            Document doc = null;
            bool owned = false;
            try
            {
                File.WriteAllText(opening, Json.Serialize(new { file = binding.File, finished = false }));
                // A drawing locked by another person must remain untouched.
                foreach (Document existing in manager)
                    if (String.Equals(existing.Database.Filename, binding.File, StringComparison.OrdinalIgnoreCase)) { doc = existing; break; }
                if (doc == null)
                {
                    // DWL files can remain after a session closes. Check the
                    // actual DWG sharing lock without deleting any lock files.
                    try { using (File.Open(binding.File, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { } }
                    catch (IOException error) { throw new InvalidOperationException("PENDIENTE: EL ARCHIVO ESTA EN USO EN OTRA SESION. " + binding.File, error); }
                    doc = manager.Open(binding.File, false);
                    owned = true;
                }
                manager.MdiActiveDocument = doc;
                if (doc.IsReadOnly) throw new InvalidOperationException("EL ARCHIVO ESTA ABIERTO SOLO PARA LECTURA.");
                var result = Apply(doc);
                if (result == null || !result.Success) throw new InvalidOperationException(result == null ? "No se encontro la hoja." : result.Error);
                using (doc.LockDocument())
                {
                    int evaluation = Convert.ToInt32(Application.GetSystemVariable("FIELDEVAL"));
                    Application.SetSystemVariable("FIELDEVAL", evaluation | 31);
                    var originalLayout = LayoutManager.Current.CurrentLayout;
                    foreach (var layout in result.Attributes.Select(a => Convert.ToString(a.GetType().GetProperty("layout").GetValue(a, null))).Distinct())
                    {
                        LayoutManager.Current.CurrentLayout = layout;
                        doc.Editor.Regen();
                    }
                    LayoutManager.Current.CurrentLayout = originalLayout;
                    doc.Editor.Regen();
                    // Read back the actual visible values after native regeneration.
                    var shown = new List<object>();
                    using (var tr = doc.Database.TransactionManager.StartTransaction())
                    {
                        foreach (var candidate in GetAttributes(doc.Database, tr, binding))
                        {
                            if (candidate.Item2.TextString.Trim() != candidate.Item3.Trim())
                                throw new InvalidOperationException("No coincide el texto visible de " + candidate.Item2.Tag + " en " + candidate.Item1 + " (" + candidate.Item2.Handle + "): " + candidate.Item2.TextString);
                            shown.Add(new { handle = candidate.Item2.Handle.ToString(), tag = candidate.Item2.Tag, value = candidate.Item2.TextString });
                        }
                        tr.Commit();
                    }
                    record.Displayed = shown.ToArray();
                    doc.Database.SaveAs(binding.File, true, DwgVersion.Current, doc.Database.SecurityParameters);
                }
                record.Success = true;
                record.Saved = true;
                if (owned) { doc.CloseAndDiscard(); doc = null; }
            }
            catch (System.Exception error) { record.Error = error.ToString(); }
            results.Add(record);
            File.WriteAllText(progress, Json.Serialize(results));
        }
        File.WriteAllText(progress, Json.Serialize(results));
        if (original != null) manager.MdiActiveDocument = original;
        File.WriteAllText(opening, Json.Serialize(new { file = "", finished = true }));
        original.Editor.WriteMessage("\nSOTRAMI_ROTULOS: " + results.Count(p => p.Success) + " GUARDADOS; " + results.Count(p => !p.Success) + " PENDIENTES.\n");
    }
    static IEnumerable<Tuple<string, AttributeReference, string>> GetAttributes(Database database, Transaction tr, TitleBlockBinding binding)
    {
        var layouts = (DBDictionary)tr.GetObject(database.LayoutDictionaryId, OpenMode.ForRead);
        foreach (DBDictionaryEntry entry in layouts)
        {
            var layout = (Layout)tr.GetObject(entry.Value, OpenMode.ForRead);
            var space = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead);
            foreach (ObjectId id in space)
            {
                var block = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                if (block == null) continue;
                var attrs = block.AttributeCollection.Cast<ObjectId>().Select(a => tr.GetObject(a, OpenMode.ForRead) as AttributeReference).Where(a => a != null).ToArray();
                var number = attrs.FirstOrDefault(a => a.Tag.Equals("DRAWING_NO", StringComparison.OrdinalIgnoreCase));
                if (number == null || number.TextString.Trim() != binding.Code) continue;
                foreach (var attr in attrs)
                {
                    if (attr.Tag.Equals("TITTLE_DRAW1", StringComparison.OrdinalIgnoreCase)) yield return Tuple.Create(layout.LayoutName, attr, binding.Line1);
                    else if (attr.Tag.Equals("TITTLE_DRAW2", StringComparison.OrdinalIgnoreCase)) yield return Tuple.Create(layout.LayoutName, attr, binding.Line2);
                    else if (attr.Tag.Equals("DRAWING_NO", StringComparison.OrdinalIgnoreCase)) yield return Tuple.Create(layout.LayoutName, attr, binding.Code);
                }
            }
        }
    }
    [CommandMethod("SOTRAMIVINCULARROTULO", CommandFlags.Modal)]
    public void BindCurrent()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        var result = Apply(doc);
        if (result == null || !result.Success) doc.Editor.WriteMessage("\nSOTRAMI_ROTULO_PENDIENTE: " + (result == null ? "El dibujo no pertenece al conjunto configurado." : result.Error) + "\n");
        else
        {
            doc.Editor.Regen();
            var binding = ReadBindings().Single(b => b.Code == result.Code);
            var visible = TitleBlockAudit.Inspect(doc, binding);
            doc.Editor.WriteMessage(visible.Success ? "\nSOTRAMI_ROTULO_OK: " + result.Code + " -> " + result.Title + "\n" : "\nSOTRAMI_ROTULO_PENDIENTE: " + visible.Error + "\n");
        }
    }
}
