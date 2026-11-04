using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

// The journal lives with the shared project so retries from another computer
// can finish a retirement. No original DWG is opened or resaved.
public static class DrawingRetirement
{
    static readonly JsonCodec Json = new JsonCodec();
    static string Root => Path.GetFullPath(PackagePaths.ProjectDirectory).TrimEnd(Path.DirectorySeparatorChar);
    static string Old => Path.Combine(Root, "Old");
    static string Journal => Path.Combine(Old, "Retiros_Excel", "retiros.json");
    public class Entry
    {
        public string Code { get; set; }
        public string Title { get; set; }
        public string Source { get; set; }
        public string SheetId { get; set; }
        public string Archive { get; set; }
        public string Backup { get; set; }
        public string SourceHash { get; set; }
        public string ArchiveHash { get; set; }
        public string Prepared { get; set; }
        public string Utc { get; set; }
        public string User { get; set; }
        public string State { get; set; }
    }
    static bool Same(string a, string b) => String.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
    static string Safe(string path, string parent)
    {
        var absolute = Path.GetFullPath(path);
        if (!absolute.StartsWith(Path.GetFullPath(parent).TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("RUTA DE RETIRO FUERA DEL PROYECTO: " + path);
        return absolute;
    }
    public static string Hash(string path)
    {
        using (var stream = System.IO.File.OpenRead(path)) return Hash(stream);
    }
    static string Hash(Stream stream)
    {
        stream.Position = 0;
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
    }
    static List<Entry> Read() => System.IO.File.Exists(Journal) ? Json.Deserialize<List<Entry>>(System.IO.File.ReadAllText(Journal)) : new List<Entry>();
    static void Save(List<Entry> entries)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Journal));
        var temp = Journal + "." + Guid.NewGuid().ToString("N") + ".tmp";
        System.IO.File.WriteAllText(temp, Json.Serialize(entries));
        if (System.IO.File.Exists(Journal)) System.IO.File.Replace(temp, Journal, null);
        else System.IO.File.Move(temp, Journal);
    }
    static void Check(Entry e)
    {
        Safe(e.Source, Root); Safe(e.Archive, Old); Safe(e.Backup, Path.Combine(Old, "Retiros_Excel"));
        if (!System.IO.File.Exists(e.Archive) || !System.IO.File.Exists(e.Backup) ||
            Hash(e.Archive) != e.ArchiveHash || Hash(e.Backup) != e.SourceHash || System.IO.File.Exists(e.Source))
            throw new InvalidOperationException("RETIRO PENDIENTE DE REVISION; ARCHIVO O RESPALDO CAMBIADO: " + e.Code);
    }
    public static void RequireArchived(string code, string source, string sheetId)
    {
        var entry = Read().LastOrDefault(e => e.Code == code && Same(e.Source, source) && e.SheetId == sheetId);
        if (entry == null) throw new InvalidOperationException("NO SE RETIRA LA HOJA SIN ARCHIVAR SU DWG: " + code);
        Check(entry);
    }
    public static int Run(List<ExcelPlanSync.Plan> plans)
    {
        var candidates = SheetSetSync.Retirements(new HashSet<string>(plans.Select(p => p.Code), StringComparer.OrdinalIgnoreCase));
        if (candidates.Count == 0) return 0;
        if (candidates.Any(r => Same(r.File, PackagePaths.DrawingFile)))
            throw new InvalidOperationException("LA PORTADA NO SE ARCHIVA AUTOMATICAMENTE. RESTAURE SU FILA EN EL EXCEL.");
        Directory.CreateDirectory(Path.GetDirectoryName(Journal));
        // Exclusive project-level lock: no two installations may retire at once.
        using (var journalLock = new FileStream(Journal + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        {
            var entries = Read();
            var leases = new Dictionary<string, FileStream>(StringComparer.OrdinalIgnoreCase);
            var prepared = new List<Tuple<SheetSetSync.Retirement, Entry>>();
            var manager = Application.DocumentManager;
            var original = manager.MdiActiveDocument;
            try
            {
                // All file/open-document checks precede any DWG or sheet removal.
                foreach (var r in candidates)
                {
                    Safe(r.File, Root);
                    if (!Path.GetFileNameWithoutExtension(r.File).Equals(r.Code, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("CODIGO Y ARCHIVO NO COINCIDEN: " + r.Code);
                    foreach (Document doc in manager)
                        if (!String.IsNullOrEmpty(doc.Database.Filename) && Same(doc.Database.Filename, r.File))
                            throw new InvalidOperationException("RETIRO PENDIENTE: GUARDE Y CIERRE EL PLANO " + r.Code + ". NO SE RETIRO NINGUNA HOJA.");
                    var previous = entries.LastOrDefault(e => e.Code == r.Code && e.SheetId == r.SheetId && Same(e.Source, r.File));
                    if (!System.IO.File.Exists(r.File))
                    {
                        if (previous == null) throw new FileNotFoundException("NO SE ARCHIVA UN DWG DESAPARECIDO: " + r.Code, r.File);
                        // Crash between rename and final journal write is recoverable.
                        if (!System.IO.File.Exists(previous.Archive) && System.IO.File.Exists(previous.Prepared) &&
                            Hash(previous.Prepared) == previous.ArchiveHash && System.IO.File.Exists(previous.Backup) && Hash(previous.Backup) == previous.SourceHash)
                            System.IO.File.Move(previous.Prepared, previous.Archive);
                        Check(previous); previous.State = "ARCHIVADO"; Save(entries);
                        prepared.Add(Tuple.Create(r, previous));
                        continue;
                    }
                    // Deny other readers/writers; permit rename while holding this
                    // handle so the original can move without a write-race window.
                    try { leases.Add(r.Code, new FileStream(r.File, FileMode.Open, FileAccess.ReadWrite, FileShare.Delete)); }
                    catch (IOException error) { throw new InvalidOperationException("RETIRO PENDIENTE: EL PLANO ESTA EN USO. PIDA GUARDAR Y CERRAR " + r.Code, error); }
                    var token = Guid.NewGuid().ToString("N");
                    var dir = Safe(Path.Combine(Old, "Retiros_Excel", token), Old);
                    var archive = Safe(Path.Combine(Old, r.Code + ".dwg"), Old);
                    if (Same(archive, r.File) || System.IO.File.Exists(archive)) archive = Safe(Path.Combine(Old, r.Code + " - RETIRADO " + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + token.Substring(0, 8) + ".dwg"), Old);
                    prepared.Add(Tuple.Create(r, new Entry { Code = r.Code, Title = r.Title, Source = Path.GetFullPath(r.File), SheetId = r.SheetId,
                        Archive = archive, Backup = Path.Combine(dir, "ORIGINAL - " + r.Code + ".dwg"), Prepared = Path.Combine(dir, "PREPARADO.dwg"),
                        SourceHash = Hash(leases[r.Code]), Utc = DateTime.UtcNow.ToString("o"), User = Environment.UserDomainName + "\\" + Environment.UserName, State = "PREPARADO" }));
                }
                // Work on private copies through the full native Civil host.
                // Never use ReadDwgFile on an original Civil database.
                foreach (var pair in prepared.Where(p => leases.ContainsKey(p.Item1.Code)))
                {
                    var r = pair.Item1; var e = pair.Item2;
                    Directory.CreateDirectory(Path.GetDirectoryName(e.Prepared));
                    var input = Path.Combine(Path.GetDirectoryName(e.Prepared), "COPIA.dwg");
                    leases[r.Code].Position = 0;
                    using (var output = new FileStream(input, FileMode.CreateNew, FileAccess.Write, FileShare.None)) leases[r.Code].CopyTo(output);
                    Document copy = null;
                    try
                    {
                        copy = manager.Open(input, false); manager.MdiActiveDocument = copy;
                        if (copy.IsReadOnly) throw new InvalidOperationException("NO SE PUEDE PREPARAR LA COPIA DE " + r.Code);
                        using (copy.LockDocument())
                        {
                            Freeze(copy.Database, r);
                            copy.Database.SaveAs(e.Prepared, true, DwgVersion.Current, copy.Database.SecurityParameters);
                        }
                    }
                    finally
                    {
                        if (copy != null) copy.CloseAndDiscard();
                        if (original != null) manager.MdiActiveDocument = original;
                    }
                    e.ArchiveHash = Hash(e.Prepared);
                    entries.Add(e); Save(entries);
                }
                // Preserve the original bytes before publishing a frozen archive.
                // The entry is durable before either rename, allowing retry.
                foreach (var pair in prepared.Where(p => leases.ContainsKey(p.Item1.Code)))
                {
                    var e = pair.Item2;
                    if (Hash(leases[e.Code]) != e.SourceHash) throw new InvalidOperationException("EL DWG CAMBIO DURANTE EL RETIRO: " + e.Code);
                    Safe(e.Source, Root); Safe(e.Backup, Old); Safe(e.Prepared, Old); Safe(e.Archive, Old);
                    System.IO.File.Move(e.Source, e.Backup);
                    try { System.IO.File.Move(e.Prepared, e.Archive); }
                    catch { System.IO.File.Move(e.Backup, e.Source); throw; }
                    leases[e.Code].Dispose(); leases.Remove(e.Code);
                    e.State = "ARCHIVADO"; Save(entries); Check(e);
                    System.IO.File.WriteAllText(e.Archive + " - RETIRO.txt", "PLANO RETIRADO DEL EXCEL\r\nCODIGO: " + e.Code + "\r\nTITULO: " + e.Title.ToUpperInvariant() + "\r\nUTC: " + e.Utc + "\r\nUSUARIO: " + e.User + "\r\nRESPALDO EXACTO: " + e.Backup + "\r\nSHA256 ORIGINAL: " + e.SourceHash + "\r\nSHA256 ARCHIVO: " + e.ArchiveHash + "\r\nMEMBRETE CONSERVADO COMO TEXTO; NO PERTENECE AL LISTADO ACTIVO.\r\n");
                }
                return prepared.Count;
            }
            finally
            {
                foreach (var stream in leases.Values) stream.Dispose();
                if (original != null) manager.MdiActiveDocument = original;
            }
        }
    }
    static void Freeze(Database db, SheetSetSync.Retirement r)
    {
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var dictionary = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
            var selected = new List<AttributeReference[]>();
            int primary = 0;
            foreach (DBDictionaryEntry item in dictionary)
            {
                var layout = (Layout)tr.GetObject(item.Value, OpenMode.ForRead);
                var space = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead);
                foreach (ObjectId id in space)
                {
                    var block = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                    if (block == null) continue;
                    var attrs = block.AttributeCollection.Cast<ObjectId>().Select(a => tr.GetObject(a, OpenMode.ForRead) as AttributeReference).Where(a => a != null).ToArray();
                    var number = attrs.SingleOrDefault(a => a.Tag.Equals("DRAWING_NO", StringComparison.OrdinalIgnoreCase));
                    if (number == null) continue;
                    // The registered layout must have exactly one matching frame.
                    // Cached invalid fields may be repaired from the live sheet metadata.
                    var value = number.TextString.Trim();
                    if (layout.LayoutName.Equals(r.Layout, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!value.Equals(r.Code, StringComparison.OrdinalIgnoreCase) && value.Any(c => c != '#' && !Char.IsWhiteSpace(c)))
                            throw new InvalidOperationException("EL MEMBRETE TIENE OTRO NUMERO: " + r.Code);
                        primary++; selected.Add(attrs);
                    }
                    else if (value.Equals(r.Code, StringComparison.OrdinalIgnoreCase)) selected.Add(attrs);
                }
            }
            if (primary != 1) throw new InvalidOperationException("NO HAY MEMBRETE UNICO PARA ARCHIVAR " + r.Code);
            var parts = TitleParts.Split(r.Title.ToUpperInvariant());
            foreach (var attrs in selected)
            foreach (var pair in new[] { Tuple.Create("TITTLE_DRAW1", parts[0]), Tuple.Create("TITTLE_DRAW2", parts[1]), Tuple.Create("DRAWING_NO", r.Code) })
            {
                var attr = attrs.Single(a => a.Tag.Equals(pair.Item1, StringComparison.OrdinalIgnoreCase));
                attr.UpgradeOpen();
                if (attr.HasFields)
                {
                    var field = (Field)tr.GetObject(attr.GetField(), OpenMode.ForWrite);
                    attr.RemoveField(); field.Erase();
                }
                attr.TextString = pair.Item2;
                if (attr.IsMTextAttribute) attr.UpdateMTextAttribute();
                attr.AdjustAlignment(db); attr.RecordGraphicsModified(true);
            }
            // Remove only this drawing's association with the retired sheet.
            var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
            if (nod.Contains("AcSheetSetData")) { nod.UpgradeOpen(); var id = nod.Remove("AcSheetSetData"); tr.GetObject(id, OpenMode.ForWrite).Erase(); }
            tr.Commit();
        }
    }
}
