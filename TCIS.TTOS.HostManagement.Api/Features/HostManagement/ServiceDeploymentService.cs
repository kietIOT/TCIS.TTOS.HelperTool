using System.Diagnostics;
using System.Diagnostics;
using TCIS.TTOS.HostManagement.API.Common.Models;
using TCIS.TTOS.ToolHelper.Dal.Entities;
using TCIS.TTOS.ToolHelper.Dal.Enums;
using TCIS.TTOS.ToolHelper.DAL.UnitOfWork;

namespace TCIS.TTOS.HostManagement.API.Features.HostManagement;

public class ServiceDeploymentService(
    IServiceScopeFactory scopeFactory,
    ILogger<ServiceDeploymentService> logger) : IServiceDeploymentService
{
    public async Task<BaseResponse<DeploymentResultDto>> DeployByServiceNameAsync(DeployByNameRequest request, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

        // 1. Look up service by name
        var service = await uow.MonitoredServiceRepository.FindOneAsync(
            x => x.Name == request.ServiceName && x.IsActive);

        if (service == null)
        {
            return new BaseResponse<DeploymentResultDto>
            {
                IsSuccess = false,
                Message = $"Service '{request.ServiceName}' not found or inactive"
            };
        }

        // 2. Look up the host
        var host = await uow.MonitoredHostRepository.FindOneAsync(x => x.Id == service.HostId);
        if (host == null)
        {
            return new BaseResponse<DeploymentResultDto>
            {
                IsSuccess = false,
                Message = $"Host for service '{request.ServiceName}' not found"
            };
        }

        if (!host.IsActive)
        {
            return new BaseResponse<DeploymentResultDto>
            {
                IsSuccess = false,
                Message = $"Host '{host.Name}' ({host.IpAddress}) is inactive"
            };
        }

        // 3. Create deployment history record
        var history = new DeploymentHistory
        {
            ServiceId = service.Id,
            Status = DeploymentStatus.InProgress,
            Version = request.Version ?? service.Version,
            TriggeredBy = request.TriggeredBy ?? "API",
            StartedAt = DateTimeOffset.UtcNow
        };

        await uow.DeploymentHistoryRepository.AddAsync(history);
        await uow.CompleteAsync();

        logger.LogInformation("[DEPLOY] Starting deployment for service '{Service}' on host '{Host}' ({Ip})",
            service.Name, host.Name, host.IpAddress);

        var sw = Stopwatch.StartNew();

        try
        {
            // 4. Build and execute the deployment command
            var deployCommand = BuildDeployCommand(service);
            if (string.IsNullOrWhiteSpace(deployCommand))
            {
                history.Status = DeploymentStatus.Failed;
                history.ErrorMessage = "No deploy command configured for this service. Set ComposeFilePath or DeployCommand.";
                history.FinishedAt = DateTimeOffset.UtcNow;
                history.DurationMs = sw.ElapsedMilliseconds;

                await uow.DeploymentHistoryRepository.UpdateAsync(history);
                await uow.CompleteAsync();

                return new BaseResponse<DeploymentResultDto>
                {
                    IsSuccess = false,
                    Data = MapToResultDto(history, service, host),
                    Message = history.ErrorMessage
                };
            }

            // 5. Execute command (locally or via SSH depending on host)
            var result = await ExecuteDeployCommandAsync(host, deployCommand, service.WorkingDirectory);

            sw.Stop();

            // 6. Update history
            history.Output = result.Output;
            history.ErrorMessage = result.Error;
            history.Status = result.Success ? DeploymentStatus.Success : DeploymentStatus.Failed;
            history.FinishedAt = DateTimeOffset.UtcNow;
            history.DurationMs = sw.ElapsedMilliseconds;

            await uow.DeploymentHistoryRepository.UpdateAsync(history);

            // 7. Update service deployment status
            service.LastDeploymentStatus = history.Status;
            service.LastDeployedAt = DateTimeOffset.UtcNow;
            if (request.Version != null) service.Version = request.Version;
            service.UpdatedAt = DateTimeOffset.UtcNow;

            await uow.MonitoredServiceRepository.UpdateAsync(service);
            await uow.CompleteAsync();

            logger.LogInformation("[DEPLOY] Deployment {Status} for service '{Service}' on host '{Host}' in {Duration}ms",
                history.Status, service.Name, host.Name, history.DurationMs);

            return new BaseResponse<DeploymentResultDto>
            {
                IsSuccess = result.Success,
                Data = MapToResultDto(history, service, host),
                Message = result.Success
                    ? $"Service '{service.Name}' deployed successfully on host '{host.Name}'"
                    : $"Deployment failed for service '{service.Name}'"
            };
        }
        catch (Exception ex)
        {
            sw.Stop();

            history.Status = DeploymentStatus.Failed;
            history.ErrorMessage = ex.Message;
            history.FinishedAt = DateTimeOffset.UtcNow;
            history.DurationMs = sw.ElapsedMilliseconds;

            await uow.DeploymentHistoryRepository.UpdateAsync(history);

            service.LastDeploymentStatus = DeploymentStatus.Failed;
            service.UpdatedAt = DateTimeOffset.UtcNow;
            await uow.MonitoredServiceRepository.UpdateAsync(service);

            await uow.CompleteAsync();

            logger.LogError(ex, "[DEPLOY] Deployment failed for service '{Service}' on host '{Host}'",
                service.Name, host.Name);

            return new BaseResponse<DeploymentResultDto>
            {
                IsSuccess = false,
                Data = MapToResultDto(history, service, host),
                Message = $"Deployment exception: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<List<DeploymentHistoryDto>>> GetDeploymentHistoryAsync(Guid serviceId, int? take = 20, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

        var service = await uow.MonitoredServiceRepository.FindOneAsync(x => x.Id == serviceId);
        if (service == null)
        {
            return new BaseResponse<List<DeploymentHistoryDto>>
            {
                IsSuccess = false,
                Message = $"Service {serviceId} not found"
            };
        }

        var histories = await uow.DeploymentHistoryRepository.FindWithPaginationAsync(
            x => x.ServiceId == serviceId,
            skip: 0,
            take: take ?? 20,
            orderBy: q => q.OrderByDescending(x => x.StartedAt));

        return new BaseResponse<List<DeploymentHistoryDto>>
        {
            IsSuccess = true,
            Data = histories.Select(h => MapToHistoryDto(h, service.Name)).ToList(),
            Message = $"{histories.Count()} deployment record(s)"
        };
    }

    public async Task<BaseResponse<List<DeploymentHistoryDto>>> GetDeploymentHistoryByNameAsync(string serviceName, int? take = 20, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

        var service = await uow.MonitoredServiceRepository.FindOneAsync(x => x.Name == serviceName);
        if (service == null)
        {
            return new BaseResponse<List<DeploymentHistoryDto>>
            {
                IsSuccess = false,
                Message = $"Service '{serviceName}' not found"
            };
        }

        var histories = await uow.DeploymentHistoryRepository.FindWithPaginationAsync(
            x => x.ServiceId == service.Id,
            skip: 0,
            take: take ?? 20,
            orderBy: q => q.OrderByDescending(x => x.StartedAt));

        return new BaseResponse<List<DeploymentHistoryDto>>
        {
            IsSuccess = true,
            Data = histories.Select(h => MapToHistoryDto(h, service.Name)).ToList(),
            Message = $"{histories.Count()} deployment record(s)"
        };
    }

    // ?????????????????????????????? PRIVATE ??????????????????????????????

    private static string? BuildDeployCommand(MonitoredService service)
    {
        // If custom deploy command is set, use it directly
        if (!string.IsNullOrWhiteSpace(service.DeployCommand))
            return service.DeployCommand;

        // If compose file is configured, build docker compose command
        if (!string.IsNullOrWhiteSpace(service.ComposeFilePath))
        {
            var composeFile = service.ComposeFilePath;
            return $"docker compose -f {composeFile} down --rmi all && docker compose -f {composeFile} up -d";
        }

        // If container name + image are configured, use docker run
        if (!string.IsNullOrWhiteSpace(service.ContainerName) && !string.IsNullOrWhiteSpace(service.ImageName))
        {
            var port = service.Port.HasValue ? $"-p {service.Port}:{service.Port} " : "";
            return $"docker stop {service.ContainerName} 2>/dev/null; docker rm {service.ContainerName} 2>/dev/null; docker pull {service.ImageName} && docker run -d --name {service.ContainerName} {port}{service.ImageName}";
        }

        return null;
    }

    private static async Task<CommandResult> ExecuteDeployCommandAsync(MonitoredHost host, string command, string? workingDirectory)
    {
        // Determine if this is a local or remote deployment
        var isLocal = IsLocalHost(host.IpAddress);

        if (isLocal)
        {
            return await ExecuteLocalCommandAsync(command, workingDirectory);
        }

        // Remote: wrap with SSH
        return await ExecuteSshCommandAsync(host, command);
    }

    private static bool IsLocalHost(string ip)
    {
        return ip is "127.0.0.1" or "localhost" or "0.0.0.0" or "::1";
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

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

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

    private static async Task<CommandResult> ExecuteSshCommandAsync(MonitoredHost host, string command)
    {
        var sshPort = host.SshPort ?? 22;
        var sshUser = host.SshUsername ?? "root";

        // Build SSH command
        string sshCommand;
        if (!string.IsNullOrWhiteSpace(host.SshPrivateKeyPath))
        {
            sshCommand = $"ssh -o StrictHostKeyChecking=no -o ConnectTimeout=30 -i {host.SshPrivateKeyPath} -p {sshPort} {sshUser}@{host.IpAddress} '{command.Replace("'", "'\\''")}'";
        }
        else if (!string.IsNullOrWhiteSpace(host.SshPassword))
        {
            sshCommand = $"sshpass -p '{host.SshPassword.Replace("'", "'\\''")}' ssh -o StrictHostKeyChecking=no -o ConnectTimeout=30 -p {sshPort} {sshUser}@{host.IpAddress} '{command.Replace("'", "'\\''")}'";
        }
        else
        {
            // Fallback: assume SSH key is already configured in agent
            sshCommand = $"ssh -o StrictHostKeyChecking=no -o ConnectTimeout=30 -p {sshPort} {sshUser}@{host.IpAddress} '{command.Replace("'", "'\\''")}'";
        }

        return await ExecuteLocalCommandAsync(sshCommand, null);
    }

    private static DeploymentResultDto MapToResultDto(DeploymentHistory history, MonitoredService service, MonitoredHost host) => new()
    {
        DeploymentId = history.Id,
        ServiceId = service.Id,
        ServiceName = service.Name,
        HostId = host.Id,
        HostName = host.Name,
        HostIp = host.IpAddress,
        Status = history.Status.ToString(),
        Output = history.Output,
        ErrorMessage = history.ErrorMessage,
        StartedAt = history.StartedAt,
        FinishedAt = history.FinishedAt,
        DurationMs = history.DurationMs
    };

    private static DeploymentHistoryDto MapToHistoryDto(DeploymentHistory history, string serviceName) => new()
    {
        Id = history.Id,
        ServiceId = history.ServiceId,
        ServiceName = serviceName,
        Status = history.Status.ToString(),
        Version = history.Version,
        TriggeredBy = history.TriggeredBy,
        Output = history.Output,
        ErrorMessage = history.ErrorMessage,
        StartedAt = history.StartedAt,
        FinishedAt = history.FinishedAt,
        DurationMs = history.DurationMs
    };

    private sealed class CommandResult
    {
        public bool Success { get; set; }
        public string? Output { get; set; }
        public string? Error { get; set; }
    }
}
