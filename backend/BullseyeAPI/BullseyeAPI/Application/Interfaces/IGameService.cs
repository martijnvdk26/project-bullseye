using BullseyeAPI.Application.DTOs;
using System.Threading.Tasks;

namespace BullseyeAPI.Application.Interfaces;

public interface IGameService
{
    // Retrieves the current state of a specific match
    Task<GameDto?> GetGameAsync(int gameId);
    
    // Processes an individually thrown dart and updates the match state
    Task<bool> SubmitScoreAsync(SubmitScoreRequest request);
    
    // Processes a completely manually entered turn and updates the match state
    Task<bool> SubmitManualTurnAsync(SubmitTurnRequest request);
}