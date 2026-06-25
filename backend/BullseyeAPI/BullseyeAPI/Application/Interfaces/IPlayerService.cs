using BullseyeAPI.Application.DTOs;

namespace BullseyeAPI.Application.Interfaces;

public interface IPlayerService
{
    Task <PlayerDto?> RegisterAsync (RegisterRequest request);
    Task <LoginResult> LoginAsync (LoginRequest request);
    Task <PlayerDto?> GetPlayerStatsAsync (int playerId);
    Task <bool> VerifyEmailAsync (string token);
    Task ResendVerificationEmailAsync (string email);
}