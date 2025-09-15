using AssetHierarchyAPI.Domain.Models;
using AssetHierarchyAPI.Presentation.Controllers;

namespace AssetHierarchyAPI.Domain.Interfaces
{
    public interface ISignalServices
    {
        Task<IEnumerable<Signals>> GetByAssetAsync(int assetId);
        Task<Signals?> GetByIdAsync(int id);
        Task<Signals> AddSignalAsync(int assetId, GlobalSignalDTO signals);
        Task<bool> UpdateSignalAsync(int id, GlobalSignalDTO updated);
        Task<bool> DeleteSignalAsync(int id);
    }
}
