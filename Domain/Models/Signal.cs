using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AssetHierarchyAPI.Domain.Models
{
    public class Signal
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int AssetId { get; set; }
        [JsonIgnore]
        public AssetNode? Asset { get; set; }
        public string ValueType { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
