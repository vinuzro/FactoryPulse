using FactoryPulse.Reporting.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;

namespace FactoryPulse.Reporting.Data;

public class ReportingDbContext : DbContext
{
    public ReportingDbContext(DbContextOptions<ReportingDbContext> opts) : base(opts) { }

    public DbSet<GeneratedReport> GeneratedReports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GeneratedReport>(e =>
        {
            e.HasIndex(x => x.GeneratedAt);
            e.HasIndex(x => x.GeneratedBy);
        });
    }
}

/// <summary>
/// Fetches inspection and equipment data from the inspection service.
/// Passes the caller's JWT through so the inspection service can enforce its own auth.
/// </summary>
public class InspectionDataClient
{
    private readonly HttpClient _http;
    private readonly ILogger<InspectionDataClient> _logger;

    public InspectionDataClient(HttpClient http, ILogger<InspectionDataClient> logger)
    {
        _http   = http;
        _logger = logger;
    }

    public void SetBearerToken(string token)
    {
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<List<InspectionRecord>> GetInspectionsAsync(
        DateTime from, DateTime to, int? equipmentId = null)
    {
        var url = $"/api/inspections?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        if (equipmentId.HasValue)
            url += $"&equipmentId={equipmentId}";

        try
        {
            var result = await _http.GetFromJsonAsync<List<InspectionRecord>>(url);
            return result ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch inspections from inspection-service");
            return [];
        }
    }

    public async Task<List<EquipmentRecord>> GetEquipmentAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<EquipmentRecord>>("/api/equipment");
            return result ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch equipment from inspection-service");
            return [];
        }
    }
}
