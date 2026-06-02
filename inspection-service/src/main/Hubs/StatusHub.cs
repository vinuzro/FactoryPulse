using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FactoryPulse.Inspection.Hubs;

[Authorize]
public class StatusHub : Hub
{
    private readonly ILogger<StatusHub> _logger;

    public StatusHub(ILogger<StatusHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var user = Context.User?.Identity?.Name ?? "unknown";
        var role = Context.User?.FindFirst("role")?.Value ?? "";

        _logger.LogInformation("Client connected: {User} ({Role})", user, role);

        // Put admins in the admin group so they get alert broadcasts
        if (role == "ADMIN")
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {User}", Context.User?.Identity?.Name);
        return base.OnDisconnectedAsync(exception);
    }

    // Called by clients to subscribe to updates for specific equipment
    public async Task SubscribeToEquipment(int equipmentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Equipment_{equipmentId}");
    }

    public async Task UnsubscribeFromEquipment(int equipmentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Equipment_{equipmentId}");
    }
}

// Typed client interface — keeps hub method names from being stringly typed
public interface IStatusClient
{
    Task EquipmentStatusChanged(EquipmentStatusEvent evt);
    Task InspectionSubmitted(InspectionSubmittedEvent evt);
    Task AlertTriggered(AlertEvent evt);
}

public record EquipmentStatusEvent(int EquipmentId, string Name, string OldStatus, string NewStatus, DateTime Timestamp);
public record InspectionSubmittedEvent(int InspectionId, int EquipmentId, string EquipmentName, string Result, string InspectorName, DateTime Timestamp);
public record AlertEvent(string Level, string Message, int? EquipmentId, DateTime Timestamp);
