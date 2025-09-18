using Application.Interfaces;
using AssetHierarchyAPI.Infrastructure.Data;
using AssetHierarchyAPI.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class CalculationBackgroundService: BackgroundService
    {
        private readonly ICalculationQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<NotificationHub> _hubContext;
        public CalculationBackgroundService(ICalculationQueue queue , IServiceScopeFactory scopeFactory , IHubContext<NotificationHub> hubContext)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;

        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) 
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_queue.TryDequeue(out var job))
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    double avg = 0;

                    if (job.ColumnName.Equals("Id", StringComparison.OrdinalIgnoreCase))
                    {
                        avg = await db.AssetNodes.AverageAsync(x => x.Id, stoppingToken);
                    }
                    else if (job.ColumnName.Equals("NameLength", StringComparison.OrdinalIgnoreCase))
                    {
                        avg = await db.AssetNodes.AverageAsync(x => x.Name.Length, stoppingToken);
                    }

                    await _hubContext.Clients.All.SendAsync("ReceiveAverageResult", new {Column = job.ColumnName , Average = avg },cancellationToken:stoppingToken);
                }
                else
                {
                    await Task.Delay(500 , stoppingToken);
                }
            }
        }
    }
}
