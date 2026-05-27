namespace BullseyeAPI.Domain.Entities;

public class Tournament
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Type { get; set; } = "round-robin"; // round-robin, knockout
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    
    // Relations
    public ICollection<Player> Players { get; set; } = new List<Player>();
    public ICollection<Game> Games { get; set; } = new List<Game>();
}