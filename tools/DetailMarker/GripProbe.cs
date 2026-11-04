using System;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
namespace DetalleLayout
{
    public sealed class GripProbe
    {
        [CommandMethod("DETALLEGRIPAPI")]
        public void Probe()
        {
            var types=new[]{typeof(AttributeDefinition),typeof(AttributeReference),typeof(MText),typeof(GripOverrule),typeof(GripData),typeof(GripDataCollection),typeof(MoveGripPointsFlags),typeof(GetGripPointsFlags),typeof(GripStatus),typeof(Overrule)}.Concat(typeof(GripData).GetNestedTypes()).ToArray();
            string root=Environment.GetEnvironmentVariable("DETALLE_OUTPUT");
            File.WriteAllLines(Path.Combine(root,"grip-api.txt"),types.SelectMany(t=>new[]{"TYPE "+t.FullName}.Concat(t.IsEnum?Enum.GetNames(t):new string[0]).Concat(t.GetConstructors().Select(m=>m.ToString())).Concat(t.GetProperties().Select(p=>p.PropertyType+" "+p.Name)).Concat(t.GetMethods().Where(m=>!m.IsSpecialName).Select(m=>m.ToString()))));
            Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nGRIP_API_OK\n");
        }
    }
}
