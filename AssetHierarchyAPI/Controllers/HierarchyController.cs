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
            try
            {
                var tree = _service.LoadHierarchy();
                return Ok(tree);
            }
            catch (Exception ex) {
                return StatusCode(500, ex.Message);
            }
        }

        
        [HttpPost("add")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public IActionResult AddNode(int parentId, [FromBody] AssetNode newNode)
        {
            try
            {
                _service.AddNode(parentId, newNode);
                return Ok("Node added successfully");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex) { 
                return StatusCode(500 , new {error = "Unexpected error occured "});
            }
        }

       
        [HttpDelete("remove/{id}")]
        public IActionResult RemoveNode(int id)
        {
            try
            {
                _service.RemoveNode(id);
                return Ok("Node removed successfully");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
