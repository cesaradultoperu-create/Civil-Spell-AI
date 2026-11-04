using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

// Locate columns by their headers so inserting or moving a column in LE does
// not change which data is used for drawing numbers, titles or revisions.
class WorkbookData
{
    public sealed class Row
    {
        public int Number;
        public Dictionary<string, string> Cells;
        public string Get(string column) { string value; return column != null && Cells.TryGetValue(column, out value) ? value : ""; }
    }
    public Dictionary<string, string> Columns = new Dictionary<string, string>();
    public List<Row> Rows = new List<Row>();
    public string Column(string header)
    {
        string column;
        if (!Columns.TryGetValue(Normalize(header), out column)) throw new InvalidOperationException("Falta la columna '" + header + "' en la hoja LE del Excel.");
        return column;
    }
    static string Normalize(string value)
    {
        var text = new StringBuilder();
        foreach (var c in value.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && Char.IsLetterOrDigit(c)) text.Append(Char.ToUpperInvariant(c));
        return text.ToString();
    }
    static XDocument Xml(ZipArchive zip, string path) { using (var stream = zip.GetEntry(path).Open()) return XDocument.Load(stream); }
    public static WorkbookData Read(string file)
    {
        XNamespace s = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace r = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        using (var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var zip = new ZipArchive(input, ZipArchiveMode.Read))
        {
            var strings = zip.GetEntry("xl/sharedStrings.xml") == null ? new string[0] : Xml(zip, "xl/sharedStrings.xml").Root.Elements(s + "si").Select(e => String.Concat(e.Descendants(s + "t").Select(t => t.Value))).ToArray();
            var sheet = Xml(zip, "xl/workbook.xml").Descendants(s + "sheet").Single(e => (string)e.Attribute("name") == "LE");
            var rel = Xml(zip, "xl/_rels/workbook.xml.rels").Root.Elements().Single(e => (string)e.Attribute("Id") == (string)sheet.Attribute(r + "id"));
            var path = (string)rel.Attribute("Target");
            path = path.StartsWith("/") ? path.TrimStart('/') : "xl/" + path;
            var result = new WorkbookData();
            foreach (var row in Xml(zip, path).Descendants(s + "row"))
            {
                var data = new Row { Number = Int32.Parse((string)row.Attribute("r")), Cells = new Dictionary<string, string>() };
                foreach (var c in row.Elements(s + "c"))
                {
                    var value = (string)c.Element(s + "v") ?? "";
                    if ((string)c.Attribute("t") == "s" && value != "") value = strings[Int32.Parse(value)];
                    else if ((string)c.Attribute("t") == "inlineStr") value = String.Concat(c.Descendants(s + "t").Select(t => t.Value));
                    data.Cells[Regex.Match((string)c.Attribute("r"), "^[A-Z]+").Value] = value;
                }
                if (result.Columns.Count == 0 && data.Cells.Values.Any(v => Normalize(v) == "CODIGO") && data.Cells.Values.Any(v => Normalize(v) == "DESCRIPCIONDEENTREGABLES"))
                {
                    foreach (var pair in data.Cells)
                    {
                        var normalized = Normalize(pair.Value);
                        if (normalized.Length == 0) continue;
                        if (result.Columns.ContainsKey(normalized)) throw new InvalidOperationException("Columna repetida en la cabecera del Excel: " + pair.Value);
                        result.Columns.Add(normalized, pair.Key);
                    }
                }
                else if (result.Columns.Count != 0) result.Rows.Add(data);
            }
            foreach (var header in new[] { "Codigo", "Descripción de entregables", "Disciplina", "Ult. Rev.", "Fecha de RA", "Fecha de RB", "Fecha de R0" }) result.Column(header);
            return result;
        }
    }
}
