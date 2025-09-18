using Application.Interfaces;
using Infrastructure.Queues;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Extensions
{
    public static class BackgroundService
    {
        public static IServiceCollection AddBackgroundSevice(this IServiceCollection services)
        {
            services.AddSingleton<ICalculationQueue, CalculationQueue>();

            services.AddSingleton<CalculationBackgroundService>();
            services.AddHostedService(sp => sp.GetRequiredService<CalculationBackgroundService>());
            return services;
        }
    }
}
