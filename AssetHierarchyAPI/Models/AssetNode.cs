using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetHierarchyAPI.Models
{
    public class AssetNode
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int? ParentId { get; set; }

        [ForeignKey("ParentId")]
        public AssetNode? Parent { get; set; }

        public List<AssetNode> Children { get; set; } = new List<AssetNode>();
    }
}
