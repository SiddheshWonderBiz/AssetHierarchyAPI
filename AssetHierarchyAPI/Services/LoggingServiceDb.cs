using AssetHierarchyAPI.Data;
using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Models;
using System.Security.Claims;

namespace AssetHierarchyAPI.Services
{
    public class LoggingServiceDb : ILoggingServiceDb
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _contextAccessor;

        public LoggingServiceDb(AppDbContext context, IHttpContextAccessor contextAccessor)
        {
            _context = context;
            _contextAccessor = contextAccessor;
        }

        public async Task LogsActionsAsync(string actionType, string? targetName = null)
        {
            var user = _contextAccessor.HttpContext?.User;
            var userId = int.TryParse(user?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
                ? id : 0;

            var username = user?.Identity?.Name ?? "Unknown";
            var role = user?.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";
            var log = new AssetLog
            {
                UserId = userId ,
                Username = username ,
                Role = role ,
                Action =    actionType ,
                TargetName = targetName,
                TimeStamp = DateTime.UtcNow,
            };
            await _context.Assetslogs.AddAsync(log);
            await _context.SaveChangesAsync();

        }
    }
}
