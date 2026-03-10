using TCIS.TTOS.HostManagement.API.Common.Models;

namespace TCIS.TTOS.HostManagement.API.Features.HostManagement;

public interface IHostService
{
    Task<BaseResponse<HostDetailDto>> CreateHostAsync(CreateHostRequest request, CancellationToken ct = default);
    Task<BaseResponse<HostDetailDto>> GetHostByIdAsync(Guid hostId, CancellationToken ct = default);
    Task<BaseResponse<List<HostDto>>> GetAllHostsAsync(bool? activeOnly = null, CancellationToken ct = default);
    Task<BaseResponse<HostDetailDto>> UpdateHostAsync(Guid hostId, UpdateHostRequest request, CancellationToken ct = default);
    Task<BaseResponse<object>> DeleteHostAsync(Guid hostId, CancellationToken ct = default);
    Task<BaseResponse<DashboardDto>> GetDashboardAsync(CancellationToken ct = default);
}
