using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;
public class Audit
{
    static string Root=@"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI\Test\DetalleLayout-20261021-V6";
    [CommandMethod("DETALLETEXTAUDIT",CommandFlags.Session)]
    public void Run()
    {
        var report=new List<string>();var doc=App.DocumentManager.MdiActiveDocument;
        using(doc.LockDocument())using(var tr=doc.Database.TransactionManager.StartTransaction())
        {
            var bt=(BlockTable)tr.GetObject(doc.Database.BlockTableId,OpenMode.ForRead);var ids=new ObjectIdCollection();
            foreach(var b in bt.Cast<ObjectId>().Select(id=>(BlockTableRecord)tr.GetObject(id,OpenMode.ForRead)).Where(b=>b.IsLayout))
            foreach(var m in b.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<MLeader>())
            {
                if(m.ContentType!=ContentType.BlockContent)continue;
                var def=(BlockTableRecord)tr.GetObject(m.BlockContentId,OpenMode.ForRead);if(!def.Name.StartsWith("DETALLE_LAYOUT"))continue;
                ids.Add(m.ObjectId);report.Add("MARKER "+m.Handle+" "+def.Name+" center="+m.BlockPosition+" scale="+m.Scale+" blockScale="+m.BlockScale);
                foreach(var a in def.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<AttributeDefinition>())
                using(var v=m.GetBlockAttribute(a.ObjectId))
                {
                    report.Add(a.Tag+" defPos="+a.Position+" defAlign="+a.AlignmentPoint+" height="+a.Height+" justify="+a.Justify+" value="+v.TextString+" pos="+v.Position+" align="+v.AlignmentPoint);
                    if(a.IsMTextAttributeDefinition)using(var mt=a.MTextAttributeDefinition)report.Add("DEF mt="+mt.Location+" attachment="+mt.Attachment+" bounds="+string.Join(";",mt.GetBoundingPoints().Cast<Point3d>()));
                    if(v.IsMTextAttribute)using(var mt=v.MTextAttribute)report.Add("REF mt="+mt.Location+" attachment="+mt.Attachment+" bounds="+string.Join(";",mt.GetBoundingPoints().Cast<Point3d>()));
                }
            }
            if(ids.Count>0)using(var snapshot=doc.Database.Wblock(ids,Point3d.Origin)){snapshot.SaveAs(Path.Combine(Root,"markers-snapshot.dwg"),DwgVersion.Current);}
            tr.Abort();
        }
        File.WriteAllLines(Path.Combine(Root,"text-audit.txt"),report);doc.Editor.WriteMessage("\nDETALLE_TEXT_AUDIT_OK\n");
    }
}
