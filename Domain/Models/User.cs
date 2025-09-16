using System.ComponentModel.DataAnnotations;

namespace AssetHierarchyAPI.Domain.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string Role { get; set; } = "Viewer";
    }
}
