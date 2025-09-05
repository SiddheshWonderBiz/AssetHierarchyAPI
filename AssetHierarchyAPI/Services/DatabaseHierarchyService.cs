using AssetHierarchyAPI.Data;
using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AssetHierarchyAPI.Services
{
    // Service responsible for handling hierarchy operations using Database (EF Core)
    public class DatabaseHierarchyService : IHierarchyService
    {
        private readonly ILoggingService _logger;
        private readonly IAssetNodeRepository _repository;

        public DatabaseHierarchyService(IAssetNodeRepository repository, ILoggingService logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // Load hierarchy tree from database
        public async Task<AssetNode> LoadHierarchy()
        {
            var allNodes = await _repository.GetAllAsync();

            // If DB is empty return a fresh Root node
            if (!allNodes.Any())
            {
                return new AssetNode { Id = 1, Name = "Root", Children = new List<AssetNode>() };
            }

            // Find the root node (ParentId is NULL)
            var root = allNodes.FirstOrDefault(n => n.ParentId == null);
            if (root == null)
            {
                return new AssetNode { Id = 1, Name = "Root", Children = new List<AssetNode>() };
            }

            // Recursively build tree from flat list
            BuildTree(root, allNodes);
            return root;
        }

        // Validation
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
                    !prop.Name.Equals("parent", StringComparison.OrdinalIgnoreCase) &&
                    !prop.Name.Equals("Signals", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(
                        $"Invalid property '{prop.Name}'. Only 'id', 'name', and 'children' are allowed."
                    );
                }
            }
        }

        // Helper function: Build parent-child relationship
        private void BuildTree(AssetNode parent, List<AssetNode> allNodes)
        {
            var children = allNodes.Where(n => n.ParentId == parent.Id).ToList();
            parent.Children = children;

            foreach (var child in children)
            {
                BuildTree(child, allNodes);
            }
        }

        // Add a new node under a parent
        public async Task AddNode(int parentId, AssetNode newNode)
        {
            if (string.IsNullOrWhiteSpace(newNode.Name))
                throw new ArgumentException("Node name cannot be empty.");

            // Ensure parent exists
            var parentExists = await _repository.ExistsAsync(parentId);
            if (!parentExists)
            {
                _logger.LogError($"Parent with ID {parentId} does not exist.");
                throw new KeyNotFoundException($"Parent with ID {parentId} does not exist.");
            }

            // Ensure node with same name doesn't exist
            var nodeExists = (await _repository.GetAllAsync()).Any(n => n.Name == newNode.Name);
            if (nodeExists)
            {
                _logger.LogError($"Node with name {newNode.Name} already exists.");
                throw new InvalidOperationException($"A node with name '{newNode.Name}' already exists.");
            }

            var nodeToAdd = new AssetNode
            {
                Name = newNode.Name,
                ParentId = parentId,
                Children = new List<AssetNode>()
            };

            await _repository.AddAsync(nodeToAdd);
            await _repository.SaveChangesAsync();
        }

        // Update node
        public async Task<bool> UpdateNodeName(int id, string newName)
        {
            var node = await _repository.GetByIdAsync(id);
            if (node == null)
            {
                return false;
            }

            var nodeExists = (await _repository.GetAllAsync()).Any(n => n.Name == newName && n.Id != id);
            if (nodeExists)
            {
                _logger.LogError($"Node with name {newName} already exists.");
                throw new InvalidOperationException($"A node with name {newName} already exists.");
            }

            node.Name = newName;
            await _repository.SaveChangesAsync();
            return true;
        }

        // Remove a node and all its descendants
        public async Task RemoveNode(int nodeId)
        {
            var allNodes = await _repository.GetAllAsync();

            if (nodeId == 1)
                throw new InvalidOperationException("Root Node can't be deleted.");

            var nodeToRemove = allNodes.FirstOrDefault(n => n.Id == nodeId);
            if (nodeToRemove == null)
                throw new KeyNotFoundException($"Node with ID {nodeId} not found.");

            var idsToRemove = GetAllDescendantIds(nodeId, allNodes);
            idsToRemove.Add(nodeId);

            var nodesToRemove = allNodes.Where(n => idsToRemove.Contains(n.Id)).ToList();
            await _repository.DeleteRangeAsync(nodesToRemove);
        }

        private List<int> GetAllDescendantIds(int parentId, List<AssetNode> allNodes)
        {
            var result = new List<int>();
            var children = allNodes.Where(n => n.ParentId == parentId).ToList();

            foreach (var child in children)
            {
                result.Add(child.Id);
                result.AddRange(GetAllDescendantIds(child.Id, allNodes));
            }

            return result;
        }

        public void AssignIds(AssetNode root, ref int currentId)
        {
            // Empty placeholder
        }

        // Count total number of nodes in DB
        public async Task<int> CountNodes()
        {
            var nodes = await _repository.GetAllAsync();
            return nodes.Count;
        }

        // Add a complete hierarchy (tree) to DB
        public async Task AddHierarchy(AssetNode node)
        {
            if (node == null)
                throw new ArgumentException("Hierarchy cannot be null.");

            var root = await _repository.GetRootAsync();
            if (root == null)
            {
                root = new AssetNode
                {
                    Name = "Root",
                    ParentId = null
                };
                await _repository.AddAsync(root);
                await _repository.SaveChangesAsync();
            }

            await AddNodeRecursively(node, root.Id);
            await _repository.SaveChangesAsync();
        }

        private async Task AddNodeRecursively(AssetNode node, int? parentId)
        {
            var entity = new AssetNode
            {
                Name = node.Name ?? "Unnamed Node",
                ParentId = parentId,
                Children = new List<AssetNode>()
            };

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    await AddNodeRecursively(child, entity.Id);
                }
            }
        }

        // Replace entire hierarchy with a new one
        public async Task ReplaceTree(AssetNode newRoot)
        {
            await _repository.BeginTransactionAsync();

            try
            {
                await _repository.ClearAsync();

                if (newRoot != null)
                {
                    await AddNodeRecursively(newRoot, null);
                }

                await _repository.CommitTransactionAsync();
            }
            catch
            {
                await _repository.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
