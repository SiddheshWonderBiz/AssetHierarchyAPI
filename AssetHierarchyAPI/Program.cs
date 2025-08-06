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


//builder.Services.AddRateLimiter(options =>
//{
//    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext , string> (httpcontext =>
//    RateLimitPartition.GetFixedWindowLimiter(
//        partitionKey : httpcontext.Request.Headers.Host.ToString(),
//        factory : _ => new FixedWindowRateLimiterOptions
//        {
//            PermitLimit = 5 ,
//            Window = TimeSpan.FromMinutes(1),
//            QueueLimit = 0 ,
//            AutoReplenishment = true
//        }
        
        
//        ));
//});
var app = builder.Build();
//app.UseRateLimiter();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();                              
    app.UseSwaggerUI();                           
}

app.UseHttpsRedirection();

app.UseMiddleware<RateLimiterMiddleware>();
//app.UseAuthorization();

app.MapControllers();

app.Run();
