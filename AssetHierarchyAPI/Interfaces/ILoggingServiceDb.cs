namespace AssetHierarchyAPI.Interfaces
{
    public interface ILoggingServiceDb
    {
        Task LogsActionsAsync(string action , string? targetname );
    }
}
