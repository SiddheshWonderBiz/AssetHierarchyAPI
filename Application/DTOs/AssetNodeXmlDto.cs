using System.Xml.Serialization;

namespace AssetHierarchyAPI.Application.DTOs
{
    [XmlRoot("AssetNode")]
    public class AssetNodeXmlDto
    {
        [XmlElement("Name")]
        public string Name { get; set; } = string.Empty;

        [XmlArray("Children")]
        [XmlArrayItem("AssetNode")]
        public List<AssetNodeXmlDto> Children { get; set; } = new();
    }
}
