using TCIS.TTOS.HelperTool.API.Infrastructure.Services.Models;

namespace TCIS.TTOS.HelperTool.API.Infrastructure.Services.Interface
{
    public interface IDockerMonitorService
    {
        Task<BaseResponse<object>> GetContainerStatsAsync(CancellationToken ct = default);
        Task<BaseResponse<object>> GetDiskUsageAsync(CancellationToken ct = default);
        Task<BaseResponse<object>> GetFullReportAsync(CancellationToken ct = default);
        Task MonitorAndAlertAsync(CancellationToken ct = default);
    }
}
