using BullseyeAPI.Application.Interfaces;
using BullseyeAPI.Domain.Entities;
using BullseyeAPI.Hubs;
using BullseyeAPI.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BullseyeAPI.Application.Services;

public class RegisteredSessionService : IRegisteredSessionService
{
    private readonly AppDbContext _context;
    private readonly IHubContext<GameHub> _hubContext;

    public RegisteredSessionService(AppDbContext context, IHubContext<GameHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task<object> CreateSessionAsync(int player1Id, string variant, int targetSets, int targetLegs)
    {
        // Checked against GuestSessions too - see the comment on the
        // equivalent method in GuestSessionService for why
        var code = await GenerateUniquePinAsync();

        var session = new RegisteredSession
        {
            SessionCode = code,
            Player1Id = player1Id,
            Variant = string.IsNullOrEmpty(variant) ? "501" : variant,
            TargetSets = targetSets,
            TargetLegs = targetLegs,
            CreatedAt = DateTime.UtcNow
        };

        _context.RegisteredSessions.Add(session);
        await _context.SaveChangesAsync();

        var player1 = await _context.Players.FindAsync(player1Id);

        return new
        {
            sessionCode = session.SessionCode,
            player1Id = session.Player1Id,
            player1Name = player1?.Name ?? string.Empty,
            variant = session.Variant,
            targetSets = session.TargetSets,
            targetLegs = session.TargetLegs
        };
    }

    public async Task<object?> GetSessionByCodeAsync(string code, int joiningPlayerId)
    {
        var session = await _context.RegisteredSessions
            .Include(s => s.Player1)
            .Include(s => s.Player2)
            .FirstOrDefaultAsync(s => s.SessionCode == code);

        if (session == null) return null;

        // Registers the second player if the slot is empty and they aren't the creator
        if (session.Player2Id == null && session.Player1Id != joiningPlayerId)
        {
            var joiningPlayer = await _context.Players.FindAsync(joiningPlayerId);
            if (joiningPlayer != null)
            {
                session.Player2Id = joiningPlayer.Id;
                session.Player2 = joiningPlayer;
                await _context.SaveChangesAsync();

                // Pushes the join to the creator's browser instantly instead of
                // making it wait for its next poll
                await _hubContext.Clients.Group($"registered-lobby-{code}")
                    .SendAsync("PlayerJoined", new { player2Name = joiningPlayer.Name });
            }
        }

        return new
        {
            sessionCode = session.SessionCode,
            player1Id = session.Player1Id,
            player1Name = session.Player1.Name,
            player2Id = session.Player2Id,
            player2Name = session.Player2?.Name,
            variant = session.Variant,
            targetSets = session.TargetSets,
            targetLegs = session.TargetLegs
        };
    }

    public async Task<object?> StartGameForSessionAsync(string code)
    {
        var session = await _context.RegisteredSessions
            .Include(s => s.Games)
            .Include(s => s.Player1)
            .Include(s => s.Player2)
            .FirstOrDefaultAsync(s => s.SessionCode == code);

        if (session == null) return null;

        if (session.Games.Any())
        {
            var existingGame = session.Games.First();
            return new { id = existingGame.Id, variant = existingGame.Variant };
        }

        var newGame = new Game
        {
            Variant = session.Variant,
            StartedAt = DateTime.UtcNow
        };

        newGame.Players.Add(session.Player1);
        if (session.Player2 != null)
        {
            newGame.Players.Add(session.Player2);
        }

        session.Games.Add(newGame);
        await _context.SaveChangesAsync();

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
        while (await _context.RegisteredSessions.AnyAsync(s => s.SessionCode == code)
            || await _context.GuestSessions.AnyAsync(s => s.SessionCode == code));

        return code;
    }
}
