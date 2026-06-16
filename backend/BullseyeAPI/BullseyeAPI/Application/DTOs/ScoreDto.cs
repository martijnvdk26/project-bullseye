namespace BullseyeAPI.Application.DTOs;

public class ScoreDto
{
    public int Points { get; set; }
    public string Segment { get; set; } = string.Empty;
    public int DartNumber { get; set; }
}