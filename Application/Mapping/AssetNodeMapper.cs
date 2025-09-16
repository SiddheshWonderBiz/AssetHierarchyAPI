using AssetHierarchyAPI.Application.DTOs;
using AssetHierarchyAPI.Domain.Models;

namespace AssetHierarchyAPI.Application.Mapping
{
    public static class AssetNodeMapper
    {
        public static AssetNode MapToDomain(AssetNodeXmlDto dto, AssetNode? parent = null)
        {
            var node = new AssetNode
            {
                Name = dto.Name,
                Parent = parent,
                Children = new List<AssetNode>()
            };

            foreach (var childDto in dto.Children)
            {
                var child = MapToDomain(childDto, node);
                node.Children.Add(child);
            }

            return node;
        }

        public static AssetNodeXmlDto MapToXmlDto(AssetNode domainNode)
        {
            return new AssetNodeXmlDto
            {
                Name = domainNode.Name,
                Children = domainNode.Children.Select(MapToXmlDto).ToList()
            };
        }
    }
}
