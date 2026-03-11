namespace TCIS.TTOS.HelperTool.API.Features.Deploy;

public sealed class DeployByNameRequest
{
    public string ServiceName { get; set; } = default!;
    public string? Version { get; set; }
    public string? TriggeredBy { get; set; }
}

public sealed class DeployResultDto
{
    public string ServiceName { get; set; } = default!;
    public bool Success { get; set; }
    public string? Output { get; set; }
    public string? Error { get; set; }
    public long DurationMs { get; set; }
}
