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

    public PlayerController(IPlayerService playerService, IJwtTokenService jwtTokenService)
    {
        _playerService = playerService;
        _jwtTokenService = jwtTokenService;
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

        // The verification email is sent from inside RegisterAsync (it needs
        // the raw token, which never leaves PlayerService). Login stays
        // blocked until that link is clicked - see LoginAsync.
        return Ok(new { message = "Account aangemaakt. Check je e-mail om je account te verifiëren." });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _playerService.LoginAsync(request);

        if (result.Status == LoginStatus.EmailNotVerified)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Verifieer eerst je e-mailadres voordat je kunt inloggen.", code = "EMAIL_NOT_VERIFIED" });
        }

        if (result.Status != LoginStatus.Success || result.Player == null)
        {
            return Unauthorized(new { message = "Ongeldig e-mailadres of wachtwoord." });
        }

        var token = _jwtTokenService.GenerateToken(result.Player);
        return Ok(new { token, player = result.Player });
    }

    [AllowAnonymous]
    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new { message = "Ongeldige verificatielink." });
        }

        var verified = await _playerService.VerifyEmailAsync(token);
        if (!verified)
        {
            return BadRequest(new { message = "Deze verificatielink is ongeldig of verlopen." });
        }

        return Ok(new { message = "E-mailadres geverifieerd. Je kunt nu inloggen." });
    }

    [AllowAnonymous]
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            await _playerService.ResendVerificationEmailAsync(request.Email);
        }

        // Always the same response, verified/unknown emails included, so this
        // endpoint can't be used to check which addresses have an account.
        return Ok(new { message = "Als dit account bestaat en nog niet geverifieerd is, is er een nieuwe e-mail verstuurd." });
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