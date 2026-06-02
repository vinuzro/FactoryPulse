// Models used by the reporting service
// These mirror what the inspection service returns — no shared library for now,
// keeping services independently deployable.

namespace FactoryPulse.Reporting.Models;

public class InspectionRecord
{
    public int      Id              { get; set; }
    public int      EquipmentId     { get; set; }
    public string   EquipmentName   { get; set; } = "";
    public string   EquipmentLocation { get; set; } = "";
    public string   InspectorName   { get; set; } = "";
    public string   Result          { get; set; } = "";
    public string?  Notes           { get; set; }
    public DateTime CreatedAt       { get; set; }
}

public class EquipmentRecord
{
    public int      Id       { get; set; }
    public string   Name     { get; set; } = "";
    public string   Location { get; set; } = "";
    public string   Status   { get; set; } = "";
    public DateTime LastUpdated { get; set; }
}

public class ReportSummary
{
    public int TotalInspections    { get; set; }
    public int PassCount           { get; set; }
    public int FailCount           { get; set; }
    public int NeedsAttentionCount { get; set; }
    public int EquipmentOnline     { get; set; }
    public int EquipmentFault      { get; set; }
    public int EquipmentMaintenance { get; set; }
    public DateOnly From           { get; set; }
    public DateOnly To             { get; set; }
}

// Cached report metadata stored in reporting DB
public class GeneratedReport
{
    public int      Id          { get; set; }
    public string   Type        { get; set; } = "";  // "PDF" or "Excel"
    public string   GeneratedBy { get; set; } = "";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime RangeFrom   { get; set; }
    public DateTime RangeTo     { get; set; }
    public int      RecordCount { get; set; }
}
