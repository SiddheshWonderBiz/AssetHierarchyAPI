using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;
using System.Xml.Serialization;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace AssetHierarchyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HierarchyController : ControllerBase
    {
        private readonly IHierarchyService _service;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public HierarchyController(IHierarchyService service, IWebHostEnvironment env, IConfiguration configuration)
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
                int totalNodes = _service.CountNodes(tree);
                return Ok(new { tree, totalNodes });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Unexpected error occurred: " + ex.Message });
            }
        }

        [HttpPost("addhierarchy")]
        public IActionResult AddHierarchy([FromBody] AssetNode newHierarchy)
        {
            try
            {
                if (newHierarchy == null)
                {
                    return BadRequest(new { error = "Invalid hierarchy data" });
                }

                _service.AddHierarchy(newHierarchy);
                return Ok(new { message = "Hierarchy added successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Unexpected error occurred: " + ex.Message });
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
                if (newNode == null)
                {
                    return BadRequest(new { error = "Invalid node data" });
                }

                if (string.IsNullOrWhiteSpace(newNode.Name))
                {
                    return BadRequest(new { error = "Node name is required" });
                }

                // Reset the ID to 0 to let EF auto-generate it
                newNode.Id = 0;

                // Initialize children if null
                if (newNode.Children == null)
                {
                    newNode.Children = new List<AssetNode>();
                }

                _service.AddNode(parentId, newNode);
                return Ok(new { message = "Node added successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Unexpected error occurred: " + ex.Message });
            }
        }

        [HttpPut("update/{id}")]
        public IActionResult UpdateNode(int id, [FromBody] string newName)
        {
            try
            {
                var update = _service.UpdateNodeName(id, newName);
                if (!update)
                    return NotFound($"Node with id {id} not found");

                return Ok($"Node {id} updated to '{newName}' successfully.");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message); // duplicate name
            }
        }



        [HttpDelete("remove/{id}")]
        public IActionResult RemoveNode(int id)
        {
            try
            {
                _service.RemoveNode(id);
                return Ok(new { message = "Node removed successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Unexpected error occurred: " + ex.Message });
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "File is invalid or empty" });
            }

            try
            {
                using var sr = new StreamReader(file.OpenReadStream());
                var data = await sr.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(data))
                {
                    return BadRequest(new { error = "File content is empty" });
                }

                AssetNode newTree;
                var storageType = _configuration["StorageType"] ?? "JSON";

                if (storageType.Equals("XML", StringComparison.OrdinalIgnoreCase))
                {
                    var serializer = new XmlSerializer(typeof(AssetNode));
                    using var reader = new StringReader(data);
                    newTree = (AssetNode?)serializer.Deserialize(reader);
                }
                else
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    };
                    newTree = JsonSerializer.Deserialize<AssetNode>(data, options);
                }

                if (newTree == null)
                {
                    return BadRequest(new { error = $"Invalid {storageType} file format" });
                }

                // Initialize children collection if null
                InitializeChildren(newTree);

                // For database storage, we don't need to manually assign IDs
                // EF will handle ID generation
                var dbStorageType = _configuration["StorageType"];
                if (!dbStorageType.Equals("DB", StringComparison.OrdinalIgnoreCase))
                {
                    int idCounter = 1;
                    _service.AssignIds(newTree, ref idCounter);
                }

                _service.ReplaceTree(newTree);

                return Ok(new { message = "Hierarchy updated successfully" });
            }
            catch (JsonException ex)
            {
                return BadRequest(new { error = "Invalid JSON format: " + ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = "Invalid XML format: " + ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Unexpected error occurred: " + ex.Message });
            }
        }

        private void InitializeChildren(AssetNode node)
        {
            if (node.Children == null)
            {
                node.Children = new List<AssetNode>();
            }

            foreach (var child in node.Children)
            {
                InitializeChildren(child);
            }
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadFile()
        {
            var storageType = _configuration["StorageType"] ?? "JSON";

            try
            {
                if (storageType.Equals("DB", StringComparison.OrdinalIgnoreCase))
                {
                    var tree = _service.LoadHierarchy();
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    var jsonData = JsonSerializer.Serialize(tree, options);
                    var memory = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonData));
                    memory.Position = 0;

                    return File(memory, "application/json", "Hierarchy.json");
                }
                else
                {
                    string path = Path.Combine(_env.ContentRootPath, "Data/hierarchy.json");

                    if (!System.IO.File.Exists(path))
                    {
                        return NotFound(new { error = "File doesn't exist" });
                    }

                    var memory = new MemoryStream();
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        await stream.CopyToAsync(memory);
                    }
                    memory.Position = 0;

                    return File(memory, "application/json", "Hierarchy.json");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Unexpected error occurred: " + ex.Message });
            }
        }
    }
}