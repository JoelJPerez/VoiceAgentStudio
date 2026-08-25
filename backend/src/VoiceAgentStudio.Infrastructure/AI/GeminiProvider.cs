using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using VoiceAgentStudio.Application.Common.Interfaces;

namespace VoiceAgentStudio.Infrastructure.AI;

/// <summary>
/// Google Gemini provider using the REST API with Server-Sent Events streaming.
/// Free tier: gemini-2.0-flash — 15 RPM, 1M TPM.
/// </summary>
public class GeminiProvider : IAiProvider
{
    public string ProviderName => "Gemini";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string BaseUrl =
        "https://generativelanguage.googleapis.com/v1beta/models";

    public GeminiProvider(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClient = httpClientFactory.CreateClient("Gemini");
        _apiKey = config["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey not configured.");
    }

    public async IAsyncEnumerable<string> StreamCompletionAsync(
        AiCompletionRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var model = string.IsNullOrWhiteSpace(request.ModelName) ? "gemini-2.0-flash" : request.ModelName;
        var url = $"{BaseUrl}/{model}:streamGenerateContent?alt=sse&key={_apiKey}";
        var body = BuildRequestBody(request);
        var content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                ct);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Gemini HTTP request failed: {ex.Message}", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Gemini API error {(int)response.StatusCode}: {error}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) continue;

            // SSE lines start with "data: "
            if (!line.StartsWith("data: ")) continue;

            var json = line["data: ".Length..].Trim();
            if (string.IsNullOrEmpty(json)) continue;

            var token = ExtractTextToken(json);
            if (token is not null)
                yield return token;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static object BuildRequestBody(AiCompletionRequest request)
    {
        // Convert messages: Gemini uses "user" / "model" (not "assistant")
        var contents = request.Messages.Select(m => new
        {
            role = m.Role == "assistant" ? "model" : "user",
            parts = new[] { new { text = m.Content } }
        }).ToList();

        return new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = request.SystemPrompt } }
            },
            contents,
            generationConfig = new
            {
                temperature = request.Temperature,
                maxOutputTokens = request.MaxTokens
            }
        };
    }

    private static string? ExtractTextToken(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("candidates", out var candidates)) return null;
            if (candidates.GetArrayLength() == 0) return null;

            var firstCandidate = candidates[0];
            if (!firstCandidate.TryGetProperty("content", out var content)) return null;
            if (!content.TryGetProperty("parts", out var parts)) return null;
            if (parts.GetArrayLength() == 0) return null;

            var text = parts[0].GetProperty("text").GetString();
            return string.IsNullOrEmpty(text) ? null : text;
        }
        catch
        {
            return null; // Ignore malformed chunks
        }
    }
}
