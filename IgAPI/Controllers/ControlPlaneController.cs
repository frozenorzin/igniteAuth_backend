using System.Text.Json;
using IgAPI.Models;
using IgniteAuth.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IgAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ControlPlaneController : ControllerBase
    {
        private readonly ControlPlaneDecision _controlPlaneDecision;

        public ControlPlaneController(ControlPlaneDecision controlPlaneDecision)
        {
            _controlPlaneDecision = controlPlaneDecision;
        }

        // Keep the controller route for existing callers and expose the documented API route.
        [HttpPost("decide")]
        [HttpPost("/decide")]
        public async Task<IActionResult> Decide(
            [FromBody] ControlPlaneCall controlPlaneCall,
            CancellationToken cancellationToken)
        {
            var rawDataJson = JsonSerializer.Serialize(controlPlaneCall);

            var result = await _controlPlaneDecision.DecideAsync(
                decision: controlPlaneCall,
                rawDataJson: rawDataJson,
                cancellationToken: cancellationToken);

            return Ok(new ControlPlaneResponse
            {
                CPCId = controlPlaneCall.CPCId,
                Intent = controlPlaneCall.Intent,
                Command = controlPlaneCall.Command,
                SubSystem = controlPlaneCall.SubSystem,
                Decision = result.Decision.ToString(),
                ReasonCode = result.ReasonCode,
                Message = result.Message,
                TimeStamp = DateTime.UtcNow
            });
        }
    }
}
