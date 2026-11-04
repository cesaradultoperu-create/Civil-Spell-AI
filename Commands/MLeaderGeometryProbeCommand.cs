#if DEBUG
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CivilSpellAI.Commands
{
    public sealed class MLeaderGeometryProbeCommand
    {
        [CommandMethod("AISPELLTESTMLEADERGEOMETRY")]
        public void Probe()
        {
            Document document = AcadApplication.DocumentManager.MdiActiveDocument;
            var lines = new List<string>();

            using (Transaction transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                BlockTableRecord space = (BlockTableRecord)transaction.GetObject(
                    document.Database.CurrentSpaceId,
                    OpenMode.ForRead);

                foreach (ObjectId objectId in space)
                {
                    MLeader leader = transaction.GetObject(objectId, OpenMode.ForRead, false) as MLeader;
                    if (leader == null)
                    {
                        continue;
                    }

                    lines.Add("MLEADER " + leader.Handle);
                    lines.Add("TEXT " + Format(leader.TextLocation));
                    lines.Add("CONN +X " + Format(leader.ConnectionPoint(Vector3d.XAxis)));
                    lines.Add("CONN -X " + Format(leader.ConnectionPoint(-Vector3d.XAxis)));
                    lines.Add("CONN +Y " + Format(leader.ConnectionPoint(Vector3d.YAxis)));
                    lines.Add("CONN -Y " + Format(leader.ConnectionPoint(-Vector3d.YAxis)));

                    foreach (int leaderIndex in leader.GetLeaderIndexes())
                    {
                        foreach (int lineIndex in leader.GetLeaderLineIndexes(leaderIndex))
                        {
                            lines.Add(string.Format(
                                CultureInfo.InvariantCulture,
                                "LINE {0} FIRST {1} LAST {2}",
                                lineIndex,
                                Format(leader.GetFirstVertex(lineIndex)),
                                Format(leader.GetLastVertex(lineIndex))));
                        }
                    }
                }
            }

            string output = Path.Combine(
                Path.GetTempPath(),
                "CivilSpellAI-mleader-geometry-probe.txt");
            File.WriteAllLines(output, lines);
            document.Editor.WriteMessage("\nGeometría MLeader escrita en: " + output);
        }

        private static string Format(Point3d point)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:R},{1:R},{2:R})",
                point.X,
                point.Y,
                point.Z);
        }
    }
}
#endif
