using AssetHierarchyAPI.Data;
using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AssetHierarchyAPI.Repositories
{
    public class AssetNodeRepository : IAssetNodeRepository
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction _transaction;
        public AssetNodeRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<AssetNode>> GetAllAsync()
        {
            return await _context.AssetNodes.AsNoTracking().ToListAsync();
        }
        public async Task<AssetNode?> GetByIdAsync(int Id)
        {
            return await _context.AssetNodes.FirstOrDefaultAsync(x => x.Id == Id);
        }
        public async Task<AssetNode?> GetRootAsync()
        {
            return await _context.AssetNodes.FirstOrDefaultAsync(x => x.ParentId == null);
        }
        public async Task AddAsync(AssetNode node)
        {
             await _context.AssetNodes.AddAsync(node);
        }
        public async Task UpdateAsync(AssetNode node)
        {
             _context.AssetNodes.Update(node);
        }
        public async Task DeleteAsync(AssetNode node)
        {
            _context.AssetNodes.Remove(node);
        }
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.AssetNodes.AnyAsync(x => x.Id == id);
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task DeleteRangeAsync(IEnumerable<AssetNode> assetNodes)
        {
            _context.AssetNodes.RemoveRange(assetNodes);
            await _context.SaveChangesAsync();
        }
        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }
        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
                await _transaction.CommitAsync();
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
                await _transaction.RollbackAsync();
        }

    public async Task ClearAsync()
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM AssetNodes");
            await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('AssetNodes', RESEED, 0)");
        }
    }
}
