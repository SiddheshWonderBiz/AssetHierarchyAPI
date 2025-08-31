using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Models;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace AssetHierarchyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SignalsController : Controller
    {
        private readonly ISignalServices _signals;

        public SignalsController(ISignalServices signal)
        {
            _signals = signal;
        }

        [HttpGet("asset/{assetId}")]
        public async Task<ActionResult<IEnumerable<Signals>>> GetSignals(int assetId)
        {
            var signals = _signals.GetByAsset(assetId);
            if (signals == null)
            {
                return NotFound();
            }
            return Ok(signals);
        }
        [HttpGet("asset/{assetId}/signals/{Id}")]
        public ActionResult Signals(int Id) { 
        var sig = _signals.GetById(Id);
            if (sig == null) return NotFound(new { error = $"Signal {Id} not found" });
            return Ok(sig);
        }
        [HttpPost("asset/{assetId}/Addsignal/")]
        public IActionResult CreateSignal(int assetId,[FromBody] GlobalSignalDTO data) {
            try
            {

                var created = _signals.AddSignal(assetId, data);
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

        [HttpPut("asset/{assetId}/{id}")]

        public IActionResult UpdateSignal(int id , [FromBody] GlobalSignalDTO data)
        {
            try
            {
                var update =_signals.UpdateSignal(id, data);
                return Ok(new { message = "Signal updated successfully", success = update });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("asset/{assetId}/{id}")]

        public IActionResult DeleteSignal(int id)
        {
            try
            {
                var delete = _signals.DeleteSignal(id);
                return Ok(new {message = "Signal Deleted Successfully" ,  success = delete});
            }catch (Exception ex) {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
    public class GlobalSignalDTO
    {
        public string Name { get; set; }
        public string ValueType { get; set; }
        public string? Description { get; set; }
    }
}
