using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
namespace AssetHierarchyAPI.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public async Task SendNotification(string message)
        {
            await Clients.All.SendAsync("nodeAdded", message);
        }
    }
}
