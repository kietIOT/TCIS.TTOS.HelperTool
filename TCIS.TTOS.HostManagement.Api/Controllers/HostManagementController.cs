using TCIS.TTOS.HostManagement.API.Features.HostManagement;

using Microsoft.AspNetCore.Mvc;
using TCIS.TTOS.HostManagement.API.Common.Models;
using TCIS.TTOS.HostManagement.API.Features.HostManagement;

namespace TCIS.TTOS.HostManagement.API.Controllers;

[ApiController]
[Route("api/hosts")]
public class HostManagementController(IHostService hostService) : ControllerBase
{
    // ?????????????????????????????? DASHBOARD ??????????????????????????????

    /// <summary>
    /// Get dashboard overview: totals of hosts and services with status breakdown.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var result = await hostService.GetDashboardAsync(ct);
        return Ok(result);
    }

    // ?????????????????????????????? HOST CRUD ??????????????????????????????

    /// <summary>
    /// Get all hosts. Optionally filter by active status.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllHosts([FromQuery] bool? activeOnly, CancellationToken ct)
    {
        var result = await hostService.GetAllHostsAsync(activeOnly, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get a single host by ID, including its services.
    /// </summary>
    [HttpGet("{hostId:guid}")]
    public async Task<IActionResult> GetHost(Guid hostId, CancellationToken ct)
    {
        var result = await hostService.GetHostByIdAsync(hostId, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Create a new host.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateHost([FromBody] CreateHostRequest request, CancellationToken ct)
    {
        var result = await hostService.CreateHostAsync(request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetHost), new { hostId = result.Data!.Id }, result)
            : BadRequest(result);
    }

    /// <summary>
    /// Update an existing host (partial update — only provided fields are changed).
    /// </summary>
    [HttpPut("{hostId:guid}")]
    public async Task<IActionResult> UpdateHost(Guid hostId, [FromBody] UpdateHostRequest request, CancellationToken ct)
    {
        var result = await hostService.UpdateHostAsync(hostId, request, ct);
        return result.IsSuccess ? Ok(result) : result.Message!.Contains("not found") ? NotFound(result) : BadRequest(result);
    }

    /// <summary>
    /// Delete a host and all its services (cascade).
    /// </summary>
    [HttpDelete("{hostId:guid}")]
    public async Task<IActionResult> DeleteHost(Guid hostId, CancellationToken ct)
    {
        var result = await hostService.DeleteHostAsync(hostId, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}
