using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AssetHierarchyAPI.Models
{
    public class Signals
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int AssetId { get; set; }

        [ForeignKey("AssetId")]
        [JsonIgnore]
        public AssetNode Asset { get; set; }
        [Required]
        public string ValueType { get; set; } 
        public string? Description { get; set; }

    }
}
