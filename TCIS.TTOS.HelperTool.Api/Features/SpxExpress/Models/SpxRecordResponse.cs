using Newtonsoft.Json;

namespace TCIS.TTOS.HelperTool.API.Features.SpxExpress.Models;

public class SpxRecordResponse
{
    [JsonProperty("retcode")]
    public int Retcode { get; init; }

    [JsonProperty("data")]
    public SpxTrackingData? Data { get; init; }

    [JsonProperty("message")]
    public string? Message { get; init; }

    [JsonProperty("detail")]
    public string? Detail { get; init; }

    [JsonProperty("debug")]
    public string? Debug { get; init; }
}

public sealed record SpxTrackingData
{
    [JsonProperty("fulfillment_info")]
    public FulfillmentInfo? FulfillmentInfo { get; init; }

    [JsonProperty("sls_tracking_info")]
    public SlsTrackingInfo? SlsTrackingInfo { get; init; }

    [JsonProperty("is_instant_order")]
    public bool IsInstantOrder { get; init; }

    [JsonProperty("is_shopee_market_order")]
    public bool IsShopeeMarketOrder { get; init; }
}

public sealed record FulfillmentInfo
{
    [JsonProperty("deliver_type")]
    public int DeliverType { get; init; }
}

public sealed record SlsTrackingInfo
{
    [JsonProperty("sls_tn")]
    public string? SlsTrackingNumber { get; init; }

    [JsonProperty("client_order_id")]
    public string? ClientOrderId { get; init; }

    [JsonProperty("receiver_name")]
    public string? ReceiverName { get; init; }

    [JsonProperty("receiver_type_name")]
    public string? ReceiverTypeName { get; init; }

    [JsonProperty("records")]
    public List<SpxTrackingRecord>? Records { get; init; }
}

public sealed record SpxTrackingRecord
{
    [JsonProperty("tracking_code")]
    public string? TrackingCode { get; init; }

    [JsonProperty("tracking_name")]
    public string? TrackingName { get; init; }

    [JsonProperty("description")]
    public string? Description { get; init; }

    [JsonProperty("display_flag")]
    public int DisplayFlag { get; init; }

    [JsonProperty("actual_time")]
    public long ActualTime { get; init; }

    [JsonProperty("reason_code")]
    public string? ReasonCode { get; init; }

    [JsonProperty("reason_desc")]
    public string? ReasonDescription { get; init; }

    [JsonProperty("epod")]
    public string? Epod { get; init; }

    [JsonProperty("current_location")]
    public SpxLocation? CurrentLocation { get; init; }

    [JsonProperty("next_location")]
    public SpxLocation? NextLocation { get; init; }

    [JsonProperty("display_flag_v2")]
    public int DisplayFlagV2 { get; init; }

    [JsonProperty("buyer_description")]
    public string? BuyerDescription { get; init; }

    [JsonProperty("seller_description")]
    public string? SellerDescription { get; init; }

    [JsonProperty("milestone_code")]
    public int MilestoneCode { get; init; }

    [JsonProperty("milestone_name")]
    public string? MilestoneName { get; init; }
}

public sealed record SpxLocation
{
    [JsonProperty("location_name")]
    public string? LocationName { get; init; }

    [JsonProperty("location_type_name")]
    public string? LocationTypeName { get; init; }

    [JsonProperty("lng")]
    public string? LngRaw { get; init; }

    [JsonProperty("lat")]
    public string? LatRaw { get; init; }

    [JsonProperty("full_address")]
    public string? FullAddress { get; init; }
}
