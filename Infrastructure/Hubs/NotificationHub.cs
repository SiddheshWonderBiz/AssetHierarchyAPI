
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace AssetHierarchyAPI.Infrastructure.Hubs
{
    public class NotificationHub : Hub
    {
        // Called by trusted server worker to broadcast to all clients
        public async Task BroadcastAverageResult(int signalId, double average)
        {
            await Clients.All.SendAsync("ReceiveAverageResult", new { signalId, average });
        }
    }
}
