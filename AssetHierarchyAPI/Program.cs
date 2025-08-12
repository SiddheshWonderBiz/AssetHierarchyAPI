using AssetHierarchyAPI.Extensions;
using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using AssetHierarchyAPI.Middleware;
var builder = WebApplication.CreateBuilder(args);
var storageType = builder.Configuration["StorageType"];

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();         
builder.Services.AddSwaggerGen();
builder.Services.AddStorageService(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();                              
    app.UseSwaggerUI();                           
}

app.UseHttpsRedirection();

app.UseMiddleware<ImportFormatValidationMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
