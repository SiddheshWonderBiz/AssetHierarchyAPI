



using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Services;

namespace AssetHierarchyAPI.Extensions
{
    public  static class StorageServices
    {
        public static IServiceCollection AddStorageService(this IServiceCollection services,IConfiguration configuration)
        {
            var storageType = configuration["StorageType"];
            if (storageType == "XML")
            {
               services.AddSingleton<IHierarchyStorage, XmlHierarchyStorage>();
            }
            else
            {
                services.AddSingleton<IHierarchyStorage, JsonHierarchyStorage>();
            }
            services.AddSingleton<IHierarchyService, HierarchyService>();
            services.AddSingleton<ILoggingService, LoggingService>();
            return services;


        }
    }
}
