namespace TCIS.TTOS.HelperTool.API.Features.Deploy;

public class DeployRequest
{
    public string JobName { get; set; } = string.Empty;
    public string? Environment { get; set; }
}

public class DeployResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Output { get; set; }
    public string? Error { get; set; }
}
