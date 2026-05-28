using BullseyeAPI.Application.DTOs;
using BullseyeAPI.Application.Interfaces;
using BullseyeAPI.Domain.Entities;
using BCrypt.Net;

namespace BullseyeAPI.Application.Services;

public class PlayerService : IPlayerService
{
    private readonly IPlayerRepository _playerRepository;

    public PlayerService(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    public async Task<PlayerDto?> LoginAsync(LoginRequest request)
    {
        // 1. Zoek jouw speler op via e-mail
        var player = await _playerRepository.GetByEmailAsync(request.Email);
        if (player == null || string.IsNullOrEmpty(player.Password)) return null;

        // 2. Verifieer het wachtwoord via BCrypt
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, player.Password);
        
        if (!isPasswordValid) return null;

        return MapToDto(player);
    }

    public async Task<PlayerDto?> GetPlayerStatsAsync(int playerId)
    {
        var player = await _playerRepository.GetByIdAsync(playerId);
        if (player == null) return null;

        return MapToDto(player);
    }

    // Helper om data netjes om te zetten
    private PlayerDto MapToDto(Player player)
    {
        return new PlayerDto
        {
            Id = player.Id,
            Name = player.Name,
            AvatarUrl = player.AvatarUrl,
            ThreeDartAverage = player.ThreeDartAverage,
            CheckoutPercentage = player.CheckoutPercentage,
            HighestFinish = player.HighestFinish
        };
    }
}