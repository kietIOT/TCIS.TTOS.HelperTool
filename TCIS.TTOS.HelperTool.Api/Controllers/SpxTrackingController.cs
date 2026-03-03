using Microsoft.AspNetCore.Mvc;
using TCIS.TTOS.HelperTool.API.Infrastructure.Services.Interface;

namespace TCIS.TTOS.HelperTool.API.Controllers
{
    [ApiController]
    [Route("api/spx-tracking")]
    public class SpxTrackingController(ISpxTrackingService trackingService) : ControllerBase
    {
        /// <summary>
        /// Subscribe to track a shipment by SPX tracking number.
        /// </summary>
        [HttpPost("subscribe/{spxTn}")]
        public async Task<IActionResult> Subscribe(string spxTn, CancellationToken ct)
        {
            var result = await trackingService.SubscribeAsync(spxTn, ct);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Unsubscribe from tracking a shipment.
        /// </summary>
        [HttpPost("unsubscribe/{spxTn}")]
        public async Task<IActionResult> Unsubscribe(string spxTn, CancellationToken ct)
        {
            var result = await trackingService.UnsubscribeAsync(spxTn, ct);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        /// <summary>
        /// Get current status and event history for a shipment.
        /// </summary>
        [HttpGet("status/{spxTn}")]
        public async Task<IActionResult> GetStatus(string spxTn, CancellationToken ct)
        {
            var result = await trackingService.GetShipmentStatusAsync(spxTn, ct);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        /// <summary>
        /// Get all active (non-terminal) shipments being tracked.
        /// </summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveShipments(CancellationToken ct)
        {
            var result = await trackingService.GetAllActiveShipmentsAsync(ct);
            return Ok(result);
        }

        /// <summary>
        /// Manually trigger a poll cycle (for testing).
        /// </summary>
        [HttpPost("poll")]
        public async Task<IActionResult> TriggerPoll(CancellationToken ct)
        {
            await trackingService.PollAndUpdateAsync(ct);
            return Ok(new { message = "Poll cycle completed" });
        }
    }
}
