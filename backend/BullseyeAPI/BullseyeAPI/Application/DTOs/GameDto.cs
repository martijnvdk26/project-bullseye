namespace BullseyeAPI.Application.DTOs;

public class GameDto
{
    public int Id { get; set; }
    public string Variant { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public int? WinnerId { get; set; }
    
    // Nieuw: Lijst met beurten toevoegen
    public IEnumerable<TurnDto> Turns { get; set; } = new List<TurnDto>();
}