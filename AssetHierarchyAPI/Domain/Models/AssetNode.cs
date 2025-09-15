using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AssetHierarchyAPI.Domain.Models
{
    public class AssetNode
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int? ParentId { get; set; }

        [ForeignKey("ParentId"), JsonIgnore]
        public AssetNode? Parent { get; set; }

        [InverseProperty("Parent")]
        public List<AssetNode> Children { get; set; } = new();

        [JsonIgnore]
        public List<Signal> Signals { get; set; } = new();
    }
}
