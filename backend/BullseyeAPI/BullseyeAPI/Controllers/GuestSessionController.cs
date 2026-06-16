using BullseyeAPI.Application.DTOs;
using BullseyeAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BullseyeAPI.Controllers;

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
    public async Task<IActionResult> CreateSession()
    {
        var result = await _guestSessionService.CreateSessionAsync();
        return Ok(result);
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetSession(string code)
    {
        var result = await _guestSessionService.GetSessionByCodeAsync(code);
        
        if (result == null)
        {
            return NotFound(new { message = "Sessiecode niet gevonden." });
        }

        return Ok(result);
    }

    // Nieuw endpoint: POST /api/guest/{code}/game
    [HttpPost("{code}/game")]
    public async Task<IActionResult> StartGame(string code, [FromBody] StartGameRequest request)
    {
        var result = await _guestSessionService.StartGameForSessionAsync(code, request);
        
        if (result == null)
        {
            return NotFound(new { message = "Sessiecode niet gevonden. Kan geen wedstrijd starten." });
        }

        return Ok(result);
    }
}