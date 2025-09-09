using AssetHierarchyAPI.Models;
using System.Text.Json;

namespace AssetHierarchyAPI.Interfaces
{
    public interface IHierarchyService
    {
        Task<AssetNode> LoadHierarchy();
        Task AddNode(int parentId, AssetNode newNode);
        Task RemoveNode(int nodeId);

        void AssignIds(AssetNode root, ref int currentId);
        Task<bool> UpdateNodeName(int id, string newName);
        Task<int> CountNodes();
        Task AddHierarchy(AssetNode node);
        Task ReplaceTree(AssetNode node);

        Task<string> ReorderNode(int id, int? newparentId);
        void ValidateNode(JsonElement element);
    }
}
