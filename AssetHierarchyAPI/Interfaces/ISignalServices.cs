using AssetHierarchyAPI.Controllers;
using AssetHierarchyAPI.Models;

namespace AssetHierarchyAPI.Interfaces
{
    public interface ISignalServices
    {
        IEnumerable<Signals> GetByAsset(int assetId);
        Signals? GetById(int id);
        Signals AddSignal(int assetId, GlobalSignalDTO signals);
        bool UpdateSignal(int id, GlobalSignalDTO updated);
        bool DeleteSignal(int id);
    }
}
