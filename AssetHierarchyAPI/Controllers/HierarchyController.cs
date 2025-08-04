using Microsoft.AspNetCore.Mvc;
using AssetHierarchyAPI.Models;
using AssetHierarchyAPI.Interfaces;

namespace AssetHierarchyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HierarchyController : ControllerBase
    {
        private readonly IHierarchyService _service;

        public HierarchyController(IHierarchyService service)
        {
            _service = service;
        }

        
        [HttpGet]
        public IActionResult GetHierarchy()
        {
            var tree = _service.LoadHierarchy();
            return Ok(tree);
        }

        
        [HttpPost("add")]
        public IActionResult AddNode(int parentId, [FromBody] AssetNode newNode)
        {
            _service.AddNode(parentId, newNode);
            return Ok("Node added successfully");
        }

       
        [HttpDelete("remove/{id}")]
        public IActionResult RemoveNode(int id)
        {
            _service.RemoveNode(id);
            return Ok("Node removed successfully");
        }
    }
}
