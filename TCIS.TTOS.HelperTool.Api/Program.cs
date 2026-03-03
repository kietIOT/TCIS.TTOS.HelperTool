using Microsoft.OpenApi.Models;
using System.Net;
using TCIS.TTOS.HelperTool.API.Infrastructure.Services.Implement;
using TCIS.TTOS.HelperTool.API.Infrastructure.Services.Interface;
using TCIS.TTOS.HelperTool.API.Infrastructure.Services.Models;

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

builder.Services.AddSingleton(new YooseeOptions
{
    TokenFilePath = builder.Configuration["Yoosee:TokenFilePath"] ?? "./token.txt",
    RtspPort = int.TryParse(builder.Configuration["Yoosee:RtspPort"], out var p) ? p : 554,
    PtzStopDelayMs = int.TryParse(builder.Configuration["Yoosee:PtzStopDelayMs"], out var d) ? d : 500
});
builder.Services.AddSingleton<IYooseePtzClient, YooseePtzClient>();

static bool IsValidIp(string ip) => IPAddress.TryParse(ip, out var a) && a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;

static async Task<string?> ReadTokenAsync(string path, CancellationToken ct)
{
    if (!File.Exists(path)) return null;
    var token = (await File.ReadAllTextAsync(path, ct)).Trim();
    return string.IsNullOrWhiteSpace(token) ? null : token;
}

var app = builder.Build();

// Token authentication middleware for the legacy /{token}/{ip}/{action} endpoint
app.Use(async (ctx, next) =>
{
    var segments = ctx.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
    if (segments.Length >= 3 && !ctx.Request.Path.Value!.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
    {
        var token = segments[0];
        var ip = segments[1];

        var opt = ctx.RequestServices.GetRequiredService<YooseeOptions>();
        var expected = await ReadTokenAsync(opt.TokenFilePath, ctx.RequestAborted);

        if (expected is null || !string.Equals(expected, token, StringComparison.Ordinal))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            await ctx.Response.WriteAsync("Forbidden", ctx.RequestAborted);
            return;
        }

        if (!IsValidIp(ip))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            await ctx.Response.WriteAsync("Not a valid IP", ctx.RequestAborted);
            return;
        }
    }

    await next();
});

var actions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["up"] = "UP",
    ["down"] = "DOWN",
    ["left"] = "LEFT",
    ["right"] = "RIGHT",
    ["stop"] = "STOP",
    ["zoom_in"] = "ZOOM_IN",
    ["zoom_out"] = "ZOOM_OUT",
};



app.UseSwagger();
app.UseSwaggerUI();


app.UseAuthorization();
app.MapControllers();

// Legacy endpoint: /{token}/{ip}/{action}
app.MapGet("/{token}/{ip}/{action}", async (string ip, string action, IYooseePtzClient ptz, CancellationToken ct) =>
{
    if (!actions.TryGetValue(action, out var cmd))
        return Results.NotFound("Unknown action");

    await ptz.MoveAsync(ip, cmd, ct);
    return Results.Ok($"Action {action} performed successfully");
});

app.Run();