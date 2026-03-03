using System.ComponentModel.DataAnnotations;
using TCIS.TTOS.ToolHelper.Dal.Enums;

namespace TCIS.TTOS.ToolHelper.Dal.Entities
{
    public sealed class NotificationOutbox
    {
        public long Id { get; set; }

        public Guid UserId { get; set; }

        [MaxLength(64)]
        public string SpxTn { get; set; } = default!;

        public NotifyChannel Channel { get; set; }
        public NotifyPref Pref { get; set; }

        [MaxLength(256)]
        public string EventKey { get; set; } = default!; // unique with user+channel

        public string Payload { get; set; } = default!;  // jsonb

        public OutboxStatus Status { get; set; } = OutboxStatus.Pending;

        public int RetryCount { get; set; } = 0;
        public DateTimeOffset NextRetryAt { get; set; } = DateTimeOffset.UtcNow;
        public string? LastError { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? SentAt { get; set; }

        // Nav
        public TrackingShipment Shipment { get; set; } = default!;
    }
}
