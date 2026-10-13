using IgAPI.DTOs.ReferenceSystem;
using IgAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IgAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReferenceSystemController : ControllerBase
{
    private readonly IIgniteAuthReferenceDataService _referenceDataService;

    public ReferenceSystemController(IIgniteAuthReferenceDataService referenceDataService)
    {
        _referenceDataService = referenceDataService;
    }

    [HttpGet]
    public async Task<ActionResult<ReferenceSystemResponseDto>> GetReferenceSystem(
        CancellationToken cancellationToken)
    {
        var data = await _referenceDataService.GetReferenceDataAsync(cancellationToken);
        var authId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? string.Empty;

        return Ok(new ReferenceSystemResponseDto(
            ReferenceSystemId: 1,
            SystemName: " Reference System - Protected with IgniteAuth",
            SystemType: " Simple Service Framework",
            SystemVersion: "v0.9",
            Description: "Reference system consisting two major services, User and Model Service",
            Status: "Active",
            CreatedAt: DateTime.UtcNow,
            AuthenticatedUser: new AuthenticatedUserDto(authId),
            RegisteredSubsystems: data.Subsystems,
            ValidIntents: data.Intents,
            ValidCommands: data.Commands));
    }
}
