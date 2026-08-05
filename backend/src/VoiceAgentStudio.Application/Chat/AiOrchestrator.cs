using VoiceAgentStudio.Application.Chat.DTOs;
using VoiceAgentStudio.Application.Common.Interfaces;
using VoiceAgentStudio.Domain.Entities;
using VoiceAgentStudio.Domain.Enums;

namespace VoiceAgentStudio.Application.Chat;

public interface IAiOrchestrator
{
    /// <summary>
    /// Streams AI response tokens for a given agent and conversation.
    /// </summary>
    IAsyncEnumerable<string> StreamResponseAsync(
        Agent agent,
        List<ChatMessageDto> history,
        string userMessage,
        CancellationToken ct = default);

    /// <summary>
    /// Checks whether the user message should trigger escalation to a human.
    /// Called before streaming — escalation takes priority over AI response.
    /// </summary>
    EscalationResult CheckEscalation(Agent agent, string userMessage);
}

public class AiOrchestrator : IAiOrchestrator
{
    private readonly IAiProviderFactory _providerFactory;

    public AiOrchestrator(IAiProviderFactory providerFactory)
        => _providerFactory = providerFactory;

    public IAsyncEnumerable<string> StreamResponseAsync(
        Agent agent,
        List<ChatMessageDto> history,
        string userMessage,
        CancellationToken ct = default)
    {
        var provider = _providerFactory.GetProvider(agent.LlmProvider.ToString());

        var request = new AiCompletionRequest
        {
            SystemPrompt = BuildSystemPrompt(agent),
            ModelName = agent.ModelName,
            Temperature = agent.Temperature,
            MaxTokens = agent.MaxTokens,
            Messages = BuildMessages(history, userMessage)
        };

        return provider.StreamCompletionAsync(request, ct);
    }

    public EscalationResult CheckEscalation(Agent agent, string userMessage)
    {
        if (!agent.AutoEscalate || string.IsNullOrWhiteSpace(agent.EscalationKeywords))
            return new EscalationResult { ShouldEscalate = false };

        var keywords = agent.EscalationKeywords
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var lowered = userMessage.ToLowerInvariant();

        foreach (var keyword in keywords)
        {
            if (lowered.Contains(keyword.ToLowerInvariant()))
            {
                return new EscalationResult
                {
                    ShouldEscalate = true,
                    MatchedKeyword = keyword,
                    Reason = $"Keyword detected: '{keyword}'"
                };
            }
        }

        return new EscalationResult { ShouldEscalate = false };
    }

    // ── Private helpers ───────────────────────────────────────────────

    private static string BuildSystemPrompt(Agent agent)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(agent.SystemPrompt))
            parts.Add(agent.SystemPrompt.Trim());

        if (!string.IsNullOrWhiteSpace(agent.CompanyContext))
            parts.Add($"\nContext about your company:\n{agent.CompanyContext.Trim()}");

        if (!string.IsNullOrWhiteSpace(agent.Objective))
            parts.Add($"\nYour primary objective: {agent.Objective.Trim()}");

        parts.Add("\nKeep responses concise and focused. Maximum 3 sentences unless asked for details.");

        return string.Join("\n", parts);
    }

    private static List<AiChatMessage> BuildMessages(
        List<ChatMessageDto> history,
        string userMessage)
    {
        var messages = history
            .Select(m => new AiChatMessage { Role = m.Role, Content = m.Content })
            .ToList();

        messages.Add(new AiChatMessage { Role = "user", Content = userMessage });
        return messages;
    }
}
