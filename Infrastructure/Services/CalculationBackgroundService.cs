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

                    var signal = await db.Signals.Include(s=>s.Values).FirstOrDefaultAsync(s => s.Id == job.SignalId , stoppingToken);
                    double avg = 0;
                    if (signal != null && signal.Values.Any()) { 
                    avg = signal.Values.Average(v =>  v.Value);
                    }
                    await _hubContext.Clients.All.SendAsync(
                                           "ReceiveAverageResult",
                                           new { SignalId = job.SignalId,  Average = avg },
                                           cancellationToken: stoppingToken);
                }
                else
                {
                    await Task.Delay(500 , stoppingToken);
                }
            }
        }
    }
}
