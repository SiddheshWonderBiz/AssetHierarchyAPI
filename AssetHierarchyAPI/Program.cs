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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();         
builder.Services.AddSwaggerGen();
builder.Services.AddStorageService(builder.Configuration);



var app = builder.Build();
app.UseCors("AllowFrontend");

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
