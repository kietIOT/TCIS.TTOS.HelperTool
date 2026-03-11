namespace TCIS.TTOS.HelperTool.API.Features.RedisManagement;

// ── Connection params (passed per-request from proxy or query) ──

public sealed class RedisConnectionParams
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6379;
    public string? Password { get; set; }
    public int Database { get; set; } = 0;
}

// ── Requests ──

public sealed class DeleteKeysRequest
{
    public string Pattern { get; set; } = default!;
}

public sealed class SetKeyRequest
{
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
    public int? TtlSeconds { get; set; }
}

// ── DTOs ──

public sealed class RedisInfoDto
{
    public bool IsConnected { get; set; }
    public string? RedisVersion { get; set; }
    public string? UsedMemory { get; set; }
    public string? UsedMemoryPeak { get; set; }
    public long TotalKeys { get; set; }
    public long ConnectedClients { get; set; }
    public long UptimeSeconds { get; set; }
    public Dictionary<string, string> RawInfo { get; set; } = [];
}

public sealed class RedisKeyDto
{
    public string Key { get; set; } = default!;
    public string Type { get; set; } = default!;
    public long Ttl { get; set; }
    public string? Value { get; set; }
}

public sealed class RedisKeyListDto
{
    public string Pattern { get; set; } = default!;
    public int Count { get; set; }
    public List<RedisKeyDto> Keys { get; set; } = [];
}

public sealed class DeleteKeysResultDto
{
    public string Pattern { get; set; } = default!;
    public long DeletedCount { get; set; }
}

public sealed class FlushResultDto
{
    public int Database { get; set; }
    public string Message { get; set; } = default!;
}
