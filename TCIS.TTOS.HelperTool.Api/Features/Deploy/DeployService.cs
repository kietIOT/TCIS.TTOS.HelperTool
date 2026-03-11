using System.Diagnostics;
using System.Text;
using TCIS.TTOS.ToolHelper.Dal.Entities;
using TCIS.TTOS.ToolHelper.DAL.UnitOfWork;

namespace TCIS.TTOS.HelperTool.API.Features.Deploy;

public class DeployService(
    IServiceScopeFactory scopeFactory,
    ILogger<DeployService> logger) : IDeployService
{
    public async Task<DeployResultDto> DeployByServiceNameAsync(DeployByNameRequest request, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

        // 1. Look up service config from DB
        var service = await uow.MonitoredServiceRepository.FindOneAsync(
            x => x.Name == request.ServiceName && x.IsActive);

        if (service == null)
        {
            return new DeployResultDto
            {
                ServiceName = request.ServiceName,
                Success = false,
                Error = $"Service '{request.ServiceName}' not found or inactive in DB"
            };
        }

        // 2. Build deploy command from DB config
        var deployCommand = BuildDeployCommand(service);
        if (string.IsNullOrWhiteSpace(deployCommand))
        {
            return new DeployResultDto
            {
                ServiceName = service.Name,
                Success = false,
                Error = "No deploy command configured. Set ComposeFilePath or DeployCommand in DB."
            };
        }

        logger.LogInformation("[DEPLOY] Executing deploy for service '{Service}': {Command}",
            service.Name, deployCommand);

        var sw = Stopwatch.StartNew();

        try
        {
            // 3. Execute docker command locally on this host
            var result = await ExecuteLocalCommandAsync(deployCommand, service.WorkingDirectory);
            sw.Stop();

            logger.LogInformation("[DEPLOY] Deploy {Status} for '{Service}' in {Duration}ms",
                result.Success ? "SUCCESS" : "FAILED", service.Name, sw.ElapsedMilliseconds);

            return new DeployResultDto
            {
                ServiceName = service.Name,
                Success = result.Success,
                Output = result.Output,
                Error = result.Error,
                DurationMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "[DEPLOY] Deploy exception for '{Service}'", service.Name);

            return new DeployResultDto
            {
                ServiceName = service.Name,
                Success = false,
                Error = ex.Message,
                DurationMs = sw.ElapsedMilliseconds
            };
        }
    }

    private static string? BuildDeployCommand(MonitoredService service)
    {
        if (!string.IsNullOrWhiteSpace(service.DeployCommand))
            return service.DeployCommand;

        if (!string.IsNullOrWhiteSpace(service.ComposeFilePath))
        {
            var f = service.ComposeFilePath;
            return $"docker compose -f {f} down --rmi all && docker compose -f {f} up -d";
        }

        if (!string.IsNullOrWhiteSpace(service.ContainerName) && !string.IsNullOrWhiteSpace(service.ImageName))
        {
            var port = service.Port.HasValue ? $"-p {service.Port}:{service.Port} " : "";
            return $"docker stop {service.ContainerName} 2>/dev/null; " +
                   $"docker rm {service.ContainerName} 2>/dev/null; " +
                   $"docker pull {service.ImageName} && " +
                   $"docker run -d --name {service.ContainerName} {port}{service.ImageName}";
        }

        return null;
    }

    private static async Task<CommandResult> ExecuteLocalCommandAsync(string command, string? workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"{command.Replace("\"", "\\\"")}\"",
            WorkingDirectory = workingDirectory ?? "/",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        return new CommandResult
        {
            Success = process.ExitCode == 0,
            Output = outputBuilder.ToString(),
            Error = string.IsNullOrEmpty(errorBuilder.ToString()) ? null : errorBuilder.ToString()
        };
    }

    private sealed class CommandResult
    {
        public bool Success { get; set; }
        public string? Output { get; set; }
        public string? Error { get; set; }
    }
}
