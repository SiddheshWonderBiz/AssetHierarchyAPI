using AssetHierarchyAPI.Data;
using AssetHierarchyAPI.Hubs;
using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AssetHierarchyAPI.Services
{
    // Service responsible for handling hierarchy operations using Database (EF Core)
    public class DatabaseHierarchyService : IHierarchyService
    {
        private readonly ILoggingService _logger;       // for technical logs (errors, exceptions)
        private readonly IAssetNodeRepository _repository;
        private readonly ILoggingServiceDb _loggerDb;   // for user action logs (audit trail)
        private readonly IHubContext<NotificationHub> _hubContext;
        public DatabaseHierarchyService(
            IAssetNodeRepository repository,
            ILoggingService logger,
            ILoggingServiceDb loggerDb ,
            IHubContext<NotificationHub> hubContext)
        {
            _repository = repository;
            _logger = logger;
            _loggerDb = loggerDb;
            _hubContext = hubContext;
        }

        // Load hierarchy tree from database
        public async Task<AssetNode> LoadHierarchy()
        {
            var allNodes = await _repository.GetAllAsync();

            if (!allNodes.Any())
            {
                return new AssetNode { Id = 1, Name = "Root", Children = new List<AssetNode>() };
            }

            var root = allNodes.FirstOrDefault(n => n.ParentId == null);
            if (root == null)
            {
                return new AssetNode { Id = 1, Name = "Root", Children = new List<AssetNode>() };
            }

            BuildTree(root, allNodes);
            return root;
        }

        // Validation
        public void ValidateNode(JsonElement element)
        {
            var nameProp = element.EnumerateObject()
                                  .FirstOrDefault(p => p.Name.Equals("name", StringComparison.OrdinalIgnoreCase));

            if (nameProp.Value.ValueKind == JsonValueKind.Undefined || nameProp.Value.ValueKind != JsonValueKind.String)
            {
                throw new ArgumentException("Each node must have a string 'name'.");
            }

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
                    ValidateNode(child);
                }
            }

            foreach (var prop in element.EnumerateObject())
            {
                if (!prop.Name.Equals("id", StringComparison.OrdinalIgnoreCase) &&
                    !prop.Name.Equals("name", StringComparison.OrdinalIgnoreCase) &&
                    !prop.Name.Equals("children", StringComparison.OrdinalIgnoreCase) &&
                    !prop.Name.Equals("parentId", StringComparison.OrdinalIgnoreCase) &&
                    !prop.Name.Equals("parent", StringComparison.OrdinalIgnoreCase) &&
                    !prop.Name.Equals("Signals", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"Invalid property '{prop.Name}'.");
                }
            }
        }
        
        private void BuildTree(AssetNode parent, List<AssetNode> allNodes)
        {
            var children = allNodes.Where(n => n.ParentId == parent.Id).ToList();
            parent.Children = children;

            foreach (var child in children)
            {
                BuildTree(child, allNodes);
            }
        }

        // Add a new node
        public async Task AddNode(int parentId, AssetNode newNode)
        {
            if (string.IsNullOrWhiteSpace(newNode.Name))
                throw new ArgumentException("Node name cannot be empty.");
            string pattern = @"^[a-zA-Z0-9_\-\s]+$";
            bool isvalid = Regex.IsMatch(newNode.Name, pattern);
            if (!isvalid)
            {
                throw new ArgumentException("Invalid name pattern allowed only a-z,1-9 and -_");
            }

            var parent = await _repository.GetByIdAsync(parentId);
            if (parent == null)
            {
                _logger.LogError($"Parent with ID {parentId} does not exist.");
                throw new KeyNotFoundException($"Parent with ID {parentId} does not exist.");
            }

            var siblings = (await _repository.GetAllAsync()).Where(n => n.ParentId == parentId);

            if (siblings.Any(n => n.Name.Equals(newNode.Name, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogError($"Node with name {newNode.Name} already exists under parent {parentId}.");
                throw new InvalidOperationException($"A node with name '{newNode.Name}' already exists under this parent.");
            }
            var nodeToAdd = new AssetNode
            {
                Name = newNode.Name,
                ParentId = parentId,
                Children = new List<AssetNode>()
            };

            await _repository.AddAsync(nodeToAdd);
            await _repository.SaveChangesAsync();

            await _loggerDb.LogsActionsAsync("Add Node", newNode.Name);
            await _hubContext.Clients.All.SendAsync("nodeAdded", $"New node {newNode.Name} added under parent {parent.Name}");

        }

        // Update node
        public async Task<bool> UpdateNodeName(int id, string newName)
        {
            var node = await _repository.GetByIdAsync(id);
            if (node == null) return false;

            string pattern = @"^[a-zA-Z0-9_\-\s]+$";
            bool isvalid = Regex.IsMatch(newName, pattern);
            if (!isvalid)
            {
                throw new ArgumentException("Invalid name pattern allowed only a-z,1-9 and -_");
            }

            var nodeExists = (await _repository.GetAllAsync()).Any(n => n.Name == newName && n.Id != id);
            if (nodeExists)
            {
                _logger.LogError($"Node with name {newName} already exists.");
                throw new InvalidOperationException($"A node with name {newName} already exists.");
            }

            node.Name = newName;
            await _repository.SaveChangesAsync();

            await _loggerDb.LogsActionsAsync("Update Node", newName);
            return true;
        }

        // Remove node
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

            await _loggerDb.LogsActionsAsync("Remove Node", nodeToRemove.Name);
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
            //EF will do automatically
        }

        public async Task<int> CountNodes()
        {
            var nodes = await _repository.GetAllAsync();
            return nodes.Count;
        }

        // Add complete hierarchy
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
            string pattern = @"^[a-zA-Z0-9_\-\s]+$";
            bool isvalid = Regex.IsMatch(node.Name, pattern);
            if (!isvalid)
            {
                throw new ArgumentException("Invalid name pattern allowed only a-z,1-9 and -_");
            }
            await AddNodeRecursively(node, root.Id);
            await _repository.SaveChangesAsync();

            await _loggerDb.LogsActionsAsync("Add Hierarchy", node.Name ?? "Unnamed Hierarchy");
        }

        private async Task AddNodeRecursively(AssetNode node, int? parentId)
        {

            if (string.IsNullOrWhiteSpace(node.Name))
            {
                throw new ArgumentException("Node name cannot be empty.");
            }
            
            if (parentId.HasValue)
            {
                var parent = await _repository.GetByIdAsync(parentId.Value);
                if(parent != null && parent.ParentId == null)
                {
                    var siblings = (await _repository.GetAllAsync()).Any(n => n.ParentId == parentId && n.Name.Equals(node.Name, StringComparison.OrdinalIgnoreCase));
                    if (siblings)
                    {
                        throw new InvalidOperationException(
                            $"A hierarchy with the name '{node.Name}' already exists under root."
                        );
                    }
                }
                
            }
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

        // Replace full tree
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

                await _loggerDb.LogsActionsAsync("Uploaded file ", newRoot?.Name ?? "Root");
            }
            catch
            {
                await _repository.RollbackTransactionAsync();
                throw;
            }
        }

        //Reorrder 
        public async Task<string> ReorderNode(int Id, int? newparentId)
        {
            var node = await _repository.GetByIdAsync(Id);
            if (node == null)
            {
                throw new ArgumentException($"Node with ID {Id} doesn't exist");
            }

            if (newparentId == Id)
            {
                throw new ArgumentException("A node cannot be its own parent");
            }

            if (newparentId != 1 && newparentId != null)
            {
                var newParent = await _repository.GetByIdAsync(newparentId.Value);
                if (newParent == null)
                {
                    throw new ArgumentException($"New parent with ID {newparentId} does not exist");
                }

                // Check circular reference
                if (await IsDescendant(Id, newparentId.Value))
                    throw new InvalidOperationException("Invalid move: cannot assign descendant as parent.");

                // ------------------ Check for duplicate name ------------------
                var siblings = await _repository.GetAllAsync();
                var childrenOfNewParent = siblings.Where(n => n.ParentId == newparentId.Value);
                if (childrenOfNewParent.Any(c => c.Name.Equals(node.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"Cannot move node '{node.Name}' under parent '{newParent.Name}' because a child with the same name already exists.");
                }
            }

            node.ParentId = (newparentId == null || newparentId == 1) ? 1 : newparentId;
            await _repository.SaveChangesAsync();
            await _loggerDb.LogsActionsAsync("Reorder node", node.Name);
            return "Reordered the node successfully";
        }

        private async Task<bool> IsDescendant(int nodeId, int potentialParentId)
        {
            var allNodes = await _repository.GetAllAsync();
            var descendants = GetAllDescendantIds(nodeId, allNodes);
            return descendants.Contains(potentialParentId);
        }

    }
}
