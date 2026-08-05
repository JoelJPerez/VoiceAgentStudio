using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using VoiceAgentStudio.Application.Common.Interfaces;

namespace VoiceAgentStudio.Infrastructure.AI;

/// <summary>
/// OpenAI provider using Chat Completions with streaming.
/// Swap to this by setting DefaultAiProvider: "OpenAI" in appsettings.json.
/// </summary>
public class OpenAiProvider : IAiProvider
{
    public string ProviderName => "OpenAI";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string Url = "https://api.openai.com/v1/chat/completions";

    public OpenAiProvider(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClient = httpClientFactory.CreateClient("OpenAI");
        _apiKey = config["OpenAI:ApiKey"] ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamCompletionAsync(
        AiCompletionRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

        var messages = new List<object>
        {
            new { role = "system", content = request.SystemPrompt }
        };
        messages.AddRange(request.Messages.Select(m => new { role = m.Role, content = m.Content }));

        var body = new
        {
            model = request.ModelName,
            messages,
            stream = true,
            temperature = request.Temperature,
            max_tokens = request.MaxTokens
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, Url)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.SendAsync(
            httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null || !line.StartsWith("data: ")) continue;

            var json = line["data: "..].Trim();
            if (json == "[DONE]") break;

            var token = ExtractTextToken(json);
            if (token is not null)
                yield return token;
        }
    }

    private static string? ExtractTextToken(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("delta")
                .GetProperty("content")
                .GetString();
        }
        catch { return null; }
    }
}
