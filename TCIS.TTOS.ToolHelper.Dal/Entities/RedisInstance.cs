using System.ComponentModel.DataAnnotations;

namespace TCIS.TTOS.ToolHelper.Dal.Entities
{
    public sealed class RedisInstance
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid HostId { get; set; }

        [MaxLength(128)]
        public string Name { get; set; } = default!;

        [MaxLength(256)]
        public string? Description { get; set; }

        [MaxLength(256)]
        public string Host { get; set; } = "localhost";

        public int Port { get; set; } = 6379;

        [MaxLength(256)]
        public string? Password { get; set; }

        public int Database { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Nav
        public MonitoredHost MonitoredHost { get; set; } = default!;
    }
}
