namespace BullseyeAPI.Application.Interfaces;

// Talks to the Python ai-service container that hosts the Dartbot's
// strategies (NaiveStrategy / HumanLikeStrategy). Returns the bot's combined
// 3-dart total for one turn so GameService can run it back through the same
// bust/win logic as a human manual turn.
public interface IAiDartbotClient
{
    // Returns null if the ai-service is unreachable or returns something
    // unusable, so the caller can simply skip the bot's turn instead of
    // failing the human's request. difficulty is "beginner" | "semi" | "pro".
    Task<int?> GetBotTurnTotalAsync(int remainingScore, string variant, string difficulty);
}
