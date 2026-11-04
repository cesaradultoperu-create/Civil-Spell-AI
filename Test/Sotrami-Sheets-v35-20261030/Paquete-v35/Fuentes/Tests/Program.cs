using System;
using System.Linq;
using System.Text.Json;

class Program
{
    static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }
    static void Main(string[] args)
    {
        Check(!PlanCodes.IsSupported("1107.001.00.14-300-00-P-001"), "Invalid stage accepted");
        Check(!PlanCodes.IsSupported("1107.001.00.16-200-00-P-001"), "Invalid stage accepted");
        Check(!PlanCodes.IsSupported("1107.001.00.16-300-00-I-001"), "Non-plan accepted");
        var plans = ExcelPlanSync.ReadPlans(args[0]);
        var final = plans.Where(p => PlanCodes.Stage(p.Code) == "16-300").ToArray();
        Check(final.Length == 3, "Expected the three final-stage plans in the reported workbook");
        Check(final.Select(p => p.Code).SequenceEqual(new[] {
            "1107.001.00.16-300-00-P-001", "1107.001.00.16-300-00-P-002", "1107.001.00.16-300-00-P-003"
        }), "Final codes or Excel order changed");
        Check(final.All(p => p.Revision == "A" && p.Date == ""), "Blank revision/date rule changed");
        var rows = ExcelPlanSync.Rows(plans);
        Check(rows.Count(r => r[0] == "ETAPA FINAL - 16-300") == 1, "Missing stage heading");
        Check(rows.Count(r => PlanCodes.IsSupported(r[1])) == plans.Count, "Table dropped plans");
        Check(rows.Where(r => PlanCodes.IsSupported(r[1])).Select(r => r[1]).SequenceEqual(plans.Select(p => p.Code)), "Table order changed");
        Check(rows.Single(r => r[1] == final[0].Code)[0] == "1", "Stage numbering did not reset");
        Check(rows.Single(r => r[1] == final[1].Code)[0] == "1", "Discipline numbering did not reset");
        Check(rows.Single(r => r[1] == final[2].Code)[0] == "2", "Discipline numbering is not consecutive");
        Check(rows.Select(r => String.Join("|", r)).SequenceEqual(ExcelPlanSync.Rows(plans).Select(r => String.Join("|", r))), "Repeated table build changed rows");
        var initial = plans.Where(p => PlanCodes.Stage(p.Code) == "14-200").ToList();
        var oldRows = ExcelPlanSync.Rows(initial);
        Check(oldRows.Select(r => String.Join("|", r)).SequenceEqual(rows.Take(oldRows.Count).Select(r => String.Join("|", r))), "Initial-stage table changed");
        Console.WriteLine(JsonSerializer.Serialize(new { success = true, total = plans.Count, initial = initial.Count, final = final.Select(p => new { p.SourceRow, p.Code, p.Title }), tableRows = rows.Count }));
    }
}
