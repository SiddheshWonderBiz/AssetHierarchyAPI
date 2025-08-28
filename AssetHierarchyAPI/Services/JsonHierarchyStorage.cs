using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Models;
//using System.Text.Json;
using Newtonsoft.Json;

namespace AssetHierarchyAPI.Services
{
    public class JsonHierarchyStorage : IHierarchyStorage
    {
        private readonly string filePath = "Data/hierarchy.json";

        public AssetNode LoadHierarchy()
        {
            if (!File.Exists(filePath))
            {
                return new AssetNode { Id = 1, Name = "Root", Children = new List<AssetNode>() };
            }

            var json = File.ReadAllText(filePath);
            var root = JsonConvert.DeserializeObject<AssetNode>(json);

            // Re-assign parentIds after loading
            AssignParentIds(root, null);

            return root;
        }
        public void SaveHierarchy(AssetNode root)
        {
            var json = JsonConvert.SerializeObject(root, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        private void AssignParentIds(AssetNode node, int? parentId)
        {
            node.ParentId = parentId;
            foreach (var child in node.Children)
            {
                AssignParentIds(child, node.Id);
            }
        }

    }
}
