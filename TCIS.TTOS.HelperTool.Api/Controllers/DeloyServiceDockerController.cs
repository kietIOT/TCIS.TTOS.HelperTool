using MediatR;
using Microsoft.AspNetCore.Mvc;
using TCIS.TTOS.HelperTool.API.Infrastructure.Services.Interface;
using TCIS.TTOS.HelperTool.API.Infrastructure.Services.Models;

namespace TCIS.TTOS.HelperTool.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class DeloyServiceDockerController(IDeployService deployService) : ControllerBase
    {
        private readonly IDeployService _deployService = deployService;


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

            var result = await _deployService.DeployAsync(request.JobName, request.Environment);

            return result.Success ? Ok(result) : StatusCode(500, result);
        }

        [HttpGet("jobs")]
        public ActionResult<List<string>> GetJobs()
        {
            var jobs = _deployService.GetAvailableJobs();
            return Ok(jobs);
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
    }
}
