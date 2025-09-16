using System.Text.Json.Serialization;

namespace AssetHierarchyAPI.Domain.Models
{
    public class AssetNode
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public int? ParentId { get; set; }
        [JsonIgnore]
        public AssetNode? Parent { get; set; }
        public ICollection<AssetNode> Children { get; set; } = new List<AssetNode>();
        public ICollection<Signal> Signals { get; set; } = new List<Signal>();
    }
}
