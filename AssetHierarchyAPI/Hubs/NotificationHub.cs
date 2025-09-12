using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AssetHierarchyAPI.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
    }
}
