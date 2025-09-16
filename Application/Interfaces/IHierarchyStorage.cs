using AssetHierarchyAPI.Domain.Models;

namespace AssetHierarchyAPI.Application.Interfaces
{
    public interface IHierarchyStorage
    {
        AssetNode LoadHierarchy();                        
        void SaveHierarchy(AssetNode root);               
                    
    }
}
