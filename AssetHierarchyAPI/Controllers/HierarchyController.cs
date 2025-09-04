using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;
using System.Xml.Serialization;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Microsoft.AspNetCore.Authorization;


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
        public async Task<IActionResult> GetHierarchy()
        {
            try
            {
                var tree = await _service.LoadHierarchy();
                int totalNodes = await _service.CountNodes();
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddHierarchy([FromBody] AssetNode newHierarchy)
        {
            try
            {
                if (newHierarchy == null)
                    return BadRequest(new { error = "Invalid hierarchy data" });

                await _service.AddHierarchy(newHierarchy);
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddNode(int parentId, [FromBody] AssetNode newNode)
        {
            try
            {
                if (newNode == null)
                    return BadRequest(new { error = "Invalid node data" });

                if (string.IsNullOrWhiteSpace(newNode.Name))
                    return BadRequest(new { error = "Node name is required" });

                newNode.Id = 0;
                newNode.Children ??= new List<AssetNode>();

                await _service.AddNode(parentId, newNode);
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateNode(int id, [FromBody] string newName)
        {
            try
            {
                var update = await _service.UpdateNodeName(id, newName);
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
                return Conflict(ex.Message);
            }
        }



        [HttpDelete("remove/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveNode(int id)
        {
            try
            {
                await _service.RemoveNode(id);
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
        [Authorize(Roles = "Admin")]
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
                    // ✅ Strict JSON validation BEFORE deserialization
                    using var jsonDoc = JsonDocument.Parse(data);
                    _service.ValidateNode(jsonDoc.RootElement);

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

                // For database storage, EF handles IDs
                var dbStorageType = _configuration["StorageType"];
                if (!dbStorageType.Equals("DB", StringComparison.OrdinalIgnoreCase))
                {
                    int idCounter = 1;
                    _service.AssignIds(newTree, ref idCounter);
                }

                await _service.ReplaceTree(newTree);
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
        [Authorize(Roles = "Admin,Viewer")]
        public async Task<IActionResult> DownloadFile()
        {
            var storageType = _configuration["StorageType"] ?? "JSON";

            try
            {
                if (storageType.Equals("DB", StringComparison.OrdinalIgnoreCase))
                {
                    var tree = await _service.LoadHierarchy();
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