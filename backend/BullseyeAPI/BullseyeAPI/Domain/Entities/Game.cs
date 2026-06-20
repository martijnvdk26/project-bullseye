namespace BullseyeAPI.Domain.Entities;

public class Game
{
    public int Id { get; set; }
    public string Variant { get; set; } // 501, 301, 170 etc.
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public int? WinnerId { get; set; }
    public Player? Winner { get; set; }
    
    // Relations
    public ICollection<Player> Players { get; set; } = new List<Player>();
    public ICollection<Turn> Turns { get; set; } = new List<Turn>();

    // A game belongs to exactly one of these two session kinds, never both
    public GuestSession? GuestSession { get; set; }
    public int? GuestSessionId { get; set; }
    public RegisteredSession? RegisteredSession { get; set; }
    public int? RegisteredSessionId { get; set; }
}