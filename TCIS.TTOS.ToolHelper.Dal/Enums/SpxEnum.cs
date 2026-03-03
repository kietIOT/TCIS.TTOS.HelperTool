namespace TCIS.TTOS.ToolHelper.Dal.Enums
{
    public enum TrackingStatus
    {
        Preparing,
        InTransit,
        OutForDelivery,
        Delivered,
        Cancelled,
        Returned,
        Exception
    }

    public enum NotifyChannel
    {
        Email,
        Push,
        Webhook
    }

    public enum NotifyPref
    {
        DeliveredOnly,
        StatusChange,
        EveryEvent
    }

    public enum OutboxStatus
    {
        Pending,
        Processing,
        Sent,
        Failed,
        Dead
    }
}
