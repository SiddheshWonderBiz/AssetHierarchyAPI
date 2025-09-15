using System.ComponentModel.DataAnnotations;

namespace AssetHierarchyAPI.Domain.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? Password { get; set; }

        [Required]
        public string Role { get; set; } = "Viewer";
    }
}
