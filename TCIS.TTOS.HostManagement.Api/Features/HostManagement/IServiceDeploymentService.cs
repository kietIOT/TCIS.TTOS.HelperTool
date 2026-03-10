
using TCIS.TTOS.HostManagement.API.Common.Models;

namespace TCIS.TTOS.HostManagement.API.Features.HostManagement;

public interface IServiceDeploymentService
{
    /// <summary>
    /// Deploy a service by its name. Looks up the service in DB, finds the host, and executes deployment.
    /// </summary>
    Task<BaseResponse<DeploymentResultDto>> DeployByServiceNameAsync(DeployByNameRequest request, CancellationToken ct = default);

    /// <summary>
    /// Get deployment history for a specific service.
    /// </summary>
    Task<BaseResponse<List<DeploymentHistoryDto>>> GetDeploymentHistoryAsync(Guid serviceId, int? take = 20, CancellationToken ct = default);

    /// <summary>
    /// Get deployment history for a service by name.
    /// </summary>
    Task<BaseResponse<List<DeploymentHistoryDto>>> GetDeploymentHistoryByNameAsync(string serviceName, int? take = 20, CancellationToken ct = default);
}
