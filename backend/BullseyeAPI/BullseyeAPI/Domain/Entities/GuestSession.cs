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
    
    public ICollection<Game> Games { get; set; } = new List<Game>();
}