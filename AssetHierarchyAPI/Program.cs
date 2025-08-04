using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection; // Add this using directive
using Microsoft.OpenApi.Models;                // Add this using directive if needed for AddSwaggerGen

var builder = WebApplication.CreateBuilder(args);
var storageType = builder.Configuration["StorageType"];

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();         // ? This line is required
builder.Services.AddSwaggerGen();     // ? This line is required
if(storageType == "XML")
{
    builder.Services.AddSingleton<IHierarchyStorage , XmlHierarchyStorage>();
}
else
{
     builder.Services.AddSingleton<IHierarchyStorage, JsonHierarchyStorage>();
}
builder.Services.AddSingleton<IHierarchyService, HierarchyService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();                              // ? This line is required
    app.UseSwaggerUI();                            // ? This line is required
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
