using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TCIS.TTOS.HelperTool.API.Infrastructure.Services.Implement;
using TCIS.TTOS.HelperTool.API.Infrastructure.Services.Interface;
using TCIS.TTOS.HelperTool.API.Infrastructure.Services.Models;
using TCIS.TTOS.ToolHelper.DAL;
using TCIS.TTOS.ToolHelper.DAL.UnitOfWork;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Deploy Gateway API", Version = "v1" });
});
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<DeploySettings>(builder.Configuration.GetSection("DeploySettings"));
builder.Services.Configure<SpxOptions>(builder.Configuration.GetSection("Spx"));

builder.Services.AddSingleton<IDeployService, DeployService>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<ISpxExpressService, SpxExpressService>();

// PostgreSQL + EF Core
builder.Services.AddDbContextFactory<ToolHelperDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ToolHelperDb")));

// UnitOfWork
builder.Services.AddScoped<IToolHelperUnitOfWork, ToolHelperUnitOfWork>();

// SPX Tracking
builder.Services.Configure<SpxTrackingOptions>(builder.Configuration.GetSection("SpxTracking"));
builder.Services.AddScoped<ISpxTrackingService, SpxTrackingService>();
builder.Services.AddHostedService<SpxTrackingBackgroundService>();

// Docker Monitor
builder.Services.Configure<DockerMonitorOptions>(builder.Configuration.GetSection("DockerMonitor"));
builder.Services.AddScoped<IDockerMonitorService, DockerMonitorService>();
builder.Services.AddHostedService<DockerMonitorBackgroundService>();

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ToolHelperDbContext>>().CreateDbContext();
    await db.Database.MigrateAsync();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("[DB] ? Database migrated successfully");
    Console.ResetColor();
}



app.UseSwagger();
app.UseSwaggerUI();


app.UseAuthorization();
app.MapControllers();


app.Run();