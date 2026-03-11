using Microsoft.AspNetCore.Mvc;
using TCIS.TTOS.HostManagement.API.Common.Models;
using TCIS.TTOS.HostManagement.API.Features.RedisManagement;

namespace TCIS.TTOS.HostManagement.API.Controllers;

/// <summary>
/// Redis management — CRUD for Redis instances and proxy operations to the HelperTool agent.
/// </summary>
[ApiController]
[Route("api/redis")]
public class RedisManagementController(IRedisProxyService redisProxy) : ControllerBase
{
    // ?? Redis Instance CRUD ??

    /// <summary>
    /// Get all Redis instances for a host.
    /// </summary>
    [HttpGet("instances/host/{hostId:guid}")]
    public async Task<IActionResult> GetRedisInstancesByHost(Guid hostId, CancellationToken ct)
    {
        var result = await redisProxy.GetRedisInstancesByHostAsync(hostId, ct);
        return result.IsSuccess ? Ok(result) : ResolveError(result);
    }

    /// <summary>
    /// Get a specific Redis instance by ID.
    /// </summary>
    [HttpGet("instances/{redisInstanceId:guid}")]
    public async Task<IActionResult> GetRedisInstance(Guid redisInstanceId, CancellationToken ct)
    {
        var result = await redisProxy.GetRedisInstanceAsync(redisInstanceId, ct);
        return result.IsSuccess ? Ok(result) : ResolveError(result);
    }

    /// <summary>
    /// Create a new Redis instance configuration.
    /// </summary>
    [HttpPost("instances")]
    public async Task<IActionResult> CreateRedisInstance([FromBody] CreateRedisInstanceRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new BaseResponse<object> { IsSuccess = false, Message = "Name is required" });

        var result = await redisProxy.CreateRedisInstanceAsync(request, ct);
        return result.IsSuccess ? Ok(result) : ResolveError(result);
    }

    /// <summary>
    /// Update an existing Redis instance configuration.
    /// </summary>
    [HttpPut("instances/{redisInstanceId:guid}")]
    public async Task<IActionResult> UpdateRedisInstance(Guid redisInstanceId, [FromBody] UpdateRedisInstanceRequest request, CancellationToken ct)
    {
        var result = await redisProxy.UpdateRedisInstanceAsync(redisInstanceId, request, ct);
        return result.IsSuccess ? Ok(result) : ResolveError(result);
    }

    /// <summary>
    /// Delete a Redis instance configuration.
    /// </summary>
    [HttpDelete("instances/{redisInstanceId:guid}")]
    public async Task<IActionResult> DeleteRedisInstance(Guid redisInstanceId, CancellationToken ct)
    {
        var result = await redisProxy.DeleteRedisInstanceAsync(redisInstanceId, ct);
        return result.IsSuccess ? Ok(result) : ResolveError(result);
    }

    // ?? Redis Operations by ID (forwarded to agent) ??

    /// <summary>
    /// Get Redis server info from the agent for a specific Redis instance.
    /// </summary>
    [HttpGet("{redisInstanceId:guid}/info")]
    public async Task<IActionResult> GetInfo(Guid redisInstanceId, CancellationToken ct)
    {
        var result = await redisProxy.GetInfoAsync(redisInstanceId, ct);
        return result.IsSuccess ? Ok(result) : ResolveError(result);
    }

    /// <summary>
    /// Search Redis keys by pattern on a specific Redis instance.
    /// </summary>
    [HttpGet("{redisInstanceId:guid}/keys")]
    public async Task<IActionResult> SearchKeys(Guid redisInstanceId, [FromQuery] string pattern = "*", [FromQuery] int maxCount = 100, CancellationToken ct = default)
    {
        var result = await redisProxy.SearchKeysAsync(redisInstanceId, pattern, maxCount, ct);
        return result.IsSuccess ? Ok(result) : ResolveError(result);
    }

    /// <summary>
    /// Get a specific Redis key value from a specific Redis instance.
    /// </summary>
    [HttpGet("{redisInstanceId:guid}/keys/{*key}")]
    public async Task<IActionResult> GetKey(Guid redisInstanceId, string key, CancellationToken ct)
    {
        var result = await redisProxy.GetKeyAsync(redisInstanceId, key, ct);
        return result.IsSuccess ? Ok(result) : ResolveError(result);
    }

    /// <summary>
    /// Set a string key on a specific Redis instance.
    /// </summary>
    [HttpPost("{redisInstanceId:guid}/keys")]
    public async Task<IActionResult> SetKey(Guid redisInstanceId, [FromBody] SetKeyRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Key))
            return BadRequest(new BaseResponse<object> { IsSuccess = false, Message = "Key is required" });

        var result = await redisProxy.SetKeyAsync(redisInstanceId, request, ct);
        return result.IsSuccess ? Ok(result) : ResolveError(result);
    }

    /// <summary>
    /// Delete Redis keys by pattern on a specific Redis instance.
    /// </summary>
    [HttpDelete("{redisInstanceId:guid}/keys")]
    public async Task<IActionResult> DeleteKeys(Guid redisInstanceId, [FromBody] DeleteKeysRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Pattern))
            return BadRequest(new BaseResponse<object> { IsSuccess = false, Message = "Pattern is required" });

        var result = await redisProxy.DeleteKeysAsync(redisInstanceId, request, ct);
        return result.IsSuccess ? Ok(result) : ResolveError(result);
    }

    /// <summary>
    /// Flush all keys in the Redis database on a specific Redis instance. USE WITH CAUTION.
    /// </summary>
    [HttpDelete("{redisInstanceId:guid}/flush")]
    public async Task<IActionResult> FlushDatabase(Guid redisInstanceId, CancellationToken ct)
    {
        var result = await redisProxy.FlushDatabaseAsync(redisInstanceId, ct);
        return result.IsSuccess ? Ok(result) : ResolveError(result);
    }

    // ?? Redis Operations by Name (looks up instance by name ? resolves host ? forwards to agent) ??

    /// <summary>
    /// Get a Redis instance by its name.
    /// </summary>
    [HttpGet("instances/by-name/{name}")]
    public async Task<IActionResult> GetRedisInstanceByName(string name, CancellationToken ct)
    {
        var result = await redisProxy.GetRedisInstanceByNameAsync(name, ct);
        return result.IsSuccess ? Ok(result) : ResolveError(result);
    }

    /// <summary>
    /// Get Redis server info by instance name. Resolves the host automatically.
    /// </summary>
    [HttpGet("by-name/{name}/info")]
    public async Task<IActionResult> GetInfoByName(string name, CancellationToken ct)
    {
        var result = await redisProxy.GetInfoByNameAsync(name, ct);
        return result.IsSuccess ? Ok(result) : ResolveError(result);
    }

    /// <summary>
    /// Search Redis keys by pattern using instance name.
    /// </summary>
    [HttpGet("by-name/{name}/keys")]
    public async Task<IActionResult> SearchKeysByName(string name, [FromQuery] string pattern = "*", [FromQuery] int maxCount = 100, CancellationToken ct = default)
    {
        var result = await redisProxy.SearchKeysByNameAsync(name, pattern, maxCount, ct);
        return result.IsSuccess ? Ok(result) : ResolveError(result);
    }

    /// <summary>
    /// Get a specific Redis key value using instance name.
    /// </summary>
    [HttpGet("by-name/{name}/keys/{*key}")]
    public async Task<IActionResult> GetKeyByName(string name, string key, CancellationToken ct)
    {
        var result = await redisProxy.GetKeyByNameAsync(name, key, ct);
        return result.IsSuccess ? Ok(result) : ResolveError(result);
    }

    /// <summary>
    /// Set a string key using instance name.
    /// </summary>
    [HttpPost("by-name/{name}/keys")]
    public async Task<IActionResult> SetKeyByName(string name, [FromBody] SetKeyRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Key))
            return BadRequest(new BaseResponse<object> { IsSuccess = false, Message = "Key is required" });

        var result = await redisProxy.SetKeyByNameAsync(name, request, ct);
        return result.IsSuccess ? Ok(result) : ResolveError(result);
    }

    /// <summary>
    /// Delete Redis keys by pattern using instance name.
    /// </summary>
    [HttpDelete("by-name/{name}/keys")]
    public async Task<IActionResult> DeleteKeysByName(string name, [FromBody] DeleteKeysRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Pattern))
            return BadRequest(new BaseResponse<object> { IsSuccess = false, Message = "Pattern is required" });

        var result = await redisProxy.DeleteKeysByNameAsync(name, request, ct);
        return result.IsSuccess ? Ok(result) : ResolveError(result);
    }

    /// <summary>
    /// Flush all keys in the Redis database using instance name. USE WITH CAUTION.
    /// </summary>
    [HttpDelete("by-name/{name}/flush")]
    public async Task<IActionResult> FlushDatabaseByName(string name, CancellationToken ct)
    {
        var result = await redisProxy.FlushDatabaseByNameAsync(name, ct);
        return result.IsSuccess ? Ok(result) : ResolveError(result);
    }

    private IActionResult ResolveError<T>(BaseResponse<T> result)
    {
        if (result.Message != null && result.Message.Contains("not found"))
            return NotFound(result);
        return BadRequest(result);
    }
}
