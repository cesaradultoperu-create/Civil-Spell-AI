using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Interop;
using Autodesk.AutoCAD.Runtime;

public class SotramiSubsetTests
{
    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static Dictionary<string, string> Hashes(IEnumerable<SheetSetSync.Record> records)
    {
        return records.ToDictionary(r => r.Number, r => DrawingRenumber.Hash(r.File));
    }

    static void CheckRecords(SheetSetSync.Record[] before, SheetSetSync.Record[] after)
    {
        Check(after.Length == before.Length && after.Select(r => r.Number).Distinct().Count() == after.Length, "Missing or duplicate sheets");
        foreach (var previous in before)
        {
            var current = after.Single(r => r.Number == previous.Number);
            Check(previous.SheetId == current.SheetId, "Changed sheet GUID: " + previous.Number);
            Check(previous.File == current.File && previous.Layout == current.Layout && previous.LayoutHandle == current.LayoutHandle, "Changed layout reference: " + previous.Number);
            var expected = PlanCodes.Stage(previous.Number) == "16-300" ? "/" + SheetSetSync.FinalStageSubset : previous.Group;
            Check(current.Group == expected, "Wrong group: " + previous.Number + " => " + current.Group);
        }
        Check(after.Where(r => PlanCodes.Stage(r.Number) == "16-300").Select(r => r.Number).SequenceEqual(after.Where(r => PlanCodes.Stage(r.Number) == "16-300").Select(r => r.Number).OrderBy(n => n)), "Final sheets are not sorted");
    }

    [CommandMethod("SOTRAMITESTSUBSETS35", CommandFlags.Modal)]
    public void Test()
    {
        var json = new JsonCodec();
        try
        {
            Check(PackagePaths.ProjectDirectory.Contains("Sotrami-Sheets-v35-20261030") && PackagePaths.ProjectDirectory.Contains("Ensayo"), "Requires isolated test directory");
            var plans = ExcelPlanSync.ReadPlans();
            var manager = new AcSmSheetSetMgr();
            var db = manager.CreateDatabase(PackagePaths.SheetSetFile, "", true);
            SheetSetSync.LayoutInfo layout;
            using (var template = new Database(false, true))
            {
                template.ReadDwgFile(NewDrawingSync.TemplateFile, FileOpenMode.OpenForReadAndAllShare, true, ""); template.CloseInput(true);
                layout = SheetSetSync.ActualLayout(template, "A", plans[0].Code);
            }
            var owner = new object(); db.LockDb(owner);
            try
            {
                var groups = new Dictionary<string, IAcSmSubset>();
                foreach (var plan in plans)
                {
                    var file = Path.Combine(PackagePaths.ProjectDirectory, plan.Code + ".dwg");
                    File.Copy(NewDrawingSync.TemplateFile, file);
                    IAcSmSubset subset;
                    var name = plan.Discipline.ToUpperInvariant();
                    if (!groups.TryGetValue(name, out subset))
                    {
                        subset = db.GetSheetSet().CreateSubset(name, "");
                        var check = db.GetSheetSet().GetSheetEnumerator();
                        bool inserted = false;
                        for (var item = check.Next(); item != null; item = check.Next())
                            if (item.GetObjectId().GetHandle() == subset.GetObjectId().GetHandle()) inserted = true;
                        if (!inserted) db.GetSheetSet().InsertComponent(subset, null);
                        groups.Add(name, subset);
                    }
                    var reference = new AcSmAcDbLayoutReference(); reference.InitNew(db);
                    reference.SetFileName(file); reference.SetName(layout.Name); reference.SetAcDbHandle(layout.Handle);
                    var sheet = new AcSmSheet(); sheet.InitNew(db); sheet.SetNumber(plan.Code); sheet.SetTitle(plan.Title); sheet.SetName(plan.Title); sheet.SetLayout(reference);
                    subset.InsertComponent(sheet, null);
                }
            }
            finally { db.UnlockDb(owner, true); }
            var before = SheetSetSync.ReadForAlerts();
            Check(before.Count(r => PlanCodes.Stage(r.Number) == "16-300") == 3, "Fixture must contain three final sheets");
            Check(before.All(r => r.Group != "/" + SheetSetSync.FinalStageSubset), "Fixture already migrated");
            var hashes = Hashes(before);
            SheetSetSync.OrganizeFinalStages();
            Check(SheetSetSync.Execute(false) == 0, "Migration failed");
            var first = SheetSetSync.ReadForAlerts(); CheckRecords(before, first);
            var bindings = TitleBlockSync.ReadBindings();
            foreach (var previous in before)
                Check(bindings.Single(b => b.Code == previous.Number).FieldNumber.Contains(previous.SheetId), "Binding no longer points to original GUID");
            Check(SheetSetSync.Execute(true) == 0, "Second sync failed");
            var second = SheetSetSync.ReadForAlerts(); CheckRecords(before, second);
            Check(first.Select(r => r.Number + r.SheetId + r.Group).SequenceEqual(second.Select(r => r.Number + r.SheetId + r.Group)), "Repeated sync changed tree order");
            manager.Close(db);
            db = manager.OpenDatabase(PackagePaths.SheetSetFile, false);
            var reopened = SheetSetSync.ReadForAlerts(); CheckRecords(before, reopened);
            var finalGroups = 0;
            var items = db.GetSheetSet().GetSheetEnumerator();
            for (var item = items.Next(); item != null; item = items.Next()) if (item.GetName() == SheetSetSync.FinalStageSubset) finalGroups++;
            Check(finalGroups == 1, "Missing or duplicate final subset after reopen");
            var afterHashes = Hashes(reopened);
            Check(hashes.All(p => afterHashes[p.Key] == p.Value), "DWG files changed");
            File.WriteAllText(Path.Combine(PackagePaths.Root, "subset-test.json"), json.Serialize(new { success = true, sheets = reopened.Length, finalGroups, final = reopened.Where(r => PlanCodes.Stage(r.Number) == "16-300"), sheetIdsPreserved = true, layoutReferencesPreserved = true, drawingFilesUnchanged = true, repeatStable = true, persistedAfterReopen = true }));
        }
        catch (System.Exception ex)
        {
            File.WriteAllText(Path.Combine(PackagePaths.Root, "subset-test.json"), json.Serialize(new { success = false, error = ex.ToString() }));
        }
    }
}
