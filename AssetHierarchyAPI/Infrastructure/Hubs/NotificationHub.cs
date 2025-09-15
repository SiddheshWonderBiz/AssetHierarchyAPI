using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AssetHierarchyAPI.Infrastructure.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
    }
}
