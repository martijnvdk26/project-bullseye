using System.Threading.Tasks;

namespace BullseyeAPI.Application.Interfaces;

public interface IRegisteredSessionService
{
    Task<object> CreateSessionAsync(int player1Id, string variant, int targetSets, int targetLegs, bool vsBot = false, string botDifficulty = "beginner");

    Task<object?> GetSessionByCodeAsync(string code, int joiningPlayerId);

    Task<object?> StartGameForSessionAsync(string code);
}
