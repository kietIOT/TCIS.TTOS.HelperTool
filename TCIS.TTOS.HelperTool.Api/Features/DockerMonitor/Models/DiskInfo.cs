namespace TCIS.TTOS.HelperTool.API.Features.DockerMonitor.Models;

public sealed class DiskInfo
{
    public string Filesystem { get; init; } = default!;
    public string Size { get; init; } = default!;
    public string Used { get; init; } = default!;
    public string Available { get; init; } = default!;
    public double UsePercent { get; init; }
    public string MountPoint { get; init; } = default!;
}
