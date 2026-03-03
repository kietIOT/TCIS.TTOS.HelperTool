namespace TCIS.TTOS.HelperTool.API.Features.DockerMonitor.Models;

public sealed class ContainerStat
{
    public string Name { get; init; } = default!;
    public double CpuPercent { get; init; }
    public string MemUsage { get; init; } = default!;
    public double MemPercent { get; init; }
    public string NetIO { get; init; } = default!;
    public string BlockIO { get; init; } = default!;
    public int Pids { get; init; }
}
