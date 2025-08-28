using AssetHierarchyAPI.Data;
using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetHierarchyAPI.Services
{
    // Service responsible for handling hierarchy operations using Database (EF Core)
    public class DatabaseHierarchyService : IHierarchyService
    {
        private readonly AppDbContext _context; // EF DbContext for DB access
        private readonly ILoggingService _logger; // For logging errors

        public DatabaseHierarchyService(AppDbContext context, ILoggingService logger)
        {
            _context = context;
            _logger = logger;
        }

        // Load hierarchy tree from database
        public AssetNode LoadHierarchy()
        {
            try
            {
                // Load all nodes from DB
                var allNodes = _context.AssetNodes.AsNoTracking().ToList();

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
        public void AddNode(int parentId, AssetNode newNode)
        {
            if (string.IsNullOrWhiteSpace(newNode.Name))
                throw new ArgumentException("Node name cannot be empty.");

            try
            {
                // Ensure parent exists
                var parentExists = _context.AssetNodes.Any(n => n.Id == parentId);
                if (!parentExists)
                {
                    _logger.LogError($"Parent with ID {parentId} does not exist.");
                    throw new KeyNotFoundException($"Parent with ID {parentId} does not exist.");
                }

                // Ensure node with same name doesn't exist
                var nodeExists = _context.AssetNodes.Any(n => n.Name == newNode.Name);
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

                _context.AssetNodes.Add(nodeToAdd);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to add new node. Please try again later.", ex);
            }
        }
        //update node 
        public bool UpdateNodeName(int id, string newName)
        {
            var node = _context.AssetNodes.FirstOrDefault(n => n.Id == id);
            if (node == null)
            {
                return false;
            }
            node.Name = newName;
            _context.SaveChanges();
            return true;
        }

        // Remove a node and all its descendants
        public void RemoveNode(int nodeId)
        {
            try
            {
                var allNodes = _context.AssetNodes.ToList();
                var nodeToRemove = allNodes.FirstOrDefault(n => n.Id == nodeId);

                if (nodeToRemove == null)
                    throw new KeyNotFoundException($"Node with ID {nodeId} not found.");

                // Get all descendant IDs
                var idsToRemove = GetAllDescendantIds(nodeId, allNodes);
                idsToRemove.Add(nodeId);

                // Remove nodes in one go
                var nodesToRemove = _context.AssetNodes.Where(n => idsToRemove.Contains(n.Id)).ToList();
                _context.AssetNodes.RemoveRange(nodesToRemove);
                _context.SaveChanges();
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
        public int CountNodes(AssetNode node)
        {
            try
            {
                return _context.AssetNodes.Count();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to count nodes.", ex);
            }
        }

        // Add a complete hierarchy (tree) to DB
        public void AddHierarchy(AssetNode node)
        {
            if (node == null)
                throw new ArgumentException("Hierarchy cannot be null.");

            try
            {
                // Ensure DB has a root, otherwise create one
                var root = _context.AssetNodes.FirstOrDefault(n => n.ParentId == null);
                if (root == null)
                {
                    root = new AssetNode
                    {
                        Name = "Root",
                        ParentId = null
                    };
                    _context.AssetNodes.Add(root);
                    _context.SaveChanges();
                }

                // Recursively add all children
                AddNodeRecursively(node, root.Id);
                _context.SaveChanges();
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

            _context.AssetNodes.Add(entity);
            _context.SaveChanges();

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
        public void ReplaceTree(AssetNode newRoot)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                try
                {
                    // Try truncate table (faster, resets IDs)
                    _context.Database.ExecuteSqlRaw("TRUNCATE TABLE AssetNodes");
                }
                catch
                {
                    // Fallback: delete all one by one  + reseed identity
                    _context.Database.ExecuteSqlRaw("DELETE FROM AssetNodes");
                    _context.Database.ExecuteSqlRaw("DBCC CHECKIDENT ('AssetNodes', RESEED, 0)");
                }

                // Insert new root + its children
                if (newRoot != null)
                {
                    AddNodeRecursively(newRoot, null);
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new ApplicationException("Failed to replace hierarchy. Please try again later.", ex);
            }
        }
    }
}
