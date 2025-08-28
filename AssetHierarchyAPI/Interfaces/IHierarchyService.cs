// Interfaces/IHierarchyService.cs
using AssetHierarchyAPI.Models;

namespace AssetHierarchyAPI.Interfaces
{
    public interface IHierarchyService
    {
        AssetNode LoadHierarchy();
        void AddNode(int parentId, AssetNode newNode);
        void RemoveNode(int nodeId);

        void AssignIds(AssetNode root, ref int currentId);
        bool UpdateNodeName(int id, string newName);
        int CountNodes(AssetNode node);
        void AddHierarchy(AssetNode node);
        void ReplaceTree(AssetNode node);
    }
}
