using FactoryPulse.Reporting.Data;
using FactoryPulse.Reporting.Models;
using FactoryPulse.Reporting.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FactoryPulse.Reporting.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public class ReportController : ControllerBase
{
    private readonly InspectionDataClient _inspectionClient;
    private readonly PdfReportService     _pdf;
    private readonly ExcelReportService   _excel;
    private readonly SummaryService       _summary;
    private readonly ReportingDbContext   _db;

    public ReportController(
        InspectionDataClient inspectionClient,
        PdfReportService pdf,
        ExcelReportService excel,
        SummaryService summary,
        ReportingDbContext db)
    {
        _inspectionClient = inspectionClient;
        _pdf              = pdf;
        _excel            = excel;
        _summary          = summary;
        _db               = db;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        var (dateFrom, dateTo) = ResolveDateRange(from, to);
        ForwardToken();

        var inspections = await _inspectionClient.GetInspectionsAsync(dateFrom, dateTo);
        var equipment   = await _inspectionClient.GetEquipmentAsync();
        var summary     = _summary.BuildSummary(inspections, equipment, DateOnly.FromDateTime(dateFrom), DateOnly.FromDateTime(dateTo));

        return Ok(summary);
    }

    [HttpGet("pdf")]
    public async Task<IActionResult> GetPdf(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int? equipmentId)
    {
        var (dateFrom, dateTo) = ResolveDateRange(from, to);
        ForwardToken();

        var inspections = await _inspectionClient.GetInspectionsAsync(dateFrom, dateTo, equipmentId);
        var caller      = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var bytes       = _pdf.GenerateInspectionReport(inspections, dateFrom, dateTo, caller);

        await RecordReportAsync("PDF", caller, dateFrom, dateTo, inspections.Count);

        return File(bytes, "application/pdf",
            $"factorypulse-inspections-{dateFrom:yyyyMMdd}-{dateTo:yyyyMMdd}.pdf");
    }

    [HttpGet("excel")]
    public async Task<IActionResult> GetExcel(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        var (dateFrom, dateTo) = ResolveDateRange(from, to);
        ForwardToken();

        var inspections = await _inspectionClient.GetInspectionsAsync(dateFrom, dateTo);
        var equipment   = await _inspectionClient.GetEquipmentAsync();
        var caller      = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var bytes       = _excel.GenerateInspectionReport(inspections, equipment, dateFrom, dateTo);

        await RecordReportAsync("Excel", caller, dateFrom, dateTo, inspections.Count);

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"factorypulse-report-{dateFrom:yyyyMMdd}-{dateTo:yyyyMMdd}.xlsx");
    }

    // ----

    private void ForwardToken()
    {
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        if (!string.IsNullOrWhiteSpace(token))
            _inspectionClient.SetBearerToken(token);
    }

    private static (DateTime from, DateTime to) ResolveDateRange(DateOnly? from, DateOnly? to)
    {
        var toDate   = to?.ToDateTime(TimeOnly.MaxValue)   ?? DateTime.UtcNow;
        var fromDate = from?.ToDateTime(TimeOnly.MinValue) ?? toDate.AddDays(-30);
        return (fromDate, toDate);
    }

    private async Task RecordReportAsync(string type, string userId, DateTime from, DateTime to, int count)
    {
        _db.GeneratedReports.Add(new GeneratedReport
        {
            Type        = type,
            GeneratedBy = userId,
            RangeFrom   = from,
            RangeTo     = to,
            RecordCount = count,
        });
        await _db.SaveChangesAsync();
    }
}
