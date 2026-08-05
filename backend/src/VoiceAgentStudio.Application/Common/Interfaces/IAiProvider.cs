namespace VoiceAgentStudio.Application.Common.Interfaces;

// ── Provider abstraction ─────────────────────────────────────────────
// Any LLM (Gemini, OpenAI, Anthropic) implements this interface.
// Switching provider = change one value in appsettings.json.

public interface IAiProvider
{
    string ProviderName { get; }

    /// <summary>
    /// Streams completion tokens as they are generated.
    /// Each yielded string is a partial text chunk.
    /// </summary>
    IAsyncEnumerable<string> StreamCompletionAsync(
        AiCompletionRequest request,
        CancellationToken ct = default);
}

public interface IAiProviderFactory
{
    IAiProvider GetProvider(string providerName);
    IAiProvider GetDefault();
}

// ── Request / Response models ────────────────────────────────────────

public class AiCompletionRequest
{
    public string SystemPrompt { get; set; } = string.Empty;
    public List<AiChatMessage> Messages { get; set; } = new();
    public string ModelName { get; set; } = string.Empty;
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 500;
}

public class AiChatMessage
{
    public string Role { get; set; } = string.Empty;   // "user" | "assistant"
    public string Content { get; set; } = string.Empty;
}
