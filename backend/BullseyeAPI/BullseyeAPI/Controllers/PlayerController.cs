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

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _playerService.RegisterAsync(request);
        if (result == null)
        {
            // Error 400 if email exists in database
            return BadRequest(new { message = "Registratie is mislukt. Dit e-mailadres is mogelijk al in gebruik." });
        }
        return Ok(result);
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