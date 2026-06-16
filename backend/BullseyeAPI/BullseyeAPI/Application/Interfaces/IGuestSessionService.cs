using BullseyeAPI.Application.DTOs;

namespace BullseyeAPI.Application.Interfaces;

public interface IGuestSessionService
{
    Task<GuestSessionDto> CreateSessionAsync();
    Task<GuestSessionDto?> GetSessionByCodeAsync(string code);
    // Nieuwe methode:
    Task<GameDto?> StartGameForSessionAsync(string code, StartGameRequest request);
}