using FactoryPulse.Inspection.Data;
using FactoryPulse.Inspection.Hubs;
using FactoryPulse.Inspection.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FactoryPulse.Inspection.Services;

public class InspectionService
{
    private readonly InspectionDbContext _db;
    private readonly IHubContext<StatusHub> _hub;
    private readonly AuditService _audit;

    public InspectionService(InspectionDbContext db, IHubContext<StatusHub> hub, AuditService audit)
    {
        _db    = db;
        _hub   = hub;
        _audit = audit;
    }

    public async Task<List<Models.Inspection>> GetAllAsync(string? userId = null, int? equipmentId = null)
    {
        var query = _db.Inspections
            .Include(i => i.Equipment)
            .AsQueryable();

        // Engineers only see their own inspections
        if (userId != null)
            query = query.Where(i => i.InspectorUserId == userId);

        if (equipmentId.HasValue)
            query = query.Where(i => i.EquipmentId == equipmentId);

        return await query
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<Models.Inspection?> GetByIdAsync(int id)
    {
        return await _db.Inspections
            .Include(i => i.Equipment)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Models.Inspection> CreateAsync(CreateInspectionDto dto, string userId, string userName)
    {
        var equipment = await _db.Equipment.FindAsync(dto.EquipmentId)
            ?? throw new KeyNotFoundException($"Equipment {dto.EquipmentId} not found");

        if (!new[] { InspectionResult.Pass, InspectionResult.Fail, InspectionResult.NeedsAttention }
                .Contains(dto.Result))
            throw new ArgumentException($"Invalid result: {dto.Result}");

        var inspection = new Models.Inspection
        {
            EquipmentId     = dto.EquipmentId,
            InspectorUserId = userId,
            InspectorName   = userName,
            Notes           = dto.Notes,
            Result          = dto.Result,
        };

        _db.Inspections.Add(inspection);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("Inspection", inspection.Id, "Created", userId,
            newValue: $"Result={dto.Result}, Equipment={dto.EquipmentId}");

        // Broadcast the new inspection
        var evt = new InspectionSubmittedEvent(
            InspectionId:   inspection.Id,
            EquipmentId:    equipment.Id,
            EquipmentName:  equipment.Name,
            Result:         dto.Result,
            InspectorName:  userName,
            Timestamp:      inspection.CreatedAt);

        await _hub.Clients.All.SendAsync("InspectionSubmitted", evt);

        return inspection;
    }

    public async Task<Models.Inspection> UpdateAsync(int id, UpdateInspectionDto dto, string userId)
    {
        var inspection = await _db.Inspections.FindAsync(id)
            ?? throw new KeyNotFoundException($"Inspection {id} not found");

        var oldNotes  = inspection.Notes;
        var oldResult = inspection.Result;

        if (dto.Notes  != null) inspection.Notes  = dto.Notes;
        if (dto.Result != null) inspection.Result = dto.Result;
        inspection.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.LogAsync("Inspection", id, "Updated", userId,
            oldValue: $"Result={oldResult}, Notes={oldNotes}",
            newValue: $"Result={inspection.Result}, Notes={inspection.Notes}");

        return inspection;
    }
}

public record CreateInspectionDto(int EquipmentId, string Result, string? Notes);
public record UpdateInspectionDto(string? Result, string? Notes);
