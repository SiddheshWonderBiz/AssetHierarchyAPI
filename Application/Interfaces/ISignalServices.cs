using AssetHierarchyAPI.Domain.Models;
using AssetHierarchyAPI.Application.DTOs;

namespace AssetHierarchyAPI.Application.Interfaces
{
    public interface ISignalServices
    {
        Task<IEnumerable<Signal>> GetByAssetAsync(int assetId);
        Task<Signal?> GetByIdAsync(int id);
        Task<Signal> AddSignalAsync(int assetId, GlobalSignalDTO signals);
        Task<bool> UpdateSignalAsync(int id, GlobalSignalDTO updated);
        Task<bool> DeleteSignalAsync(int id);
    }
}
