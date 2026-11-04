using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Autodesk.AutoCAD.Interop;

class SheetSetSync
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern bool SetDllDirectory(string path);
    static string Root => PackagePaths.Root;
    static string SourceDir => PackagePaths.ProjectDirectory;
    static string Workbook => PackagePaths.WorkbookFile;
    static readonly JsonCodec Json = new JsonCodec();
    static readonly Dictionary<string, string> Disciplines = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    internal const string FinalStageSubset = "ETAPA FINAL - 16-300";

    static IAcSmSubset FindOrCreateSubset(IAcSmSubset parent, string name)
    {
        var items = parent.GetSheetEnumerator();
        IAcSmSubset found = null;
        for (var item = items.Next(); item != null; item = items.Next())
        {
            if (item.GetTypeName() != "AcSmSubset" || !item.GetName().Equals(name, StringComparison.OrdinalIgnoreCase)) continue;
            if (found != null) throw new InvalidOperationException("Hay grupos duplicados en SOTRAMI: " + name);
            found = (IAcSmSubset)item;
        }
        if (found != null) return found;
        var created = parent.CreateSubset(name, "");
        // Some AcSm versions insert CreateSubset immediately; others require it.
        var check = parent.GetSheetEnumerator();
        bool inserted = false;
        for (var item = check.Next(); item != null; item = check.Next())
            if (item.GetObjectId().GetHandle() == created.GetObjectId().GetHandle()) inserted = true;
        if (!inserted) parent.InsertComponent(created, null);
        return created;
    }

    static IAcSmSubset TargetSubset(AcSmDatabase db, string code)
    {
        return FindOrCreateSubset(db.GetSheetSet(), PlanCodes.Stage(code) == "16-300" ? FinalStageSubset : Disciplines[code]);
    }

    static void InsertInCodeOrder(IAcSmSubset target, IAcSmSheet sheet)
    {
        var items = target.GetSheetEnumerator();
        IAcSmComponent next = null;
        IAcSmComponent last = null;
        for (var item = items.Next(); item != null; item = items.Next())
        {
            if (item.GetTypeName() == "AcSmSheet" && StringComparer.OrdinalIgnoreCase.Compare(((IAcSmSheet)item).GetNumber(), sheet.GetNumber()) > 0)
            { next = item; break; }
            last = item;
        }
        if (next != null) target.InsertComponent(sheet, next);
        else if (last == null) target.InsertComponent(sheet, null);
        else target.InsertComponentAfter(sheet, last);
    }

    static void MoveFinalSheet(AcSmDatabase db, IAcSmSheet sheet)
    {
        if (PlanCodes.Stage(sheet.GetNumber()) != "16-300") return;
        var target = TargetSubset(db, sheet.GetNumber());
        var parent = (IAcSmSubset)sheet.GetOwner();
        if (parent.GetObjectId().GetHandle() == target.GetObjectId().GetHandle()) return;
        var id = sheet.GetObjectId().GetHandle();
        // Move the existing component so fields referring to its GUID stay valid.
        parent.RemoveSheet((AcSmSheet)sheet);
        InsertInCodeOrder(target, sheet);
        if (sheet.GetObjectId().GetHandle() != id) throw new InvalidOperationException("Cambio inesperado de identidad al mover " + sheet.GetNumber());
    }
    static void DefineTitleProperties(IAcSmSheetSet set)
    {
        var bag = set.GetCustomPropertyBag();
        foreach (var name in new[] { "EXCEL_TITULO_1", "EXCEL_TITULO_2" })
        {
            var value = new AcSmCustomPropertyValue();
            value.InitNew(bag);
            value.SetValue("\u00A0");
            value.SetFlags(PropertyFlags.CUSTOM_SHEET_PROP);
            bag.SetProperty(name, value);
        }
        // Sheet custom properties require a definition on the sheet set, not
        // only values on individual sheets. Otherwise reopening drops them.
        set.UpdateSheetCustomProps();
    }
    static void SetTitleProperties(IAcSmSheet sheet, string title)
    {
        var lines = TitleParts.Split(title);
        var bag = sheet.GetCustomPropertyBag();
        for (int i = 0; i < 2; i++)
        {
            var value = new AcSmCustomPropertyValue();
            value.InitNew(bag);
            // A nonbreaking space renders blank without AcSm treating the
            // property as missing and displaying its default placeholder.
            value.SetValue(lines[i].Length == 0 ? "\u00A0" : lines[i]);
            value.SetFlags(PropertyFlags.CUSTOM_SHEET_PROP);
            bag.SetProperty("EXCEL_TITULO_" + (i + 1), value);
        }
    }
    static string DrawingPath(string code)
    {
        var expected = Path.Combine(SourceDir, code + ".dwg");
        if (File.Exists(expected)) return expected;
        var archived = Path.Combine(SourceDir, "Old", code + ".dwg");
        return File.Exists(archived) ? archived : expected;
    }
    public class LayoutInfo
    {
        public string File { get; set; }
        public string Name { get; set; }
        public string Handle { get; set; }
    }
    public static LayoutInfo ActualLayout(Autodesk.AutoCAD.DatabaseServices.Database sourceDb, string preferredName, string code)
    {
            using (var tr = sourceDb.TransactionManager.StartTransaction())
            {
                var dictionary = (Autodesk.AutoCAD.DatabaseServices.DBDictionary)tr.GetObject(sourceDb.LayoutDictionaryId, Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead);
                var layouts = new List<Autodesk.AutoCAD.DatabaseServices.Layout>();
                foreach (Autodesk.AutoCAD.DatabaseServices.DBDictionaryEntry entry in dictionary)
                {
                    var layout = (Autodesk.AutoCAD.DatabaseServices.Layout)tr.GetObject(entry.Value, Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead);
                    if (!layout.ModelType) layouts.Add(layout);
                }
                var selected = layouts.SingleOrDefault(p => p.LayoutName  .Equals(preferredName, StringComparison.OrdinalIgnoreCase));
                if (selected == null && layouts.Count == 1) selected = layouts[0];
                if (selected == null)
                {
                    // A renamed DWG can retain several paper layouts and fields
                    // for its old drawing code. Its native stored association
                    // identifies the registered layout without guessing by order.
                    var nod = (Autodesk.AutoCAD.DatabaseServices.DBDictionary)tr.GetObject(sourceDb.NamedObjectsDictionaryId, Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead);
                    if (nod.Contains("AcSheetSetData"))
                    {
                        var info = tr.GetObject(nod.GetAt("AcSheetSetData"), Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead) as Autodesk.AutoCAD.DatabaseServices.DBDictionary;
                        if (info != null)
                        {
                            Func<string, string[]> values = key =>
                            {
                                if (!info.Contains(key)) return new string[0];
                                var record = tr.GetObject(info.GetAt(key), Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead) as Autodesk.AutoCAD.DatabaseServices.Xrecord;
                                if (record == null || record.Data == null) return new string[0];
                                return record.Data.AsArray().Select(v => v.Value is Autodesk.AutoCAD.DatabaseServices.ObjectId ? ((Autodesk.AutoCAD.DatabaseServices.ObjectId)v.Value).Handle.ToString() : Convert.ToString(v.Value)).ToArray();
                            };
                            var handles = values("LayoutHandle").Select(v => (v ?? "").Trim().TrimStart('0').ToUpperInvariant()).ToArray();
                            var associated = layouts.Where(l => handles.Contains(l.Handle.ToString().TrimStart('0').ToUpperInvariant())).ToArray();
                            if (associated.Length == 1) selected = associated[0];
                            if (selected == null)
                            {
                                var names = values("LayoutName");
                                associated = layouts.Where(l => names.Any(n => l.LayoutName.Equals(n, StringComparison.OrdinalIgnoreCase))).ToArray();
                                if (associated.Length == 1) selected = associated[0];
                            }
                        }
                    }
                }
                if (selected == null)
                {
                    var withFrame = new List<Autodesk.AutoCAD.DatabaseServices.Layout>();
                    foreach (var layout in layouts)
                    {
                        var space = (Autodesk.AutoCAD.DatabaseServices.BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead);
                        foreach (Autodesk.AutoCAD.DatabaseServices.ObjectId id in space)
                        {
                            var block = tr.GetObject(id, Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead) as Autodesk.AutoCAD.DatabaseServices.BlockReference;
                            if (block == null) continue;
                            var tags = block.AttributeCollection.Cast<Autodesk.AutoCAD.DatabaseServices.ObjectId>().Select(a => tr.GetObject(a, Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead) as Autodesk.AutoCAD.DatabaseServices.AttributeReference).Where(a => a != null).Select(a => a.Tag.ToUpperInvariant()).ToArray();
                            if (new[] { "TITTLE_DRAW1", "TITTLE_DRAW2", "DRAWING_NO" }.All(tag => tags.Contains(tag))) { withFrame.Add(layout); break; }
                        }
                    }
                    if (withFrame.Count == 1) selected = withFrame[0];
                }
                if (selected == null)
                {
                    var matches = new List<Autodesk.AutoCAD.DatabaseServices.Layout>();
                    foreach (var layout in layouts)
                    {
                        var block = (Autodesk.AutoCAD.DatabaseServices.BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead);
                        bool matchingCode = false;
                        foreach (Autodesk.AutoCAD.DatabaseServices.ObjectId id in block)
                        {
                            var insert = tr.GetObject(id, Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead) as Autodesk.AutoCAD.DatabaseServices.BlockReference;
                            if (insert == null) continue;
                            foreach (Autodesk.AutoCAD.DatabaseServices.ObjectId attributeId in insert.AttributeCollection)
                            {
                                var attribute = tr.GetObject(attributeId, Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead) as Autodesk.AutoCAD.DatabaseServices.AttributeReference;
                                if (attribute != null && attribute.Tag.Equals("DRAWING_NO", StringComparison.OrdinalIgnoreCase) && attribute.TextString.Trim().Equals(code, StringComparison.OrdinalIgnoreCase)) matchingCode = true;
                            }
                        }
                        if (matchingCode) matches.Add(layout);
                    }
                    if (matches.Count == 1) selected = matches[0];
                }
                if (selected == null) throw new InvalidOperationException("No se identifica una presentacion unica para " + code);

                return new LayoutInfo { File = sourceDb.Filename, Name = selected.LayoutName, Handle = selected.Handle.ToString() };
            }
    }
    static bool ResolveLayout(AcSmAcDbLayoutReference reference, string drawing, string code, Dictionary<string, LayoutInfo> catalog)
    {
        foreach (Autodesk.AutoCAD.ApplicationServices.Document doc in Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager)
            if (String.Equals(doc.Database.Filename, drawing, StringComparison.OrdinalIgnoreCase))
            {
                var actual = ActualLayout(doc.Database, reference.GetName(), code);
                reference.SetName(actual.Name); reference.SetAcDbHandle(actual.Handle); return true;
            }
        // Existing live references are authoritative. A shipped catalog must not
        // restore an obsolete presentation after a DWG was replaced or renamed.
        if (!String.IsNullOrEmpty(reference.GetName()) && !String.IsNullOrEmpty(reference.GetAcDbHandle()) && reference.GetAcDbHandle() != "0") return true;
        LayoutInfo known;
        if (catalog.TryGetValue(code, out known) && String.Equals(known.File, drawing, StringComparison.OrdinalIgnoreCase))
        {
            reference.SetName(known.Name); reference.SetAcDbHandle(known.Handle); return true;
        }
        return false;
    }
    public static LayoutInfo RepairOpenLayout(Autodesk.AutoCAD.ApplicationServices.Document doc, string code, string backupRoot)
    {
        RegisteredCodes();
        var manager = new AcSmSheetSetMgr();
        var db = manager.OpenDatabase(PackagePaths.SheetSetFile, false);
        var sheets = new List<IAcSmSheet>(); var records = new List<Record>();
        Visit(db.GetSheetSet(), "", sheets, records);
        var index = records.FindIndex(r => r.Number.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (index < 0 || !String.Equals(Path.GetFullPath(records[index].File), Path.GetFullPath(doc.Database.Filename), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("EL DWG NO CORRESPONDE A LA HOJA " + code);
        var reference = sheets[index].GetLayout();
        var actual = ActualLayout(doc.Database, reference.GetName(), code);
        if (String.Equals(reference.GetName(), actual.Name, StringComparison.OrdinalIgnoreCase) && String.Equals(reference.GetAcDbHandle(), actual.Handle, StringComparison.OrdinalIgnoreCase)) return actual;
        Directory.CreateDirectory(backupRoot);
        var backup = Path.Combine(backupRoot, "SOTRAMI - ANTES DE REPARAR PRESENTACIONES.dst");
        if (!File.Exists(backup)) File.Copy(PackagePaths.SheetSetFile, backup);
        var owner = new object(); db.LockDb(owner); bool commit = false;
        try { reference.SetName(actual.Name); reference.SetAcDbHandle(actual.Handle); commit = true; }
        finally { db.UnlockDb(owner, commit); }
        var catalogPath = Path.Combine(Root, "layout-catalog.json");
        var catalog = File.Exists(catalogPath) ? Json.Deserialize<Dictionary<string, LayoutInfo>>(File.ReadAllText(catalogPath)) : new Dictionary<string, LayoutInfo>();
        catalog[code] = actual; File.WriteAllText(catalogPath, Json.Serialize(catalog));
        if (Execute(false) != 0) throw new InvalidOperationException("NO SE PUEDE VERIFICAR LA PRESENTACION REPARADA: " + code);
        return actual;
    }
    internal class Record
    {
        public string Group { get; set; }
        public string Number { get; set; }
        public string Title { get; set; }
        public string File { get; set; }
        public string Layout { get; set; }
        public string LayoutHandle { get; set; }
        public string SheetId { get; set; }
    }
    internal static Record[] ReadForAlerts()
    {
        var manager = new AcSmSheetSetMgr();
        AcSmDatabase db = null;
        var all = manager.GetDatabaseEnumerator(); all.Reset();
        for (var current = all.Next(); current != null; current = all.Next())
            if (String.Equals(Path.GetFullPath(current.GetFileName()), Path.GetFullPath(PackagePaths.SheetSetFile), StringComparison.OrdinalIgnoreCase)) { db = current; break; }
        if (db == null) db = manager.OpenDatabase(PackagePaths.SheetSetFile, false);
        var sheets = new List<IAcSmSheet>(); var records = new List<Record>();
        Visit(db.GetSheetSet(), "", sheets, records);
        // No update, lock or save. Keep native instances alive for the palette.
        return records.ToArray();
    }
    static XDocument Xml(ZipArchive zip, string path)
    {
        using (var stream = zip.GetEntry(path).Open()) return XDocument.Load(stream);
    }
    static Dictionary<string, string> ReadTitles()
    {
        Disciplines.Clear();
        var titles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var plan in ExcelPlanSync.ReadPlans())
        {
            titles.Add(plan.Code, plan.Title);
            Disciplines.Add(plan.Code, plan.Discipline.ToUpperInvariant());
        }
        return titles;
    }
    static void Visit(IAcSmSubset subset, string group, List<IAcSmSheet> sheets, List<Record> records)
    {
        var enumerator = subset.GetSheetEnumerator();
        for (var item = enumerator.Next(); item != null; item = enumerator.Next())
        {
            if (item.GetTypeName() == "AcSmSubset") Visit((IAcSmSubset)item, group + "/" + item.GetName(), sheets, records);
            else if (item.GetTypeName() == "AcSmSheet")
            {
                var sheet = (IAcSmSheet)item;
                var layout = sheet.GetLayout();
                sheets.Add(sheet);
                records.Add(new Record { Group = group, Number = sheet.GetNumber(), Title = sheet.GetTitle(), File = layout.GetFileName(), Layout = layout.GetName(), LayoutHandle = layout.GetAcDbHandle(), SheetId = sheet.GetObjectId().GetHandle() });
            }
        }
    }
    public class Retirement
    {
        public string Code { get; set; }
        public string Title { get; set; }
        public string File { get; set; }
        public string Layout { get; set; }
        public string SheetId { get; set; }
    }
    public static List<Retirement> Retirements(HashSet<string> codes)
    {
        RegisteredCodes(); // checks dirty/locked sets and duplicate databases
        var manager = new AcSmSheetSetMgr();
        var db = manager.OpenDatabase(PackagePaths.SheetSetFile, false);
        var sheets = new List<IAcSmSheet>(); var records = new List<Record>();
        Visit(db.GetSheetSet(), "", sheets, records);
        return records.Where(r => PlanCodes.IsSupported(r.Number) && !codes.Contains(r.Number))
            .Select(r => new Retirement { Code = r.Number, Title = r.Title, File = r.File, Layout = r.Layout, SheetId = r.SheetId }).ToList();
    }
    [STAThread]
    static int Main(string[] args) => Execute(args.Length > 0 && args[0] == "update", true);
    public static HashSet<string> RegisteredCodes()
    {
        var manager = new AcSmSheetSetMgr();
        AcSmDatabase db = null;
        var databases = manager.GetDatabaseEnumerator();
        for (var current = databases.Next(); current != null; current = databases.Next())
            if (String.Equals(Path.GetFullPath(current.GetFileName()), Path.GetFullPath(PackagePaths.SheetSetFile), StringComparison.OrdinalIgnoreCase)) { db = current; break; }
        if (db == null) db = manager.OpenDatabase(PackagePaths.SheetSetFile, false);
        if (db.GetIsDirty() || db.GetLockStatus() != AcSmLockStatus.AcSmLockStatus_UnLocked) throw new InvalidOperationException("SOTRAMI tiene cambios pendientes o esta bloqueado; no se crean planos.");
        var setId = db.GetSheetSet().GetObjectId().GetHandle();
        databases = manager.GetDatabaseEnumerator();
        for (var current = databases.Next(); current != null; current = databases.Next())
            if (!String.Equals(Path.GetFullPath(current.GetFileName()), Path.GetFullPath(PackagePaths.SheetSetFile), StringComparison.OrdinalIgnoreCase) && current.GetSheetSet().GetObjectId().GetHandle() == setId)
                throw new InvalidOperationException("Cierre las copias antiguas de SOTRAMI antes de actualizar: " + current.GetFileName());
        var sheets = new List<IAcSmSheet>(); var records = new List<Record>();
        Visit(db.GetSheetSet(), "", sheets, records);
        return new HashSet<string>(records.Select(r => r.Number), StringComparer.OrdinalIgnoreCase);
    }
    public static int Execute(bool update, bool standalone = false)
    {
        var primary = ExecuteFile(update, standalone, PackagePaths.SheetSetFile, false);
        if (primary != 0) return primary;
        if (String.Equals(PackagePaths.SheetSetFile, PackagePaths.TeamSheetSetFile, StringComparison.OrdinalIgnoreCase)) return primary;
        return ExecuteFile(update, standalone, PackagePaths.TeamSheetSetFile, true);
    }

    public static void OrganizeFinalStages()
    {
        RegisteredCodes();
        var db = new AcSmSheetSetMgr().OpenDatabase(PackagePaths.SheetSetFile, false);
        var before = ReadForAlerts();
        File.WriteAllText(Path.Combine(PackagePaths.LogDirectory, "etapas-before.json"), Json.Serialize(before));
        var sheets = new List<IAcSmSheet>(); var records = new List<Record>();
        Visit(db.GetSheetSet(), "", sheets, records);
        var owner = new object(); db.LockDb(owner); bool commit = false;
        try
        {
            foreach (var sheet in sheets) MoveFinalSheet(db, sheet);
            commit = true;
        }
        finally { db.UnlockDb(owner, commit); }
        var after = ReadForAlerts();
        if (before.Length != after.Length || after.Select(p => p.Number).Distinct().Count() != after.Length)
            throw new InvalidOperationException("Cambio inesperado en la cantidad de hojas.");
        foreach (var previous in before)
        {
            var current = after.Single(p => p.Number == previous.Number);
            var group = PlanCodes.Stage(previous.Number) == "16-300" ? "/" + FinalStageSubset : previous.Group;
            if (current.SheetId != previous.SheetId || current.File != previous.File || current.Layout != previous.Layout || current.LayoutHandle != previous.LayoutHandle || current.Title != previous.Title || current.Group != group)
                throw new InvalidOperationException("No se verifico el traslado de " + previous.Number);
        }
        File.WriteAllText(Path.Combine(PackagePaths.LogDirectory, "etapas-status.json"), Json.Serialize(new {
            success = true, sheets = after.Length, group = FinalStageSubset,
            final = after.Where(p => PlanCodes.Stage(p.Number) == "16-300").ToArray(),
            sheetIdsPreserved = true, layoutReferencesPreserved = true
        }));
    }
    static IAcSmSheet RenumberSheet(AcSmDatabase db, string id)
    {
        var sheets = new List<IAcSmSheet>(); var records = new List<Record>();
        Visit(db.GetSheetSet(), "", sheets, records);
        return sheets.Single(s => s.GetObjectId().GetHandle() == id);
    }
    public static string RenumberSheetId(string code)
    {
        RegisteredCodes();
        var db = new AcSmSheetSetMgr().OpenDatabase(PackagePaths.SheetSetFile, false);
        var sheets = new List<IAcSmSheet>(); var records = new List<Record>();
        Visit(db.GetSheetSet(), "", sheets, records);
        return sheets.Single(s => s.GetNumber() == code).GetObjectId().GetHandle();
    }
    public static string RenumberCurrent(string id)
    {
        RegisteredCodes();
        return RenumberSheet(new AcSmSheetSetMgr().OpenDatabase(PackagePaths.SheetSetFile, false), id).GetNumber();
    }
    public static void Renumber(string id, string from, string to, string source, string destination)
    {
        var codes = RegisteredCodes();
        if (codes.Contains(to)) throw new InvalidOperationException("EL DESTINO YA EXISTE EN SOTRAMI.");
        var db = new AcSmSheetSetMgr().OpenDatabase(PackagePaths.SheetSetFile, false);
        var sheet = RenumberSheet(db, id);
        if (sheet.GetNumber() != from || !String.Equals(Path.GetFullPath(sheet.GetLayout().GetFileName()), Path.GetFullPath(source), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("LA HOJA O SU ARCHIVO CAMBIARON DESDE LA VISTA PREVIA.");
        var owner = new object(); db.LockDb(owner); bool commit = false;
        try
        {
            // Keep the same component, owner, position, custom properties and
            // layout identity. Existing fields retain their component GUID.
            sheet.SetNumber(to); sheet.GetLayout().SetFileName(destination); commit = true;
        }
        finally { db.UnlockDb(owner, commit); }
        if (RenumberSheet(db, id).GetNumber() != to) throw new InvalidOperationException("NO SE VERIFICO EL NUMERO DE LA HOJA.");
    }
    static int ExecuteFile(bool update, bool standalone, string file, bool team)
    {
        try
        {
            if (standalone) SetDllDirectory(@"C:\Program Files\Autodesk\AutoCAD 2024");
            if (!File.Exists(file)) throw new FileNotFoundException("No se encuentra el conjunto del proyecto.", file);
            var logPrefix = team ? "team-" : "";
            var titles = ReadTitles();
            var mgr = new AcSmSheetSetMgr();
            AcSmDatabase db = null;
            var openDatabases = mgr.GetDatabaseEnumerator();
            for (var opened = openDatabases.Next(); opened != null; opened = openDatabases.Next())
                if (String.Equals(Path.GetFullPath(opened.GetFileName()), Path.GetFullPath(file), StringComparison.OrdinalIgnoreCase)) { db = opened; break; }
            bool wasOpen = db != null;
            if (!wasOpen) db = mgr.OpenDatabase(file, false);
            var canonicalId = db.GetSheetSet().GetObjectId().GetHandle();
            var registered = mgr.GetDatabaseEnumerator();
            for (var other = registered.Next(); other != null; other = registered.Next())
                if (!String.Equals(Path.GetFullPath(other.GetFileName()), Path.GetFullPath(file), StringComparison.OrdinalIgnoreCase)
                    && other.GetSheetSet().GetObjectId().GetHandle() == canonicalId)
                    throw new InvalidOperationException("Hay una copia antigua de SOTRAMI abierta con el mismo identificador: " + other.GetFileName() + ". Guarde su trabajo, cierre ese conjunto o reinicie Civil 3D y abra solo " + file);
            if (update && (db.GetIsDirty() || db.GetLockStatus() != AcSmLockStatus.AcSmLockStatus_UnLocked)) throw new InvalidOperationException("Conjunto con cambios pendientes o bloqueo; no se modifica: " + file);
            var sheets = new List<IAcSmSheet>();
            var records = new List<Record>();
            Visit(db.GetSheetSet(), "", sheets, records);
            var catalogPath = Path.Combine(Root, "layout-catalog.json");
            var catalog = File.Exists(catalogPath) ? Json.Deserialize<Dictionary<string, LayoutInfo>>(File.ReadAllText(catalogPath)) : new Dictionary<string, LayoutInfo>();
            var pendingLayouts = new List<string>();
            for (var i = 0; i < sheets.Count; i++)
            {
                var reference = sheets[i].GetLayout();
                if (!catalog.ContainsKey(records[i].Number) && !String.IsNullOrEmpty(reference.GetName()) && !String.IsNullOrEmpty(reference.GetAcDbHandle()))
                    catalog[records[i].Number] = new LayoutInfo { File = reference.GetFileName(), Name = reference.GetName(), Handle = reference.GetAcDbHandle() };
            }
            if (!File.Exists(Path.Combine(PackagePaths.LogDirectory, "sheetset-before.json"))) File.WriteAllText(Path.Combine(PackagePaths.LogDirectory, "sheetset-before.json"), Json.Serialize(records));
            if (update)
            {
                var owner = new object();
                db.LockDb(owner);
                var commit = false;
                try
                {
                    DefineTitleProperties(db.GetSheetSet());
                    var removedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (var i = 0; i < sheets.Count; i++)
                    {
                        if (PlanCodes.IsSupported(records[i].Number) && !titles.ContainsKey(records[i].Number))
                        {
                            DrawingRetirement.RequireArchived(records[i].Number, records[i].File, records[i].SheetId);
                            var parent = (IAcSmSubset)sheets[i].GetOwner();
                            parent.RemoveSheet((AcSmSheet)sheets[i]);
                            removedCodes.Add(records[i].Number);
                            continue;
                        }
                        string title;
                        if (titles.TryGetValue(records[i].Number, out title)) { sheets[i].SetTitle(title); SetTitleProperties(sheets[i], title); }
                        var layout = sheets[i].GetLayout();
                        if (titles.ContainsKey(records[i].Number)) layout.SetFileName(DrawingPath(records[i].Number));
                        if (records[i].Number == "1107.001.00.14-200-00-P-001")
                            layout.SetFileName(PackagePaths.DrawingFile);
                        if (titles.ContainsKey(records[i].Number) && !ResolveLayout(layout, layout.GetFileName(), records[i].Number, catalog)) pendingLayouts.Add(records[i].Number);
                        if (titles.ContainsKey(records[i].Number)) MoveFinalSheet(db, sheets[i]);
                    }
                    var existing = new HashSet<string>(records.Where(p => !removedCodes.Contains(p.Number)).Select(p => p.Number), StringComparer.OrdinalIgnoreCase);
                    foreach (var pair in titles.Where(p => PlanCodes.IsSupported(p.Key) && !existing.Contains(p.Key)))
                    {
                        var drawing = DrawingPath(pair.Key);
                        if (!File.Exists(drawing)) continue;
                        var reference = new AcSmAcDbLayoutReference();
                        reference.InitNew(db);
                        reference.SetFileName(drawing);
                        if (!ResolveLayout(reference, drawing, pair.Key, catalog)) { pendingLayouts.Add(pair.Key); continue; }
                        var target = TargetSubset(db, pair.Key);
                        // Register the existing DWG without creating a drawing from
                        // the sheet set template (AddNewSheet can create a new file).
                        var added = new AcSmSheet();
                        added.InitNew(db);
                        added.SetName(pair.Value);
                        added.SetDesc("");
                        added.SetLayout(reference);
                        added.SetNumber(pair.Key);
                        added.SetTitle(pair.Value);
                        SetTitleProperties(added, pair.Value);
                        InsertInCodeOrder(target, added);
                    }
                    db.GetSheetSet().SetName("SOTRAMI");
                    var bag = db.GetSheetSet().GetCustomPropertyBag();
                    var prop = new AcSmCustomPropertyValue();
                    prop.InitNew(bag);
                    prop.SetValue(Workbook);
                    prop.SetFlags(PropertyFlags.CUSTOM_SHEETSET_PROP);
                    bag.SetProperty("ExcelFuenteListaPlanos", prop);
                    commit = true;
                }
                finally { db.UnlockDb(owner, commit); }
            }
            records.Clear();
            sheets.Clear();
            Visit(db.GetSheetSet(), "", sheets, records);
            if (update)
            {
                foreach (var sheet in sheets)
                {
                    var reference = sheet.GetLayout();
                    if (!pendingLayouts.Contains(sheet.GetNumber())) catalog[sheet.GetNumber()] = new LayoutInfo { File = reference.GetFileName(), Name = reference.GetName(), Handle = reference.GetAcDbHandle() };
                }
                File.WriteAllText(catalogPath, Json.Serialize(catalog));
            }
            File.WriteAllText(Path.Combine(PackagePaths.LogDirectory, logPrefix + "sheetset-content.json"), Json.Serialize(records));
            var setId = db.GetSheetSet().GetObjectId().GetHandle();
            var bindings = records.Where(p => PlanCodes.IsSupported(p.Number)).Select(p => new TitleBlockBinding {
                Code = p.Number, File = p.File, Layout = p.Layout, LayoutHandle = p.LayoutHandle, Title = p.Title,
                Line1 = TitleParts.Split(p.Title)[0], Line2 = TitleParts.Split(p.Title)[1],
                Field1 = "\\AcSm Database(\"" + PackagePaths.SheetSetFile + "\").SheetSet(\"" + setId + "\").Component(\"" + p.SheetId + "\").EXCEL_TITULO_1 \\f \"%tc1\"",
                Field2 = "\\AcSm Database(\"" + PackagePaths.SheetSetFile + "\").SheetSet(\"" + setId + "\").Component(\"" + p.SheetId + "\").EXCEL_TITULO_2 \\f \"%tc1\"",
                FieldNumber = "\\AcSm Database(\"" + PackagePaths.SheetSetFile + "\").SheetSet(\"" + setId + "\").Component(\"" + p.SheetId + "\").Number \\f \"%tc1\""
            }).ToArray();
            if (!team) File.WriteAllText(Path.Combine(PackagePaths.LogDirectory, "titleblock-bindings.json"), Json.Serialize(bindings));
            var mismatches = records.Where(p => titles.ContainsKey(p.Number) && p.Title != titles[p.Number]).ToArray();
            var missing = titles.Keys.Where(code => PlanCodes.IsSupported(code) && !records.Any(p => p.Number.Equals(code, StringComparison.OrdinalIgnoreCase))).ToArray();
            var missingFiles = records.Where(p => !File.Exists(p.File)).Select(p => new { code = p.Number, file = p.File }).ToArray();
            var archivedFiles = records.Where(p => p.File.IndexOf("\\Old\\", StringComparison.OrdinalIgnoreCase) >= 0).Select(p => new { code = p.Number, file = p.File }).ToArray();
            var misplacedFinalSheets = records.Where(p => PlanCodes.Stage(p.Number) == "16-300" && !p.Group.Equals("/" + FinalStageSubset, StringComparison.OrdinalIgnoreCase)).Select(p => p.Number).ToArray();
            var summary = Json.Serialize(new { mode = update ? "update" : "inspect", sheets = records.Count, matched = records.Count(p => titles.ContainsKey(p.Number)), mismatches = mismatches.Length, missingInitialSheets = missing, missingFiles, archivedFiles, pendingLayouts, misplacedFinalSheets, layoutPolicy = "existing references or already open drawings; no external DWG loading" });
            File.WriteAllText(Path.Combine(PackagePaths.LogDirectory, logPrefix + "sheetset-summary.json"), summary);
            Console.WriteLine(summary);
            // Keep native database instances alive. Closing a database that the
            // palette still references leaves stale GUI entries on reopening.
            // No DWG is opened or saved here.
            return mismatches.Length != 0 || missing.Length != 0 || missingFiles.Length != 0 || pendingLayouts.Count != 0 || misplacedFinalSheets.Length != 0 ? 1 : 0;
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(PackagePaths.LogDirectory, "sheetset-error.txt"), ex.ToString()); Console.Error.WriteLine(ex); return 1; }
    }
}
