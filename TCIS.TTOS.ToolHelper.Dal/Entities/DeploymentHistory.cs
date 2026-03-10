using System.ComponentModel.DataAnnotations;
using TCIS.TTOS.ToolHelper.Dal.Enums;

namespace TCIS.TTOS.ToolHelper.Dal.Entities
{
    public sealed class DeploymentHistory
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ServiceId { get; set; }

        public DeploymentStatus Status { get; set; } = DeploymentStatus.Pending;

        [MaxLength(128)]
        public string? Version { get; set; }

        [MaxLength(128)]
        public string? TriggeredBy { get; set; }

        public string? Output { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? FinishedAt { get; set; }

        public long? DurationMs { get; set; }

        // Nav
        public MonitoredService Service { get; set; } = default!;
    }
}
