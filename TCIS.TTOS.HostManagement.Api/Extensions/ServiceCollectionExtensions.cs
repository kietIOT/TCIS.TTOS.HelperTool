using TCIS.TTOS.HostManagement.API.Features.HostManagement;
using Microsoft.EntityFrameworkCore;
using TCIS.TTOS.ToolHelper.DAL;
using TCIS.TTOS.ToolHelper.DAL.UnitOfWork;

namespace TCIS.TTOS.HostManagement.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextFactory<ToolHelperDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("ToolHelperDb")));
        services.AddScoped<IToolHelperUnitOfWork, ToolHelperUnitOfWork>();
        return services;
    }

    public static IServiceCollection AddHostManagementFeature(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddScoped<IHostService, HostService>();
        services.AddScoped<IMonitoredServiceService, MonitoredServiceService>();
        services.AddScoped<IServiceDeploymentService, ServiceDeploymentService>();
        return services;
    }
}
