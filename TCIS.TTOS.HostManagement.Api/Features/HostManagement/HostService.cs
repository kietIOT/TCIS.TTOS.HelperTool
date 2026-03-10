using TCIS.TTOS.ToolHelper.Dal.Entities;
using TCIS.TTOS.HostManagement.API.Common.Models;
using TCIS.TTOS.ToolHelper.Dal.Entities;
using TCIS.TTOS.ToolHelper.Dal.Enums;
using TCIS.TTOS.ToolHelper.DAL.UnitOfWork;

namespace TCIS.TTOS.HostManagement.API.Features.HostManagement;

public class HostService(
    IServiceScopeFactory scopeFactory,
    ILogger<HostService> logger) : IHostService
{
    public async Task<BaseResponse<HostDetailDto>> CreateHostAsync(CreateHostRequest request, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

        var existing = await uow.MonitoredHostRepository.FindOneAsync(x => x.IpAddress == request.IpAddress);
        if (existing != null)
        {
            return new BaseResponse<HostDetailDto>
            {
                IsSuccess = false,
                Message = $"Host with IP '{request.IpAddress}' already exists"
            };
        }

        var host = new MonitoredHost
        {
            Name = request.Name,
            Description = request.Description,
            IpAddress = request.IpAddress,
            SshPort = request.SshPort ?? 22,
            SshUsername = request.SshUsername,
            SshPrivateKeyPath = request.SshPrivateKeyPath,
            SshPassword = request.SshPassword,
            Os = request.Os,
            Status = HostStatus.Unknown,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await uow.MonitoredHostRepository.AddAsync(host);
        await uow.CompleteAsync();

        logger.LogInformation("[HOST] Created host: {Name} ({Ip})", host.Name, host.IpAddress);

        return new BaseResponse<HostDetailDto>
        {
            IsSuccess = true,
            Data = MapToDetailDto(host),
            Message = $"Host '{host.Name}' created"
        };
    }

    public async Task<BaseResponse<HostDetailDto>> GetHostByIdAsync(Guid hostId, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

        var host = await uow.MonitoredHostRepository.FindOneAsync(x => x.Id == hostId);
        if (host == null)
        {
            return new BaseResponse<HostDetailDto>
            {
                IsSuccess = false,
                Message = $"Host {hostId} not found"
            };
        }

        var services = await uow.MonitoredServiceRepository.FindAsync(x => x.HostId == hostId);
        host.Services = services.ToList();

        return new BaseResponse<HostDetailDto>
        {
            IsSuccess = true,
            Data = MapToDetailDto(host),
            Message = "Host detail"
        };
    }

    public async Task<BaseResponse<List<HostDto>>> GetAllHostsAsync(bool? activeOnly = null, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

        var hosts = activeOnly == true
            ? await uow.MonitoredHostRepository.FindAsync(x => x.IsActive)
            : await uow.MonitoredHostRepository.GetAllAsync();

        var allServices = await uow.MonitoredServiceRepository.GetAllAsync();
        var servicesByHost = allServices.GroupBy(s => s.HostId).ToDictionary(g => g.Key, g => g.ToList());

        var result = hosts.Select(h =>
        {
            var svc = servicesByHost.GetValueOrDefault(h.Id, []);
            return MapToDto(h, svc);
        }).ToList();

        return new BaseResponse<List<HostDto>>
        {
            IsSuccess = true,
            Data = result,
            Message = $"{result.Count} host(s)"
        };
    }

    public async Task<BaseResponse<HostDetailDto>> UpdateHostAsync(Guid hostId, UpdateHostRequest request, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

        var host = await uow.MonitoredHostRepository.FindOneAsync(x => x.Id == hostId);
        if (host == null)
        {
            return new BaseResponse<HostDetailDto>
            {
                IsSuccess = false,
                Message = $"Host {hostId} not found"
            };
        }

        if (request.Name != null) host.Name = request.Name;
        if (request.Description != null) host.Description = request.Description;
        if (request.IpAddress != null)
        {
            var duplicate = await uow.MonitoredHostRepository.FindOneAsync(
                x => x.IpAddress == request.IpAddress && x.Id != hostId);
            if (duplicate != null)
            {
                return new BaseResponse<HostDetailDto>
                {
                    IsSuccess = false,
                    Message = $"IP '{request.IpAddress}' is already used by host '{duplicate.Name}'"
                };
            }
            host.IpAddress = request.IpAddress;
        }
        if (request.SshPort.HasValue) host.SshPort = request.SshPort.Value;
        if (request.Os != null) host.Os = request.Os;
        if (request.SshUsername != null) host.SshUsername = request.SshUsername;
        if (request.SshPrivateKeyPath != null) host.SshPrivateKeyPath = request.SshPrivateKeyPath;
        if (request.SshPassword != null) host.SshPassword = request.SshPassword;
        if (request.IsActive.HasValue) host.IsActive = request.IsActive.Value;
        host.UpdatedAt = DateTimeOffset.UtcNow;

        await uow.MonitoredHostRepository.UpdateAsync(host);
        await uow.CompleteAsync();

        var services = await uow.MonitoredServiceRepository.FindAsync(x => x.HostId == hostId);
        host.Services = services.ToList();

        logger.LogInformation("[HOST] Updated host: {Name} ({Ip})", host.Name, host.IpAddress);

        return new BaseResponse<HostDetailDto>
        {
            IsSuccess = true,
            Data = MapToDetailDto(host),
            Message = $"Host '{host.Name}' updated"
        };
    }

    public async Task<BaseResponse<object>> DeleteHostAsync(Guid hostId, CancellationToken ct = default)
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

        // Cascade delete will remove services
        await uow.MonitoredHostRepository.DeleteAsync(host);
        await uow.CompleteAsync();

        logger.LogInformation("[HOST] Deleted host: {Name} ({Ip})", host.Name, host.IpAddress);

        return new BaseResponse<object>
        {
            IsSuccess = true,
            Message = $"Host '{host.Name}' and its services deleted"
        };
    }

    public async Task<BaseResponse<DashboardDto>> GetDashboardAsync(CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IToolHelperUnitOfWork>();

        var hosts = (await uow.MonitoredHostRepository.FindAsync(x => x.IsActive)).ToList();
        var allServices = (await uow.MonitoredServiceRepository.GetAllAsync()).ToList();
        var servicesByHost = allServices.GroupBy(s => s.HostId).ToDictionary(g => g.Key, g => g.ToList());

        var hostDtos = hosts.Select(h =>
        {
            var svc = servicesByHost.GetValueOrDefault(h.Id, []);
            return MapToDto(h, svc);
        }).ToList();

        return new BaseResponse<DashboardDto>
        {
            IsSuccess = true,
            Data = new DashboardDto
            {
                TotalHosts = hosts.Count,
                OnlineHosts = hosts.Count(h => h.Status == HostStatus.Online),
                OfflineHosts = hosts.Count(h => h.Status == HostStatus.Offline),
                TotalServices = allServices.Count,
                RunningServices = allServices.Count(s => s.Status == ServiceStatus.Running),
                StoppedServices = allServices.Count(s => s.Status == ServiceStatus.Stopped),
                ErrorServices = allServices.Count(s => s.Status == ServiceStatus.Error),
                Hosts = hostDtos
            },
            Message = "Dashboard"
        };
    }

    // ?????????????????????????????? MAPPING ??????????????????????????????

    private static HostDto MapToDto(MonitoredHost host, List<MonitoredService> services) => new()
    {
        Id = host.Id,
        Name = host.Name,
        Description = host.Description,
        IpAddress = host.IpAddress,
        SshPort = host.SshPort,
        SshUsername = host.SshUsername,
        Os = host.Os,
        Status = host.Status.ToString(),
        LastCheckedAt = host.LastCheckedAt,
        IsActive = host.IsActive,
        ServiceCount = services.Count,
        RunningServiceCount = services.Count(s => s.Status == ServiceStatus.Running),
        CreatedAt = host.CreatedAt,
        UpdatedAt = host.UpdatedAt
    };

    private static HostDetailDto MapToDetailDto(MonitoredHost host)
    {
        var services = host.Services?.ToList() ?? [];
        return new HostDetailDto
        {
            Id = host.Id,
            Name = host.Name,
            Description = host.Description,
            IpAddress = host.IpAddress,
            SshPort = host.SshPort,
            SshUsername = host.SshUsername,
            Os = host.Os,
            Status = host.Status.ToString(),
            LastCheckedAt = host.LastCheckedAt,
            IsActive = host.IsActive,
            ServiceCount = services.Count,
            RunningServiceCount = services.Count(s => s.Status == ServiceStatus.Running),
            CreatedAt = host.CreatedAt,
            UpdatedAt = host.UpdatedAt,
            Services = services.Select(s => MapToServiceDto(s, host.Name)).ToList()
        };
    }

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
