namespace BullseyeAPI.Application.DTOs;

public class SubmitScoreRequest
{
    public int GameId { get; set; }
    public int PlayerId { get; set; } // Om bij te houden wie de pijl gooide
    public int Points { get; set; } // Bijv. 60
    public string Segment { get; set; } = string.Empty; // Bijv. "T20"
    public bool IsDouble { get; set; } // Belangrijk voor de DartGameRules
    public int DartNumber { get; set; } // 1, 2, of 3
}