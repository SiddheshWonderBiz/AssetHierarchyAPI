using AssetHierarchyAPI.Domain.Models;

namespace AssetHierarchyAPI.Domain.Interfaces
{
    public interface IHierarchyStorage
    {
        AssetNode LoadHierarchy();                        
        void SaveHierarchy(AssetNode root);               
                    
    }
}
