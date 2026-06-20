namespace BullseyeAPI.Application.Interfaces;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string playerName);
}
