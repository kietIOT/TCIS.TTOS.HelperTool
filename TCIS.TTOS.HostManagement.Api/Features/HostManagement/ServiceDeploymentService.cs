using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using TCIS.TTOS.HostManagement.API.Common.Models;
using TCIS.TTOS.ToolHelper.Dal.Entities;
using TCIS.TTOS.ToolHelper.Dal.Enums;
using TCIS.TTOS.ToolHelper.DAL.UnitOfWork;

namespace TCIS.TTOS.HostManagement.API.Features.HostManagement;

public class ServiceDeploymentService(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<ServiceDeploymentService> logger) : IServiceDeploymentService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

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

        logger.LogInformation("[DEPLOY] Starting deployment for service '{Service}' on host '{Host}' ({Ip}:{Port})",
            service.Name, host.Name, host.IpAddress, host.AgentPort);

        var sw = Stopwatch.StartNew();

        try
        {
            // 4. Call HelperTool.API on the target host via HTTP
            var agentResult = await CallHelperToolAgentAsync(host, request, ct);

            sw.Stop();

            // 5. Update history
            history.Output = agentResult.Output;
            history.ErrorMessage = agentResult.Error;
            history.Status = agentResult.Success ? DeploymentStatus.Success : DeploymentStatus.Failed;
            history.FinishedAt = DateTimeOffset.UtcNow;
            history.DurationMs = agentResult.DurationMs > 0 ? agentResult.DurationMs : sw.ElapsedMilliseconds;

            await uow.DeploymentHistoryRepository.UpdateAsync(history);

            // 6. Update service deployment status
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
                IsSuccess = agentResult.Success,
                Data = MapToResultDto(history, service, host),
                Message = agentResult.Success
                    ? $"Service '{service.Name}' deployed successfully on host '{host.Name}'"
                    : $"Deployment failed for service '{service.Name}'"
            };
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            return await HandleDeployFailureAsync(uow, history, service, host, sw,
                $"Cannot connect to HelperTool agent at {host.IpAddress}:{host.AgentPort} — {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            sw.Stop();
            return await HandleDeployFailureAsync(uow, history, service, host, sw,
                $"Request to HelperTool agent timed out — {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return await HandleDeployFailureAsync(uow, history, service, host, sw,
                $"Deployment exception: {ex.Message}", ex);
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

    // ?????????? PRIVATE ??????????

    /// <summary>
    /// Calls POST http://{host.IpAddress}:{host.AgentPort}/api/deploy on the target HelperTool agent.
    /// </summary>
    private async Task<AgentDeployResult> CallHelperToolAgentAsync(MonitoredHost host, DeployByNameRequest request, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(10);

        var agentUrl = $"http://{host.IpAddress}:{host.AgentPort}/api/deploy";

        var payload = new
        {
            serviceName = request.ServiceName,
            version = request.Version,
            triggeredBy = request.TriggeredBy
        };

        logger.LogInformation("[DEPLOY] Calling agent at {Url} for service '{Service}'", agentUrl, request.ServiceName);

        var response = await client.PostAsJsonAsync(agentUrl, payload, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (response.IsSuccessStatusCode)
        {
            var result = JsonSerializer.Deserialize<AgentDeployResult>(body, JsonOptions);
            return result ?? new AgentDeployResult { Success = false, Error = "Empty response from agent" };
        }

        // Agent returned error
        var errorResult = JsonSerializer.Deserialize<AgentDeployResult>(body, JsonOptions);
        return errorResult ?? new AgentDeployResult
        {
            Success = false,
            Error = $"Agent returned {response.StatusCode}: {body}"
        };
    }

    private async Task<BaseResponse<DeploymentResultDto>> HandleDeployFailureAsync(
        IToolHelperUnitOfWork uow, DeploymentHistory history, MonitoredService service,
        MonitoredHost host, Stopwatch sw, string errorMessage, Exception ex)
    {
        history.Status = DeploymentStatus.Failed;
        history.ErrorMessage = errorMessage;
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
            Message = errorMessage
        };
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

    /// <summary>
    /// Maps the JSON response from HelperTool.API POST /api/deploy
    /// </summary>
    private sealed class AgentDeployResult
    {
        public string ServiceName { get; set; } = default!;
        public bool Success { get; set; }
        public string? Output { get; set; }
        public string? Error { get; set; }
        public long DurationMs { get; set; }
    }
}
