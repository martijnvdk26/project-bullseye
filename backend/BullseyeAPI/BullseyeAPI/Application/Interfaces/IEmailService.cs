namespace BullseyeAPI.Application.Interfaces;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string playerName);
    Task SendVerificationEmailAsync(string toEmail, string playerName, string verificationLink);
}
