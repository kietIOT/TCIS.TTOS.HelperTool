using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using TCIS.TTOS.ToolHelper.Dal.Enums;

namespace TCIS.TTOS.ToolHelper.Dal.Entities
{
    public sealed class MonitoredService
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid HostId { get; set; }

        [MaxLength(128)]
        public string Name { get; set; } = default!;

        [MaxLength(256)]
        public string? Description { get; set; }

        public ServiceType Type { get; set; } = ServiceType.DockerContainer;

        public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;

        public int? Port { get; set; }

        [MaxLength(512)]
        public string? HealthCheckUrl { get; set; }

        [MaxLength(256)]
        public string? ImageName { get; set; }

        [MaxLength(128)]
        public string? Version { get; set; }

        // ?? Deployment configuration ??

        [MaxLength(512)]
        public string? ComposeFilePath { get; set; }

        [MaxLength(512)]
        public string? WorkingDirectory { get; set; }

        [MaxLength(256)]
        public string? DockerfilePath { get; set; }

        [MaxLength(128)]
        public string? ContainerName { get; set; }

        [MaxLength(1024)]
        public string? DeployCommand { get; set; }

        [MaxLength(1024)]
        public string? StopCommand { get; set; }

        [MaxLength(1024)]
        public string? RestartCommand { get; set; }

        public DeploymentStatus? LastDeploymentStatus { get; set; }

        public DateTimeOffset? LastDeployedAt { get; set; }

        // ?? Timestamps ??

        public DateTimeOffset? LastCheckedAt { get; set; }

        public DateTimeOffset? StartedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Nav
        public MonitoredHost Host { get; set; } = default!;
        public ICollection<DeploymentHistory> DeploymentHistories { get; set; } = new List<DeploymentHistory>();
    }
}
