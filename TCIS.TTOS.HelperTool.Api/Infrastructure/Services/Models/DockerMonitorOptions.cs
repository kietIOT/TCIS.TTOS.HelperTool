namespace TCIS.TTOS.HelperTool.API.Infrastructure.Services.Models
{
    public sealed class DockerMonitorOptions
    {
        public int PollIntervalSeconds { get; set; } = 30;
        public double CpuSpikeThresholdPercent { get; set; } = 80.0;
        public double MemSpikeThresholdPercent { get; set; } = 80.0;
        public double DiskWarningThresholdPercent { get; set; } = 85.0;
        public double DiskCriticalThresholdPercent { get; set; } = 95.0;
        public int SpikeConsecutiveCount { get; set; } = 2;
    }
}
