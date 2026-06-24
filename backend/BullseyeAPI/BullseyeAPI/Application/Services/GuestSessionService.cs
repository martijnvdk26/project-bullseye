using BullseyeAPI.Application.Interfaces;
using BullseyeAPI.Domain.Entities;
using BullseyeAPI.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using BullseyeAPI.Infrastructure.Data;

namespace BullseyeAPI.Application.Services;

public class GuestSessionService : IGuestSessionService
{
    private readonly AppDbContext _context;
    private readonly IHubContext<GameHub> _hubContext;

    public GuestSessionService(AppDbContext context, IHubContext<GameHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task<object> CreateSessionAsync(string playerName, string variant, int targetSets, int targetLegs, bool vsBot = false, string botDifficulty = "beginner")
    {
        // Generates a 4-digit PIN that's free in both this table and
        // RegisteredSessions - the two are checked together because a guest
        // accidentally joining the wrong lobby type with a colliding PIN
        // would otherwise silently land on someone else's unrelated session
        var code = await GenerateUniquePinAsync();

        // Creates a new session entity, assigns the creator's name, and locks in
        // the match rules they chose before the PIN is shared
        var session = new GuestSession
        {
            SessionCode = code,
            Player1Name = playerName,
            // A bot opponent never needs to join via the PIN, so its name is
            // filled in immediately instead of waiting for GetSessionByCodeAsync
            Player2Name = vsBot ? "Dartbot" : null,
            Variant = string.IsNullOrEmpty(variant) ? "501" : variant,
            TargetSets = targetSets,
            TargetLegs = targetLegs,
            VsBot = vsBot,
            BotDifficulty = string.IsNullOrEmpty(botDifficulty) ? "beginner" : botDifficulty,
            CreatedAt = DateTime.UtcNow
        };

        _context.GuestSessions.Add(session);
        await _context.SaveChangesAsync();

        // Returns the generated code, player name and match rules to the frontend
        return new
        {
            sessionCode = session.SessionCode,
            player1Name = session.Player1Name,
            player2Name = session.Player2Name,
            variant = session.Variant,
            targetSets = session.TargetSets,
            targetLegs = session.TargetLegs,
            vsBot = session.VsBot,
            botDifficulty = session.BotDifficulty
        };
    }

    public async Task<object?> GetSessionByCodeAsync(string code, string? playerName = null)
    {
        // Retrieves the requested session from the database
        var session = await _context.GuestSessions.FirstOrDefaultAsync(s => s.SessionCode == code);
        if (session == null) return null;

        // Registers the second player if a name is provided and the slot is empty
        if (!string.IsNullOrEmpty(playerName) && string.IsNullOrEmpty(session.Player2Name))
        {
            // Prevents the creator from joining their own game as the second player
            if (session.Player1Name != playerName)
            {
                session.Player2Name = playerName;
                await _context.SaveChangesAsync();

                // Pushes the join to the creator's browser instantly instead of
                // making it wait for its next poll
                await _hubContext.Clients.Group($"guest-lobby-{code}")
                    .SendAsync("PlayerJoined", new { player2Name = session.Player2Name });
            }
        }

        // Returns the session details including both player names and match rules
        return new {
            sessionCode = session.SessionCode,
            player1Name = session.Player1Name,
            player2Name = session.Player2Name,
            variant = session.Variant,
            targetSets = session.TargetSets,
            targetLegs = session.TargetLegs,
            vsBot = session.VsBot
        };
    }

    public async Task<object?> StartGameForSessionAsync(string code)
    {
        // Retrieves the session from the database, including its associated games
        var session = await _context.GuestSessions
            .Include(s => s.Games)
            .FirstOrDefaultAsync(s => s.SessionCode == code);

        if (session == null) return null;

        // Checks if a game has already been created for this session to prevent duplicate matches
        if (session.Games.Any())
        {
            var existingGame = session.Games.First();
            // Returns the existing game ID to ensure both players join the exact same match
            return new { id = existingGame.Id, variant = existingGame.Variant };
        }

        // Creates a genuine new game entity using the session's locked-in variant
        var newGame = new Game
        {
            Variant = session.Variant,
            StartedAt = DateTime.UtcNow,
            // Guest convention: the opponent is always in-match PlayerId 2,
            // whether that's a joined human or, here, the Dartbot
            BotPlayerId = session.VsBot ? 2 : null,
            BotDifficulty = session.BotDifficulty
        };

        // Links the newly created match to this specific guest session
        session.Games.Add(newGame);

        // Saves the changes to PostgreSQL, which generates a unique game ID
        await _context.SaveChangesAsync();

        // Returns the real database ID to the Angular frontend for navigation
        return new { id = newGame.Id, variant = newGame.Variant };
    }

    private async Task<string> GenerateUniquePinAsync()
    {
        var random = new Random();
        string code;
        do
        {
            code = random.Next(1000, 9999).ToString();
        }
        while (await _context.GuestSessions.AnyAsync(s => s.SessionCode == code)
            || await _context.RegisteredSessions.AnyAsync(s => s.SessionCode == code));

        return code;
    }
}