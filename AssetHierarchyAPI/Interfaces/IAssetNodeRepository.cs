
using AssetHierarchyAPI.Models;

namespace AssetHierarchyAPI.Interfaces
{
    public interface IAssetNodeRepository
    {
        Task<List<AssetNode>> GetAllAsync();
        Task<AssetNode?> GetByIdAsync(int id);
        Task<AssetNode?> GetRootAsync();
        Task AddAsync(AssetNode assetNode);
        Task UpdateAsync(AssetNode assetNode);
        Task DeleteAsync(AssetNode assetNode);
        Task DeleteRangeAsync(IEnumerable<AssetNode> assetNodes);
        Task<bool> ExistsAsync(int id);
        Task SaveChangesAsync();

        Task ClearAsync();  // deletes and reseeds table
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
