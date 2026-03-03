using System.ComponentModel.DataAnnotations;
using TCIS.TTOS.ToolHelper.Dal.Enums;

namespace TCIS.TTOS.ToolHelper.Dal.Entities
{
    public sealed class TrackingSubscription
    {
        public long Id { get; set; }

        public Guid UserId { get; set; }

        [MaxLength(64)]
        public string SpxTn { get; set; } = default!;

        public NotifyPref Pref { get; set; } = NotifyPref.StatusChange;
        public NotifyChannel Channel { get; set; } = NotifyChannel.Push;

        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Nav
        public TrackingShipment Shipment { get; set; } = default!;
    }
}
