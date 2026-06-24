using System.Text;
using System.Text.Json;
using BullseyeAPI.Application.Interfaces;

namespace BullseyeAPI.Application.Services;

// HTTP client for the Dartbot endpoint exposed by the Python ai-service.
// Mirrors EmailService's resilience approach: a down or slow AI service must
// never block or fail the human player's turn submission, so every failure
// path here is caught and logged rather than propagated.
public class AiDartbotClient : IAiDartbotClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiDartbotClient> _logger;

    public AiDartbotClient(HttpClient httpClient, IConfiguration configuration, ILogger<AiDartbotClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var baseUrl = configuration["AiService:BaseUrl"] ?? "http://localhost:8000";
        var timeoutSeconds = configuration.GetValue<int?>("AiService:TimeoutSeconds") ?? 5;

        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }

    public async Task<int?> GetBotTurnTotalAsync(int remainingScore, string variant, string difficulty)
    {
        try
        {
            var payload = new { remainingScore, variant, difficulty };
            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/bot/throw", content);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Dartbot service returned {StatusCode}; skipping bot turn.", response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<BotThrowResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result?.TotalPoints;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not reach Dartbot service; skipping bot turn.");
            return null;
        }
    }

    private class BotThrowResponse
    {
        public int TotalPoints { get; set; }
    }
}
