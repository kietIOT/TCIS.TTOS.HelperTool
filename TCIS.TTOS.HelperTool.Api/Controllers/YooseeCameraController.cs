using Microsoft.AspNetCore.Mvc;
using System.Net;
using TCIS.TTOS.HelperTool.API.Common.Models;
using TCIS.TTOS.HelperTool.API.Features.YooseeCamera;

namespace TCIS.TTOS.HelperTool.API.Controllers;

[ApiController]
[Route("api/yoosee")]
public class YooseeCameraController(
    IYooseePtzClient ptzClient,
    YooseeOptions yooseeOptions,
    ILogger<YooseeCameraController> logger) : ControllerBase
{
    /// <summary>
    /// Get supported PTZ actions.
    /// </summary>
    [HttpGet("actions")]
    public IActionResult GetActions()
    {
        return Ok(new BaseResponse<IReadOnlyCollection<string>>
        {
            IsSuccess = true,
            Data = ptzClient.SupportedActions,
            Message = "Supported PTZ actions"
        });
    }

    /// <summary>
    /// Send a PTZ command to a camera (continuous move, no auto-stop).
    /// </summary>
    [HttpPost("{ip}/move/{action}")]
    public async Task<IActionResult> Move(string ip, string action, CancellationToken ct)
    {
        if (!IsValidIp(ip))
            return BadRequest(new BaseResponse<object> { IsSuccess = false, Message = "Invalid IP address" });

        try
        {
            await ptzClient.MoveAsync(ip, action, ct);
            return Ok(new BaseResponse<object>
            {
                IsSuccess = true,
                Message = $"PTZ command '{action}' sent to {ip}"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new BaseResponse<object> { IsSuccess = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send PTZ command to {Ip}", ip);
            return StatusCode(500, new BaseResponse<object>
            {
                IsSuccess = false,
                Message = $"Failed to communicate with camera at {ip}"
            });
        }
    }

    /// <summary>
    /// Send a PTZ command then automatically send STOP after a configured delay.
    /// </summary>
    [HttpPost("{ip}/move-stop/{action}")]
    public async Task<IActionResult> MoveAndStop(string ip, string action, CancellationToken ct)
    {
        if (!IsValidIp(ip))
            return BadRequest(new BaseResponse<object> { IsSuccess = false, Message = "Invalid IP address" });

        try
        {
            await ptzClient.MoveAndStopAsync(ip, action, ct);
            return Ok(new BaseResponse<object>
            {
                IsSuccess = true,
                Message = $"PTZ command '{action}' + STOP sent to {ip}"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new BaseResponse<object> { IsSuccess = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send PTZ move-stop to {Ip}", ip);
            return StatusCode(500, new BaseResponse<object>
            {
                IsSuccess = false,
                Message = $"Failed to communicate with camera at {ip}"
            });
        }
    }

    /// <summary>
    /// Stop camera movement.
    /// </summary>
    [HttpPost("{ip}/stop")]
    public async Task<IActionResult> Stop(string ip, CancellationToken ct)
    {
        if (!IsValidIp(ip))
            return BadRequest(new BaseResponse<object> { IsSuccess = false, Message = "Invalid IP address" });

        try
        {
            await ptzClient.MoveAsync(ip, "STOP", ct);
            return Ok(new BaseResponse<object>
            {
                IsSuccess = true,
                Message = $"STOP sent to {ip}"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send STOP to {Ip}", ip);
            return StatusCode(500, new BaseResponse<object>
            {
                IsSuccess = false,
                Message = $"Failed to communicate with camera at {ip}"
            });
        }
    }

    /// <summary>
    /// Get the RTSP stream URL for the camera.
    /// </summary>
    [HttpGet("{ip}/stream-url")]
    public IActionResult GetStreamUrl(string ip)
    {
        if (!IsValidIp(ip))
            return BadRequest(new BaseResponse<object> { IsSuccess = false, Message = "Invalid IP address" });

        var url = ptzClient.GetStreamUrl(ip);
        return Ok(new BaseResponse<object>
        {
            IsSuccess = true,
            Data = new { StreamUrl = url },
            Message = "RTSP stream URL"
        });
    }

    private static bool IsValidIp(string ip)
        => IPAddress.TryParse(ip, out var a) && a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
}
