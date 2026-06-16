namespace BullseyeAPI.Domain.Entities;

public class Turn
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public Game Game { get; set; } = null!;
    
    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    
    public int ScoreBefore { get; set; }
    public int ScoreAfter { get; set; }
    public bool IsBust { get; set; }
    public DateTime ThrownAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<Score> Scores { get; set; } = new List<Score>();
}