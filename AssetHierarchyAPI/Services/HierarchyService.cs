using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Models;

namespace AssetHierarchyAPI.Services
{
    public class HierarchyService : IHierarchyService
    {
        private readonly IHierarchyStorage _storage;
        private readonly ILoggingService _logger;
        private int _nextId = 1;
        public HierarchyService(IHierarchyStorage storage, ILoggingService logger)
        {
            _storage = storage;
            _logger = logger;
        }

        public AssetNode LoadHierarchy() => _storage.LoadHierarchy();

        public void SaveHierarchy(AssetNode root) => _storage.SaveHierarchy(root);

        public void AddNode(int parentId, AssetNode newNode)
        {
            var root = _storage.LoadHierarchy();
            if (NodeExists(root, newNode.Id))
            {
                _logger.LogError($"Node with ID {newNode.Id} already exists.");
                throw new InvalidOperationException($"A node with ID {newNode.Id} already exists.");
            }

            var parent = FindNode(root, parentId);
            if (parent != null)
            {
                parent.Children.Add(newNode);
                _storage.SaveHierarchy(root);
                _logger.LogInfo($"Node {newNode.Id}:{newNode.Name} added under parent {parentId}.");
            }
        }

        public bool NodeExists(AssetNode curr, int id)
        {
            if (curr.Id == id) return true;
            foreach (var child in curr.Children)
            {
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
                _logger.LogInfo($"Root node {nodeId} removed.");
                return;
            }

            bool removed = RemoveNodeRecursive(root, nodeId);
            if (!removed)
            {
                throw new InvalidOperationException($"Node not found {nodeId}");
            }
            _storage.SaveHierarchy(root);
            _logger.LogInfo($"Node {nodeId} removed.");
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

        public void ReplaceTree(AssetNode newRoot)
        {
            NormalizeChildren(newRoot);

            int maxId = FindMaxId(newRoot);

            AssignMissingIds(newRoot, ref maxId);

            int totalCount = CountNodes(newRoot);

            _storage.SaveHierarchy(newRoot);

            _logger.LogInfo($"Hierarchy replaced successfully. Total nodes: {totalCount}");
        }

        private void NormalizeChildren(AssetNode node)
        {
            if (node.Children == null)
                node.Children = new List<AssetNode>();

            foreach (var child in node.Children)
            {
                NormalizeChildren(child);
            }
        }

        private int FindMaxId(AssetNode node)
        {
            int max = node.Id;
            foreach (var child in node.Children)
            {
                max = Math.Max(max, FindMaxId(child));
            }
            return max;
        }

        private void AssignMissingIds(AssetNode node, ref int maxId)
        {
            if (node.Id <= 0)
            {
                maxId++;
                node.Id = maxId;
            }
            foreach (var child in node.Children)
            {
                AssignMissingIds(child, ref maxId);
            }
        }

        private int CountNodes(AssetNode node)
        {
            int count = 1;
            foreach (var child in node.Children)
            {
                count += CountNodes(child);
            }
            return count;
        }
        public void AssignIds(AssetNode node, ref int currentId)
        {
            if (node == null) return;

            node.Id = currentId++;
            foreach (var child in node.Children)
            {
                AssignIds(child, ref currentId);
            }
        }

    }
}
