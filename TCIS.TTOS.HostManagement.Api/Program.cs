using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TCIS.TTOS.HostManagement.API.Extensions;
using TCIS.TTOS.ToolHelper.DAL;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:4500")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Host Management API", Version = "v1" });
});
builder.Services.AddHttpContextAccessor();

// Feature registrations
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddHostManagementFeature();

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

app.UseCors("AllowFrontend");

app.UseAuthorization();
app.MapControllers();

app.Run();
