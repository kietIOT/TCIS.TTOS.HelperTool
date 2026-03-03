using System.ComponentModel.DataAnnotations;

namespace TCIS.TTOS.ToolHelper.Dal.Entities
{
    public sealed class ShipmentPollLock
    {
        [Key]
        [MaxLength(64)]
        public string SpxTn { get; set; } = default!; // PK + FK

        [MaxLength(128)]
        public string LockedBy { get; set; } = default!;

        public DateTimeOffset LockedUntil { get; set; }

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Nav
        public TrackingShipment Shipment { get; set; } = default!;
    }
}
