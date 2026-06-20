namespace BullseyeAPI.Application.DTOs;

public class PlayerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public decimal ThreeDartAverage { get; set; }
    public decimal CheckoutPercentage { get; set; }
    public decimal HighestFinish { get; set; }
}