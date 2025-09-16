using AssetHierarchyAPI.Infrastructure.Data;
using AssetHierarchyAPI.Domain.Models;
using System.Security.Claims;
using AssetHierarchyAPI.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AssetHierarchyAPI.Infrastructure.Services
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
            
            var username = user?.Identity?.Name ?? "Unknown";
            var role = user?.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";
            var log = new AssetLog
            {
                
                Username = username ,
                Role = role ,
                Action =    actionType ,
                TargetName = targetName,
                TimeStamp = TimeZoneInfo.ConvertTimeFromUtc(
    DateTime.UtcNow,
    TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")
)
            };
            await _context.AssetLogs.AddAsync(log);
            await _context.SaveChangesAsync();

        }
    }
}
