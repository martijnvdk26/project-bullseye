namespace BullseyeAPI.Application.DTOs;

public class TurnDto
{
    public int PlayerId { get; set; }
    public int ScoreBefore { get; set; }
    public int ScoreAfter { get; set; }
    public bool IsBust { get; set; }
    public IEnumerable<ScoreDto> Scores { get; set; } = new List<ScoreDto>();
}