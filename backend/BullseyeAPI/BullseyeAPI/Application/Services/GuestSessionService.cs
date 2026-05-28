using BullseyeAPI.Application.DTOs;
using BullseyeAPI.Application.Interfaces;
using BullseyeAPI.Domain.Entities;

namespace BullseyeAPI.Application.Services;

public class GuestSessionService : IGuestSessionService
{
    private readonly IGuestSessionRepository _sessionRepository;
    private readonly IGameRepository _gameRepository;

    // Constructor met beide repositories
    public GuestSessionService(IGuestSessionRepository sessionRepository, IGameRepository gameRepository)
    {
        _sessionRepository = sessionRepository;
        _gameRepository = gameRepository;
    }

    public async Task<GuestSessionDto> CreateSessionAsync()
    {
        var random = new Random();
        string code = $"BULL-{random.Next(1000, 9999)}";

        var session = new GuestSession { SessionCode = code };
        await _sessionRepository.AddAsync(session);
        await _sessionRepository.SaveChangesAsync();

        return new GuestSessionDto
        {
            Id = session.Id,
            SessionCode = session.SessionCode,
            CreatedAt = session.CreatedAt,
            Games = new List<GameDto>()
        };
    }

    public async Task<GuestSessionDto?> GetSessionByCodeAsync(string code)
    {
        var session = await _sessionRepository.GetByCodeAsync(code.ToUpper());
        if (session == null) return null;

        var gameDtos = session.Games.Select(g => new GameDto
        {
            Id = g.Id,
            Variant = g.Variant,
            StartedAt = g.StartedAt,
            WinnerId = g.WinnerId
        }).ToList();

        return new GuestSessionDto
        {
            Id = session.Id,
            SessionCode = session.SessionCode,
            CreatedAt = session.CreatedAt,
            Games = gameDtos
        };
    }

    // Nieuwe implementatie:
    public async Task<GameDto?> StartGameForSessionAsync(string code, StartGameRequest request)
    {
        // 1. Zoek de bestaande gastsessie op via de unieke code
        var session = await _sessionRepository.GetByCodeAsync(code.ToUpper());
        if (session == null) return null;

        // 2. Maak een nieuwe Game entiteit aan gekoppeld aan deze sessie
        var game = new Game
        {
            Variant = request.Variant,
            StartedAt = DateTime.UtcNow,
            GuestSessionId = session.Id
        };

        // 3. Sla de game op via de game repository
        await _gameRepository.AddAsync(game);
        await _gameRepository.SaveChangesAsync();

        // 4. Geef het resultaat netjes terug als GameDto
        return new GameDto
        {
            Id = game.Id,
            Variant = game.Variant,
            StartedAt = game.StartedAt,
            WinnerId = game.WinnerId
        };
    }
}