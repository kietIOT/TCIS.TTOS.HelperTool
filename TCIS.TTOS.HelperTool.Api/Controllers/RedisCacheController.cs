using Microsoft.AspNetCore.Mvc;
using TCIS.TTOS.HelperTool.API.Common.Models;
using TCIS.TTOS.HelperTool.API.Features.RedisManagement;

namespace TCIS.TTOS.HelperTool.API.Controllers;

/// <summary>
/// Redis cache management — runs locally on the agent host.
/// Called by HostManagement.API (proxy), not directly by frontend.
/// Connection params are passed per-request via query string.
/// </summary>
[ApiController]
[Route("api/redis")]
public class RedisCacheController(IRedisService redisService) : ControllerBase
{
    [HttpGet("info")]
    public async Task<IActionResult> GetInfo([FromQuery] RedisConnectionParams connParams, CancellationToken ct)
    {
        var result = await redisService.GetInfoAsync(connParams, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(500, result);
    }

    [HttpGet("keys")]
    public async Task<IActionResult> SearchKeys([FromQuery] RedisConnectionParams connParams, [FromQuery] string pattern = "*", [FromQuery] int maxCount = 100, CancellationToken ct = default)
    {
        var result = await redisService.SearchKeysAsync(connParams, pattern, maxCount, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(500, result);
    }

    [HttpGet("keys/{*key}")]
    public async Task<IActionResult> GetKey([FromQuery] RedisConnectionParams connParams, string key, CancellationToken ct)
    {
        var result = await redisService.GetKeyAsync(connParams, key, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpPost("keys")]
    public async Task<IActionResult> SetKey([FromQuery] RedisConnectionParams connParams, [FromBody] SetKeyRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Key))
            return BadRequest(new BaseResponse<object> { IsSuccess = false, Message = "Key is required" });

        var result = await redisService.SetKeyAsync(connParams, request, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(500, result);
    }

    [HttpDelete("keys")]
    public async Task<IActionResult> DeleteKeys([FromQuery] RedisConnectionParams connParams, [FromBody] DeleteKeysRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Pattern))
            return BadRequest(new BaseResponse<object> { IsSuccess = false, Message = "Pattern is required" });

        var result = await redisService.DeleteKeysAsync(connParams, request, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(500, result);
    }

    [HttpDelete("flush")]
    public async Task<IActionResult> FlushDatabase([FromQuery] RedisConnectionParams connParams, CancellationToken ct)
    {
        var result = await redisService.FlushDatabaseAsync(connParams, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(500, result);
    }
}
