using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AssetHierarchyAPI.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        // No need for SendNotification method
        // Backend services will broadcast directly using IHubContext<NotificationHub>
    }
}
