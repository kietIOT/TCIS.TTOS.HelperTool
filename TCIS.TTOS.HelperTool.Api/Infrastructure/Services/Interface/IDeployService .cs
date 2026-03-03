using TCIS.TTOS.HelperTool.API.Infrastructure.Services.Models;

namespace TCIS.TTOS.HelperTool.API.Infrastructure.Services.Interface
{
    public interface IDeployService
    {
        Task<DeployResponse> DeployAsync(string jobName, string? environment);
        List<string> GetAvailableJobs();
    }
}
