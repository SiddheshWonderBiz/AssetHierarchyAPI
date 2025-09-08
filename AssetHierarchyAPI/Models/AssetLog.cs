namespace AssetHierarchyAPI.Models
{
    public class AssetLog
    {
        public int Id { get; set; }
     
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Action {  get; set; } = string.Empty;
        public string? TargetName { get; set; }

        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

    }
}
