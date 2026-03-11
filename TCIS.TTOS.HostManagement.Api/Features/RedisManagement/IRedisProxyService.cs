using TCIS.TTOS.HostManagement.API.Common.Models;


namespace TCIS.TTOS.HostManagement.API.Features.RedisManagement;

public interface IRedisProxyService
{
    // ?? Redis Instance CRUD ??
    Task<BaseResponse<List<RedisInstanceDto>>> GetRedisInstancesByHostAsync(Guid hostId, CancellationToken ct = default);
    Task<BaseResponse<RedisInstanceDto>> GetRedisInstanceAsync(Guid redisInstanceId, CancellationToken ct = default);
    Task<BaseResponse<RedisInstanceDto>> CreateRedisInstanceAsync(CreateRedisInstanceRequest request, CancellationToken ct = default);
    Task<BaseResponse<RedisInstanceDto>> UpdateRedisInstanceAsync(Guid redisInstanceId, UpdateRedisInstanceRequest request, CancellationToken ct = default);
    Task<BaseResponse<object>> DeleteRedisInstanceAsync(Guid redisInstanceId, CancellationToken ct = default);

    // ?? Redis Instance lookup by name ??
    Task<BaseResponse<RedisInstanceDto>> GetRedisInstanceByNameAsync(string name, CancellationToken ct = default);

    // ?? Redis operations by ID (forwarded to agent) ??
    Task<BaseResponse<RedisInfoDto>> GetInfoAsync(Guid redisInstanceId, CancellationToken ct = default);
    Task<BaseResponse<RedisKeyListDto>> SearchKeysAsync(Guid redisInstanceId, string pattern, int maxCount = 100, CancellationToken ct = default);
    Task<BaseResponse<RedisKeyDto>> GetKeyAsync(Guid redisInstanceId, string key, CancellationToken ct = default);
    Task<BaseResponse<RedisKeyDto>> SetKeyAsync(Guid redisInstanceId, SetKeyRequest request, CancellationToken ct = default);
    Task<BaseResponse<DeleteKeysResultDto>> DeleteKeysAsync(Guid redisInstanceId, DeleteKeysRequest request, CancellationToken ct = default);
    Task<BaseResponse<FlushResultDto>> FlushDatabaseAsync(Guid redisInstanceId, CancellationToken ct = default);

    // ?? Redis operations by Name (forwarded to agent) ??
    Task<BaseResponse<RedisInfoDto>> GetInfoByNameAsync(string name, CancellationToken ct = default);
    Task<BaseResponse<RedisKeyListDto>> SearchKeysByNameAsync(string name, string pattern, int maxCount = 100, CancellationToken ct = default);
    Task<BaseResponse<RedisKeyDto>> GetKeyByNameAsync(string name, string key, CancellationToken ct = default);
    Task<BaseResponse<RedisKeyDto>> SetKeyByNameAsync(string name, SetKeyRequest request, CancellationToken ct = default);
    Task<BaseResponse<DeleteKeysResultDto>> DeleteKeysByNameAsync(string name, DeleteKeysRequest request, CancellationToken ct = default);
    Task<BaseResponse<FlushResultDto>> FlushDatabaseByNameAsync(string name, CancellationToken ct = default);
}
