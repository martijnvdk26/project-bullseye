namespace BullseyeAPI.Domain.Entities;

public class GuestSession
{
    public int Id { get; set; }
    public string SessionCode { get; set; } = null!; // "BULL-4821"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Relations
    public ICollection<Game> Games { get; set; } = new List<Game>();
}