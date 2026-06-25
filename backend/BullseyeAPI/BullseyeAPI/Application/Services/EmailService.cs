using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BullseyeAPI.Application.Interfaces;

namespace BullseyeAPI.Application.Services;

// Sends transactional email via Resend's REST API. Registration must never
// fail because an email could not be sent, so every failure path here is
// caught and logged rather than propagated.
public class EmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(HttpClient httpClient, IConfiguration configuration, ILogger<EmailService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public Task SendWelcomeEmailAsync(string toEmail, string playerName)
    {
        var subject = "Welcome to Bullseye!";
        var html = $"<p>Hi {playerName},</p><p>Welcome to Bullseye - your account is ready. Good luck out there!</p>";
        return SendAsync(toEmail, subject, html, "welcome");
    }

    public Task SendVerificationEmailAsync(string toEmail, string playerName, string verificationLink)
    {
        var subject = "Verify your Bullseye email address";
        var html = $"<p>Hi {playerName},</p>"
            + $"<p>Confirm your email address to activate your Bullseye account:</p>"
            + $"<p><a href=\"{verificationLink}\">Verify my email</a></p>"
            + $"<p>This link expires in 24 hours. If you didn't create this account, you can ignore this email.</p>";
        return SendAsync(toEmail, subject, html, "verification");
    }

    private async Task SendAsync(string toEmail, string subject, string html, string emailKind)
    {
        var apiKey = _configuration["Resend:ApiKey"];
        var fromAddress = _configuration["Resend:FromAddress"] ?? "Bullseye <onboarding@resend.dev>";

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Resend:ApiKey is not configured; skipping {EmailKind} email to {Email}.", emailKind, toEmail);
            return;
        }

        try
        {
            var payload = new
            {
                from = fromAddress,
                to = new[] { toEmail },
                subject,
                html
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Resend {EmailKind} email failed ({StatusCode}) for {Email}: {Body}", emailKind, response.StatusCode, toEmail, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending {EmailKind} email to {Email}.", emailKind, toEmail);
        }
    }
}
