namespace BullseyeAPI.Application.DTOs;

public enum LoginStatus
{
    Success,
    InvalidCredentials,
    EmailNotVerified,
}

public class LoginResult
{
    public LoginStatus Status { get; set; }
    public PlayerDto? Player { get; set; }
}
