using IgAPI.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IgAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,User")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IDecisionAnalyticsStore _store;

    public AnalyticsController(IDecisionAnalyticsStore store)
    {
        _store = store;
    }

    [HttpGet]
    [HttpGet("/analytics")]
    public async Task<ActionResult<DecisionAnalyticsSnapshot>> Get(
        [FromQuery] int limit = 100,
        [FromQuery] DateTimeOffset? fromUtc = null,
        [FromQuery] string? subsystem = null,
        [FromQuery] string? decision = null,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 1000)
            return BadRequest(new { message = "limit must be between 1 and 1000." });

        var snapshot = await _store.QueryAsync(
            new DecisionAnalyticsQuery(limit, fromUtc, subsystem, decision),
            cancellationToken);
        return Ok(snapshot);
    }
}
