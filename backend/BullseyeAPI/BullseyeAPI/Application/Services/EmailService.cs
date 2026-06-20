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

    public async Task SendWelcomeEmailAsync(string toEmail, string playerName)
    {
        var apiKey = _configuration["Resend:ApiKey"];
        var fromAddress = _configuration["Resend:FromAddress"] ?? "Bullseye <onboarding@resend.dev>";

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Resend:ApiKey is not configured; skipping welcome email to {Email}.", toEmail);
            return;
        }

        try
        {
            var payload = new
            {
                from = fromAddress,
                to = new[] { toEmail },
                subject = "Welcome to Bullseye!",
                html = $"<p>Hi {playerName},</p><p>Welcome to Bullseye - your account is ready. Good luck out there!</p>"
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
                _logger.LogWarning("Resend welcome email failed ({StatusCode}) for {Email}: {Body}", response.StatusCode, toEmail, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending welcome email to {Email}.", toEmail);
        }
    }
}
