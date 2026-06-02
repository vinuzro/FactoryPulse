using FactoryPulse.Reporting.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace FactoryPulse.Reporting.Services;

public class ExcelReportService
{
    static ExcelReportService()
    {
        // EPPlus 5+ requires license context for non-commercial use
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public byte[] GenerateInspectionReport(
        List<InspectionRecord> inspections,
        List<EquipmentRecord>  equipment,
        DateTime from,
        DateTime to)
    {
        using var pkg = new ExcelPackage();

        AddInspectionsSheet(pkg, inspections, from, to);
        AddEquipmentSheet(pkg, equipment);
        AddSummarySheet(pkg, inspections, equipment);

        return pkg.GetAsByteArray();
    }

    private static void AddInspectionsSheet(ExcelPackage pkg, List<InspectionRecord> inspections, DateTime from, DateTime to)
    {
        var ws = pkg.Workbook.Worksheets.Add("Inspections");

        // Title row
        ws.Cells["A1"].Value = $"FactoryPulse — Inspection Report ({from:dd/MM/yyyy} to {to:dd/MM/yyyy})";
        ws.Cells["A1:G1"].Merge = true;
        ws.Cells["A1"].Style.Font.Size  = 14;
        ws.Cells["A1"].Style.Font.Bold  = true;
        ws.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(30, 60, 90));
        ws.Cells["A1"].Style.Font.Color.SetColor(Color.White);

        // Headers
        var headers = new[] { "ID", "Equipment", "Location", "Inspector", "Result", "Notes", "Date" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cells[3, i + 1].Value = headers[i];
            ws.Cells[3, i + 1].Style.Font.Bold = true;
            ws.Cells[3, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[3, i + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(52, 100, 145));
            ws.Cells[3, i + 1].Style.Font.Color.SetColor(Color.White);
        }

        // Data rows
        int row = 4;
        foreach (var insp in inspections)
        {
            ws.Cells[row, 1].Value = insp.Id;
            ws.Cells[row, 2].Value = insp.EquipmentName;
            ws.Cells[row, 3].Value = insp.EquipmentLocation;
            ws.Cells[row, 4].Value = insp.InspectorName;
            ws.Cells[row, 5].Value = insp.Result;
            ws.Cells[row, 6].Value = insp.Notes ?? "";
            ws.Cells[row, 7].Value = insp.CreatedAt;
            ws.Cells[row, 7].Style.Numberformat.Format = "dd/mm/yyyy hh:mm";

            // Colour-code result column
            var resultColor = insp.Result switch
            {
                "Pass"           => Color.FromArgb(39, 174, 96),
                "Fail"           => Color.FromArgb(192, 57, 43),
                "NeedsAttention" => Color.FromArgb(230, 126, 34),
                _                => Color.Black
            };
            ws.Cells[row, 5].Style.Font.Color.SetColor(resultColor);
            ws.Cells[row, 5].Style.Font.Bold = true;

            // Zebra striping
            if (row % 2 == 0)
            {
                var range = ws.Cells[row, 1, row, 7];
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(245, 247, 250));
            }

            row++;
        }

        ws.Cells[ws.Dimension.Address].AutoFitColumns();
        ws.Cells["A1:G1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        // Add auto-filter
        ws.Cells[3, 1, row - 1, 7].AutoFilter = true;
    }

    private static void AddEquipmentSheet(ExcelPackage pkg, List<EquipmentRecord> equipment)
    {
        var ws = pkg.Workbook.Worksheets.Add("Equipment");

        ws.Cells["A1"].Value = "Equipment Status";
        ws.Cells["A1:E1"].Merge = true;
        StyleTitleCell(ws.Cells["A1"]);

        var headers = new[] { "ID", "Name", "Location", "Status", "Last Updated" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cells[3, i + 1].Value = headers[i];
            StyleHeaderCell(ws.Cells[3, i + 1]);
        }

        int row = 4;
        foreach (var eq in equipment)
        {
            ws.Cells[row, 1].Value = eq.Id;
            ws.Cells[row, 2].Value = eq.Name;
            ws.Cells[row, 3].Value = eq.Location;
            ws.Cells[row, 4].Value = eq.Status;
            ws.Cells[row, 5].Value = eq.LastUpdated;
            ws.Cells[row, 5].Style.Numberformat.Format = "dd/mm/yyyy hh:mm";

            var statusColor = eq.Status switch
            {
                "Online"      => Color.FromArgb(39, 174, 96),
                "Fault"       => Color.FromArgb(192, 57, 43),
                "Maintenance" => Color.FromArgb(230, 126, 34),
                "Offline"     => Color.Gray,
                _             => Color.Black
            };
            ws.Cells[row, 4].Style.Font.Color.SetColor(statusColor);
            ws.Cells[row, 4].Style.Font.Bold = true;
            row++;
        }

        ws.Cells[ws.Dimension.Address].AutoFitColumns();
    }

    private static void AddSummarySheet(ExcelPackage pkg, List<InspectionRecord> inspections, List<EquipmentRecord> equipment)
    {
        var ws = pkg.Workbook.Worksheets.Add("Summary");
        ws.Cells["A1"].Value = "Report Summary";
        ws.Cells["A1:B1"].Merge = true;
        StyleTitleCell(ws.Cells["A1"]);

        var stats = new (string Label, object Value)[]
        {
            ("Total Inspections",   inspections.Count),
            ("Pass",                inspections.Count(i => i.Result == "Pass")),
            ("Fail",                inspections.Count(i => i.Result == "Fail")),
            ("Needs Attention",     inspections.Count(i => i.Result == "NeedsAttention")),
            ("",                    ""),
            ("Equipment Online",    equipment.Count(e => e.Status == "Online")),
            ("Equipment Fault",     equipment.Count(e => e.Status == "Fault")),
            ("Equipment Maintenance", equipment.Count(e => e.Status == "Maintenance")),
            ("Equipment Offline",   equipment.Count(e => e.Status == "Offline")),
        };

        int row = 3;
        foreach (var (label, value) in stats)
        {
            ws.Cells[row, 1].Value = label;
            ws.Cells[row, 2].Value = value;
            ws.Cells[row, 1].Style.Font.Bold = true;
            row++;
        }

        ws.Column(1).Width = 24;
        ws.Column(2).Width = 14;
    }

    private static void StyleTitleCell(ExcelRange cell)
    {
        cell.Style.Font.Size  = 13;
        cell.Style.Font.Bold  = true;
        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
        cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(30, 60, 90));
        cell.Style.Font.Color.SetColor(Color.White);
    }

    private static void StyleHeaderCell(ExcelRange cell)
    {
        cell.Style.Font.Bold = true;
        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
        cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(52, 100, 145));
        cell.Style.Font.Color.SetColor(Color.White);
    }
}
