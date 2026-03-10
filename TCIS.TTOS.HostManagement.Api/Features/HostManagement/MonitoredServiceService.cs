using TCIS.TTOS.ToolHelper.Dal.Entities;
using TCIS.TTOS.HostManagement.API.Common.Models;
using TCIS.TTOS.ToolHelper.Dal.Entities;
using TCIS.TTOS.ToolHelper.Dal.Enums;
using TCIS.TTOS.ToolHelper.DAL.UnitOfWork;

namespace TCIS.TTOS.HostManagement.API.Features.HostManagement;

public class MonitoredServiceService(
    IServiceScopeFactory scopeFactory,
    ILogger<MonitoredServiceService> logger) : IMonitoredServiceService
{
    public async Task<BaseResponse<ServiceDto>> AddServiceAsync(Guid hostId, CreateServiceRequest request, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

        var host = await uow.MonitoredHostRepository.FindOneAsync(x => x.Id == hostId);
        if (host == null)
        {
            return new BaseResponse<ServiceDto>
            {
                IsSuccess = false,
                Message = $"Host {hostId} not found"
            };
        }

        var duplicate = await uow.MonitoredServiceRepository.FindOneAsync(
            x => x.HostId == hostId && x.Name == request.Name);
        if (duplicate != null)
        {
            return new BaseResponse<ServiceDto>
            {
                IsSuccess = false,
                Message = $"Service '{request.Name}' already exists on host '{host.Name}'"
            };
        }

        var service = new MonitoredService
        {
            HostId = hostId,
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            Port = request.Port,
            HealthCheckUrl = request.HealthCheckUrl,
            ImageName = request.ImageName,
            Version = request.Version,
            ComposeFilePath = request.ComposeFilePath,
            WorkingDirectory = request.WorkingDirectory,
            DockerfilePath = request.DockerfilePath,
            ContainerName = request.ContainerName,
            DeployCommand = request.DeployCommand,
            StopCommand = request.StopCommand,
            RestartCommand = request.RestartCommand,
            Status = ServiceStatus.Unknown,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await uow.MonitoredServiceRepository.AddAsync(service);
        await uow.CompleteAsync();

        logger.LogInformation("[SERVICE] Added service: {Name} on host {Host}", service.Name, host.Name);

        return new BaseResponse<ServiceDto>
        {
            IsSuccess = true,
            Data = MapToServiceDto(service, host.Name),
            Message = $"Service '{service.Name}' added to host '{host.Name}'"
        };
    }

    public async Task<BaseResponse<ServiceDto>> GetServiceByIdAsync(Guid hostId, Guid serviceId, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

        var host = await uow.MonitoredHostRepository.FindOneAsync(x => x.Id == hostId);
        if (host == null)
        {
            return new BaseResponse<ServiceDto>
            {
                IsSuccess = false,
                Message = $"Host {hostId} not found"
            };
        }

        var service = await uow.MonitoredServiceRepository.FindOneAsync(
            x => x.Id == serviceId && x.HostId == hostId);
        if (service == null)
        {
            return new BaseResponse<ServiceDto>
            {
                IsSuccess = false,
                Message = $"Service {serviceId} not found on host '{host.Name}'"
            };
        }

        return new BaseResponse<ServiceDto>
        {
            IsSuccess = true,
            Data = MapToServiceDto(service, host.Name),
            Message = "Service detail"
        };
    }

    public async Task<BaseResponse<List<ServiceDto>>> GetServicesByHostAsync(Guid hostId, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

        var host = await uow.MonitoredHostRepository.FindOneAsync(x => x.Id == hostId);
        if (host == null)
        {
            return new BaseResponse<List<ServiceDto>>
            {
                IsSuccess = false,
                Message = $"Host {hostId} not found"
            };
        }

        var services = await uow.MonitoredServiceRepository.FindAsync(x => x.HostId == hostId);

        return new BaseResponse<List<ServiceDto>>
        {
            IsSuccess = true,
            Data = services.Select(s => MapToServiceDto(s, host.Name)).ToList(),
            Message = $"{services.Count()} service(s) on host '{host.Name}'"
        };
    }

    public async Task<BaseResponse<ServiceDto>> UpdateServiceAsync(Guid hostId, Guid serviceId, UpdateServiceRequest request, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

        var host = await uow.MonitoredHostRepository.FindOneAsync(x => x.Id == hostId);
        if (host == null)
        {
            return new BaseResponse<ServiceDto>
            {
                IsSuccess = false,
                Message = $"Host {hostId} not found"
            };
        }

        var service = await uow.MonitoredServiceRepository.FindOneAsync(
            x => x.Id == serviceId && x.HostId == hostId);
        if (service == null)
        {
            return new BaseResponse<ServiceDto>
            {
                IsSuccess = false,
                Message = $"Service {serviceId} not found on host '{host.Name}'"
            };
        }

        if (request.Name != null)
        {
            var duplicate = await uow.MonitoredServiceRepository.FindOneAsync(
                x => x.HostId == hostId && x.Name == request.Name && x.Id != serviceId);
            if (duplicate != null)
            {
                return new BaseResponse<ServiceDto>
                {
                    IsSuccess = false,
                    Message = $"Service name '{request.Name}' already exists on this host"
                };
            }
            service.Name = request.Name;
        }
        if (request.Description != null) service.Description = request.Description;
        if (request.Type.HasValue) service.Type = request.Type.Value;
        if (request.Port.HasValue) service.Port = request.Port.Value;
        if (request.HealthCheckUrl != null) service.HealthCheckUrl = request.HealthCheckUrl;
        if (request.ImageName != null) service.ImageName = request.ImageName;
        if (request.Version != null) service.Version = request.Version;
        if (request.ComposeFilePath != null) service.ComposeFilePath = request.ComposeFilePath;
        if (request.WorkingDirectory != null) service.WorkingDirectory = request.WorkingDirectory;
        if (request.DockerfilePath != null) service.DockerfilePath = request.DockerfilePath;
        if (request.ContainerName != null) service.ContainerName = request.ContainerName;
        if (request.DeployCommand != null) service.DeployCommand = request.DeployCommand;
        if (request.StopCommand != null) service.StopCommand = request.StopCommand;
        if (request.RestartCommand != null) service.RestartCommand = request.RestartCommand;
        if (request.IsActive.HasValue) service.IsActive = request.IsActive.Value;
        service.UpdatedAt = DateTimeOffset.UtcNow;

        await uow.MonitoredServiceRepository.UpdateAsync(service);
        await uow.CompleteAsync();

        logger.LogInformation("[SERVICE] Updated service: {Name} on host {Host}", service.Name, host.Name);

        return new BaseResponse<ServiceDto>
        {
            IsSuccess = true,
            Data = MapToServiceDto(service, host.Name),
            Message = $"Service '{service.Name}' updated"
        };
    }

    public async Task<BaseResponse<object>> DeleteServiceAsync(Guid hostId, Guid serviceId, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

        var host = await uow.MonitoredHostRepository.FindOneAsync(x => x.Id == hostId);
        if (host == null)
        {
            return new BaseResponse<object>
            {
                IsSuccess = false,
                Message = $"Host {hostId} not found"
            };
        }

        var service = await uow.MonitoredServiceRepository.FindOneAsync(
            x => x.Id == serviceId && x.HostId == hostId);
        if (service == null)
        {
            return new BaseResponse<object>
            {
                IsSuccess = false,
                Message = $"Service {serviceId} not found on host '{host.Name}'"
            };
        }

        await uow.MonitoredServiceRepository.DeleteAsync(service);
        await uow.CompleteAsync();

        logger.LogInformation("[SERVICE] Deleted service: {Name} from host {Host}", service.Name, host.Name);

        return new BaseResponse<object>
        {
            IsSuccess = true,
            Message = $"Service '{service.Name}' deleted from host '{host.Name}'"
        };
    }

    // ?????????????????????????????? MAPPING ??????????????????????????????

    private static ServiceDto MapToServiceDto(MonitoredService service, string hostName) => new()
    {
        Id = service.Id,
        HostId = service.HostId,
        HostName = hostName,
        Name = service.Name,
        Description = service.Description,
        Type = service.Type.ToString(),
        Status = service.Status.ToString(),
        Port = service.Port,
        HealthCheckUrl = service.HealthCheckUrl,
        ImageName = service.ImageName,
        Version = service.Version,
        ComposeFilePath = service.ComposeFilePath,
        WorkingDirectory = service.WorkingDirectory,
        DockerfilePath = service.DockerfilePath,
        ContainerName = service.ContainerName,
        DeployCommand = service.DeployCommand,
        StopCommand = service.StopCommand,
        RestartCommand = service.RestartCommand,
        LastDeploymentStatus = service.LastDeploymentStatus?.ToString(),
        LastDeployedAt = service.LastDeployedAt,
        LastCheckedAt = service.LastCheckedAt,
        StartedAt = service.StartedAt,
        IsActive = service.IsActive,
        CreatedAt = service.CreatedAt,
        UpdatedAt = service.UpdatedAt
    };
}
