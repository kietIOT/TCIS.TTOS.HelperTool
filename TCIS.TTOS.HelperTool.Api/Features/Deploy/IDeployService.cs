namespace TCIS.TTOS.HelperTool.API.Features.Deploy;

public interface IDeployService
{
    /// <summary>
    /// Deploy a service by name. Reads compose file path from DB, then executes docker compose locally.
    /// </summary>
    Task<DeployResultDto> DeployByServiceNameAsync(DeployByNameRequest request, CancellationToken ct = default);
}
