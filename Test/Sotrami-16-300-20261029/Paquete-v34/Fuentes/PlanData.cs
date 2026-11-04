using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO.Compression;
using System.Xml.Linq;

public partial class ExcelPlanSync
{
    public class Plan
    {
        public int SourceRow { get; set; }
        public string Number { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }
        public string Discipline { get; set; }
        public string Revision { get; set; }
        public string Date { get; set; }
    }
    static XDocument Xml(ZipArchive zip, string path) { using (var stream = zip.GetEntry(path).Open()) return XDocument.Load(stream); }
    public static List<Plan> ReadPlans(string workbook)
    {
        var data = WorkbookData.Read(workbook);
        var plans = new List<Plan>();
        foreach (var row in data.Rows)
        {
            Func<string, string> get = header => row.Get(data.Column(header));
            var code = get("Codigo").Trim().ToUpperInvariant();
            if (!PlanCodes.IsSupported(code)) continue;
            var rev = get("Ult. Rev.").Trim().ToUpperInvariant();
            int misplacedNumber;
            if (code.Contains("-200-10-P-") && Int32.TryParse(rev, out misplacedNumber) && misplacedNumber >= 1 && misplacedNumber <= 8) rev = "";
            if (String.IsNullOrWhiteSpace(rev)) rev = "A";
            var date = rev == "A" ? get("Fecha de RA") : rev == "B" ? get("Fecha de RB") : rev == "0" ? get("Fecha de R0") : "";
            double serial;
            if (Double.TryParse(date, NumberStyles.Float, CultureInfo.InvariantCulture, out serial)) date = DateTime.FromOADate(serial).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            plans.Add(new Plan { SourceRow = row.Number, Number = "", Code = code, Title = get("Descripción de entregables").Trim().ToUpperInvariant(), Discipline = get("Disciplina").Trim(), Revision = rev, Date = date.ToUpperInvariant() });
        }
        if (plans.Count == 0 || plans.GroupBy(p => p.Code).Any(g => g.Count() != 1)) throw new InvalidOperationException("La fuente no tiene un listado valido de codigos unicos.");
        return plans;
    }
    internal static List<string[]> Rows(List<Plan> plans)
    {
        var headers = new Dictionary<string, string> { { "General", "PLANOS GENERALES" }, { "Geología", "PLANOS GEOLÓGICOS" }, { "Geotecnia", "PLANOS GEOTÉCNICOS" }, { "Hidráulica", "PLANOS HIDRÁULICA" }, { "Diseño Civil", "PLANOS CIVILES" } };
        var result = new List<string[]> { new[] { "LISTA DE PLANOS", "", "", "", "" }, new[] { "", "", "", "", "" }, new[] { "", "NÚMERO DE PLANO", "TÍTULO DEL PLANO", "REVISIÓN", "FECHA DE EMISIÓN" } };
        string last = null;
        string lastStage = null;
        int groupNumber = 0;
        foreach (var p in plans)
        {
            var stage = PlanCodes.Stage(p.Code);
            if (stage != lastStage)
            {
                if (stage == "16-300") result.Add(new[] { "ETAPA FINAL - 16-300", "", "", "", "" });
                lastStage = stage; last = null; groupNumber = 0;
            }
            if (p.Discipline != last) { result.Add(new[] { headers.ContainsKey(p.Discipline) ? headers[p.Discipline] : p.Discipline.ToUpperInvariant(), "", "", "", "" }); last = p.Discipline; groupNumber = 0; }
            result.Add(new[] { (++groupNumber).ToString(CultureInfo.InvariantCulture), p.Code, p.Title, p.Revision, p.Date });
        }
        return result;
    }
}
