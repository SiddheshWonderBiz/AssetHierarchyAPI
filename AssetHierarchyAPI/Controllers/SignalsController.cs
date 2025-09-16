using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AssetHierarchyAPI.Domain.Models;
using AssetHierarchyAPI.Application.Interfaces;
using AssetHierarchyAPI.Application.DTOs;


namespace AssetHierarchyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SignalsController : ControllerBase
    {
        private readonly ISignalRepository _signals;

        public SignalsController(ISignalRepository signals)
        {
            _signals = signals;
        }

        // GET all signals for an asset
        [HttpGet("asset/{assetId}")]
        public async Task<ActionResult<IEnumerable<Signal>>> GetByAssetAsync(int assetId)
        {
            var signals = await _signals.GetByAssetAsync(assetId);

            if (signals == null || !signals.Any())
                return NotFound(new { error = $"No signals found for asset {assetId}" });

            return Ok(signals);
        }

        // GET a single signal by id
        [HttpGet("asset/{assetId}/signals/{id}")]
        public async Task<ActionResult<Signal>> GetByIdAsync(int assetId, int id)
        {
            var sig = await _signals.GetByIdAsync(id);
            if (sig == null)
                return NotFound(new { error = $"Signal {id} not found" });

            return Ok(sig);
        }

        // POST - add signal
        [HttpPost("asset/{assetId}/Addsignal")]
        [Authorize(Roles = "Admin,Viewer")]
        public async Task<IActionResult> CreateSignalAsync(int assetId, [FromBody] GlobalSignalDTO data)
        {
            try
            {
                var created = await _signals.AddSignalAsync(assetId, data);
                return Ok(new
                {
                    message = "Created Signal Successfully",
                    signal = created
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // PUT - update signal
        [HttpPut("asset/{assetId}/{id}")]
        [Authorize(Roles = "Admin,Viewer")]
        public async Task<IActionResult> UpdateSignalAsync(int id, [FromBody] GlobalSignalDTO data)
        {
            try
            {
                var updated = await _signals.UpdateSignalAsync(id, data);
                return Ok(new { message = "Signal updated successfully", success = updated });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // DELETE - delete signal
        [HttpDelete("asset/{assetId}/{id}")]
        [Authorize(Roles = "Admin,Viewer")]
        public async Task<IActionResult> DeleteSignalAsync(int id)
        {
            try
            {
                var deleted = await _signals.DeleteSignalAsync(id);
                return Ok(new { message = "Signal deleted successfully", success = deleted });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }


}
