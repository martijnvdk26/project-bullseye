using BullseyeAPI.Application.DTOs;
using System.Threading.Tasks;

namespace BullseyeAPI.Application.Interfaces;

public interface IGuestSessionService
{
    // Updates the signature to accept the creator's name
    Task<object> CreateSessionAsync(string playerName);
    
    // Updates the signature to accept an optional opponent's name
    Task<object?> GetSessionByCodeAsync(string code, string? playerName = null);
    
    Task<object?> StartGameForSessionAsync(string code, StartGameRequest request);
}