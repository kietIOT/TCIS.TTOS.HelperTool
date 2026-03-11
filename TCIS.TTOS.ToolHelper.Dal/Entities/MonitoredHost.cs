using System.ComponentModel.DataAnnotations;
using TCIS.TTOS.ToolHelper.Dal.Enums;

namespace TCIS.TTOS.ToolHelper.Dal.Entities
{
    public sealed class MonitoredHost
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [MaxLength(128)]
        public string Name { get; set; } = default!;

        [MaxLength(256)]
        public string? Description { get; set; }

        [MaxLength(64)]
        public string IpAddress { get; set; } = default!;

        public int AgentPort { get; set; } = 5155;

        public int? SshPort { get; set; } = 22;

        [MaxLength(128)]
        public string? SshUsername { get; set; }

        [MaxLength(512)]
        public string? SshPrivateKeyPath { get; set; }

        [MaxLength(256)]
        public string? SshPassword { get; set; }

        [MaxLength(64)]
        public string? Os { get; set; }

        public HostStatus Status { get; set; } = HostStatus.Unknown;

        public DateTimeOffset? LastCheckedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Nav
        public ICollection<MonitoredService> Services { get; set; } = new List<MonitoredService>();
    }
}


