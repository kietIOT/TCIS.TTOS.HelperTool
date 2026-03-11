using System.Net.Http.Json;
using System.Text.Json;
using TCIS.TTOS.HostManagement.API.Common.Models;
using TCIS.TTOS.ToolHelper.Dal.Entities;
using TCIS.TTOS.ToolHelper.DAL.UnitOfWork;

namespace TCIS.TTOS.HostManagement.API.Features.RedisManagement;

/// <summary>
/// Manages Redis instances in DB and proxies Redis operations to the HelperTool agent.
/// Resolves RedisInstance by ID ? looks up host ? builds agent URL with connection params ? forwards HTTP request.
/// </summary>
public class RedisProxyService(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<RedisProxyService> logger) : IRedisProxyService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    // ?? Redis Instance CRUD ??

    public async Task<BaseResponse<List<RedisInstanceDto>>> GetRedisInstancesByHostAsync(Guid hostId, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

        var instances = await uow.RedisInstanceRepository.FindAsync(x => x.HostId == hostId);
        var dtos = instances.Select(MapToDto).ToList();

        return new BaseResponse<List<RedisInstanceDto>>
        {
            IsSuccess = true,
            Data = dtos,
            Message = $"{dtos.Count} Redis instance(s) found"
        };
    }

    public async Task<BaseResponse<RedisInstanceDto>> GetRedisInstanceAsync(Guid redisInstanceId, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

        var instance = await uow.RedisInstanceRepository.FindOneAsync(x => x.Id == redisInstanceId);
        if (instance == null)
            return new BaseResponse<RedisInstanceDto> { IsSuccess = false, Message = $"Redis instance '{redisInstanceId}' not found" };

        return new BaseResponse<RedisInstanceDto>
        {
            IsSuccess = true,
            Data = MapToDto(instance),
            Message = "Redis instance found"
        };
    }

    public async Task<BaseResponse<RedisInstanceDto>> CreateRedisInstanceAsync(CreateRedisInstanceRequest request, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

        var host = await uow.MonitoredHostRepository.FindOneAsync(x => x.Id == request.HostId);
        if (host == null)
            return new BaseResponse<RedisInstanceDto> { IsSuccess = false, Message = $"Host '{request.HostId}' not found" };

        var existing = await uow.RedisInstanceRepository.FindOneAsync(
            x => x.HostId == request.HostId && x.Name == request.Name);
        if (existing != null)
            return new BaseResponse<RedisInstanceDto> { IsSuccess = false, Message = $"Redis instance with name '{request.Name}' already exists on this host" };

        var entity = new RedisInstance
        {
            HostId = request.HostId,
            Name = request.Name,
            Description = request.Description,
            Host = request.Host,
            Port = request.Port,
            Password = request.Password,
            Database = request.Database
        };

        await uow.RedisInstanceRepository.AddAsync(entity);
        await uow.CompleteAsync();

        return new BaseResponse<RedisInstanceDto>
        {
            IsSuccess = true,
            Data = MapToDto(entity),
            Message = "Redis instance created"
        };
    }

    public async Task<BaseResponse<RedisInstanceDto>> UpdateRedisInstanceAsync(Guid redisInstanceId, UpdateRedisInstanceRequest request, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

        var entity = await uow.RedisInstanceRepository.FindOneAsync(x => x.Id == redisInstanceId);
        if (entity == null)
            return new BaseResponse<RedisInstanceDto> { IsSuccess = false, Message = $"Redis instance '{redisInstanceId}' not found" };

        if (request.Name != null) entity.Name = request.Name;
        if (request.Description != null) entity.Description = request.Description;
        if (request.Host != null) entity.Host = request.Host;
        if (request.Port.HasValue) entity.Port = request.Port.Value;
        if (request.Password != null) entity.Password = request.Password;
        if (request.Database.HasValue) entity.Database = request.Database.Value;
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await uow.RedisInstanceRepository.UpdateAsync(entity);
        await uow.CompleteAsync();

        return new BaseResponse<RedisInstanceDto>
        {
            IsSuccess = true,
            Data = MapToDto(entity),
            Message = "Redis instance updated"
        };
    }

    public async Task<BaseResponse<object>> DeleteRedisInstanceAsync(Guid redisInstanceId, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

        var entity = await uow.RedisInstanceRepository.FindOneAsync(x => x.Id == redisInstanceId);
        if (entity == null)
            return new BaseResponse<object> { IsSuccess = false, Message = $"Redis instance '{redisInstanceId}' not found" };

        await uow.RedisInstanceRepository.DeleteAsync(entity);
        await uow.CompleteAsync();

        return new BaseResponse<object>
        {
            IsSuccess = true,
            Message = "Redis instance deleted"
        };
    }

    public async Task<BaseResponse<RedisInstanceDto>> GetRedisInstanceByNameAsync(string name, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

        var instance = await uow.RedisInstanceRepository.FindOneAsync(x => x.Name == name && x.IsActive);
        if (instance == null)
            return new BaseResponse<RedisInstanceDto> { IsSuccess = false, Message = $"Redis instance '{name}' not found or inactive" };

        return new BaseResponse<RedisInstanceDto>
        {
            IsSuccess = true,
            Data = MapToDto(instance),
            Message = "Redis instance found"
        };
    }

    // ?? Redis operations by ID (forwarded to agent) ??

    public async Task<BaseResponse<RedisInfoDto>> GetInfoAsync(Guid redisInstanceId, CancellationToken ct = default)
    {
        var (baseUrl, connQuery, error) = await ResolveAgentUrlAsync(redisInstanceId);
        if (error != null)
            return new BaseResponse<RedisInfoDto> { IsSuccess = false, Message = error };

        return await ForwardGetAsync<RedisInfoDto>(baseUrl!, $"api/redis/info?{connQuery}", ct);
    }

    public async Task<BaseResponse<RedisKeyListDto>> SearchKeysAsync(Guid redisInstanceId, string pattern, int maxCount = 100, CancellationToken ct = default)
    {
        var (baseUrl, connQuery, error) = await ResolveAgentUrlAsync(redisInstanceId);
        if (error != null)
            return new BaseResponse<RedisKeyListDto> { IsSuccess = false, Message = error };

        var query = $"{connQuery}&pattern={Uri.EscapeDataString(pattern)}&maxCount={maxCount}";
        return await ForwardGetAsync<RedisKeyListDto>(baseUrl!, $"api/redis/keys?{query}", ct);
    }

    public async Task<BaseResponse<RedisKeyDto>> GetKeyAsync(Guid redisInstanceId, string key, CancellationToken ct = default)
    {
        var (baseUrl, connQuery, error) = await ResolveAgentUrlAsync(redisInstanceId);
        if (error != null)
            return new BaseResponse<RedisKeyDto> { IsSuccess = false, Message = error };

        return await ForwardGetAsync<RedisKeyDto>(baseUrl!, $"api/redis/keys/{Uri.EscapeDataString(key)}?{connQuery}", ct);
    }

    public async Task<BaseResponse<RedisKeyDto>> SetKeyAsync(Guid redisInstanceId, SetKeyRequest request, CancellationToken ct = default)
    {
        var (baseUrl, connQuery, error) = await ResolveAgentUrlAsync(redisInstanceId);
        if (error != null)
            return new BaseResponse<RedisKeyDto> { IsSuccess = false, Message = error };

        return await ForwardPostAsync<SetKeyRequest, RedisKeyDto>(baseUrl!, $"api/redis/keys?{connQuery}", request, ct);
    }

    public async Task<BaseResponse<DeleteKeysResultDto>> DeleteKeysAsync(Guid redisInstanceId, DeleteKeysRequest request, CancellationToken ct = default)
    {
        var (baseUrl, connQuery, error) = await ResolveAgentUrlAsync(redisInstanceId);
        if (error != null)
            return new BaseResponse<DeleteKeysResultDto> { IsSuccess = false, Message = error };

        return await ForwardDeleteAsync<DeleteKeysRequest, DeleteKeysResultDto>(baseUrl!, $"api/redis/keys?{connQuery}", request, ct);
    }

    public async Task<BaseResponse<FlushResultDto>> FlushDatabaseAsync(Guid redisInstanceId, CancellationToken ct = default)
    {
        var (baseUrl, connQuery, error) = await ResolveAgentUrlAsync(redisInstanceId);
        if (error != null)
            return new BaseResponse<FlushResultDto> { IsSuccess = false, Message = error };

        return await ForwardDeleteAsync<object, FlushResultDto>(baseUrl!, $"api/redis/flush?{connQuery}", null, ct);
    }

    // ?? Redis operations by Name (forwarded to agent) ??

    public async Task<BaseResponse<RedisInfoDto>> GetInfoByNameAsync(string name, CancellationToken ct = default)
    {
        var (baseUrl, connQuery, error) = await ResolveAgentUrlByNameAsync(name);
        if (error != null)
            return new BaseResponse<RedisInfoDto> { IsSuccess = false, Message = error };

        return await ForwardGetAsync<RedisInfoDto>(baseUrl!, $"api/redis/info?{connQuery}", ct);
    }

    public async Task<BaseResponse<RedisKeyListDto>> SearchKeysByNameAsync(string name, string pattern, int maxCount = 100, CancellationToken ct = default)
    {
        var (baseUrl, connQuery, error) = await ResolveAgentUrlByNameAsync(name);
        if (error != null)
            return new BaseResponse<RedisKeyListDto> { IsSuccess = false, Message = error };

        var query = $"{connQuery}&pattern={Uri.EscapeDataString(pattern)}&maxCount={maxCount}";
        return await ForwardGetAsync<RedisKeyListDto>(baseUrl!, $"api/redis/keys?{query}", ct);
    }

    public async Task<BaseResponse<RedisKeyDto>> GetKeyByNameAsync(string name, string key, CancellationToken ct = default)
    {
        var (baseUrl, connQuery, error) = await ResolveAgentUrlByNameAsync(name);
        if (error != null)
            return new BaseResponse<RedisKeyDto> { IsSuccess = false, Message = error };

        return await ForwardGetAsync<RedisKeyDto>(baseUrl!, $"api/redis/keys/{Uri.EscapeDataString(key)}?{connQuery}", ct);
    }

    public async Task<BaseResponse<RedisKeyDto>> SetKeyByNameAsync(string name, SetKeyRequest request, CancellationToken ct = default)
    {
        var (baseUrl, connQuery, error) = await ResolveAgentUrlByNameAsync(name);
        if (error != null)
            return new BaseResponse<RedisKeyDto> { IsSuccess = false, Message = error };

        return await ForwardPostAsync<SetKeyRequest, RedisKeyDto>(baseUrl!, $"api/redis/keys?{connQuery}", request, ct);
    }

    public async Task<BaseResponse<DeleteKeysResultDto>> DeleteKeysByNameAsync(string name, DeleteKeysRequest request, CancellationToken ct = default)
    {
        var (baseUrl, connQuery, error) = await ResolveAgentUrlByNameAsync(name);
        if (error != null)
            return new BaseResponse<DeleteKeysResultDto> { IsSuccess = false, Message = error };

        return await ForwardDeleteAsync<DeleteKeysRequest, DeleteKeysResultDto>(baseUrl!, $"api/redis/keys?{connQuery}", request, ct);
    }

    public async Task<BaseResponse<FlushResultDto>> FlushDatabaseByNameAsync(string name, CancellationToken ct = default)
    {
        var (baseUrl, connQuery, error) = await ResolveAgentUrlByNameAsync(name);
        if (error != null)
            return new BaseResponse<FlushResultDto> { IsSuccess = false, Message = error };

        return await ForwardDeleteAsync<object, FlushResultDto>(baseUrl!, $"api/redis/flush?{connQuery}", null, ct);
    }

    // ?? Private helpers ??

    private async Task<(string? baseUrl, string? connQuery, string? error)> ResolveAgentUrlByNameAsync(string name)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

        var redisInstance = await uow.RedisInstanceRepository.FindOneAsync(x => x.Name == name && x.IsActive);
        if (redisInstance == null)
            return (null, null, $"Redis instance '{name}' not found or inactive");

        var host = await uow.MonitoredHostRepository.FindOneAsync(x => x.Id == redisInstance.HostId);
        if (host == null)
            return (null, null, $"Host for Redis instance '{name}' not found");

        if (!host.IsActive)
            return (null, null, $"Host '{host.Name}' ({host.IpAddress}) is inactive");

        var baseUrl = $"http://{host.IpAddress}:{host.AgentPort}";
        var connQuery = $"Host={Uri.EscapeDataString(redisInstance.Host)}" +
                        $"&Port={redisInstance.Port}" +
                        $"&Database={redisInstance.Database}";
        if (!string.IsNullOrEmpty(redisInstance.Password))
            connQuery += $"&Password={Uri.EscapeDataString(redisInstance.Password)}";

        logger.LogInformation("[REDIS-PROXY] Resolved instance '{Name}' on host '{Host}' ({Ip}:{Port})",
            name, host.Name, host.IpAddress, host.AgentPort);

        return (baseUrl, connQuery, null);
    }

    private async Task<(string? baseUrl, string? connQuery, string? error)> ResolveAgentUrlAsync(Guid redisInstanceId)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

        var redisInstance = await uow.RedisInstanceRepository.FindOneAsync(x => x.Id == redisInstanceId);
        if (redisInstance == null)
            return (null, null, $"Redis instance '{redisInstanceId}' not found");

        if (!redisInstance.IsActive)
            return (null, null, $"Redis instance '{redisInstance.Name}' is inactive");

        var host = await uow.MonitoredHostRepository.FindOneAsync(x => x.Id == redisInstance.HostId);
        if (host == null)
            return (null, null, $"Host for Redis instance '{redisInstance.Name}' not found");

        if (!host.IsActive)
            return (null, null, $"Host '{host.Name}' ({host.IpAddress}) is inactive");

        var baseUrl = $"http://{host.IpAddress}:{host.AgentPort}";
        var connQuery = $"Host={Uri.EscapeDataString(redisInstance.Host)}" +
                        $"&Port={redisInstance.Port}" +
                        $"&Database={redisInstance.Database}";
        if (!string.IsNullOrEmpty(redisInstance.Password))
            connQuery += $"&Password={Uri.EscapeDataString(redisInstance.Password)}";

        return (baseUrl, connQuery, null);
    }

    private async Task<BaseResponse<TResult>> ForwardGetAsync<TResult>(string baseUrl, string path, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            var url = $"{baseUrl}/{path}";
            logger.LogInformation("[REDIS-PROXY] GET {Url}", url);

            var response = await client.GetAsync(url, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<BaseResponse<TResult>>(body, JsonOptions)
                   ?? new BaseResponse<TResult> { IsSuccess = false, Message = "Empty response from agent" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[REDIS-PROXY] Failed to forward GET");
            return new BaseResponse<TResult> { IsSuccess = false, Message = $"Agent communication failed: {ex.Message}" };
        }
    }

    private async Task<BaseResponse<TResult>> ForwardPostAsync<TBody, TResult>(string baseUrl, string path, TBody body, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            var url = $"{baseUrl}/{path}";
            logger.LogInformation("[REDIS-PROXY] POST {Url}", url);

            var response = await client.PostAsJsonAsync(url, body, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<BaseResponse<TResult>>(responseBody, JsonOptions)
                   ?? new BaseResponse<TResult> { IsSuccess = false, Message = "Empty response from agent" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[REDIS-PROXY] Failed to forward POST");
            return new BaseResponse<TResult> { IsSuccess = false, Message = $"Agent communication failed: {ex.Message}" };
        }
    }

    private async Task<BaseResponse<TResult>> ForwardDeleteAsync<TBody, TResult>(string baseUrl, string path, TBody? body, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            var url = $"{baseUrl}/{path}";
            logger.LogInformation("[REDIS-PROXY] DELETE {Url}", url);

            HttpResponseMessage response;
            if (body != null)
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, url)
                {
                    Content = JsonContent.Create(body)
                };
                response = await client.SendAsync(request, ct);
            }
            else
            {
                response = await client.DeleteAsync(url, ct);
            }

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<BaseResponse<TResult>>(responseBody, JsonOptions)
                   ?? new BaseResponse<TResult> { IsSuccess = false, Message = "Empty response from agent" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[REDIS-PROXY] Failed to forward DELETE");
            return new BaseResponse<TResult> { IsSuccess = false, Message = $"Agent communication failed: {ex.Message}" };
        }
    }

    private static RedisInstanceDto MapToDto(RedisInstance entity) => new()
    {
        Id = entity.Id,
        HostId = entity.HostId,
        Name = entity.Name,
        Description = entity.Description,
        Host = entity.Host,
        Port = entity.Port,
        Database = entity.Database,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
