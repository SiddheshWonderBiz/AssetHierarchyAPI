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

        // Loads the hierarchy tree from storage
        public AssetNode LoadHierarchy() => _storage.LoadHierarchy();

        // Saves the hierarchy tree to storage
        public void SaveHierarchy(AssetNode root) => _storage.SaveHierarchy(root);

        // Adds a new node 
        public void AddNode(int parentId, AssetNode newNode)
        {
            var root = _storage.LoadHierarchy();

            //dupliaction avoidance check
            if (NodeExists(root, newNode.Id))
            {
                _logger.LogError($"Node with ID {newNode.Id} already exists.");
                throw new InvalidOperationException($"A node with ID {newNode.Id} already exists.");
            }

            // Find the parent node where the new node will be added
            var parent = FindNode(root, parentId);
            if (parent != null)
            {
                parent.Children.Add(newNode);   
                _storage.SaveHierarchy(root);   
                _logger.LogInfo($"Node {newNode.Id}:{newNode.Name} added under parent {parentId}.");
            }
        }

        // Checks if a node with a given ID already exists in the hierarchy
        public bool NodeExists(AssetNode curr, int id)
        {
            if (curr.Id == id) return true;
            foreach (var child in curr.Children)
            {
                if (NodeExists(child, id)) return true;
            }
            return false;
        }

        // Finds a node by ID 
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

        // Removes a node from the hierarchy by ID
        public void RemoveNode(int nodeId)
        {
            var root = _storage.LoadHierarchy();

            // Special case: If removing the root node, clear the entire hierarchy
            if (root.Id == nodeId)
            {
                _storage.SaveHierarchy(null);
                _logger.LogInfo($"Root node {nodeId} removed.");
                return;
            }

            // Remove node recursively from children
            bool removed = RemoveNodeRecursive(root, nodeId);
            if (!removed)
            {
                throw new InvalidOperationException($"Node not found {nodeId}");
            }
            _storage.SaveHierarchy(root);
            _logger.LogInfo($"Node {nodeId} removed.");
        }

        // Helper method for recursive node removal
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

        // Completely replaces the existing tree with a new one
        public void ReplaceTree(AssetNode newRoot)
        {
            NormalizeChildren(newRoot); 

            int maxId = FindMaxId(newRoot);

            AssignMissingIds(newRoot, ref maxId); // Assign IDs to nodes that don't have one

            int totalCount = CountNodes(newRoot); // Count total number of nodes

            _storage.SaveHierarchy(newRoot);
            _logger.LogInfo($"Hierarchy replaced successfully. Total nodes: {totalCount}");
        }

        // Ensures no null Children lists 
        private void NormalizeChildren(AssetNode node)
        {
            if (node.Children == null)
                node.Children = new List<AssetNode>();

            foreach (var child in node.Children)
            {
                NormalizeChildren(child);
            }
        }

        // Finds the largest ID in the tree
        private int FindMaxId(AssetNode node)
        {
            int max = node.Id;
            foreach (var child in node.Children)
            {
                max = Math.Max(max, FindMaxId(child)); 
            }
            return max;
        }

        // Assigns new unique IDs to any node missing an ID (<= 0)
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

        // Counts the total number of nodes 
        public int CountNodes(AssetNode node)
        {
            int count = 1; 
            foreach (var child in node.Children)
            {
                count += CountNodes(child); 
            }
            return count;
        }

       // id auto genration 
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
