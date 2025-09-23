// AssetHierarchyAPI/Controllers/CalculationController.cs
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class CalculationController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;
    private const string WorkerBaseUrl = "http://localhost:6000"; 

    public CalculationController(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    [HttpPost("avg")]
    public async Task<IActionResult> TriggerCalculation([FromQuery] int signalId)
    {
        var client = _httpFactory.CreateClient();
        var resp = await client.PostAsync($"{WorkerBaseUrl}/enqueue?signalId={signalId}", null);

        if (!resp.IsSuccessStatusCode)
            return StatusCode((int)resp.StatusCode, "Failed to enqueue at worker.");

        return Accepted(new { message = $"SignalId {signalId} sent to worker" });
    }
}
