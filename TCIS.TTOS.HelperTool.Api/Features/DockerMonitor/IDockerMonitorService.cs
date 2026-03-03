using TCIS.TTOS.HelperTool.API.Common.Models;

namespace TCIS.TTOS.HelperTool.API.Features.DockerMonitor;

public interface IDockerMonitorService
{
    Task<BaseResponse<object>> GetContainerStatsAsync(CancellationToken ct = default);
    Task<BaseResponse<object>> GetDiskUsageAsync(CancellationToken ct = default);
    Task<BaseResponse<object>> GetFullReportAsync(CancellationToken ct = default);
    Task MonitorAndAlertAsync(CancellationToken ct = default);
}
