using Newtonsoft.Json;

namespace AssetHierarchyAPI.Models
{
    public class AssetNode
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public List<AssetNode> Children { get; set; } = new List<AssetNode>();
    }
}
