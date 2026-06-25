using System.Security.Cryptography;
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
    private static readonly TimeSpan VerificationTokenLifetime = TimeSpan.FromHours(24);

    private readonly IPlayerRepository _playerRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public PlayerService(IPlayerRepository playerRepository, IEmailService emailService, IConfiguration configuration)
    {
        _playerRepository = playerRepository;
        _emailService = emailService;
        _configuration = configuration;
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
            CreatedAt = DateTime.UtcNow,
            EmailVerified = false,
            VerificationToken = GenerateVerificationToken(),
            VerificationTokenExpiresAt = DateTime.UtcNow.Add(VerificationTokenLifetime),
        };

        await _playerRepository.AddAsync(player);
        await _playerRepository.SaveChangesAsync();

        await SendVerificationEmailAsync(player);

        return MapToDto(player);
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request)
    {
        var player = await _playerRepository.GetByEmailAsync(request.Email);
        if (player == null || string.IsNullOrEmpty(player.Password))
        {
            return new LoginResult { Status = LoginStatus.InvalidCredentials };
        }

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, player.Password);
        if (!isPasswordValid)
        {
            return new LoginResult { Status = LoginStatus.InvalidCredentials };
        }

        if (!player.EmailVerified)
        {
            return new LoginResult { Status = LoginStatus.EmailNotVerified };
        }

        return new LoginResult { Status = LoginStatus.Success, Player = MapToDto(player) };
    }

    public async Task<PlayerDto?> GetPlayerStatsAsync(int playerId)
    {
        var player = await _playerRepository.GetByIdAsync(playerId);
        if (player == null) return null;

        return MapToDto(player);
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var player = await _playerRepository.GetByVerificationTokenAsync(token);
        if (player == null || player.VerificationTokenExpiresAt is null || player.VerificationTokenExpiresAt < DateTime.UtcNow)
        {
            return false;
        }

        player.EmailVerified = true;
        player.VerificationToken = null;
        player.VerificationTokenExpiresAt = null;
        await _playerRepository.UpdateAsync(player);
        await _playerRepository.SaveChangesAsync();

        if (!string.IsNullOrEmpty(player.Email))
        {
            await _emailService.SendWelcomeEmailAsync(player.Email, player.Name);
        }

        return true;
    }

    public async Task ResendVerificationEmailAsync(string email)
    {
        var player = await _playerRepository.GetByEmailAsync(email);

        // Deliberately silent no-op for "doesn't exist" / "already verified" -
        // the caller always gets the same generic response so this endpoint
        // can't be used to enumerate registered email addresses.
        if (player == null || player.EmailVerified) return;

        player.VerificationToken = GenerateVerificationToken();
        player.VerificationTokenExpiresAt = DateTime.UtcNow.Add(VerificationTokenLifetime);
        await _playerRepository.UpdateAsync(player);
        await _playerRepository.SaveChangesAsync();

        await SendVerificationEmailAsync(player);
    }

    private async Task SendVerificationEmailAsync(Player player)
    {
        if (string.IsNullOrEmpty(player.Email) || string.IsNullOrEmpty(player.VerificationToken)) return;

        var frontendBaseUrl = _configuration["Frontend:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:4200";
        var verificationLink = $"{frontendBaseUrl}/verify-email?token={Uri.EscapeDataString(player.VerificationToken)}";

        await _emailService.SendVerificationEmailAsync(player.Email, player.Name, verificationLink);
    }

    private static string GenerateVerificationToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
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
