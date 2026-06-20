using BullseyeAPI.Application.DTOs;
using BullseyeAPI.Application.Interfaces;
using BullseyeAPI.Domain.Entities;
using BCrypt.Net;

namespace BullseyeAPI.Application.Services;

// Handles registered-account auth (registration/login) and stats lookup -
// separate from, and not connected to, the anonymous guest-session flow.
// JWT issuance happens in PlayerController via IJwtTokenService, not here.
public class PlayerService : IPlayerService
{
    private readonly IPlayerRepository _playerRepository;

    public PlayerService(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    public async Task<PlayerDto?> RegisterAsync(RegisterRequest request)
    {
        var existingPlayer = await _playerRepository.GetByEmailAsync(request.Email);
        if (existingPlayer != null) return null;

        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var player = new Player
        {
            Name = request.Name,
            Email = request.Email.ToLower(),
            Password = hashedPassword,
            CreatedAt = DateTime.UtcNow
        };
        
        await _playerRepository.AddAsync(player);
        await _playerRepository.SaveChangesAsync();
        
        return MapToDto(player);
    }

    public async Task<PlayerDto?> LoginAsync(LoginRequest request)
    {
        var player = await _playerRepository.GetByEmailAsync(request.Email);
        if (player == null || string.IsNullOrEmpty(player.Password)) return null;

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

    // Helper to tidy up data
    private PlayerDto MapToDto(Player player)
    {
        return new PlayerDto
        {
            Id = player.Id,
            Name = player.Name,
            Email = player.Email,
            AvatarUrl = player.AvatarUrl,
            ThreeDartAverage = player.ThreeDartAverage,
            CheckoutPercentage = player.CheckoutPercentage,
            HighestFinish = player.HighestFinish
        };
    }
}