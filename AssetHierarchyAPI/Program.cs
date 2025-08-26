using AssetHierarchyAPI.Data;
using AssetHierarchyAPI.Extensions;
using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Middleware;
using AssetHierarchyAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Ui.Web;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

//   Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

//   EF Core DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Connect")));

//   CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

//   Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//   Storage service extension
builder.Services.AddStorageService(builder.Configuration);

//   Serilog UI
builder.Services.AddSerilogUi(optionsBuilder => { });

var app = builder.Build();

//   HTTPS first
app.UseHttpsRedirection();

//   Then CORS
app.UseCors("AllowFrontend");

//   Swagger (dev only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//   Middleware
app.UseAuthorization();
//app.UseMiddleware<ImportFormatValidationMiddleware>();

//   Serilog UI dashboard
app.UseSerilogUi();

//   Map controllers
app.MapControllers();

app.Run();
