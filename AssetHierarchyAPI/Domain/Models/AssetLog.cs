using System.ComponentModel.DataAnnotations;

namespace AssetHierarchyAPI.Domain.Models
{
    public class AssetLog
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        [Required]
        public string Action { get; set; } = string.Empty;

        public string? TargetName { get; set; }

        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }
}
