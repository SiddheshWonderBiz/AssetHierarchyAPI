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
        private readonly ISignalServices _signalService;

        public SignalsController(ISignalServices signalService)
        {
            _signalService = signalService;
        }

        // GET all signals for an asset
        [HttpGet("asset/{assetId}")]
        public async Task<ActionResult<IEnumerable<Signal>>> GetByAssetAsync(int assetId)
        {
            var signals = await _signalService.GetByAssetAsync(assetId);

            if (signals == null || !signals.Any())
                return NotFound(new { error = $"No signals found for asset {assetId}" });

            return Ok(signals);
        }

        // GET a single signal by id
        [HttpGet("signals/{id}")]
        public async Task<ActionResult<Signal>> GetByIdAsync(int id)
        {
            var signal = await _signalService.GetByIdAsync(id);
            if (signal == null)
                return NotFound(new { error = $"Signal {id} not found" });

            return Ok(signal);
        }

        // POST - add signal
        [HttpPost("asset/{assetId}/add")]
        [Authorize(Roles = "Admin,Viewer")]
        public async Task<IActionResult> CreateSignalAsync(int assetId, [FromBody] GlobalSignalDTO data)
        {
            try
            {
                var created = await _signalService.AddSignalAsync(assetId, data);
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
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Viewer")]
        public async Task<IActionResult> UpdateSignalAsync(int id, [FromBody] GlobalSignalDTO data)
        {
            try
            {
                var updated = await _signalService.UpdateSignalAsync(id, data);
                return Ok(new { message = "Signal updated successfully", success = updated });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // DELETE - delete signal
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Viewer")]
        public async Task<IActionResult> DeleteSignalAsync(int id)
        {
            try
            {
                var deleted = await _signalService.DeleteSignalAsync(id);
                return Ok(new { message = "Signal deleted successfully", success = deleted });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
