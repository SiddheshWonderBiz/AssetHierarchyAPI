using AssetHierarchyAPI.Application.Interfaces;
using AssetHierarchyAPI.Domain.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AssetHierarchyAPI.Infrastructure.Services
{
    public class LoggingServiceDb : ILoggingServiceDb
    {
        private readonly IAssetLogRepository _logRepository;
        private readonly IHttpContextAccessor _contextAccessor;

        public LoggingServiceDb(IAssetLogRepository logRepository, IHttpContextAccessor contextAccessor)
        {
            _logRepository = logRepository;
            _contextAccessor = contextAccessor;
        }

        public async Task LogsActionsAsync(string actionType, string? targetName = null)
        {
            var user = _contextAccessor.HttpContext?.User;

            var username = user?.Identity?.Name ?? "Unknown";
            var role = user?.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";

            var log = new AssetLog
            {
                Username = username,
                Role = role,
                Action = actionType,
                TargetName = targetName,
                TimeStamp = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")
                )
            };

            await _logRepository.AddAsync(log);
            await _logRepository.SaveChangesAsync();
        }
    }
}
