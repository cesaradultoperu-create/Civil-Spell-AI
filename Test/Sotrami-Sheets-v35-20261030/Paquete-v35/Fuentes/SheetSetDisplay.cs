using System;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(SheetSetDisplay))]
public class SheetSetDisplay : IExtensionApplication
{
    public void Initialize()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly == typeof(SheetSetDisplay).Assembly || !assembly.GetName().Name.StartsWith("VinculoPlanosSOTRAMI.", StringComparison.Ordinal)) continue;
            try
            {
                var previous = assembly.GetType("SheetSetDisplay");
                if (previous != null) previous.GetMethod("Terminate").Invoke(Activator.CreateInstance(previous), null);
            }
            catch (System.Exception) { }
        }
        // Do not close/reopen sheet sets on DocumentActivated. Native palette
        // entries retain references and would accumulate stale duplicates.
        DrawingSaveAudit.Start();
        DrawingExcelAlert.Start();
    }
    public void Terminate() { DrawingExcelAlert.Stop(); DrawingSaveAudit.Stop(); }
    public static void Show()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        // Native OPEN selects an existing live view or opens a closed set.
        // Never invalidate its database with manager.Close beforehand.
        doc.Editor.Command("_.-OPENSHEETSET", PackagePaths.TeamSheetSetFile);
        doc.Editor.Command("_.REGENALL");
        File.WriteAllText(Path.Combine(PackagePaths.LogDirectory, "sheetset-visible.json"), new JsonCodec().Serialize(new { success = true, file = PackagePaths.TeamSheetSetFile, displayName = "SOTRAMI", activeDrawing = doc.Database.Filename }));
        doc.Editor.WriteMessage("\nSOTRAMI_CONJUNTO: " + PackagePaths.TeamSheetSetFile + "\n");
    }
    [CommandMethod("SOTRAMICONJUNTO", CommandFlags.Modal)]
    public void ShowCommand()
    {
        try { Show(); }
        catch (System.Exception error)
        {
            File.WriteAllText(Path.Combine(PackagePaths.LogDirectory, "sheetset-visible.json"), new JsonCodec().Serialize(new { success = false, error = error.ToString() }));
            Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nSOTRAMI_CONJUNTO_PENDIENTE: " + error.Message + "\n");
        }
    }
}
