using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

// Change one literal code cell. All other rows, styles, formulas and parts stay
// in the workbook; the caller publishes the prepared file only after backup.
internal static class RenumberWorkbook
{
    public static string Prepare(string copy, string oldCode, string newCode)
    {
        var data = WorkbookData.Read(copy);
        var col = data.Column("Codigo");
        if (data.Rows.Any(r => r.Get(col).Trim().Equals(newCode, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("EL CODIGO NUEVO YA EXISTE EN EL EXCEL.");
        var row = data.Rows.Single(r => r.Get(col).Trim().Equals(oldCode, StringComparison.OrdinalIgnoreCase));
        string address = col + row.Number;
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace relns = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        using (var input = new FileStream(copy, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        using (var zip = new ZipArchive(input, ZipArchiveMode.Update))
        {
            Func<string, XDocument> xml = name => { using (var s = zip.GetEntry(name).Open()) return XDocument.Load(s, LoadOptions.PreserveWhitespace); };
            var sheet = xml("xl/workbook.xml").Descendants(ns + "sheet").Single(e => (string)e.Attribute("name") == "LE");
            var rel = xml("xl/_rels/workbook.xml.rels").Root.Elements().Single(e => (string)e.Attribute("Id") == (string)sheet.Attribute(relns + "id"));
            var path = (string)rel.Attribute("Target"); path = path.StartsWith("/") ? path.TrimStart('/') : "xl/" + path;
            var doc = xml(path);
            var cell = doc.Descendants(ns + "c").Single(c => (string)c.Attribute("r") == address);
            if (cell.Element(ns + "f") != null) throw new InvalidOperationException("EL CODIGO ES UNA FORMULA; NO SE MODIFICA AUTOMATICAMENTE.");
            foreach (var e in cell.Elements().Where(e => e.Name == ns + "v" || e.Name == ns + "is").ToArray()) e.Remove();
            cell.SetAttributeValue("t", "inlineStr");
            cell.Add(new XElement(ns + "is", new XElement(ns + "t", newCode)));
            zip.GetEntry(path).Delete();
            using (var output = zip.CreateEntry(path).Open()) doc.Save(output, SaveOptions.DisableFormatting);
        }
        var after = WorkbookData.Read(copy);
        if (after.Rows.Count != data.Rows.Count || after.Rows.Single(r => r.Number == row.Number).Get(col) != newCode)
            throw new InvalidOperationException("NO SE PUDO VERIFICAR LA CELDA MODIFICADA.");
        foreach (var r in data.Rows)
        foreach (var c in r.Cells)
        {
            var expected = r.Number == row.Number && c.Key == col ? newCode : c.Value;
            if (after.Rows.Single(a => a.Number == r.Number).Get(c.Key) != expected)
                throw new InvalidOperationException("CAMBIO INESPERADO EN EL EXCEL.");
        }
        return address;
    }
}
