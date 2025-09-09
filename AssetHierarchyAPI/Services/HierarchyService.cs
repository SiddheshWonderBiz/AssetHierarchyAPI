using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Models;
using System.Text.Json;

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

        // loads the hierarchy tree from storage
        public Task<AssetNode> LoadHierarchy()
        {
            return Task.FromResult(_storage.LoadHierarchy());
        }

        // saves the hierarchy tree to storage
        public void SaveHierarchy(AssetNode root) => _storage.SaveHierarchy(root);

        // Adds a new node 
        public  Task AddNode(int parentId, AssetNode newNode)
        {
            var root = _storage.LoadHierarchy();

            int maxid = FindMaxId(root);
            newNode.Id = maxid + 1;
            newNode.Children = newNode.Children ?? new List<AssetNode>();

            //dupliaction avoidance check
            if (NodeExists(root, newNode.Id , newNode.Name))
            {
                _logger.LogError($"Node with ID  {newNode.Name} already exists.");
                throw new InvalidOperationException($"A node with ID  {newNode.Name} already exists.");
            }

            // Find the parent node where the new node will be added
            var parent = FindNode(root, parentId);
            if (parent != null)
            {
                parent.Children.Add(newNode);   
                _storage.SaveHierarchy(root);   
                _logger.LogInfo($"Node {newNode.Id}:{newNode.Name} added under parent {parentId}.");
            }
            return Task.CompletedTask;

        }


        //Validate Node
        public void ValidateNode(JsonElement element)
        {
            // Find "name" property (case-insensitive)
            var nameProp = element.EnumerateObject()
                                  .FirstOrDefault(p => p.Name.Equals("name", StringComparison.OrdinalIgnoreCase));

            if (nameProp.Value.ValueKind == JsonValueKind.Undefined || nameProp.Value.ValueKind != JsonValueKind.String)
            {
                throw new ArgumentException("Each node must have a string 'name'.");
            }

            // If "children" exists, it must be an array
            var childrenProp = element.EnumerateObject()
                                      .FirstOrDefault(p => p.Name.Equals("children", StringComparison.OrdinalIgnoreCase));

            if (childrenProp.Value.ValueKind != JsonValueKind.Undefined)
            {
                if (childrenProp.Value.ValueKind != JsonValueKind.Array)
                {
                    throw new ArgumentException("'children' must be an array.");
                }

                foreach (var child in childrenProp.Value.EnumerateArray())
                {
                    ValidateNode(child); // recurse
                }
            }

            // Fail on unknown properties
            foreach (var prop in element.EnumerateObject())
            {
                if (!prop.Name.Equals("id", StringComparison.OrdinalIgnoreCase) &&
                    !prop.Name.Equals("name", StringComparison.OrdinalIgnoreCase) &&
                    !prop.Name.Equals("children", StringComparison.OrdinalIgnoreCase) &&
                    !prop.Name.Equals("parentId", StringComparison.OrdinalIgnoreCase) &&
                                        !prop.Name.Equals("parent", StringComparison.OrdinalIgnoreCase)


                    )
                {
                    throw new ArgumentException(
                        $"Invalid property '{prop.Name}'. Only 'id', 'name', and 'children' are allowed."
                    );
                }
            }
        }

        //update Node
        public Task<bool> UpdateNodeName(int id, string newName)
        {
            var root = _storage.LoadHierarchy();
            var node = FindNode(root, id);
            if (node == null)
            {
                _logger.LogError($"Node with ID {id} not found.");
                return Task.FromResult(false);
            }
            if(NodeExists(root , -1 , newName))
            {
                _logger.LogError($"Node with name {newName} already exists.");
                throw new InvalidOperationException($"A node with name {newName} already exists.");
            }
            string oldName = node.Name;
            oldName = newName;
            _storage.SaveHierarchy(root);
            _logger.LogInfo($"Node {id} renamed from '{oldName}' to '{newName}'.");

            return Task.FromResult(true);
        }

        // Checks if a node with a given ID already exists in the hierarchy
        public bool NodeExists(AssetNode curr, int id , string name)
        {
            if (curr.Id == id  || curr.Name == name) return true;
            foreach (var child in curr.Children)
            {
                if (NodeExists(child, id , name)) return true;
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
        public Task RemoveNode(int nodeId)
        {
            var root = _storage.LoadHierarchy();

            // Special case: If removing the root node, clear the entire hierarchy
            if (root.Id == nodeId)
            {
                _logger.LogError("You cant remove root node");
                throw new InvalidOperationException("You cant remove root node ");
            }

            // Remove node recursively from children
            bool removed = RemoveNodeRecursive(root, nodeId);
            if (!removed)
            {
                throw new InvalidOperationException($"Node not found {nodeId}");
            }
            _storage.SaveHierarchy(root);
            _logger.LogInfo($"Node {nodeId} removed.");
            return Task.CompletedTask;
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
        // Validates the hierarchy tree structure

        private void Validate(AssetNode node)
        {
            if(string.IsNullOrWhiteSpace(node.Name))
            {
                _logger.LogError("Node name cannot be null or empty.");
                throw new ArgumentException("Node name cannot be null or empty.");
            }
            if(node.Children == null)
            {
                _logger.LogError("Node children cannot be null.");
                throw new ArgumentException("Node children cannot be null.");
            }
            foreach(var child in node.Children)
            {
                Validate(child); // Recursively validate children
            }
        }
        // Checks for duplicate IDs or Names in the hierarchy
        private void ValidateUniqueness(AssetNode root)
        {
            var nameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Traverse(AssetNode node)
            {

                if (!nameSet.Add(node.Name))
                {
                    _logger.LogError($"Duplicate Name found: {node.Name}");
                    throw new InvalidOperationException($"Duplicate Name found: {node.Name}");
                }

                foreach (var child in node.Children)
                {
                    Traverse(child);
                }
            }

            Traverse(root);
        }


        // Completely replaces the existing tree with a new one
        public Task ReplaceTree(AssetNode newRoot)
        {
            Validate(newRoot); // Validate the new root node
            NormalizeChildren(newRoot); 

            int maxId = FindMaxId(newRoot);

            AssignMissingIds(newRoot, ref maxId); // Assign IDs to nodes that don't have one
            ValidateUniqueness(newRoot); // Ensure no duplicate names

            int totalCount = CountNodesRecursive(newRoot) - 1   ; // Count total number of nodes
            Console.WriteLine(totalCount);

            _storage.SaveHierarchy(newRoot);
            _logger.LogInfo($"Hierarchy replaced successfully. Total nodes: {totalCount  }");
            return Task.CompletedTask;
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
        private int CountNodesRecursive(AssetNode node)
        {
            int count = 1;
            foreach (var child in node.Children)
            {
                count += CountNodesRecursive(child);
            }
            return count;
        }
        // Replace the CountNodes method with a synchronous version
        public Task<int> CountNodes()
        {
            var root = _storage.LoadHierarchy();
            int count = CountNodesRecursive(root);
            return Task.FromResult(count);
        }

        //to add new hierarchy 
        public Task AddHierarchy(AssetNode node )
        {
            var tree = _storage.LoadHierarchy();
            if(tree.Children == null)
            {
                tree.Children = new List<AssetNode>();
            }

            int maxid = FindMaxId(tree);
            node.Id = maxid + 1;

            if(node.Children == null)
            {
                node.Children = new List<AssetNode>();
            }

            if (NodeExists(tree, node.Id, node.Name))
            {
                _logger.LogError($"A node with  {node.Name} already exists.");
                throw new InvalidOperationException($"A node with {node.Name} already exists.");
            }
            tree.Children.Add(node);
            _storage.SaveHierarchy(tree);
            return Task.CompletedTask;
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

        //Reorder node 

        public Task<string> ReorderNode(int id, int? newparentId)
        {
            throw new InvalidOperationException("Not implemeted for file type ");
        }

    }
}
