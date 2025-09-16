using AssetHierarchyAPI.Domain.Models;

namespace AssetHierarchyAPI.Application.Interfaces
{
    public interface ISignalRepository
    {
        Task<IEnumerable<Signal>> GetByAssetAsync(int assetId);
        Task<Signal?> GetByIdAsync(int id);
        Task AddAsync(Signal signal);
        Task UpdateAsync(Signal signal);
        Task DeleteAsync(Signal signal);
        Task<bool> ExistsAsync(int assetId, string signalName, int? excludeId = null);
    }
}
