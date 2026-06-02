using FactoryPulse.Reporting.Data;
using FactoryPulse.Reporting.Models;

namespace FactoryPulse.Reporting.Services;

public class SummaryService
{
    public ReportSummary BuildSummary(
        List<InspectionRecord> inspections,
        List<EquipmentRecord>  equipment,
        DateOnly from,
        DateOnly to)
    {
        return new ReportSummary
        {
            TotalInspections     = inspections.Count,
            PassCount            = inspections.Count(i => i.Result == "Pass"),
            FailCount            = inspections.Count(i => i.Result == "Fail"),
            NeedsAttentionCount  = inspections.Count(i => i.Result == "NeedsAttention"),
            EquipmentOnline      = equipment.Count(e => e.Status == "Online"),
            EquipmentFault       = equipment.Count(e => e.Status == "Fault"),
            EquipmentMaintenance = equipment.Count(e => e.Status == "Maintenance"),
            From = from,
            To   = to,
        };
    }
}
