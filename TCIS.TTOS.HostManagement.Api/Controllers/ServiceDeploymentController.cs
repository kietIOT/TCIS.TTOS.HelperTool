using Microsoft.AspNetCore.Mvc;
using TCIS.TTOS.HostManagement.API.Common.Models;
using TCIS.TTOS.HostManagement.API.Features.HostManagement;

namespace TCIS.TTOS.HostManagement.API.Controllers;

[ApiController]
[Route("api/deployments")]
public class ServiceDeploymentController(IServiceDeploymentService deploymentService) : ControllerBase
{
    /// <summary>
    /// Deploy a service by name. Looks up the service in DB, finds the host, and executes deployment.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DeployByServiceName([FromBody] DeployByNameRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ServiceName))
        {
            return BadRequest(new BaseResponse<DeploymentResultDto>
            {
                IsSuccess = false,
                Message = "ServiceName is required"
            });
        }

        var result = await deploymentService.DeployByServiceNameAsync(request, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get deployment history for a service by its ID.
    /// </summary>
    [HttpGet("history/{serviceId:guid}")]
    public async Task<IActionResult> GetDeploymentHistory(Guid serviceId, [FromQuery] int? take, CancellationToken ct)
    {
        var result = await deploymentService.GetDeploymentHistoryAsync(serviceId, take, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Get deployment history for a service by its name.
    /// </summary>
    [HttpGet("history/by-name/{serviceName}")]
    public async Task<IActionResult> GetDeploymentHistoryByName(string serviceName, [FromQuery] int? take, CancellationToken ct)
    {
        var result = await deploymentService.GetDeploymentHistoryByNameAsync(serviceName, take, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}
