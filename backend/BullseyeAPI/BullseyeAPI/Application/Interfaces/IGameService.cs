using BullseyeAPI.Application.DTOs;

namespace BullseyeAPI.Application.Interfaces;

public interface IGameService
{
    Task<GameDto?> GetGameAsync(int gameId);
    Task<bool> SubmitScoreAsync(SubmitScoreRequest request);
    Task<bool> SubmitManualTurnAsync(SubmitTurnRequest request);
}