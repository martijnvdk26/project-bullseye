namespace BullseyeAPI.Domain.Entities;

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    //Statistics
    public decimal ThreeDartAverage { get; set; }
    public decimal CheckoutPercentage { get; set; }
    public decimal HighestFinish { get; set; }
    
    // Relations
    public ICollection<Game> Games { get; set; } = new List<Game>();
    public ICollection<Tournament> Tournaments { get; set; } = new List<Tournament>();
}