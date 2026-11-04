using System;
using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.Runtime;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;

public sealed class FiguraSelectionPreview
{
    [CommandMethod("FIGURAPRUEBACORREGIDA",CommandFlags.Modal|CommandFlags.UsePickSet)]
    public void Run()
    {
        var doc=App.DocumentManager.MdiActiveDocument;
        var folder=Path.GetDirectoryName(typeof(FiguraSelectionPreview).Assembly.Location);
        try
        {
            // Load a separate in-memory candidate solely for this test command.
            // It does not replace the installed DLL or register its commands.
            var candidate=Assembly.Load(File.ReadAllBytes(Path.Combine(folder,"CivilSpellAI-candidato.dll")));
            File.WriteAllText(Path.Combine(folder,"Candidato-en-prueba.txt"),candidate.ManifestModule.ModuleVersionId.ToString());
            var command=candidate.GetType("CivilSpellAI.Commands.Figure3DCommands");
            command.GetMethod("CreateFigure3D").Invoke(Activator.CreateInstance(command),null);
        }
        catch(System.Exception e)
        {
            var error=(e.InnerException??e).ToString();
            File.WriteAllText(Path.Combine(folder,"Error-vista-prueba.txt"),error);
            doc.Editor.WriteMessage("\nFIGURA_PREVIEW_FAILED {0}",error);
        }
    }
}
