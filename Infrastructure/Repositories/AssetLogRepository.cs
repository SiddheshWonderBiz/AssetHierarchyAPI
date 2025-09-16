using AssetHierarchyAPI.Application.Interfaces;
using AssetHierarchyAPI.Domain.Models;
using AssetHierarchyAPI.Infrastructure.Data;
using System.Threading.Tasks;

namespace AssetHierarchyAPI.Infrastructure.Repositories
{
    public class AssetLogRepository : IAssetLogRepository
    {
        private readonly AppDbContext _context;

        public AssetLogRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(AssetLog log)
        {
            await _context.AssetLogs.AddAsync(log);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
