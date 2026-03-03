using Microsoft.AspNetCore.Mvc;
using TCIS.TTOS.HelperTool.API.Infrastructure.Services.Interface;

namespace TCIS.TTOS.HelperTool.API.Controllers
{
    [ApiController]
    [Route("api/monitor")]
    public class DockerMonitorController(IDockerMonitorService monitorService) : ControllerBase
    {
        /// <summary>
        /// Get CPU/Memory stats for all running Docker containers.
        /// </summary>
        [HttpGet("containers")]
        public async Task<IActionResult> GetContainerStats(CancellationToken ct)
        {
            var result = await monitorService.GetContainerStatsAsync(ct);
            return Ok(result);
        }

        /// <summary>
        /// Get disk usage of the host.
        /// </summary>
        [HttpGet("disk")]
        public async Task<IActionResult> GetDiskUsage(CancellationToken ct)
        {
            var result = await monitorService.GetDiskUsageAsync(ct);
            return Ok(result);
        }

        /// <summary>
        /// Get full report: containers + disk.
        /// </summary>
        [HttpGet("report")]
        public async Task<IActionResult> GetFullReport(CancellationToken ct)
        {
            var result = await monitorService.GetFullReportAsync(ct);
            return Ok(result);
        }

        /// <summary>
        /// Manually trigger a monitor check cycle.
        /// </summary>
        [HttpPost("check")]
        public async Task<IActionResult> TriggerCheck(CancellationToken ct)
        {
            await monitorService.MonitorAndAlertAsync(ct);
            return Ok(new { message = "Monitor check completed" });
        }
    }
}
