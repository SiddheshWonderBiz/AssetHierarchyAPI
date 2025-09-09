using AssetHierarchyAPI.Controllers;
using AssetHierarchyAPI.Data;
using AssetHierarchyAPI.Hubs;
using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AssetHierarchyAPI.Services
{
    public class SignalService : ISignalServices
    {
        private readonly AppDbContext _context;
        private readonly ILoggingServiceDb _loggerdb;
        private readonly IHubContext<NotificationHub>   _hubContext;

        private static readonly HashSet<string> allowed =
            new(StringComparer.OrdinalIgnoreCase) { "int", "string", "real" };

        public SignalService(AppDbContext context, ILoggingServiceDb loggerdb , IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _loggerdb = loggerdb;
            _hubContext = hubContext;
        }

        public async Task<IEnumerable<Signals>> GetByAssetAsync(int assetId)
        {
            return await _context.Signals
                .AsNoTracking()
                .Where(x => x.AssetId == assetId)
                .OrderBy(x => x.AssetId)
                .ToListAsync();
        }

        public async Task<Signals?> GetByIdAsync(int id)
        {
            return await _context.Signals
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        private async Task<bool> DuplicateSignalAsync(GlobalSignalDTO dto, int assetId)
        {
            return await _context.Signals
                .AnyAsync(s => s.AssetId == assetId && s.Name.ToLower() == dto.Name.ToLower());
        }

        public async Task<Signals> AddSignalAsync(int assetId, GlobalSignalDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "Signal cannot be null");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Name cannot be empty");

            if (string.IsNullOrWhiteSpace(dto.ValueType))
                throw new ArgumentException("ValueType cannot be empty");
            string pattern = @"^[a-zA-Z0-9_\-\s]+$";
            bool isvalid = Regex.IsMatch(dto.Name, pattern);
            if (!isvalid) {
                throw new ArgumentException("Invalid name pattern allowed only a-z,1-9 and -_");
            }

            if (!allowed.Contains(dto.ValueType))
                throw new ArgumentException(
                    $"Invalid value type '{dto.ValueType}'. Allowed: {string.Join(", ", allowed)}"
                );

            var asset= await _context.AssetNodes.FirstOrDefaultAsync(a => a.Id == assetId);
            if (asset == null)
                throw new InvalidOperationException($"Asset with id {assetId} not found.");

            if (await DuplicateSignalAsync(dto, assetId))
                throw new InvalidOperationException($"Signal '{dto.Name}' already exists for this asset.");

            var signal = new Signals
            {
                Name = dto.Name,
                ValueType = dto.ValueType,
                Description = dto.Description,
                AssetId = assetId
            };

            _context.Signals.Add(signal);
            await _context.SaveChangesAsync();

            await _loggerdb.LogsActionsAsync("Signal added", signal.Name);
           await _hubContext.Clients.All.SendAsync("signalAdded", $"Signal {signal.Name} addded under {asset.Name}");

            return signal;
        }

        public async Task<bool> UpdateSignalAsync(int id, GlobalSignalDTO dto)
        {
            var signal = await _context.Signals.FirstOrDefaultAsync(s => s.Id == id);
            if (signal == null)
                throw new InvalidOperationException($"Signal with id {id} not found.");

            if (dto == null)
                throw new ArgumentException("Signal cannot be null.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Signal name is required.");

            if (string.IsNullOrWhiteSpace(dto.ValueType))
                throw new ArgumentException("Signal value type is required.");
            string pattern = @"^[a-zA-Z0-9_\-\s]+$";
            bool isvalid = Regex.IsMatch(dto.Name, pattern);
            if (!isvalid)
            {
                throw new ArgumentException("Invalid name pattern allowed only a-z,1-9 and -_");
            }

            if (!allowed.Contains(dto.ValueType))
                throw new ArgumentException(
                    $"Invalid value type '{dto.ValueType}'. Allowed: {string.Join(", ", allowed)}"
                );

            var duplicate = await _context.Signals
                .AnyAsync(s => s.AssetId == signal.AssetId &&
                               s.Name.ToLower() == dto.Name.ToLower() &&
                               s.Id != id);

            if (duplicate)
                throw new InvalidOperationException($"Signal '{dto.Name}' already exists for this asset.");

            signal.Name = dto.Name;
            signal.ValueType = dto.ValueType;
            signal.Description = dto.Description;

            await _context.SaveChangesAsync();
            await _loggerdb.LogsActionsAsync("Signal updated", signal.Name);

            return true;
        }

        public async Task<bool> DeleteSignalAsync(int id)
        {
            var signal = await _context.Signals.FirstOrDefaultAsync(s => s.Id == id);
            if (signal == null)
                throw new InvalidOperationException($"Signal with id {id} not found.");

            _context.Signals.Remove(signal);
            await _context.SaveChangesAsync();

            await _loggerdb.LogsActionsAsync("Signal deleted", signal.Name);

            return true;
        }
    }
}
