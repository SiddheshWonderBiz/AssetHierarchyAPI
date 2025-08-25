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
            var root = _context.AssetNodes
                .Include(n => n.Children)
                .FirstOrDefault(n => n.ParentId == null);
            return root ?? new AssetNode { Id = 1 , Name = "root" , Children = new List<AssetNode>() };
        }
        public void SaveHierarchy(AssetNode root)
        {
            _context.AssetNodes.RemoveRange(_context.AssetNodes);  // clear old hierarchy
            _context.SaveChanges();

            InsertRecursively(root, null);   // insert root + children
            _context.SaveChanges();
        }
        public void InsertRecursively(AssetNode node , int? parentId)
        {
            var entity = new AssetNode
            {
                Name = node.Name,
                ParentId = parentId,
            };
            _context.AssetNodes.Add(entity);
            _context.SaveChanges();
            foreach(var child in node.Children)
            {
                InsertRecursively(child, entity.Id);
            }
        }
    }
}
