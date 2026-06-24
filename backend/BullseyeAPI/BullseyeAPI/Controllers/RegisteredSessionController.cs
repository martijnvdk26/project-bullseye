using BullseyeAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BullseyeAPI.Controllers;

// Same PIN create/join mechanic as GuestSessionController, but every
// action requires a logged-in Player - the participants are taken from the
// JWT's "sub" claim rather than free-text names supplied by the client.
public class CreateRegisteredSessionRequest
{
    public string Variant { get; set; } = "501";
    public int TargetSets { get; set; } = 1;
    public int TargetLegs { get; set; } = 3;

    // When true, the session's opponent slot is filled by the Dartbot
    // instead of waiting for a second registered player to join via the PIN.
    public bool VsBot { get; set; } = false;

    // "beginner" | "semi" | "pro" - only used when VsBot is true.
    public string BotDifficulty { get; set; } = "beginner";
}

[ApiController]
[Route("api/registered-session")]
[Authorize]
public class RegisteredSessionController : ControllerBase
{
    private readonly IRegisteredSessionService _registeredSessionService;

    public RegisteredSessionController(IRegisteredSessionService registeredSessionService)
    {
        _registeredSessionService = registeredSessionService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateSession([FromBody] CreateRegisteredSessionRequest request)
    {
        var playerId = GetCurrentPlayerId();
        var result = await _registeredSessionService.CreateSessionAsync(
            playerId, request.Variant, request.TargetSets, request.TargetLegs, request.VsBot, request.BotDifficulty);
        return Ok(result);
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetSession(string code)
    {
        var playerId = GetCurrentPlayerId();
        var result = await _registeredSessionService.GetSessionByCodeAsync(code, playerId);

        if (result == null)
        {
            return NotFound(new { message = "Session code not found." });
        }

        return Ok(result);
    }

    [HttpPost("{code}/game")]
    public async Task<IActionResult> StartGame(string code)
    {
        var result = await _registeredSessionService.StartGameForSessionAsync(code);

        if (result == null)
        {
            return NotFound(new { message = "Session code not found. Cannot start match." });
        }

        return Ok(result);
    }

    private int GetCurrentPlayerId()
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.Parse(rawId!);
    }
}
