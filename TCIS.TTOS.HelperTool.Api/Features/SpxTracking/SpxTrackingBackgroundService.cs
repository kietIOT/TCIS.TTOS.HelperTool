using Microsoft.Extensions.Options;

namespace TCIS.TTOS.HelperTool.API.Features.SpxTracking;

public class SpxTrackingBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<SpxTrackingOptions> options,
    ILogger<SpxTrackingBackgroundService> logger) : BackgroundService
{
    private readonly SpxTrackingOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("????????????????????????????????????????????????????????????????");
        Console.WriteLine("?        ?? SPX TRACKING SERVICE STARTED                      ?");
        Console.WriteLine($"?        Poll interval: {_options.PollIntervalSeconds}s                                  ?");
        Console.WriteLine("????????????????????????????????????????????????????????????????");
        Console.ResetColor();

        // Wait for app to fully start
        await Task.Delay(2000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var trackingService = scope.ServiceProvider.GetRequiredService<ISpxTrackingService>();
                await trackingService.PollAndUpdateAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in SPX tracking poll cycle");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[TRACKING] ? Poll cycle error: {ex.Message}");
                Console.ResetColor();
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("[TRACKING] ?? SPX Tracking Service stopped");
        Console.ResetColor();
    }
}
