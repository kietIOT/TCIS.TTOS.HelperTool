using TCIS.TTOS.HelperTool.API.Features.Deploy;

namespace TCIS.TTOS.HelperTool.API.Infrastructure.Services.Interface
{
    public interface IDeployService
    {
        Task<DeployResponse> DeployAsync(string jobName, string? environment);
        List<string> GetAvailableJobs();
    }
}
