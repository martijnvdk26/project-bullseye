using BullseyeAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BullseyeAPI.Controllers;

// Defines the expected JSON structure from the Angular frontend. The match
// rules are chosen by the creator here, before the PIN is shared - the
// joiner only ever reads them back from the session, never sets them.
public class CreateGuestSessionRequest
{
    public string PlayerName { get; set; } = string.Empty;
    public string Variant { get; set; } = "501";
    public int TargetSets { get; set; } = 1;
    public int TargetLegs { get; set; } = 3;
}

[ApiController]
[Route("api/guest")]
[AllowAnonymous]
public class GuestSessionController : ControllerBase
{
    private readonly IGuestSessionService _guestSessionService;

    public GuestSessionController(IGuestSessionService guestSessionService)
    {
        _guestSessionService = guestSessionService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateSession([FromBody] CreateGuestSessionRequest request)
    {
        // Passes the player name and chosen match rules to the service layer for session creation
        var result = await _guestSessionService.CreateSessionAsync(
            request.PlayerName, request.Variant, request.TargetSets, request.TargetLegs);
        return Ok(result);
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetSession(string code, [FromQuery] string? playerName = null)
    {
        // Passes the session code and the optional opponent name to the service layer
        var result = await _guestSessionService.GetSessionByCodeAsync(code, playerName);
        
        if (result == null)
        {
            return NotFound(new { message = "Session code not found." });
        }

        return Ok(result);
    }

    [HttpPost("{code}/game")]
    public async Task<IActionResult> StartGame(string code)
    {
        var result = await _guestSessionService.StartGameForSessionAsync(code);
        
        if (result == null)
        {
            return NotFound(new { message = "Session code not found. Cannot start match." });
        }

        return Ok(result);
    }
}