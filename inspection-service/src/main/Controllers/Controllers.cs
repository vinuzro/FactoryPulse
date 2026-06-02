using FactoryPulse.Inspection.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FactoryPulse.Inspection.Controllers;

[ApiController]
[Authorize]
[Route("api/inspections")]
public class InspectionController : ControllerBase
{
    private readonly InspectionService _service;

    public InspectionController(InspectionService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? equipmentId)
    {
        // Engineers only see their own inspections; admins/viewers see all
        var role   = User.FindFirstValue("role") ?? "";
        var userId = role == "ENGINEER" ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

        var inspections = await _service.GetAllAsync(userId, equipmentId);
        return Ok(inspections);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var inspection = await _service.GetByIdAsync(id);
        return inspection is null ? NotFound() : Ok(inspection);
    }

    [HttpPost]
    [Authorize(Policy = "EngineerOrAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateInspectionDto dto)
    {
        try
        {
            var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var userName = User.FindFirstValue("fullName") ?? userId;
            var result   = await _service.CreateAsync(dto, userId, userName);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex)  { return NotFound(new  { error = ex.Message }); }
        catch (ArgumentException ex)     { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "EngineerOrAdmin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInspectionDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            return Ok(await _service.UpdateAsync(id, dto, userId));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex)    { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("{id}/audit")]
    [Authorize(Policy = "EngineerOrAdmin")]
    public async Task<IActionResult> GetAudit(int id, [FromServices] AuditService auditService)
    {
        var logs = await auditService.GetForEntityAsync("Inspection", id);
        return Ok(logs);
    }
}

[ApiController]
[Authorize]
[Route("api/equipment")]
public class EquipmentController : ControllerBase
{
    private readonly EquipmentService _service;

    public EquipmentController(EquipmentService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var eq = await _service.GetByIdAsync(id);
        return eq is null ? NotFound() : Ok(eq);
    }

    [HttpPut("{id}/status")]
    [Authorize(Policy = "EngineerOrAdmin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            return Ok(await _service.UpdateStatusAsync(id, dto.Status, userId));
        }
        catch (KeyNotFoundException ex) { return NotFound(new  { error = ex.Message }); }
        catch (ArgumentException ex)    { return BadRequest(new { error = ex.Message }); }
    }
}

public record UpdateStatusDto(string Status);
