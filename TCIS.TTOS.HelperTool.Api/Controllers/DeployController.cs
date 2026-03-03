using Microsoft.AspNetCore.Mvc;
using TCIS.TTOS.HelperTool.API.Features.Deploy;

namespace TCIS.TTOS.HelperTool.API.Controllers;

[ApiController]
[Route("api")]
public class DeployController(IDeployService deployService) : ControllerBase
{
    [HttpPost("deploy")]
    public async Task<ActionResult<DeployResponse>> Deploy([FromBody] DeployRequest request)
    {
        if (string.IsNullOrEmpty(request.JobName))
        {
            return BadRequest(new DeployResponse
            {
                Success = false,
                Message = "JobName is required"
            });
        }

        var result = await deployService.DeployAsync(request.JobName, request.Environment);

        return result.Success ? Ok(result) : StatusCode(500, result);
    }

    [HttpGet("jobs")]
    public ActionResult<List<string>> GetJobs()
    {
        var jobs = deployService.GetAvailableJobs();
        return Ok(jobs);
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
