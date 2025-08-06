using Microsoft.AspNetCore.Mvc;
using AssetHierarchyAPI.Models;
using AssetHierarchyAPI.Interfaces;
using System.Text.Json;
using System.Globalization;

namespace AssetHierarchyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HierarchyController : ControllerBase
    {
        private readonly IHierarchyService _service;
        private readonly IWebHostEnvironment _env;

        public HierarchyController(IHierarchyService service , IWebHostEnvironment env )
        {
            _service = service;
            _env = env;
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

        [HttpPost("upload")]

        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0) {
                return BadRequest("File Invalid");
            }

            try
            {
                using var sr = new StreamReader(file.OpenReadStream());
                var data = await sr.ReadToEndAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };
                var NewTree = JsonSerializer.Deserialize<AssetNode>(data, options);
                if (NewTree == null || NewTree.Id == null) { 
                return BadRequest("Invalid Json file");
                }

                _service.ReplaceTree(NewTree);
                return Ok("File uploaded successfully");

            }
            catch (Exception ex)
            {
                return StatusCode(500 , new {   error = ex.Message });
            }
            
        }

        [HttpGet("download")]

        public async Task<ActionResult> DownloadFile()
        {
            string path = Path.Combine(_env.ContentRootPath, "Data/hierarchy.json");

            if (!System.IO.File.Exists(path) ){
                return NotFound("File doesnt exist");
            }

            var memory = new MemoryStream();

            using (var stream= new FileStream(path, FileMode.Open, FileAccess.Read)) { 
            
            await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, "application/json","Hierarchy.json");
        }
      

    }
}
