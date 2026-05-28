using BullseyeAPI.Application.Interfaces;
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
}