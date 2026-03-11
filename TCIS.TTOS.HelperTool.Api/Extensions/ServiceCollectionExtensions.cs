using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TCIS.TTOS.HelperTool.API.Features.Deploy;
using TCIS.TTOS.HelperTool.API.Features.DockerMonitor;
using TCIS.TTOS.HelperTool.API.Features.SpxExpress;
using TCIS.TTOS.HelperTool.API.Features.SpxTracking;
using TCIS.TTOS.HelperTool.API.Features.YooseeCamera;
using TCIS.TTOS.ToolHelper.DAL;
using TCIS.TTOS.ToolHelper.DAL.UnitOfWork;

namespace TCIS.TTOS.HelperTool.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeployFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDeployService, DeployService>();
        return services;
    }

    public static IServiceCollection AddSpxExpressFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SpxOptions>(configuration.GetSection("Spx"));
        services.AddHttpClient();
        services.AddSingleton<ISpxExpressService, SpxExpressService>();
        return services;
    }

    public static IServiceCollection AddSpxTrackingFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SpxTrackingOptions>(configuration.GetSection("SpxTracking"));
        services.AddScoped<ISpxTrackingService, SpxTrackingService>();
        services.AddHostedService<SpxTrackingBackgroundService>();
        return services;
    }

    public static IServiceCollection AddDockerMonitorFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DockerMonitorOptions>(configuration.GetSection("DockerMonitor"));
        services.AddScoped<IDockerMonitorService, DockerMonitorService>();
        services.AddHostedService<DockerMonitorBackgroundService>();
        return services;
    }

    public static IServiceCollection AddYooseeCameraFeature(this IServiceCollection services, IConfiguration configuration)
    {
        var yooseeOptions = configuration.GetSection("Yoosee").Get<YooseeOptions>() ?? new YooseeOptions();
        services.AddSingleton(yooseeOptions);
        services.AddSingleton<IYooseePtzClient, YooseePtzClient>();
        return services;
    }

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextFactory<ToolHelperDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("ToolHelperDb")));
        services.AddScoped<IToolHelperUnitOfWork, ToolHelperUnitOfWork>();
        return services;
    }
}
