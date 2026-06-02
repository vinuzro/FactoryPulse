using FactoryPulse.Inspection.Data;
using FactoryPulse.Inspection.Models;

namespace FactoryPulse.Inspection.Services;

public class AuditService
{
    private readonly InspectionDbContext _db;

    public AuditService(InspectionDbContext db) => _db = db;

    public async Task LogAsync(
        string entityType,
        int entityId,
        string action,
        string userId,
        string? oldValue = null,
        string? newValue = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            EntityType = entityType,
            EntityId   = entityId,
            Action     = action,
            UserId     = userId,
            OldValue   = oldValue,
            NewValue   = newValue,
            Timestamp  = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetForEntityAsync(string entityType, int entityId)
    {
        return await System.Threading.Tasks.Task.FromResult(
            _db.AuditLogs
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .OrderByDescending(a => a.Timestamp)
                .ToList()
        );
    }
}
