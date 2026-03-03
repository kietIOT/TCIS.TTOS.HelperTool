namespace TCIS.TTOS.HelperTool.API.Features.YooseeCamera;

public interface IYooseePtzClient
{
    Task MoveAsync(string ip, string action, CancellationToken ct);
    Task MoveAndStopAsync(string ip, string action, CancellationToken ct);
    string GetStreamUrl(string ip);
    IReadOnlyCollection<string> SupportedActions { get; }
}
