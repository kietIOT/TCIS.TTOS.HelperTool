

using TCIS.TTOS.ToolHelper.Dal.Enums;

namespace TCIS.TTOS.HostManagement.API.Features.HostManagement;

// ???????????? Host ????????????

public sealed class CreateHostRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string IpAddress { get; set; } = default!;
    public int AgentPort { get; set; } = 5155;
    public int? SshPort { get; set; } = 22;
    public string? SshUsername { get; set; }
    public string? SshPrivateKeyPath { get; set; }
    public string? SshPassword { get; set; }
    public string? Os { get; set; }
}

public sealed class UpdateHostRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? IpAddress { get; set; }
    public int? AgentPort { get; set; }
    public int? SshPort { get; set; }
    public string? SshUsername { get; set; }
    public string? SshPrivateKeyPath { get; set; }
    public string? SshPassword { get; set; }
    public string? Os { get; set; }
    public bool? IsActive { get; set; }
}

public class HostDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string IpAddress { get; set; } = default!;
    public int AgentPort { get; set; }
    public int? SshPort { get; set; }
    public string? SshUsername { get; set; }
    public string? Os { get; set; }
    public string Status { get; set; } = default!;
    public DateTimeOffset? LastCheckedAt { get; set; }
    public bool IsActive { get; set; }
    public int ServiceCount { get; set; }
    public int RunningServiceCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class HostDetailDto : HostDto
{
    public List<ServiceDto> Services { get; set; } = [];
}

// ???????????? Service ????????????

public sealed class CreateServiceRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public ServiceType Type { get; set; } = ServiceType.DockerContainer;
    public int? Port { get; set; }
    public string? HealthCheckUrl { get; set; }
    public string? ImageName { get; set; }
    public string? Version { get; set; }
    public string? ComposeFilePath { get; set; }
    public string? WorkingDirectory { get; set; }
    public string? DockerfilePath { get; set; }
    public string? ContainerName { get; set; }
    public string? DeployCommand { get; set; }
    public string? StopCommand { get; set; }
    public string? RestartCommand { get; set; }
}

public sealed class UpdateServiceRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public ServiceType? Type { get; set; }
    public int? Port { get; set; }
    public string? HealthCheckUrl { get; set; }
    public string? ImageName { get; set; }
    public string? Version { get; set; }
    public string? ComposeFilePath { get; set; }
    public string? WorkingDirectory { get; set; }
    public string? DockerfilePath { get; set; }
    public string? ContainerName { get; set; }
    public string? DeployCommand { get; set; }
    public string? StopCommand { get; set; }
    public string? RestartCommand { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class ServiceDto
{
    public Guid Id { get; set; }
    public Guid HostId { get; set; }
    public string HostName { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Type { get; set; } = default!;
    public string Status { get; set; } = default!;
    public int? Port { get; set; }
    public string? HealthCheckUrl { get; set; }
    public string? ImageName { get; set; }
    public string? Version { get; set; }
    public string? ComposeFilePath { get; set; }
    public string? WorkingDirectory { get; set; }
    public string? DockerfilePath { get; set; }
    public string? ContainerName { get; set; }
    public string? DeployCommand { get; set; }
    public string? StopCommand { get; set; }
    public string? RestartCommand { get; set; }
    public string? LastDeploymentStatus { get; set; }
    public DateTimeOffset? LastDeployedAt { get; set; }
    public DateTimeOffset? LastCheckedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

// ???????????? Dashboard ????????????

public sealed class DashboardDto
{
    public int TotalHosts { get; set; }
    public int OnlineHosts { get; set; }
    public int OfflineHosts { get; set; }
    public int TotalServices { get; set; }
    public int RunningServices { get; set; }
    public int StoppedServices { get; set; }
    public int ErrorServices { get; set; }
    public List<HostDto> Hosts { get; set; } = [];
}

// ???????????? Deployment ????????????

public sealed class DeployByNameRequest
{
    public string ServiceName { get; set; } = default!;
    public string? Version { get; set; }
    public string? TriggeredBy { get; set; }
}

public sealed class DeploymentResultDto
{
    public Guid DeploymentId { get; set; }
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = default!;
    public Guid HostId { get; set; }
    public string HostName { get; set; } = default!;
    public string HostIp { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string? Output { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public long? DurationMs { get; set; }
}

public sealed class DeploymentHistoryDto
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string? Version { get; set; }
    public string? TriggeredBy { get; set; }
    public string? Output { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public long? DurationMs { get; set; }
}
