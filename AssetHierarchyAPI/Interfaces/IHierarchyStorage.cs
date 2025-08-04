using AssetHierarchyAPI.Models;

namespace AssetHierarchyAPI.Interfaces
{
    public interface IHierarchyStorage
    {
        AssetNode LoadHierarchy();                        
        void SaveHierarchy(AssetNode root);               
                    
    }
}
