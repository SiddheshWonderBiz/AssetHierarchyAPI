



using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Services;

namespace AssetHierarchyAPI.Extensions
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
                    services.AddScoped<IHierarchyService,HierarchyService>();
                    break;
                case "DB":
                    //services.AddScoped<IHierarchyStorage, DatabaseHierarchyStorage>();
                    services.AddScoped<IHierarchyService, DatabaseHierarchyService>();
                    break;
                default:
                    services.AddScoped<IHierarchyStorage, JsonHierarchyStorage>();
                    services.AddScoped<IHierarchyService, HierarchyService>();
                    break;
            }
            return services;


        }
    }
}
