using System.ComponentModel.DataAnnotations;

namespace TCIS.TTOS.ToolHelper.Dal.Entities
{
    public sealed class TrackingEvent
    {
        public long Id { get; set; }

        [MaxLength(64)]
        public string SpxTn { get; set; } = default!;

        public DateTimeOffset EventTime { get; set; } // actual_time
        [MaxLength(16)]
        public string TrackingCode { get; set; } = default!;

        public int? MilestoneCode { get; set; }
        public string? MilestoneName { get; set; }

        public string? BuyerMessage { get; set; }
        public string? SellerMessage { get; set; }
        public string? Description { get; set; }

        public string? CurrentLocation { get; set; } // jsonb
        public string? NextLocation { get; set; }    // jsonb
        public string? RawRecord { get; set; }       // jsonb

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Nav
        public TrackingShipment Shipment { get; set; } = default!;
    }
}
