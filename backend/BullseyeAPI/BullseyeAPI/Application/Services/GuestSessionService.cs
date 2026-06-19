using BullseyeAPI.Application.DTOs;
using BullseyeAPI.Application.Interfaces;
using BullseyeAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using BullseyeAPI.Infrastructure.Data;

namespace BullseyeAPI.Application.Services;

public class GuestSessionService : IGuestSessionService
{
    private readonly AppDbContext _context;

    public GuestSessionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<object> CreateSessionAsync(string playerName)
    {
        // Generates a random 4-character PIN for the guest session
        var code = GenerateRandomPin(); 
        
        // Creates a new session entity and assigns the creator's name
        var session = new GuestSession
        {
            SessionCode = code,
            Player1Name = playerName,
            CreatedAt = DateTime.UtcNow
        };

        _context.GuestSessions.Add(session);
        await _context.SaveChangesAsync();

        // Returns the generated code and player name to the frontend
        return new { sessionCode = session.SessionCode, player1Name = session.Player1Name };
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
            }
        }

        // Returns the session details including both player names
        return new { 
            sessionCode = session.SessionCode, 
            player1Name = session.Player1Name, 
            player2Name = session.Player2Name 
        };
    }

    public async Task<object?> StartGameForSessionAsync(string code, StartGameRequest request)
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

        // Creates a genuine new game entity using the requested variant
        var newGame = new Game
        {
            Variant = string.IsNullOrEmpty(request.Variant) ? "501" : request.Variant,
            StartedAt = DateTime.UtcNow
        };

        // Links the newly created match to this specific guest session
        session.Games.Add(newGame);
        
        // Saves the changes to PostgreSQL, which generates a unique game ID
        await _context.SaveChangesAsync();

        // Returns the real database ID to the Angular frontend for navigation
        return new { id = newGame.Id, variant = newGame.Variant }; 
    }
    
    private string GenerateRandomPin()
    {
        // Creates a random 4-digit number to serve as the session PIN
        var random = new Random();
        return random.Next(1000, 9999).ToString();
    }
}