using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using StackExchange.Redis;
using TCIS.TTOS.HelperTool.API.Common.Models;

namespace TCIS.TTOS.HelperTool.API.Features.RedisManagement;

public class RedisService : IRedisService, IDisposable
{
    private readonly ILogger<RedisService> _logger;
    private readonly ConcurrentDictionary<string, ConnectionMultiplexer> _connections = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public RedisService(ILogger<RedisService> logger)
    {
        _logger = logger;
    }

    public async Task<BaseResponse<RedisInfoDto>> GetInfoAsync(RedisConnectionParams connParams, CancellationToken ct = default)
    {
        try
        {
            var mux = await GetConnectionAsync(connParams);
            var server = GetServer(mux);
            var info = await server.InfoAsync();

            var rawInfo = info.SelectMany(g => g).ToDictionary(p => p.Key, p => p.Value);

            var totalKeys = 0L;
            if (rawInfo.TryGetValue($"db{connParams.Database}", out var dbInfo))
            {
                foreach (var part in dbInfo.Split(','))
                {
                    var kv = part.Split('=');
                    if (kv.Length == 2 && kv[0] == "keys" && long.TryParse(kv[1], out var keys))
                        totalKeys = keys;
                }
            }

            return new BaseResponse<RedisInfoDto>
            {
                IsSuccess = true,
                Data = new RedisInfoDto
                {
                    IsConnected = mux.IsConnected,
                    RedisVersion = rawInfo.GetValueOrDefault("redis_version"),
                    UsedMemory = rawInfo.GetValueOrDefault("used_memory_human"),
                    UsedMemoryPeak = rawInfo.GetValueOrDefault("used_memory_peak_human"),
                    TotalKeys = totalKeys,
                    ConnectedClients = long.TryParse(rawInfo.GetValueOrDefault("connected_clients"), out var cc) ? cc : 0,
                    UptimeSeconds = long.TryParse(rawInfo.GetValueOrDefault("uptime_in_seconds"), out var up) ? up : 0,
                    RawInfo = rawInfo
                },
                Message = "Redis server info"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REDIS] Failed to get info for {Host}:{Port}", connParams.Host, connParams.Port);
            return Error<RedisInfoDto>($"Failed to connect to Redis: {ex.Message}");
        }
    }

    public async Task<BaseResponse<RedisKeyListDto>> SearchKeysAsync(RedisConnectionParams connParams, string pattern, int maxCount = 100, CancellationToken ct = default)
    {
        try
        {
            var mux = await GetConnectionAsync(connParams);
            var server = GetServer(mux);
            var db = mux.GetDatabase(connParams.Database);

            var keys = new List<RedisKeyDto>();
            await foreach (var key in server.KeysAsync(connParams.Database, pattern, maxCount))
            {
                if (keys.Count >= maxCount) break;

                var type = await db.KeyTypeAsync(key);
                var ttl = await db.KeyTimeToLiveAsync(key);
                string? value = null;

                if (type == RedisType.String)
                {
                    var raw = await db.StringGetAsync(key);
                    value = raw.HasValue ? raw.ToString() : null;
                    if (value is { Length: > 1024 })
                        value = value[..1024] + "... (truncated)";
                }

                keys.Add(new RedisKeyDto
                {
                    Key = key.ToString(),
                    Type = type.ToString(),
                    Ttl = ttl.HasValue ? (long)ttl.Value.TotalSeconds : -1,
                    Value = value
                });
            }

            return new BaseResponse<RedisKeyListDto>
            {
                IsSuccess = true,
                Data = new RedisKeyListDto { Pattern = pattern, Count = keys.Count, Keys = keys },
                Message = $"{keys.Count} key(s) matching '{pattern}'"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REDIS] Failed to search keys");
            return Error<RedisKeyListDto>($"Failed to search keys: {ex.Message}");
        }
    }

    public async Task<BaseResponse<RedisKeyDto>> GetKeyAsync(RedisConnectionParams connParams, string key, CancellationToken ct = default)
    {
        try
        {
            var mux = await GetConnectionAsync(connParams);
            var db = mux.GetDatabase(connParams.Database);

            if (!await db.KeyExistsAsync(key))
                return new BaseResponse<RedisKeyDto> { IsSuccess = false, Message = $"Key '{key}' not found" };

            var type = await db.KeyTypeAsync(key);
            var ttl = await db.KeyTimeToLiveAsync(key);
            string? value = type switch
            {
                RedisType.String => (await db.StringGetAsync(key)).ToString(),
                RedisType.List => $"[{string.Join(", ", (await db.ListRangeAsync(key, 0, 99)).Select(v => v.ToString()))}]",
                RedisType.Set => $"[{string.Join(", ", (await db.SetMembersAsync(key)).Select(v => v.ToString()))}]",
                RedisType.Hash => $"{{{string.Join(", ", (await db.HashGetAllAsync(key)).Select(h => $"\"{h.Name}\": \"{h.Value}\""))}}}",
                _ => $"({type} - use redis-cli for full view)"
            };

            return new BaseResponse<RedisKeyDto>
            {
                IsSuccess = true,
                Data = new RedisKeyDto
                {
                    Key = key,
                    Type = type.ToString(),
                    Ttl = ttl.HasValue ? (long)ttl.Value.TotalSeconds : -1,
                    Value = value
                },
                Message = "Key detail"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REDIS] Failed to get key '{Key}'", key);
            return Error<RedisKeyDto>($"Failed to get key: {ex.Message}");
        }
    }

    public async Task<BaseResponse<RedisKeyDto>> SetKeyAsync(RedisConnectionParams connParams, SetKeyRequest request, CancellationToken ct = default)
    {
        try
        {
            var mux = await GetConnectionAsync(connParams);
            var db = mux.GetDatabase(connParams.Database);

            TimeSpan? expiry = request.TtlSeconds.HasValue
                ? TimeSpan.FromSeconds(request.TtlSeconds.Value)
                : null;

            await db.StringSetAsync(request.Key, request.Value, expiry);

            return new BaseResponse<RedisKeyDto>
            {
                IsSuccess = true,
                Data = new RedisKeyDto
                {
                    Key = request.Key,
                    Type = "String",
                    Ttl = request.TtlSeconds ?? -1,
                    Value = request.Value
                },
                Message = $"Key '{request.Key}' set successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REDIS] Failed to set key '{Key}'", request.Key);
            return Error<RedisKeyDto>($"Failed to set key: {ex.Message}");
        }
    }

    public async Task<BaseResponse<DeleteKeysResultDto>> DeleteKeysAsync(RedisConnectionParams connParams, DeleteKeysRequest request, CancellationToken ct = default)
    {
        try
        {
            var mux = await GetConnectionAsync(connParams);
            var server = GetServer(mux);
            var db = mux.GetDatabase(connParams.Database);

            var keysToDelete = new List<RedisKey>();
            await foreach (var key in server.KeysAsync(connParams.Database, request.Pattern))
                keysToDelete.Add(key);

            long deleted = keysToDelete.Count > 0
                ? await db.KeyDeleteAsync(keysToDelete.ToArray())
                : 0;

            _logger.LogInformation("[REDIS] Deleted {Count} key(s) matching '{Pattern}'", deleted, request.Pattern);

            return new BaseResponse<DeleteKeysResultDto>
            {
                IsSuccess = true,
                Data = new DeleteKeysResultDto { Pattern = request.Pattern, DeletedCount = deleted },
                Message = $"{deleted} key(s) deleted matching '{request.Pattern}'"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REDIS] Failed to delete keys");
            return Error<DeleteKeysResultDto>($"Failed to delete keys: {ex.Message}");
        }
    }

    public async Task<BaseResponse<FlushResultDto>> FlushDatabaseAsync(RedisConnectionParams connParams, CancellationToken ct = default)
    {
        try
        {
            var mux = await GetConnectionAsync(connParams);
            var server = GetServer(mux);

            await server.FlushDatabaseAsync(connParams.Database);

            _logger.LogWarning("[REDIS] Flushed database {Db}", connParams.Database);

            return new BaseResponse<FlushResultDto>
            {
                IsSuccess = true,
                Data = new FlushResultDto { Database = connParams.Database, Message = $"Database {connParams.Database} flushed" },
                Message = $"All keys in database {connParams.Database} have been deleted"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REDIS] Failed to flush database");
            return Error<FlushResultDto>($"Failed to flush database: {ex.Message}");
        }
    }

    // ?? Private ??

    private static string BuildConnectionKey(RedisConnectionParams p) => $"{p.Host}:{p.Port}:{p.Database}";

    private async Task<ConnectionMultiplexer> GetConnectionAsync(RedisConnectionParams connParams)
    {
        var connKey = BuildConnectionKey(connParams);

        if (_connections.TryGetValue(connKey, out var existing) && existing.IsConnected)
            return existing;

        await _lock.WaitAsync();
        try
        {
            if (_connections.TryGetValue(connKey, out existing) && existing.IsConnected)
                return existing;

            var configStr = $"{connParams.Host}:{connParams.Port},abortConnect=false,connectTimeout=5000";
            if (!string.IsNullOrEmpty(connParams.Password))
                configStr += $",password={connParams.Password}";

            var connection = await ConnectionMultiplexer.ConnectAsync(configStr);

            _connections[connKey] = connection;
            return connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static IServer GetServer(ConnectionMultiplexer mux)
    {
        return mux.GetServer(mux.GetEndPoints().First());
    }

    private static BaseResponse<T> Error<T>(string message) => new()
    {
        IsSuccess = false,
        Message = message
    };

    public void Dispose()
    {
        foreach (var conn in _connections.Values)
            conn.Dispose();
        _connections.Clear();
        _lock.Dispose();
    }
}
