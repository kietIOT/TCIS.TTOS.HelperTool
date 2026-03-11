using Microsoft.AspNetCore.Mvc;
using TCIS.TTOS.HostManagement.API.Features.HostManagement;

namespace TCIS.TTOS.HostManagement.API.Controllers;

[ApiController]
[Route("api/hosts/{hostId:guid}/services")]
public class MonitoredServiceController(IMonitoredServiceService serviceService) : ControllerBase
{
    /// <summary>
    /// Get all services for a host.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetServices(Guid hostId, CancellationToken ct)
    {
        var result = await serviceService.GetServicesByHostAsync(hostId, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Get a single service by ID.
    /// </summary>
    [HttpGet("{serviceId:guid}")]
    public async Task<IActionResult> GetService(Guid hostId, Guid serviceId, CancellationToken ct)
    {
        var result = await serviceService.GetServiceByIdAsync(hostId, serviceId, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Add a service to a host.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddService(Guid hostId, [FromBody] CreateServiceRequest request, CancellationToken ct)
    {
        var result = await serviceService.AddServiceAsync(hostId, request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetService), new { hostId, serviceId = result.Data!.Id }, result)
            : BadRequest(result);
    }

    /// <summary>
    /// Update an existing service (partial update).
    /// </summary>
    [HttpPut("{serviceId:guid}")]
    public async Task<IActionResult> UpdateService(Guid hostId, Guid serviceId, [FromBody] UpdateServiceRequest request, CancellationToken ct)
    {
        var result = await serviceService.UpdateServiceAsync(hostId, serviceId, request, ct);
        return result.IsSuccess ? Ok(result) : result.Message!.Contains("not found") ? NotFound(result) : BadRequest(result);
    }

    /// <summary>
    /// Delete a service from a host.
    /// </summary>
    [HttpDelete("{serviceId:guid}")]
    public async Task<IActionResult> DeleteService(Guid hostId, Guid serviceId, CancellationToken ct)
    {
        var result = await serviceService.DeleteServiceAsync(hostId, serviceId, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}
