using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using App = Autodesk.AutoCAD.ApplicationServices.Application;
using Exception = System.Exception;

public class SotramiNativeTests
{
    [CommandMethod("SOTRAMIREVISION34", CommandFlags.Modal)]
    public void PrepareReview()
    {
        var root = Path.GetFullPath(PackagePaths.ProjectDirectory);
        if (!root.Contains("Sotrami-16-300-20261029") || root.IndexOf("Ensayo", StringComparison.OrdinalIgnoreCase) < 0)
            throw new InvalidOperationException("Review requires the isolated Ensayo directory");
        var doc = App.DocumentManager.MdiActiveDocument;
        ExcelPlanSync.UpdateTableFromPlans(doc, ExcelPlanSync.ReadPlans());
        new ExcelPlanSync().Verify();
        var output = Path.Combine(PackagePaths.Root, "Portada-con-52-planos-REVISION.dwg");
        doc.Database.SaveAs(output, DwgVersion.AC1032);
        App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nSOTRAMI_REVISION_34_OK: " + output + "\n");
    }

    [CommandMethod("SOTRAMITEST34", CommandFlags.Modal)]
    public void Test()
    {
        var json = new JsonCodec();
        var output = Path.Combine(PackagePaths.Root, "native-test.json");
        try
        {
            var root = Path.GetFullPath(PackagePaths.ProjectDirectory);
            if (!root.Contains("Sotrami-16-300-20261029") || root.IndexOf("Ensayo", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException("Native tests require the isolated Ensayo directory");
            var plans = ExcelPlanSync.ReadPlans();
            var manager = new Autodesk.AutoCAD.Interop.AcSmSheetSetMgr();
            var set = manager.CreateDatabase(PackagePaths.SheetSetFile, "", true);
            var owner = new object(); set.LockDb(owner);
            try { set.GetSheetSet().SetName("SOTRAMI ENSAYO V34"); }
            finally { set.UnlockDb(owner, true); }
            var catalog = new Dictionary<string, SheetSetSync.LayoutInfo>();
            foreach (var plan in plans)
            {
                var file = Path.Combine(root, plan.Code + ".dwg");
                if (plan.Code.EndsWith("16-300-00-P-002") || plan.Code.EndsWith("16-300-00-P-003")) continue;
                if (String.Equals(file, PackagePaths.DrawingFile, StringComparison.OrdinalIgnoreCase))
                    catalog[plan.Code] = SheetSetSync.ActualLayout(App.DocumentManager.MdiActiveDocument.Database, "A", plan.Code);
                else
                {
                    if (!File.Exists(file)) File.Copy(NewDrawingSync.TemplateFile, file);
                    using (var db = new Database(false, true))
                    {
                        db.ReadDwgFile(file, FileOpenMode.OpenForReadAndAllShare, true, ""); db.CloseInput(true);
                        catalog[plan.Code] = SheetSetSync.ActualLayout(db, "A", plan.Code);
                    }
                }
            }
            File.WriteAllText(Path.Combine(PackagePaths.Root, "layout-catalog.json"), json.Serialize(catalog));
            var doc = App.DocumentManager.MdiActiveDocument;
            var firstExisting = DrawingRenumber.Hash(Path.Combine(root, "1107.001.00.16-300-00-P-001.dwg"));
            var created = NewDrawingSync.Prepare(plans);
            if (created.Count != 2) throw new Exception("Expected only the two missing final drawings to be created");
            ExcelPlanSync.UpdateTableFromPlans(doc, plans);
            if (SheetSetSync.Execute(true) != 0) throw new Exception("First sync failed");
            NewDrawingSync.LinkCreated(created);
            new ExcelPlanSync().Verify();
            var first = SheetSetSync.ReadForAlerts();
            if (first.Length != 52 || first.Select(p => p.Number).Distinct().Count() != 52) throw new Exception("Missing or duplicate sheet");
            if (TitleBlockSync.ReadBindings().Length != 52) throw new Exception("Missing title bindings");
            if (NewDrawingSync.Prepare(plans).Count != 0) throw new Exception("Repeat creates drawings");
            ExcelPlanSync.UpdateTableFromPlans(doc, plans);
            if (SheetSetSync.Execute(true) != 0) throw new Exception("Repeat sync failed");
            new ExcelPlanSync().Verify();
            var second = SheetSetSync.ReadForAlerts();
            if (!first.Select(p => p.Number + p.SheetId).SequenceEqual(second.Select(p => p.Number + p.SheetId))) throw new Exception("Repeat changed sheet identities");
            if (DrawingRenumber.Hash(Path.Combine(root, "1107.001.00.16-300-00-P-001.dwg")) != firstExisting) throw new Exception("Existing drawing overwritten");
            File.WriteAllText(output, json.Serialize(new { success = true, plans = plans.Count, sheets = second.Length, created = created.Select(p => p.Code), final = second.Where(p => PlanCodes.Stage(p.Number) == "16-300").Select(p => new { p.Number, p.Title, p.Layout }), repeatStable = true, existingPreserved = true }));
            App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nSOTRAMI_NATIVE_V34_OK\n");
        }
        catch (System.Exception ex)
        {
            File.WriteAllText(output, json.Serialize(new { success = false, error = ex.ToString() }));
            App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nSOTRAMI_NATIVE_V34_FAILED: " + ex + "\n");
        }
    }
}
