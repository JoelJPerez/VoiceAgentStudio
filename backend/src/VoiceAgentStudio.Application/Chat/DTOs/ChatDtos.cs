namespace VoiceAgentStudio.Application.Chat.DTOs;

/// <summary>A single message in the chat history sent from the frontend.</summary>
public class ChatMessageDto
{
    public string Role { get; set; } = "user";     // "user" | "assistant"
    public string Content { get; set; } = string.Empty;
}

/// <summary>Payload the frontend sends to start or continue a conversation.</summary>
public class SendMessageDto
{
    public Guid AgentId { get; set; }
    public string UserMessage { get; set; } = string.Empty;

    /// <summary>
    /// Full conversation history excluding the current UserMessage.
    /// Frontend owns the history in Sprint 2 (no DB persistence yet).
    /// </summary>
    public List<ChatMessageDto> History { get; set; } = new();
}

/// <summary>Result of escalation detection.</summary>
public class EscalationResult
{
    public bool ShouldEscalate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string MatchedKeyword { get; set; } = string.Empty;
}

/// <summary>SignalR event types sent to the client.</summary>
public static class ChatHubEvents
{
    public const string Token = "ReceiveToken";
    public const string StreamEnd = "StreamComplete";
    public const string Escalation = "EscalationTriggered";
    public const string Error = "StreamError";
    public const string AgentInfo = "AgentInfo";
}
