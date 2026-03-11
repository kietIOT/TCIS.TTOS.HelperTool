using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using TCIS.TTOS.HelperTool.API.Features.Deploy;

namespace TCIS.TTOS.HelperTool.API.Controllers;

[ApiController]
[Route("api")]
public class DeployController(IDeployService deployService) : ControllerBase
{
    /// <summary>
    /// Deploy a service by name. Reads config from DB, executes docker compose locally.
    /// Called by HostManagement.API.
    /// </summary>
    [HttpPost("deploy")]
    public async Task<IActionResult> Deploy([FromBody] DeployByNameRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ServiceName))
        {
            return BadRequest(new DeployResultDto
            {
                ServiceName = "",
                Success = false,
                Error = "ServiceName is required"
            });
        }

        var result = await deployService.DeployByServiceNameAsync(request, ct);
        return result.Success ? Ok(result) : StatusCode(500, result);
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
