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
            var parent = FindNode(root, parentId);
            if (parent != null)
            {
                parent.Children.Add(newNode);
                _storage.SaveHierarchy(root);
            }
        }

        public void RemoveNode(int nodeId)
        {
            var root = _storage.LoadHierarchy();
            if (root.Id == nodeId)
                throw new InvalidOperationException("Cannot remove root node");

            RemoveNodeRecursive(root, nodeId);
            _storage.SaveHierarchy(root);
        }

        // helper methods: FindNode & RemoveNodeRecursive
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
