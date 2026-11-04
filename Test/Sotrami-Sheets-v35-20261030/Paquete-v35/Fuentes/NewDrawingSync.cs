using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

public class NewDrawingSync
{
    static readonly JsonCodec Json = new JsonCodec();
    const string TemplateMarker = "SOTRAMI_EMPTY_TEMPLATE";
    const string NewMarker = "SOTRAMI_NEW_FROM_TEMPLATE";
    public static string TemplateFile => Path.Combine(PackagePaths.Root, "Plantillas", "SOTRAMI - PLANO VACIO.dwg");
    static string JournalFile => Path.Combine(PackagePaths.LogDirectory, "planos-creados.json");
    public class Created
    {
        public string Code { get; set; }
        public string File { get; set; }
        public string Token { get; set; }
        public string Hash { get; set; }
        public string Layout { get; set; }
        public string Handle { get; set; }
        public bool Linked { get; set; }
    }
    static string Hash(string file) { using (var stream = System.IO.File.OpenRead(file)) using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", ""); }
    static void WriteMarker(Database db, Transaction tr, string key, string value)
    {
        var dict = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForWrite);
        Xrecord record;
        if (dict.Contains(key)) record = (Xrecord)tr.GetObject(dict.GetAt(key), OpenMode.ForWrite);
        else { record = new Xrecord(); dict.SetAt(key, record); tr.AddNewlyCreatedDBObject(record, true); }
        record.Data = new ResultBuffer(new TypedValue((int)DxfCode.Text, value));
    }
    static string Marker(Database db, Transaction tr, string key)
    {
        var dict = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        return dict.Contains(key) ? Convert.ToString(((Xrecord)tr.GetObject(dict.GetAt(key), OpenMode.ForRead)).Data.AsArray()[0].Value) : "";
    }
    static Layout Paper(Database db, Transaction tr)
    {
        var dict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
        var layouts = new List<Layout>();
        foreach (DBDictionaryEntry entry in dict) { var layout = (Layout)tr.GetObject(entry.Value, OpenMode.ForRead); if (!layout.ModelType) layouts.Add(layout); }
        if (layouts.Count != 1) throw new InvalidOperationException("La plantilla debe tener una sola presentacion.");
        return layouts[0];
    }
    static void CheckEmpty(Database db, Transaction tr)
    {
        var table = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var model = (BlockTableRecord)tr.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForRead);
        if (model.Cast<ObjectId>().Any()) throw new InvalidOperationException("La plantilla contiene dibujo en Modelo; no se utilizara.");
        foreach (ObjectId id in table)
        {
            var block = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
            if (block.IsFromExternalReference || block.IsFromOverlayReference) throw new InvalidOperationException("La plantilla tiene una referencia DWG externa: " + block.Name + "; utilice un marco independiente.");
        }
    }
    static AttributeReference[] TitleAttributes(Database db, Transaction tr)
    {
        var layout = Paper(db, tr);
        var block = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead);
        var candidates = new List<AttributeReference[]>();
        foreach (ObjectId id in block)
        {
            var insert = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
            if (insert == null) continue;
            var attrs = insert.AttributeCollection.Cast<ObjectId>().Select(a => (AttributeReference)tr.GetObject(a, OpenMode.ForRead)).ToArray();
            if (new[] { "TITTLE_DRAW1", "TITTLE_DRAW2", "DRAWING_NO" }.All(tag => attrs.Count(a => a.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase)) == 1)) candidates.Add(attrs);
        }
        if (candidates.Count != 1) throw new InvalidOperationException("El marco debe contener un solo membrete con titulo y numero de plano.");
        return candidates[0];
    }
    static void RemoveFields(DBObject obj, Transaction tr)
    {
        if (!obj.HasFields) return;
        var text = obj as DBText;
        var mtext = obj as MText;
        var visible = text != null ? text.TextString : mtext != null ? mtext.Text : null;
        var field = (Field)tr.GetObject(obj.GetField(), OpenMode.ForRead);
        obj.UpgradeOpen();
        obj.RemoveField();
        field.UpgradeOpen(); field.Erase();
        if (text != null) text.TextString = visible;
        else if (mtext != null) mtext.Contents = visible;
    }
    static void StaticText(AttributeReference attr, string value, Database db, Transaction tr)
    {
        RemoveFields(attr, tr); attr.UpgradeOpen(); attr.TextString = value;
        if (attr.IsMTextAttribute) attr.UpdateMTextAttribute();
        attr.AdjustAlignment(db);
    }
    // Export ONLY the selected paper layout into a fresh AutoCAD database.
    // The source document is never saved, edited, closed or duplicated wholesale.
    public static void ExportTemplate(Database source, string layoutName)
    {
        if (System.IO.File.Exists(TemplateFile)) throw new InvalidOperationException("La plantilla ya existe: " + TemplateFile);
        Directory.CreateDirectory(Path.GetDirectoryName(TemplateFile));
        var previous = HostApplicationServices.WorkingDatabase;
        var temp = Path.Combine(Path.GetDirectoryName(TemplateFile), Guid.NewGuid().ToString("N") + ".dwg");
        try
        {
            using (var target = new Database(true, true))
            using (var sourceTr = source.TransactionManager.StartTransaction())
            {
                var layouts = (DBDictionary)sourceTr.GetObject(source.LayoutDictionaryId, OpenMode.ForRead);
                if (!layouts.Contains(layoutName)) throw new InvalidOperationException("No existe la presentacion de referencia: " + layoutName);
                var from = (Layout)sourceTr.GetObject(layouts.GetAt(layoutName), OpenMode.ForRead);
                if (from.ModelType) throw new InvalidOperationException("Seleccione la presentacion del marco, no Modelo.");
                HostApplicationServices.WorkingDatabase = target;
                using (var tr = target.TransactionManager.StartTransaction())
                {
                    var manager = LayoutManager.Current;
                    var id = manager.CreateLayout("A");
                    var to = (Layout)tr.GetObject(id, OpenMode.ForWrite);
                    to.CopyFrom(from); to.LayoutName = "A";
                    var oldNames = new List<string>();
                    foreach (DBDictionaryEntry entry in (DBDictionary)tr.GetObject(target.LayoutDictionaryId, OpenMode.ForRead)) if (entry.Key != "Model" && entry.Key != "A") oldNames.Add(entry.Key);
                    manager.CurrentLayout = "A";
                    foreach (var name in oldNames) manager.DeleteLayout(name);
                    // Rebuild floating viewports without their frozen Xref layer
                    // lists, clipping dependencies or previous model associations.
                    var space = (BlockTableRecord)sourceTr.GetObject(from.BlockTableRecordId, OpenMode.ForRead);
                    var ids = new ObjectIdCollection();
                    var viewports = new List<Viewport>();
                    var logoCandidates = new List<Entity>();
                    double footerTop = Double.NegativeInfinity;
                    foreach (ObjectId oid in space)
                    {
                        var vp = sourceTr.GetObject(oid, OpenMode.ForRead) as Viewport;
                        if (vp == null)
                        {
                            var entity = sourceTr.GetObject(oid, OpenMode.ForRead) as Entity;
                            if (entity is RasterImage || entity is Ole2Frame) logoCandidates.Add(entity);
                            var insert = entity as BlockReference;
                            if (insert == null) continue;
                            var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            foreach (ObjectId attributeId in insert.AttributeCollection) tags.Add(((AttributeReference)sourceTr.GetObject(attributeId, OpenMode.ForRead)).Tag);
                            // Clone the complete frame/title-block insert only.
                            // Drawing-specific notes, legends, scale bars and
                            // other paper-space graphics are not a blank template.
                            if (new[] { "TITTLE_DRAW1", "TITTLE_DRAW2", "DRAWING_NO" }.All(tags.Contains))
                            {
                                ids.Add(oid);
                                foreach (ObjectId attributeId in insert.AttributeCollection)
                                {
                                    var attribute = (AttributeReference)sourceTr.GetObject(attributeId, OpenMode.ForRead);
                                    try { footerTop = Math.Max(footerTop, attribute.GeometricExtents.MaxPoint.Y); }
                                    catch (Autodesk.AutoCAD.Runtime.Exception) { }
                                }
                            }
                        }
                        else if (vp.Number != 1) viewports.Add(vp);
                    }
                    foreach (var logo in logoCandidates)
                    {
                        var box = logo.GeometricExtents;
                        if (box.MaxPoint.Y <= footerTop + 15) ids.Add(logo.ObjectId);
                    }
                    source.WblockCloneObjects(ids, to.BlockTableRecordId, new IdMapping(), DuplicateRecordCloning.Replace, false);
                    var targetSpace = (BlockTableRecord)tr.GetObject(to.BlockTableRecordId, OpenMode.ForWrite);
                    foreach (var original in viewports)
                    {
                        var layers = (LayerTable)tr.GetObject(target.LayerTableId, OpenMode.ForRead);
                        if (!layers.Has(original.Layer)) source.WblockCloneObjects(new ObjectIdCollection(new[] { original.LayerId }), target.LayerTableId, new IdMapping(), DuplicateRecordCloning.Ignore, false);
                        var viewport = new Viewport(); targetSpace.AppendEntity(viewport); tr.AddNewlyCreatedDBObject(viewport, true);
                        viewport.CenterPoint = original.CenterPoint; viewport.Width = original.Width; viewport.Height = original.Height;
                        viewport.ViewDirection = original.ViewDirection; viewport.ViewTarget = original.ViewTarget; viewport.ViewCenter = original.ViewCenter; viewport.ViewHeight = original.ViewHeight;
                        viewport.TwistAngle = original.TwistAngle; viewport.Layer = original.Layer; viewport.On = original.On; viewport.Locked = original.Locked;
                    }
                    tr.Commit();
                }
                // Embed only the reference's paper-space assets (for example
                // its logo DWG) in this new database, never in the source.
                var xrefs = new ObjectIdCollection();
                var cached = new List<ObjectId>();
                using (var tr = target.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId id in (BlockTable)tr.GetObject(target.BlockTableId, OpenMode.ForRead))
                    {
                        var block = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
                        if (!block.IsFromExternalReference && !block.IsFromOverlayReference) continue;
                        var file = block.PathName;
                        if (!Path.IsPathRooted(file)) file = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source.Filename), file));
                        if (!System.IO.File.Exists(file)) { cached.Add(id); continue; }
                        block.UpgradeOpen(); block.PathName = file; xrefs.Add(id);
                    }
                    tr.Commit();
                }
                if (xrefs.Count != 0) { target.ResolveXrefs(false, false); target.BindXrefs(xrefs, false); }
                // Some legacy logo references no longer have a source DWG but
                // retain their visible contents in the reference. Preserve that
                // cached appearance as ordinary blocks, with no missing-file link.
                foreach (var external in cached)
                {
                    using (var tr = target.TransactionManager.StartTransaction())
                    {
                        var old = (BlockTableRecord)tr.GetObject(external, OpenMode.ForRead);
                        var blocks = (BlockTable)tr.GetObject(target.BlockTableId, OpenMode.ForWrite);
                        var replacement = new BlockTableRecord { Name = "SOTRAMI_RECURSO_" + Guid.NewGuid().ToString("N"), Origin = old.Origin };
                        blocks.Add(replacement); tr.AddNewlyCreatedDBObject(replacement, true);
                        var contents = new ObjectIdCollection();
                        foreach (ObjectId cachedId in old) contents.Add(cachedId);
                        if (contents.Count > 0) target.DeepCloneObjects(contents, replacement.ObjectId, new IdMapping(), false);
                        foreach (ObjectId entityId in replacement)
                        {
                            var entity = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                            if (entity != null) entity.LayerId = target.LayerZero;
                        }
                        foreach (ObjectId referenceId in old.GetBlockReferenceIds(true, false))
                        {
                            var reference = (BlockReference)tr.GetObject(referenceId, OpenMode.ForWrite);
                            reference.BlockTableRecord = replacement.ObjectId;
                        }
                        tr.Commit();
                    }
                    target.DetachXref(external);
                }
                using (var tr = target.TransactionManager.StartTransaction())
                {
                    CheckEmpty(target, tr);
                    // Flatten fields copied from the reference. No previous sheet
                    // identity is allowed in the reusable template.
                    var table = (BlockTable)tr.GetObject(target.BlockTableId, OpenMode.ForRead);
                    foreach (ObjectId bid in table)
                    {
                        var btr = (BlockTableRecord)tr.GetObject(bid, OpenMode.ForRead);
                        foreach (ObjectId oid in btr)
                        {
                            var entity = tr.GetObject(oid, OpenMode.ForRead);
                            if (entity is DBText || entity is MText) RemoveFields(entity, tr);
                            var insert = entity as BlockReference;
                            if (insert != null) foreach (ObjectId aid in insert.AttributeCollection) RemoveFields(tr.GetObject(aid, OpenMode.ForRead), tr);
                        }
                    }
                    foreach (var attr in TitleAttributes(target, tr))
                    {
                        if (attr.Tag.Equals("DRAWING_NO", StringComparison.OrdinalIgnoreCase)) StaticText(attr, "NUMERO DESDE EXCEL", target, tr);
                        else if (attr.Tag.Equals("TITTLE_DRAW1", StringComparison.OrdinalIgnoreCase)) StaticText(attr, "TITULO DESDE EXCEL", target, tr);
                        else if (attr.Tag.Equals("TITTLE_DRAW2", StringComparison.OrdinalIgnoreCase)) StaticText(attr, "", target, tr);
                        else if (System.Text.RegularExpressions.Regex.IsMatch(attr.Tag, @"^R[1-8]_(NO|DATE|DESCRIPTION|BY|DS|CH|AP)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            StaticText(attr, attr.Tag == "R1_NO" ? "A" : attr.Tag == "R1_DESCRIPTION" ? "REVISIÓN INTERNA" : ".", target, tr);
                    }
                    WriteMarker(target, tr, TemplateMarker, "1"); tr.Commit();
                }
                target.Insunits = source.Insunits; target.Measurement = source.Measurement;
                target.TileMode = false;
                target.SaveAs(temp, DwgVersion.AC1032);
            }
            System.IO.File.Move(temp, TemplateFile);
        }
        finally { HostApplicationServices.WorkingDatabase = previous; if (System.IO.File.Exists(temp)) System.IO.File.Delete(temp); }
    }
    [CommandMethod("SOTRAMIPLANTILLA", CommandFlags.Modal)]
    public void MakeTemplate()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        try { ExportTemplate(doc.Database, LayoutManager.Current.CurrentLayout); doc.Editor.WriteMessage("\nPLANTILLA_SOTRAMI_OK: " + TemplateFile + ". MODELO VACIO.\n"); }
        catch (System.Exception ex) { doc.Editor.WriteMessage("\nPLANTILLA_SOTRAMI_PENDIENTE: " + ex.Message + "\n"); }
    }
    static List<Created> Journal() => System.IO.File.Exists(JournalFile) ? Json.Deserialize<List<Created>>(System.IO.File.ReadAllText(JournalFile)) : new List<Created>();
    static void SaveJournal(List<Created> rows) => System.IO.File.WriteAllText(JournalFile, Json.Serialize(rows));
    static void SaveCatalog(Created row)
    {
        var path = Path.Combine(PackagePaths.Root, "layout-catalog.json");
        var catalog = System.IO.File.Exists(path) ? Json.Deserialize<Dictionary<string, SheetSetSync.LayoutInfo>>(System.IO.File.ReadAllText(path)) : new Dictionary<string, SheetSetSync.LayoutInfo>();
        catalog[row.Code] = new SheetSetSync.LayoutInfo { File = row.File, Name = row.Layout, Handle = row.Handle };
        System.IO.File.WriteAllText(path, Json.Serialize(catalog));
    }
    public static List<Created> Prepare(List<ExcelPlanSync.Plan> plans)
    {
        var registered = SheetSetSync.RegisteredCodes(); // Also checks locks before creating files.
        var journal = Journal();
        var pending = new List<Created>();
        var missing = new List<ExcelPlanSync.Plan>();
        foreach (var plan in plans)
        {
            var path = Path.Combine(PackagePaths.ProjectDirectory, plan.Code + ".dwg");
            var own = journal.LastOrDefault(r => r.Code == plan.Code && !r.Linked && r.File.Equals(path, StringComparison.OrdinalIgnoreCase));
            if (System.IO.File.Exists(path))
            {
                if (own != null) { if (Hash(path) != own.Hash) throw new InvalidOperationException("El plano nuevo fue editado; se conserva sin sobrescribir: " + path); pending.Add(own); SaveCatalog(own); }
                continue;
            }
            if (System.IO.File.Exists(Path.Combine(PackagePaths.ProjectDirectory, "Old", plan.Code + ".dwg"))) continue;
            if (registered.Contains(plan.Code)) throw new InvalidOperationException("Falta el DWG de un plano ya vinculado; no se recreara ni reasignara: " + plan.Code);
            missing.Add(plan);
        }
        if (missing.Count == 0) return pending;
        if (!System.IO.File.Exists(TemplateFile)) throw new InvalidOperationException("Hay planos nuevos y falta la plantilla: " + TemplateFile + ". Prepare el marco con SOTRAMIPLANTILLA.");
        foreach (var plan in missing)
        {
            var record = new Created { Code = plan.Code, File = Path.Combine(PackagePaths.ProjectDirectory, plan.Code + ".dwg"), Token = Guid.NewGuid().ToString("N") };
            var staging = Directory.CreateDirectory(Path.Combine(PackagePaths.ProjectDirectory, "Old", "Altas_temporales")).FullName;
            var temp = Path.Combine(staging, record.Token + ".dwg");
            var previous = HostApplicationServices.WorkingDatabase;
            try
            {
                using (var db = new Database(false, true))
                {
                    db.ReadDwgFile(TemplateFile, FileOpenMode.OpenForReadAndAllShare, true, ""); db.CloseInput(true);
                    HostApplicationServices.WorkingDatabase = db;
                    using (var tr = db.TransactionManager.StartTransaction())
                    {
                        if (Marker(db, tr, TemplateMarker) != "1") throw new InvalidOperationException("La plantilla no ha sido preparada por SOTRAMIPLANTILLA.");
                        CheckEmpty(db, tr); var layout = Paper(db, tr);
                        record.Layout = layout.LayoutName; record.Handle = layout.Handle.ToString();
                        var lines = TitleParts.Split(plan.Title);
                        foreach (var attr in TitleAttributes(db, tr))
                        {
                            var tag = attr.Tag.ToUpperInvariant();
                            var value = tag == "DRAWING_NO" ? plan.Code : tag == "TITTLE_DRAW1" ? lines[0] : tag == "TITTLE_DRAW2" ? lines[1] : tag == "DISCIPLINE" ? plan.Discipline.ToUpperInvariant() : tag == "REV" || tag == "R1_NO" ? plan.Revision : tag == "R1_DATE" ? plan.Date : null;
                            if (value != null) StaticText(attr, value, db, tr);
                        }
                        WriteMarker(db, tr, NewMarker, record.Token); tr.Commit();
                    }
                    db.SaveAs(temp, DwgVersion.AC1032);
                }
                record.Hash = Hash(temp); journal.Add(record); SaveJournal(journal);
                // File.Move never replaces an existing DWG, even if another user
                // creates it after our initial existence check.
                System.IO.File.Move(temp, record.File); SaveCatalog(record); pending.Add(record);
            }
            finally { HostApplicationServices.WorkingDatabase = previous; if (System.IO.File.Exists(temp)) System.IO.File.Delete(temp); }
        }
        return pending;
    }
    public static void LinkCreated(List<Created> records)
    {
        if (records.Count == 0) return;
        var bindings = Json.Deserialize<TitleBlockBinding[]>(System.IO.File.ReadAllText(Path.Combine(PackagePaths.LogDirectory, "titleblock-bindings.json")));
        foreach (var record in records)
        {
            var binding = bindings.Single(b => b.Code == record.Code && b.File.Equals(record.File, StringComparison.OrdinalIgnoreCase));
            if (Hash(record.File) != record.Hash) throw new InvalidOperationException("El plano nuevo ha cambiado; se conserva sin sobrescribir: " + record.File);
            foreach (Document doc in Application.DocumentManager)
                if (doc.Database.Filename.Equals(record.File, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Cierre el plano nuevo antes de terminar su vinculacion: " + record.File);
            var previous = HostApplicationServices.WorkingDatabase;
            var temp = Path.Combine(PackagePaths.ProjectDirectory, "Old", "Altas_temporales", record.Token + "-vinculado.dwg");
            try
            {
                using (var db = new Database(false, true))
                {
                    db.ReadDwgFile(record.File, FileOpenMode.OpenForReadAndWriteNoShare, true, ""); db.CloseInput(true);
                    HostApplicationServices.WorkingDatabase = db;
                    using (var tr = db.TransactionManager.StartTransaction())
                    {
                        if (Marker(db, tr, NewMarker) != record.Token) throw new InvalidOperationException("No es un plano creado por esta operacion.");
                        CheckEmpty(db, tr);
                        foreach (var attr in TitleAttributes(db, tr))
                        {
                            var tag = attr.Tag.ToUpperInvariant();
                            var expression = tag == "DRAWING_NO" ? binding.FieldNumber : tag == "TITTLE_DRAW1" ? binding.Field1 : tag == "TITTLE_DRAW2" ? binding.Field2 : null;
                            var expected = tag == "DRAWING_NO" ? binding.Code : tag == "TITTLE_DRAW1" ? binding.Line1 : binding.Line2;
                            if (expression == null) continue;
                            attr.UpgradeOpen(); RemoveFields(attr, tr);
                            var field = new Field("%<" + expression + ">%"); attr.SetField(field); tr.AddNewlyCreatedDBObject(field, true);
                            field.EvaluationOption = FieldEvaluationOptions.Automatic; field.Evaluate((int)FieldEvaluationContext.Demand, db);
                            if (field.EvaluationStatus.Status != FieldEvaluationStatus.Success || (Convert.ToString(field.Value) ?? "").Trim() != expected.Trim()) throw new InvalidOperationException("No se ha validado el campo " + tag + " de " + record.Code);
                            if (attr.IsMTextAttribute) attr.UpdateMTextAttribute(); attr.AdjustAlignment(db); attr.RecordGraphicsModified(true);
                        }
                        tr.Commit();
                    }
                    db.SaveAs(temp, DwgVersion.AC1032);
                }
                // Only the freshly generated, unchanged file may be replaced.
                // Existing project drawings never enter this branch.
                using (System.IO.File.Open(record.File, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                if (Hash(record.File) != record.Hash) throw new InvalidOperationException("Otro usuario modifico el plano nuevo. No se sobrescribe.");
                System.IO.File.Replace(temp, record.File, null);
                var journal = Journal(); var saved = journal.Single(r => r.Token == record.Token); saved.Hash = Hash(record.File); saved.Linked = true; SaveJournal(journal);
                record.Linked = true;
            }
            finally { HostApplicationServices.WorkingDatabase = previous; if (System.IO.File.Exists(temp)) System.IO.File.Delete(temp); }
        }
    }
}
