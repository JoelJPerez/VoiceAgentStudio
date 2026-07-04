namespace VoiceAgentStudio.Domain.Enums;

public enum AgentStatus
{
    Active = 1,
    Inactive = 2,
    Draft = 3
}

public enum AgentTone
{
    Professional = 1,
    Friendly = 2,
    Formal = 3,
    Empathetic = 4
}
public enum LlmProvider
{
    OpenAI = 1,
    Anthropic = 2
}

public enum SessionStatus
{
    Pending = 1,
    Active = 2,
    Transferred = 3,
    Completed = 4,
    Failed = 5
}

public enum IntentionType
{
    Unknown = 0,
    Satisfied = 1,
    NeedsHuman = 2,
    Objection = 3,
    Interested = 4,
    Closed = 5
}