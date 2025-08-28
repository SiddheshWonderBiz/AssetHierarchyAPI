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
            var root = (AssetNode)serializer.Deserialize(stream);

            // Re-assign parentIds after loading
            AssignParentIds(root, null);

            return root;
        }

        public void SaveHierarchy(AssetNode root)
        {
            // Ensure parentIds are set before saving
            AssignParentIds(root, null);

            using var stream = new FileStream(filepath, FileMode.Create);
            var serializer = new XmlSerializer(typeof(AssetNode));
            serializer.Serialize(stream, root);
        }

        private void AssignParentIds(AssetNode node, int? parentId)
        {
            node.ParentId = parentId;

            foreach (var child in node.Children)
            {
                AssignParentIds(child, node.Id);
            }
        }
    }
}
