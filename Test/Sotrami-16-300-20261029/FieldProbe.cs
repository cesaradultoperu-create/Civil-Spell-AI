using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Interop;

public class FieldProbe
{
    const string Root = @"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI\artifacts\plan-list-update-20261016";
    static void Visit(IAcSmSubset subset, List<IAcSmSheet> sheets)
    {
        var en = subset.GetSheetEnumerator();
        for (var item = en.Next(); item != null; item = en.Next())
            if (item.GetTypeName() == "AcSmSubset") Visit((IAcSmSubset)item, sheets);
            else if (item.GetTypeName() == "AcSmSheet") sheets.Add((IAcSmSheet)item);
    }
    [CommandMethod("SOTRAMICAMPOTEST")]
    public void Probe()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        const string file = @"Q:\Proyectos 2026\1107.001.00.16 - Pad SOTRAMI\05_Drawing\SOTRAMI.dst";
        var mgr = new AcSmSheetSetMgr();
        var db = mgr.FindOpenDatabase(file);
        bool owned = db == null;
        if (owned) db = mgr.OpenDatabase(file, false);
        var sheets = new List<IAcSmSheet>();
        Visit(db.GetSheetSet(), sheets);
        var sheet = sheets.Single(s => s.GetNumber() == "1107.001.00.14-200-15-P-004");
        var handle = sheet.GetObjectId().GetHandle();
        var setHandle = db.GetSheetSet().GetObjectId().GetHandle();
        var tests = new List<object>();
        foreach (var path in new[] { file, file.Replace("\\", "\\\\"), file.Replace("\\", "/") })
        foreach (var suffix in new[] { ".Title", ".EXCEL_TITULO_1", ".EXCEL_TITULO_2", ".CustomProperty(\"EXCEL_TITULO_1\")" })
        {
            var code = "\\AcSm Database(\"" + path + "\").SheetSet(\"" + setHandle + "\").Component(\"" + handle + "\")" + suffix + " \\f \"%tc1\"";
            try
            {
                using (var field = new Field("%<" + code + ">%"))
                {
                    field.Evaluate((int)FieldEvaluationContext.Demand, doc.Database);
                    tests.Add(new { code, status = field.EvaluationStatus.Status.ToString(), field.EvaluationStatus.ErrorCode, field.EvaluationStatus.ErrorMessage, value = field.Value });
                }
            }
            catch (System.Exception e) { tests.Add(new { code, error = e.ToString() }); }
        }
        var attrs = new List<object>();
        var residentTests = new List<object>();
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            var dict = (DBDictionary)tr.GetObject(doc.Database.LayoutDictionaryId, OpenMode.ForRead);
            foreach (DBDictionaryEntry entry in dict)
            {
                var layout = (Layout)tr.GetObject(entry.Value, OpenMode.ForRead);
                var block = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead);
                foreach (ObjectId id in block)
                {
                    var insert = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                    if (insert == null) continue;
                    foreach (ObjectId aid in insert.AttributeCollection)
                    {
                        var attr = (AttributeReference)tr.GetObject(aid, OpenMode.ForRead);
                        if (!attr.Tag.StartsWith("TITTLE_DRAW")) continue;
                        var code = attr.HasFields ? ((Field)tr.GetObject(attr.GetField(), OpenMode.ForRead)).GetFieldCode() : null;
                        attrs.Add(new { layout = layout.LayoutName, attr.Tag, attr.TextString, hasFields = attr.HasFields, field = code, mtext = attr.IsMTextAttribute, width = attr.IsMTextAttribute ? attr.MTextAttribute.Width : 0, alignment = attr.AlignmentPoint.ToString() });
                        if (attr.HasFields && attr.Tag == "TITTLE_DRAW1" && residentTests.Count == 0)
                        {
                            var field = (Field)tr.GetObject(attr.GetField(), OpenMode.ForWrite);
                            foreach (var suffix in new[] { ".Title", ".EXCEL_TITULO_1", ".EXCEL_TITULO_2", ".CustomProperty(\"EXCEL_TITULO_1\")" })
                            {
                                var expression = "\\AcSm Database(\"" + file + "\").SheetSet(\"" + setHandle + "\").Component(\"" + handle + "\")" + suffix + " \\f \"%tc1\"";
                                field.SetFieldCode(expression);
                                field.Evaluate((int)FieldEvaluationContext.Demand,doc.Database);
                                residentTests.Add(new { expression, evaluator = field.EvaluatorId, isText = field.IsTextField, status = field.EvaluationStatus.Status.ToString(), value = field.Value });
                            }
                        }
                    }
                }
            }
        }
        File.WriteAllText(Path.Combine(Root,"field-probe.json"),new JavaScriptSerializer().Serialize(new { file = doc.Name, sheetId = handle, tests, residentTests, attrs }));
        if (owned) mgr.Close(db);
        doc.Editor.WriteMessage("\nPRUEBA DE CAMPOS TERMINADA.\n");
    }
}
