using Microsoft.Extensions.Options;
using TCIS.TTOS.HelperTool.API.Features.DockerMonitor;

namespace TCIS.TTOS.HelperTool.API.Infrastructure.Services.Implement
{
    public class DockerMonitorBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<DockerMonitorOptions> options,
        ILogger<DockerMonitorBackgroundService> logger) : BackgroundService
    {
        private readonly DockerMonitorOptions _options = options.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("????????????????????????????????????????????????????????????????");
            Console.WriteLine("?        ???  DOCKER MONITOR SERVICE STARTED                    ?");
            Console.WriteLine($"?        Poll: {_options.PollIntervalSeconds}s | CPU alert: {_options.CpuSpikeThresholdPercent}% | Disk warn: {_options.DiskWarningThresholdPercent}%  ?");
            Console.WriteLine("????????????????????????????????????????????????????????????????");
            Console.ResetColor();

            await Task.Delay(3000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var monitorService = scope.ServiceProvider.GetRequiredService<IDockerMonitorService>();
                    await monitorService.MonitorAndAlertAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in Docker monitor poll cycle");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[MONITOR] ? Error: {ex.Message}");
                    Console.ResetColor();
                }

                await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[MONITOR] ?? Docker Monitor Service stopped");
            Console.ResetColor();
        }
    }
}
