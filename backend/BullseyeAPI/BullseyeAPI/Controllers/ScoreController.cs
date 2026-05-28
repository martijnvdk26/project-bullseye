using BullseyeAPI.Application.DTOs;
using BullseyeAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BullseyeAPI.Controllers;

[ApiController]
[Route("api/score")]
[AllowAnonymous]
public class ScoreController : ControllerBase
{
    private readonly IGameService _gameService;

    public ScoreController(IGameService gameService)
    {
        _gameService = gameService;
    }

    [HttpPost]
    public async Task<IActionResult> SubmitScore([FromBody] SubmitScoreRequest request)
    {
        var success = await _gameService.SubmitScoreAsync(request);
        
        if (!success)
        {
            return BadRequest(new { message = "Score kon niet worden verwerkt. Controleer of de game ID klopt en de game nog niet is afgelopen." });
        }

        return Ok(new { message = "Score succesvol verwerkt!" });
    }
}