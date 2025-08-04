using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Models;
using System.Xml.Serialization;

namespace AssetHierarchyAPI.Services
{
    public class XmlHierarchyStorage : IHierarchyStorage
    {
        private readonly string filepath = "Data/hierarchy.xml";

        public AssetNode LoadHierarchy()
        {
            if (!File.Exists(filepath))
                return new AssetNode { Id = 1, Name = "Root", Children = new List<AssetNode>() };

            using var stream = new FileStream(filepath, FileMode.Open);
            var serializer = new XmlSerializer(typeof(AssetNode));
            return (AssetNode)serializer.Deserialize(stream);
        }

        public void SaveHierarchy(AssetNode root)
        {
            using var stream = new FileStream(filepath, FileMode.Create);
            var serializer = new XmlSerializer(typeof(AssetNode));
            serializer.Serialize(stream, root);
        }
    }
}
