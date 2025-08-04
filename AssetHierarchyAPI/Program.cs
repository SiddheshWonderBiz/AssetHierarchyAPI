using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ✅ Register config to hold user's selected storage type
builder.Services.AddSingleton<StorageConfig>();

// ✅ Register both concrete implementations
builder.Services.AddTransient<XmlHierarchyStorage>();
builder.Services.AddTransient<JsonHierarchyStorage>();

// ✅ Register IHierarchyStorage with dynamic resolution at runtime
builder.Services.AddScoped<IHierarchyStorage>(sp =>
{
    var config = sp.GetRequiredService<StorageConfig>();
    return config.StorageType.ToLower() == "xml"
        ? sp.GetRequiredService<XmlHierarchyStorage>()
        : sp.GetRequiredService<JsonHierarchyStorage>();
});

// ✅ Your main service logic
builder.Services.AddScoped<IHierarchyService, HierarchyService>();

// Boilerplate
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
