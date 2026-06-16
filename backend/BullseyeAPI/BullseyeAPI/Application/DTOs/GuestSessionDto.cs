namespace BullseyeAPI.Application.DTOs;

public class GuestSessionDto
{
    public int Id { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public IEnumerable<GameDto> Games { get; set; } = new List<GameDto>();
}