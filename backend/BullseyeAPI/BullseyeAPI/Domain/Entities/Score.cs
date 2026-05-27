namespace BullseyeAPI.Domain.Entities;

public class Score
{
    public int Id { get; set; }
    public int TurnId { get; set; }
    public Turn Turn { get; set; } = null!;
    
    public int Points { get; set; }
    public string Segment { get; set; } = ""; // "T20", "D10", "S5", "Bull", etc.
    public int DartNumber { get; set; } // 1, 2, of 3
}