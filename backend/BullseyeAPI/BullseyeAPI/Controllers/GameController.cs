using BullseyeAPI.Application.Interfaces;
using BullseyeAPI.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BullseyeAPI.Controllers;

[ApiController]
[Route("api/game")]
[AllowAnonymous]
public class GameController : ControllerBase
{
    private readonly IGameService _gameService;

    public GameController(IGameService gameService)
    {
        _gameService = gameService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGame(int id)
    {
        var game = await _gameService.GetGameAsync(id);
        if (game == null) return NotFound();

        return Ok(game);
    }
    
    [HttpPost("turn")]
    public async Task<IActionResult> SubmitManualTurn([FromBody] SubmitTurnRequest request)
    {
        try
        {
            var success = await _gameService.SubmitManualTurnAsync(request);
            if (!success) return NotFound(new { message = "Wedstrijd niet gevonden." });
            return Ok(new { message = "Complete beurt succesvol verwerkt!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}