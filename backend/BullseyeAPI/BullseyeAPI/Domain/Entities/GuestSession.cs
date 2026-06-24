using System;

namespace BullseyeAPI.Domain.Entities;

public class GuestSession
{
    public int Id { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Stores the name of the session creator
    public string Player1Name { get; set; } = string.Empty;
    
    // Stores the name of the opponent joining via the PIN
    public string? Player2Name { get; set; }

    // Match rules chosen by the creator before the PIN is shared; the joiner
    // only ever reads these, never sets them, so both players agree on the
    // same rules instead of each picking their own in local component state.
    public string Variant { get; set; } = "501";
    public int TargetSets { get; set; } = 1;
    public int TargetLegs { get; set; } = 3;

    // True when the creator chose to play against the Dartbot instead of
    // waiting for a second human to join via the PIN.
    public bool VsBot { get; set; }

    // "beginner" | "semi" | "pro" - chosen by the creator alongside VsBot.
    public string BotDifficulty { get; set; } = "beginner";

    public ICollection<Game> Games { get; set; } = new List<Game>();
}