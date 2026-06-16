namespace BullseyeAPI.Application.DTOs;

public class SubmitTurnRequest
{
    public int GameId { get; set; }
    public int PlayerId { get; set; }
    public int TotalPoints { get; set; } // Bijv. 100 of 180
}