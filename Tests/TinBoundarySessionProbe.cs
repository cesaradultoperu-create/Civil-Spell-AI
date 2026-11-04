using System;
using System.IO;
using System.Linq;
using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using CivilSpellAI.Autodesk;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CivilSpellAI.Tests
{
    public sealed class TinBoundarySessionProbe
    {
        [CommandMethod("CSAITINREADSESSION9", CommandFlags.Session | CommandFlags.NoUndoMarker)]
        public void Export()
        {
            string folder = Path.GetDirectoryName(typeof(TinBoundarySessionProbe).Assembly.Location);
            string status = Path.Combine(folder, "session-status.txt");
            try
            {
                var doc = AcadApplication.DocumentManager.Cast<global::Autodesk.AutoCAD.ApplicationServices.Document>()
                    .First(d => string.Equals(Path.GetFileName(d.Name), "prueba estructura proyectada.dwg", StringComparison.OrdinalIgnoreCase));
                using (doc.LockDocument())
                {
                    var ids = TinBoundaryDrawingService.VisibleTinIds(doc.Database);
                    var footprints = TinBoundaryDrawingService.Read(doc.Database, ids, true);
                    using (var writer = new StreamWriter(Path.Combine(folder, "session-nine.tsv")))
                    {
                        writer.WriteLine("surface\tring\tx\ty\tz");
                        for (int s = 0; s < footprints.Count; s++)
                        for (int r = 0; r < footprints[s].Rings.Count; r++)
                        foreach (var p in footprints[s].Rings[r])
                            writer.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0}\t{1}\t{2:R}\t{3:R}\t{4:R}",s,r,p.X,p.Y,p.Z));
                    }
                    File.WriteAllLines(status, new[] { "READ_OK " + doc.Name, "TIN_COUNT " + ids.Length }
                        .Concat(footprints.Select((f,i) => i + ": " + f.Name + ", vertices=" + f.Rings.Sum(r => r.Count))));
                    File.AppendAllText(status, Environment.NewLine + "READ_ONLY_COMPLETE");
                }
            }
            catch (System.Exception ex) { File.WriteAllText(status, ex.ToString()); }
        }
    }
}
