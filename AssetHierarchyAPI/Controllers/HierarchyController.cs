using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;
using System.Xml.Serialization;

namespace AssetHierarchyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HierarchyController : ControllerBase
    {
        private readonly IHierarchyService _service;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public HierarchyController(IHierarchyService service , IWebHostEnvironment env  , IConfiguration configuration)
        {
            _service = service;
            _env = env;
            _configuration = configuration;
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
            if (file == null || file.Length == 0)
            {
                return BadRequest("File Invalid");
            }

            try
            {
                using var sr = new StreamReader(file.OpenReadStream());
                var data = await sr.ReadToEndAsync();

                AssetNode newTree;

                // Get storage type from config
                var storageType = _configuration["StorageType"];

                if (storageType == "XML")
                {
                    var serializer = new XmlSerializer(typeof(AssetNode));
                    using var reader = new StringReader(data);
                    newTree = (AssetNode)serializer.Deserialize(reader);
                }
                else // JSON
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    };
                    newTree = JsonSerializer.Deserialize<AssetNode>(data, options);
                }

                if (newTree == null)
                {
                    return BadRequest($"Invalid {storageType} file");
                }

                _service.ReplaceTree(newTree);
                return Ok("File uploaded successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
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
