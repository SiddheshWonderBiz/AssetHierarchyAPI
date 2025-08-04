using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Models;
using System.Text.Json;

namespace AssetHierarchyAPI.Services
{
    public class JsonHierarchyStorage : IHierarchyStorage
    {
        private readonly string filePath = "Data/hierarchy.json";

        public AssetNode LoadHierarchy()
        {
            if (!File.Exists(filePath)) { 
            return new AssetNode { Id = 1 ,Name = "Root", Children = new List<AssetNode>() };
            }
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<AssetNode>(json);

        }
        public void SaveHierarchy(AssetNode root)
        {
            var json = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }
}
