namespace TCIS.TTOS.HelperTool.API.Infrastructure.Services.Interface
{
    public interface IYooseePtzClient
    {
        Task MoveAsync(string ip, string action, CancellationToken ct);
        Task MoveAndStopAsync(string ip, string action, CancellationToken ct);
        string GetStreamUrl(string ip);
        IReadOnlyCollection<string> SupportedActions { get; }
    }
}
