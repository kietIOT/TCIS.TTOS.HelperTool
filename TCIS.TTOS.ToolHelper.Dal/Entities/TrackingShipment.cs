using System.ComponentModel.DataAnnotations;
using TCIS.TTOS.ToolHelper.Dal.Enums;

namespace TCIS.TTOS.ToolHelper.Dal.Entities
{
    public sealed class TrackingShipment
    {
        [Key]
        [MaxLength(64)]
        public string SpxTn { get; set; } = default!; // PK

        [MaxLength(32)]
        public string Carrier { get; set; } = "SPX_VN";

        [MaxLength(64)]
        public string? ClientOrderId { get; set; }

        public int? DeliverType { get; set; }

        public TrackingStatus Status { get; set; } = TrackingStatus.Preparing;

        public DateTimeOffset? LastEventTime { get; set; }
        [MaxLength(16)]
        public string? LastEventCode { get; set; }
        public string? LastMessage { get; set; }
        public string? LastMilestoneName { get; set; }

        // fingerprint/hash (sha256 hex string)
        [MaxLength(128)]
        public string? Fingerprint { get; set; }

        public string? CurrentLocationName { get; set; }
        public string? CurrentFullAddress { get; set; }
        public double? CurrentLat { get; set; }
        public double? CurrentLng { get; set; }

        public string? NextLocationName { get; set; }
        public string? NextFullAddress { get; set; }
        public double? NextLat { get; set; }
        public double? NextLng { get; set; }

        // jsonb
        public string? LastRawJson { get; set; } // hoặc JsonDocument/JsonElement tùy style
        public int RawVersion { get; set; } = 1;

        public DateTimeOffset NextPollAt { get; set; } = DateTimeOffset.UtcNow;
        public int PollIntervalSec { get; set; } = 900;
        public int PollFailCount { get; set; } = 0;
        public DateTimeOffset? LastPolledAt { get; set; }

        public bool IsTerminal { get; set; } = false;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Nav
        public ICollection<TrackingSubscription> Subscriptions { get; set; } = new List<TrackingSubscription>();
        public ICollection<TrackingEvent> Events { get; set; } = new List<TrackingEvent>();
        public ICollection<NotificationOutbox> OutboxMessages { get; set; } = new List<NotificationOutbox>();
        public ShipmentPollLock? PollLock { get; set; }
    }
}
