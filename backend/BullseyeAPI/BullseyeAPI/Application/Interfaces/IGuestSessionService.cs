using System.Threading.Tasks;

namespace BullseyeAPI.Application.Interfaces;

public interface IGuestSessionService
{
    // Accepts the creator's name plus the match rules they chose before the PIN is shared
    Task<object> CreateSessionAsync(string playerName, string variant, int targetSets, int targetLegs, bool vsBot = false, string botDifficulty = "beginner");

    // Updates the signature to accept an optional opponent's name
    Task<object?> GetSessionByCodeAsync(string code, string? playerName = null);

    // Match rules now live on the session itself, set once by the creator
    Task<object?> StartGameForSessionAsync(string code);
}