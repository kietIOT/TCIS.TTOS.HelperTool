namespace TCIS.TTOS.HelperTool.API.Features.Deploy;

public interface IDeployService
{
    Task<DeployResponse> DeployAsync(string jobName, string? environment);
    List<string> GetAvailableJobs();
}
