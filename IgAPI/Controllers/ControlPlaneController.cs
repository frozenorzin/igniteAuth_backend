using System.Text.Json;
using System.Security.Claims;
using IgAPI.Security;
using IgAPI.Logging;
using IgAPI.Models;
using IgAPI.Services.Interfaces;
using IgniteAuth.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using IgAPI.Analytics;

namespace IgAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,User")]
    public class ControlPlaneController : ControllerBase
    {
        private readonly ControlPlaneDecision _controlPlaneDecision;
        private readonly ICpcIdGenerator _cpcIdGenerator;
        private readonly IDecisionAnalyticsStore _analyticsStore;

        public ControlPlaneController(
            ControlPlaneDecision controlPlaneDecision,
            ICpcIdGenerator cpcIdGenerator,
            IDecisionAnalyticsStore analyticsStore)
        {
            _controlPlaneDecision = controlPlaneDecision;
            _cpcIdGenerator = cpcIdGenerator;
            _analyticsStore = analyticsStore;
        }

        // Keep the controller route for existing callers and expose the documented API route.
        [HttpPost("decide")]
        [HttpPost("/decide")]
        public async Task<IActionResult> Decide(
            [FromBody] ControlPlaneCall controlPlaneCall,
            CancellationToken cancellationToken)
        {
            var decisionStartedAt = Stopwatch.GetTimestamp();
            // A CPC ID belongs to this server-side decision call. Any value sent
            // by the client is ignored so callers cannot choose correlation IDs.
            controlPlaneCall.CPCId = _cpcIdGenerator.Issue();

            // Identity comes from the validated token, never from the request body.
            controlPlaneCall.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var rawDataJson = JsonSerializer.Serialize(controlPlaneCall);

            var result = controlPlaneCall.ReferenceSystemId != 1
                ? new ControlPlaneResult { Decision = ControlPlaneDecisionVerdict.Deny,
                    ReasonCode = "REFERENCE_SYSTEM_INVALID", Message = "Unknown reference system." }
                : await _controlPlaneDecision.DecideAsync(
                decision: controlPlaneCall,
                rawDataJson: rawDataJson,
                cancellationToken: cancellationToken);

            if (result.Decision == ControlPlaneDecisionVerdict.Allow &&
                !User.IsInRole("Admin") &&
                !ReadOnlyCommands.Contains(controlPlaneCall.SubSystem, controlPlaneCall.Command))
            {
                result = result with { Decision = ControlPlaneDecisionVerdict.Deny,
                    ReasonCode = "ROLE_FORBIDDEN", Message = "This role may request only approved read-only commands." };
            }

            HttpContext.Items[ResponseLogWriter.DecisionDetailsKey] = new DecisionLogDetails(
                controlPlaneCall.CPCId, controlPlaneCall.ReferenceSystemId,
                controlPlaneCall.SubSystem, controlPlaneCall.SubSystemId, controlPlaneCall.Intent,
                controlPlaneCall.Command, result.Decision.ToString(), result.ReasonCode, result.Message);

            var recordedAt = DateTimeOffset.UtcNow;
            await _analyticsStore.AppendAsync(new DecisionAnalyticsEvent(
                Guid.NewGuid(),
                recordedAt,
                HttpContext.TraceIdentifier,
                controlPlaneCall.CPCId,
                controlPlaneCall.ReferenceSystemId,
                User.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
                controlPlaneCall.SubSystem,
                controlPlaneCall.SubSystemId,
                controlPlaneCall.Intent,
                controlPlaneCall.Command,
                result.Decision.ToString(),
                result.ReasonCode,
                Math.Round(Stopwatch.GetElapsedTime(decisionStartedAt).TotalMilliseconds, 2)),
                cancellationToken);

            return Ok(new ControlPlaneResponse
            {
                CPCId = controlPlaneCall.CPCId,
                Intent = controlPlaneCall.Intent,
                Command = controlPlaneCall.Command,
                SubSystem = controlPlaneCall.SubSystem,
                Decision = result.Decision.ToString(),
                ReasonCode = result.ReasonCode,
                Message = result.Message,
                TimeStamp = recordedAt.UtcDateTime
            });
        }
    }
}
