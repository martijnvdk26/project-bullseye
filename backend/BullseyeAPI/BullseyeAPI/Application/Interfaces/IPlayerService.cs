using BullseyeAPI.Application.DTOs;

namespace BullseyeAPI.Application.Interfaces;

public interface IPlayerService
{
    Task <PlayerDto?> LoginAsync (LoginRequest request);
    Task <PlayerDto?> GetPlayerStatsAsync (int playerId);
}