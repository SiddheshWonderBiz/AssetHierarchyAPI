namespace AssetHierarchyAPI.Models
{
    public class User
    {
        public int Id { get; set; } 

        
        public string Username { get; set; }

        public string UserEmail { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } = "Viewer";

    }
}
