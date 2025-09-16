using AssetHierarchyAPI.Application.Interfaces;
using AssetHierarchyAPI.Infrastructure.Repositories;
using AssetHierarchyAPI.Infrastructure.Services;
using AssetHierarchyAPI.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;


namespace AssetHierarchyAPI.Infrastructure.Extensions
{
    public  static class StorageServices
    {
        public static IServiceCollection AddStorageService(this IServiceCollection services,IConfiguration configuration)
        {
            var storageType = configuration.GetValue<string>("StorageType")?.ToUpperInvariant() ?? "JSON";
            services.AddSingleton<ILoggingService, LoggingService>();
            switch (storageType) {
                case "XML":
                    services.AddScoped<IHierarchyStorage, XmlHierarchyStorage>();
                    services.AddScoped<ISignalRepository , SignalService>();
                    services.AddScoped<IHierarchyService,HierarchyService>();
                    break;
                case "DB":
                    //services.AddScoped<IHierarchyStorage, DatabaseHierarchyStorage>();
                    services.AddScoped<IAssetNodeRepository, AssetNodeRepository>();
                    services.AddScoped<ISignalRepository, SignalService>();
                    services.AddScoped<IHierarchyService, DatabaseHierarchyService>();
                    services.AddScoped<ILoggingServiceDb, LoggingServiceDb>();
                    break;
                default:
                    services.AddScoped<IHierarchyStorage, JsonHierarchyStorage>();
                    services.AddScoped<ISignalRepository, SignalService>();
                    services.AddScoped<IHierarchyService, HierarchyService>();
                    break;
            }
            return services;


        }
    }
}
