using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Models;

namespace AssetHierarchyAPI.Services
{
    public class HierarchyService : IHierarchyService
    {
        private readonly IHierarchyStorage _storage;

        public HierarchyService(IHierarchyStorage storage)
        {
            _storage = storage;
        }

        public AssetNode LoadHierarchy() => _storage.LoadHierarchy();

        public void SaveHierarchy(AssetNode root) => _storage.SaveHierarchy(root);

        public void AddNode(int parentId, AssetNode newNode)
        {
            var root = _storage.LoadHierarchy();
            if(NodeExists(root , newNode.Id))
            {
                throw new InvalidOperationException($"A node with ID {newNode.Id} already exists.");
            }

            var parent = FindNode(root, parentId);
            if (parent != null)
            {
                parent.Children.Add(newNode);
                _storage.SaveHierarchy(root);
            }
        }

        public bool NodeExists(AssetNode curr , int id)
        {
            if(curr.Id == id) return true;
            foreach (var child in curr.Children) { 
            if (NodeExists(child, id)) return true;
            }
            return false;
        }

        private AssetNode FindNode(AssetNode current, int id)
        {
            if (current.Id == id)
                return current;

            foreach (var child in current.Children)
            {
                var found = FindNode(child, id);
                if (found != null)
                    return found;
            }

            return null;
        }

        public void RemoveNode(int nodeId)
        {
            var root = _storage.LoadHierarchy();
            if (root.Id == nodeId)
            {
                _storage.SaveHierarchy(null);
                return;
            }

            bool removed = RemoveNodeRecursive(root, nodeId);
            if (!removed) {
                throw new InvalidOperationException($"Node not found {nodeId}");
            }
            _storage.SaveHierarchy(root);
        }

        private bool RemoveNodeRecursive(AssetNode parent, int nodeId)
        {
            foreach (var child in parent.Children.ToList())
            {
                if (child.Id == nodeId)
                {
                    parent.Children.Remove(child);
                    return true;
                }
                if (RemoveNodeRecursive(child, nodeId))
                    return true;
            }
            return false;
        }
    }
}
