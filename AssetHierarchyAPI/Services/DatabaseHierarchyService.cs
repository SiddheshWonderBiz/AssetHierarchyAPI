using AssetHierarchyAPI.Data;
using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetHierarchyAPI.Services
{
    public class DatabaseHierarchyService : IHierarchyService
    {
        private readonly AppDbContext _context;

        public DatabaseHierarchyService(AppDbContext context)
        {
            _context = context;
        }

        public AssetNode LoadHierarchy()
        {
            try
            {
                var allNodes = _context.AssetNodes.AsNoTracking().ToList();

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
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to load hierarchy. Please try again later.", ex);
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

        public void AddNode(int parentId, AssetNode newNode)
        {
            if (string.IsNullOrWhiteSpace(newNode.Name))
                throw new ArgumentException("Node name cannot be empty.");

            try
            {
                var parentExists = _context.AssetNodes.Any(n => n.Id == parentId);
                if (!parentExists)
                    throw new KeyNotFoundException($"Parent with ID {parentId} does not exist.");

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

        public void RemoveNode(int nodeId)
        {
            try
            {
                var allNodes = _context.AssetNodes.ToList();
                var nodeToRemove = allNodes.FirstOrDefault(n => n.Id == nodeId);

                if (nodeToRemove == null)
                    throw new KeyNotFoundException($"Node with ID {nodeId} not found.");

                var idsToRemove = GetAllDescendantIds(nodeId, allNodes);
                idsToRemove.Add(nodeId);

                var nodesToRemove = _context.AssetNodes.Where(n => idsToRemove.Contains(n.Id)).ToList();
                _context.AssetNodes.RemoveRange(nodesToRemove);
                _context.SaveChanges();
            }
            catch (KeyNotFoundException)
            {
                throw; // Bubble up with friendly message
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to remove node. Please try again later.", ex);
            }
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
            root.Id = 0; // EF will generate
            if (root.Children != null)
            {
                foreach (var child in root.Children)
                {
                    AssignIds(child, ref currentId);
                }
            }
        }

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

        public void AddHierarchy(AssetNode node)
        {
            if (node == null)
                throw new ArgumentException("Hierarchy cannot be null.");

            try
            {
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

                AddNodeRecursively(node, root.Id);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to add hierarchy. Please try again later.", ex);
            }
        }

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

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    AddNodeRecursively(child, entity.Id);
                }
            }
        }

        public void ReplaceTree(AssetNode newRoot)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                try
                {
                    _context.Database.ExecuteSqlRaw("TRUNCATE TABLE AssetNodes");
                }
                catch
                {
                    _context.Database.ExecuteSqlRaw("DELETE FROM AssetNodes");
                    _context.Database.ExecuteSqlRaw("DBCC CHECKIDENT ('AssetNodes', RESEED, 0)");
                }

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
