using AssetHierarchyAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AssetHierarchyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigController : ControllerBase
    {
        private readonly StorageConfig _config ;

        public ConfigController(StorageConfig  config)
        {
            _config = config;
        }

        [HttpPost("Storage")]

        public IActionResult SetStorageType([FromBody ]string storageType) {
            if (storageType.ToLower() != "json" && storageType.ToLower() != "xml")
            {
                return BadRequest("Only 'json' or 'xml' are supported");
            }

            _config.StorageType = storageType.ToLower();
            return Ok($"Storage type set to {storageType}");
        }

    }
}
