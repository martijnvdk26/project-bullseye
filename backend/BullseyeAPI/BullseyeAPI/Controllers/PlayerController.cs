using BullseyeAPI.Application.DTOs;
using BullseyeAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BullseyeAPI.Controllers;

[ApiController]
[Route("api/player")]
public class PlayerController : ControllerBase
{
    private readonly IPlayerService _playerService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;

    public PlayerController(IPlayerService playerService, IJwtTokenService jwtTokenService, IEmailService emailService)
    {
        _playerService = playerService;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _playerService.RegisterAsync(request);
        if (result == null)
        {
            // Error 400 if email exists in database
            return BadRequest(new { message = "Registratie is mislukt. Dit e-mailadres is mogelijk al in gebruik." });
        }

        if (!string.IsNullOrEmpty(result.Email))
        {
            await _emailService.SendWelcomeEmailAsync(result.Email, result.Name);
        }

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var player = await _playerService.LoginAsync(request);
        if (player == null)
        {
            return Unauthorized(new { message = "Ongeldig e-mailadres of wachtwoord." });
        }

        var token = _jwtTokenService.GenerateToken(player);
        return Ok(new { token, player });
    }

    [Authorize]
    [HttpGet("{id}/stats")]
    public async Task<IActionResult> GetStats(int id)
    {
        var result = await _playerService.GetPlayerStatsAsync(id);
        if (result == null) return NotFound(new { message = "Speler niet gevonden." });

        return Ok(result);
    }
}