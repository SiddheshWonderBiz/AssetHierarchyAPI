using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using AssetHierarchyAPI.Extensions;
var builder = WebApplication.CreateBuilder(args);
var storageType = builder.Configuration["StorageType"];

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();         
builder.Services.AddSwaggerGen();
builder.Services.AddStorageService(builder.Configuration);
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();                              
    app.UseSwaggerUI();                           
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
