using TCIS.TTOS.HostManagement.API.Common.Models;

namespace TCIS.TTOS.HostManagement.API.Features.HostManagement;

public interface IMonitoredServiceService
{
    Task<BaseResponse<ServiceDto>> AddServiceAsync(Guid hostId, CreateServiceRequest request, CancellationToken ct = default);
    Task<BaseResponse<ServiceDto>> GetServiceByIdAsync(Guid hostId, Guid serviceId, CancellationToken ct = default);
    Task<BaseResponse<List<ServiceDto>>> GetServicesByHostAsync(Guid hostId, CancellationToken ct = default);
    Task<BaseResponse<ServiceDto>> UpdateServiceAsync(Guid hostId, Guid serviceId, UpdateServiceRequest request, CancellationToken ct = default);
    Task<BaseResponse<object>> DeleteServiceAsync(Guid hostId, Guid serviceId, CancellationToken ct = default);
}
