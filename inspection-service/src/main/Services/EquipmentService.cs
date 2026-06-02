using FactoryPulse.Inspection.Data;
using FactoryPulse.Inspection.Hubs;
using FactoryPulse.Inspection.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FactoryPulse.Inspection.Services;

public class EquipmentService
{
    private readonly InspectionDbContext _db;
    private readonly IHubContext<StatusHub> _hub;
    private readonly AuditService _audit;
    private readonly ILogger<EquipmentService> _logger;

    public EquipmentService(
        InspectionDbContext db,
        IHubContext<StatusHub> hub,
        AuditService audit,
        ILogger<EquipmentService> logger)
    {
        _db    = db;
        _hub   = hub;
        _audit = audit;
        _logger = logger;
    }

    public async Task<List<Equipment>> GetAllAsync()
    {
        return await _db.Equipment
            .OrderBy(e => e.Location)
            .ThenBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<Equipment?> GetByIdAsync(int id)
    {
        return await _db.Equipment.FindAsync(id);
    }

    public async Task<Equipment> UpdateStatusAsync(int id, string newStatus, string userId)
    {
        if (!EquipmentStatus.All.Contains(newStatus))
            throw new ArgumentException($"Invalid status: {newStatus}");

        var equipment = await _db.Equipment.FindAsync(id)
            ?? throw new KeyNotFoundException($"Equipment {id} not found");

        var oldStatus = equipment.Status;

        if (oldStatus == newStatus)
            return equipment;

        equipment.Status      = newStatus;
        equipment.LastUpdated = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.LogAsync("Equipment", id, "StatusChanged", userId,
            oldValue: oldStatus, newValue: newStatus);

        // Push real-time update to all subscribers and the general channel
        var evt = new EquipmentStatusEvent(
            EquipmentId: id,
            Name: equipment.Name,
            OldStatus: oldStatus,
            NewStatus: newStatus,
            Timestamp: equipment.LastUpdated);

        await _hub.Clients.Group($"Equipment_{id}")
            .SendAsync("EquipmentStatusChanged", evt);

        await _hub.Clients.All
            .SendAsync("EquipmentStatusChanged", evt);

        // Fault status triggers an admin alert
        if (newStatus == EquipmentStatus.Fault)
        {
            var alert = new AlertEvent(
                Level: "Warning",
                Message: $"{equipment.Name} at {equipment.Location} entered FAULT state",
                EquipmentId: id,
                Timestamp: DateTime.UtcNow);

            await _hub.Clients.Group("Admins").SendAsync("AlertTriggered", alert);
        }

        _logger.LogInformation("Equipment {Id} status changed {Old} -> {New} by {User}",
            id, oldStatus, newStatus, userId);

        return equipment;
    }
}
