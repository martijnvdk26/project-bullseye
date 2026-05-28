using BullseyeAPI.Application.DTOs;
using BullseyeAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BullseyeAPI.Controllers;

[ApiController]
[Route("api/player")]
[AllowAnonymous]
public class PlayerController : ControllerBase
{
    private readonly IPlayerService _playerService;

    public PlayerController(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _playerService.LoginAsync(request);
        if (result == null)
        {
            return Unauthorized(new { message = "Ongeldig e-mailadres of wachtwoord." });
        }

        return Ok(result);
    }

    [HttpGet("{id}/stats")]
    public async Task<IActionResult> GetStats(int id)
    {
        var result = await _playerService.GetPlayerStatsAsync(id);
        if (result == null) return NotFound(new { message = "Speler niet gevonden." });

        return Ok(result);
    }
}