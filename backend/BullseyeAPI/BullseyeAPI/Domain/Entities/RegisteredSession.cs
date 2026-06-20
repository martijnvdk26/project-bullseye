using System;

namespace BullseyeAPI.Domain.Entities;

// The registered-player counterpart to GuestSession: same PIN create/join
// mechanic, but bound to authenticated Player accounts instead of free-text
// names, so the resulting Game is attributed to both accounts.
public class RegisteredSession
{
    public int Id { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int Player1Id { get; set; }
    public Player Player1 { get; set; } = null!;

    public int? Player2Id { get; set; }
    public Player? Player2 { get; set; }

    public string Variant { get; set; } = "501";
    public int TargetSets { get; set; } = 1;
    public int TargetLegs { get; set; } = 3;

    public ICollection<Game> Games { get; set; } = new List<Game>();
}
