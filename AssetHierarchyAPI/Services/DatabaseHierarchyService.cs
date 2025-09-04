using AssetHierarchyAPI.Data;
using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Xml.Linq;

namespace AssetHierarchyAPI.Services
{
    // Service responsible for handling hierarchy operations using Database (EF Core)
    public class DatabaseHierarchyService : IHierarchyService
    {
        //private readonly AppDbContext _context; // EF DbContext for DB access
        private readonly ILoggingService _logger; // For logging errors
        private readonly IAssetNodeRepository _repository;

        public DatabaseHierarchyService(IAssetNodeRepository repository, ILoggingService logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // Load hierarchy tree from database
        public async Task<AssetNode> LoadHierarchy()
        {
            try
            {
                // Load all nodes from DB
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
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to load hierarchy. Please try again later.", ex);
            }
        }
        //Validation
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
                    !prop.Name.Equals("parent", StringComparison.OrdinalIgnoreCase)&&
                    !prop.Name.Equals("Signals", StringComparison.OrdinalIgnoreCase)


                    )
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
            // Find children of this parent
            var children = allNodes.Where(n => n.ParentId == parent.Id).ToList();
            parent.Children = children;

            // Recursively build children of each child
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

            try
            {
                // Ensure parent exists
                var parentExists = _repository.ExistsAsync(parentId).Result;
                if (!parentExists)
                {
                    _logger.LogError($"Parent with ID {parentId} does not exist.");
                    throw new KeyNotFoundException($"Parent with ID {parentId} does not exist.");
                }

                // Ensure node with same name doesn't exist
                var nodeExists = _repository.GetAllAsync().Result.Any(n => n.Name == newNode.Name);
                if (nodeExists)
                {
                    _logger.LogError($"Node with name {newNode.Name} already exists.");
                    throw new InvalidOperationException($"A node with name {newNode.Name} already exists.");
                }

                // Create and save new node
                var nodeToAdd = new AssetNode
                {
                    Name = newNode.Name,
                    ParentId = parentId,
                    Children = new List<AssetNode>()
                };

                _repository.AddAsync(nodeToAdd).Wait();
                _repository.SaveChangesAsync().Wait();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to add new node. Please try again later.", ex);
            }
        }
        //update node 
        public async Task<bool> UpdateNodeName(int id, string newName)
        {
            var node = await _repository.GetByIdAsync(id);
            if (node == null)
            {
                return false;
            }
            var nodeExists = _repository.GetAllAsync().Result.Any(n => n.Name == newName && n.Id != id);
            if (nodeExists)
            {
                _logger.LogError($"Node with name {newName} already exists.");
                throw new InvalidOperationException($"A node with name {newName} already exists.");
            }
            node.Name = newName;
            _repository.SaveChangesAsync().Wait();
            return true;
        }

        // Remove a node and all its descendants
        public async Task RemoveNode(int nodeId)
        {
            try
            {
                var allNodes = _repository.GetAllAsync().Result;
                if(nodeId == 1 )
                {
                    throw new InvalidOperationException("Root Node Can't be deleted ");
                }

                var nodeToRemove = allNodes.FirstOrDefault(n => n.Id == nodeId);

                if (nodeToRemove == null)
                    throw new KeyNotFoundException($"Node with ID {nodeId} not found.");

                // Get all descendant IDs
                var idsToRemove = GetAllDescendantIds(nodeId, allNodes);
                idsToRemove.Add(nodeId);

                // Remove nodes in one go
                var nodesToRemove = allNodes.Where(n => idsToRemove.Contains(n.Id)).ToList();
                _repository.DeleteRangeAsync(nodesToRemove);
            }
            catch (KeyNotFoundException)
            {
                throw; 
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to remove node. Please try again later.", ex);
            }
        }

        // Helper: Recursively collect IDs of all descendants of a node
        private List<int> GetAllDescendantIds(int parentId, List<AssetNode> allNodes)
        {
            var result = new List<int>();
            var children = allNodes.Where(n => n.ParentId == parentId).ToList();

            foreach (var child in children)
            {
                result.Add(child.Id);
                result.AddRange(GetAllDescendantIds(child.Id, allNodes)); // recurse
            }

            return result;
        }
        
        // Assign IDs (EF will do)
        public void AssignIds(AssetNode root, ref int currentId)
        {
            // Empty placeholder
        }

        // Count total number of nodes in DB
        public async Task<int> CountNodes()
        {
            try
            {
                var nodes = await _repository.GetAllAsync();
                return nodes.Count;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to count nodes.", ex);
            }
        }


        // Add a complete hierarchy (tree) to DB
        public async Task AddHierarchy(AssetNode node)
        {
            if (node == null)
                throw new ArgumentException("Hierarchy cannot be null.");

            try
            {
                // Ensure DB has a root, otherwise create one
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

                // Recursively add all children
                AddNodeRecursively(node, root.Id);
                await _repository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to add hierarchy. Please try again later.", ex);
            }
        }

        // Helper: Recursively add a node and its children
        private void AddNodeRecursively(AssetNode node, int? parentId)
        {
            var entity = new AssetNode
            {
                Name = node.Name ?? "Unnamed Node",
                ParentId = parentId,
                Children = new List<AssetNode>()
            };

            _repository.AddAsync(entity);
            _repository.SaveChangesAsync();

            // Recurse for children
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    AddNodeRecursively(child, entity.Id);
                }
            }
        }

        // Replace entire hierarchy with a new one
        public async Task ReplaceTree(AssetNode newRoot)
        {
            await _repository.BeginTransactionAsync();
            try
            {
               
                    // Fallback: delete all one by one  + reseed identity
                    await _repository.ClearAsync();

                // Insert new root + its children
                if (newRoot != null)
                {
                    AddNodeRecursively(newRoot, null);
                }

                await _repository.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _repository.RollbackTransactionAsync();
                throw new ApplicationException("Failed to replace hierarchy. Please try again later.", ex);
            }
        }
    }
}
