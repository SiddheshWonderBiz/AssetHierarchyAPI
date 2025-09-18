using Application.Interfaces;
using Application.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace AssetHierarchyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CalculationController : ControllerBase
    {
        private readonly ICalculationQueue _queue;

        public CalculationController(ICalculationQueue queue)
        {
            _queue = queue;
        }

        [HttpPost("avg")]
        public IActionResult CalculateAvg([FromQuery] string colname)
        {
            _queue.Enqueue(new CalculationJob { ColumnName = colname });
            return Accepted(new {message = $"Caluclation for {colname} queued"});
        }

    }
}
