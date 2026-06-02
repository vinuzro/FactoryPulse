using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FactoryPulse.Inspection.Models;

public class Equipment
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [Required, MaxLength(100)]
    public string Location { get; set; } = "";

    [Required, MaxLength(20)]
    public string Status { get; set; } = EquipmentStatus.Online;

    // Bitfield for extended state — parsed via native C interop
    public int StatusFlags { get; set; } = 0;

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;

    public ICollection<Inspection> Inspections { get; set; } = [];
}

public static class EquipmentStatus
{
    public const string Online      = "Online";
    public const string Offline     = "Offline";
    public const string Maintenance = "Maintenance";
    public const string Fault       = "Fault";

    public static readonly string[] All = [Online, Offline, Maintenance, Fault];
}

public class Inspection
{
    public int Id { get; set; }

    public int EquipmentId { get; set; }

    [ForeignKey(nameof(EquipmentId))]
    public Equipment? Equipment { get; set; }

    [Required, MaxLength(50)]
    public string InspectorUserId { get; set; } = "";

    [Required, MaxLength(50)]
    public string InspectorName { get; set; } = "";

    public string? Notes { get; set; }

    [Required, MaxLength(20)]
    public string Result { get; set; } = InspectionResult.Pass;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public static class InspectionResult
{
    public const string Pass           = "Pass";
    public const string Fail           = "Fail";
    public const string NeedsAttention = "NeedsAttention";
}

public class AuditLog
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string EntityType { get; set; } = "";

    public int EntityId { get; set; }

    [Required, MaxLength(50)]
    public string Action { get; set; } = "";

    [Required, MaxLength(50)]
    public string UserId { get; set; } = "";

    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
