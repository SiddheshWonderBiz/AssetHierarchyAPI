using AssetHierarchyAPI.Data;
using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetHierarchyAPI.Services
{
    public class DatabaseHierarchyStorage : IHierarchyStorage
    {
        private readonly AppDbContext _context;

        public DatabaseHierarchyStorage(AppDbContext context)
        {
            _context = context;
        }

        public AssetNode LoadHierarchy()
        {
            // Load all nodes from DB in one query
            var allNodes = _context.AssetNodes.AsNoTracking().ToList();

            if (!allNodes.Any())
            {
                // If no data, return empty root
                return new AssetNode { Id = 1, Name = "Root", Children = new List<AssetNode>() };
            }

            // Get root (the one with no parent)
            var root = allNodes.FirstOrDefault(n => n.ParentId == null);

            if (root == null)
            {
                // If no root found, create one
                return new AssetNode { Id = 1, Name = "Root", Children = new List<AssetNode>() };
            }

            BuildTree(root, allNodes);
            return root;
        }

        private void BuildTree(AssetNode parent, List<AssetNode> allNodes)
        {
            // Find all direct children of the current parent
            parent.Children = allNodes.Where(n => n.ParentId == parent.Id).ToList();

            // Recursively build the tree for each child
            foreach (var child in parent.Children)
            {
                BuildTree(child, allNodes);
            }
        }

        public void SaveHierarchy(AssetNode root)
        {
            // Clear existing data
            _context.AssetNodes.RemoveRange(_context.AssetNodes);
            _context.SaveChanges();

            // Reset identity counter to start from 1
            
                _context.Database.ExecuteSqlRaw("DBCC CHECKIDENT ('AssetNodes', RESEED, 0)");
            
            
            // Insert new hierarchy
            if (root != null)
            {
                InsertRecursively(root, null);
                _context.SaveChanges();
            }
        }

        private void InsertRecursively(AssetNode node, int? parentId)
        {
            var entity = new AssetNode
            {
                Name = node.Name,
                ParentId = parentId,
                Children = new List<AssetNode>()
            };

            _context.AssetNodes.Add(entity);
            _context.SaveChanges(); // Save to get the generated ID

            // Update the original node's ID for reference
            node.Id = entity.Id;

            // Recursively insert children
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    InsertRecursively(child, entity.Id);
                }
            }
        }
    }
}