using TCIS.TTOS.HelperTool.API.Common.Models;


namespace TCIS.TTOS.HelperTool.API.Features.RedisManagement;

public interface IRedisService
{
    Task<BaseResponse<RedisInfoDto>> GetInfoAsync(RedisConnectionParams connParams, CancellationToken ct = default);
    Task<BaseResponse<RedisKeyListDto>> SearchKeysAsync(RedisConnectionParams connParams, string pattern, int maxCount = 100, CancellationToken ct = default);
    Task<BaseResponse<RedisKeyDto>> GetKeyAsync(RedisConnectionParams connParams, string key, CancellationToken ct = default);
    Task<BaseResponse<RedisKeyDto>> SetKeyAsync(RedisConnectionParams connParams, SetKeyRequest request, CancellationToken ct = default);
    Task<BaseResponse<DeleteKeysResultDto>> DeleteKeysAsync(RedisConnectionParams connParams, DeleteKeysRequest request, CancellationToken ct = default);
    Task<BaseResponse<FlushResultDto>> FlushDatabaseAsync(RedisConnectionParams connParams, CancellationToken ct = default);
}
