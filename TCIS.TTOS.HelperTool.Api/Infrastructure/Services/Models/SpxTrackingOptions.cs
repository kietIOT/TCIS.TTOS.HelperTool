namespace TCIS.TTOS.HelperTool.API.Infrastructure.Services.Models
{
    public sealed class SpxTrackingOptions
    {
        public int PollIntervalSeconds { get; set; } = 60;
        public int TerminalPollIntervalSeconds { get; set; } = 0;
        public int MaxPollFailCount { get; set; } = 10;
    }
}
