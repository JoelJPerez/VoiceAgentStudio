using VoiceAgentStudio.Domain.Common;
using VoiceAgentStudio.Domain.Enums;

namespace VoiceAgentStudio.Domain.Entities;

public class Agent : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AgentStatus Status { get; set; } = AgentStatus.Draft;
    public AgentTone Tone { get; set; } = AgentTone.Professional;

    // LLM configuration
    public LlmProvider LlmProvider { get; set; } = LlmProvider.OpenAI;
    public string ModelName { get; set; } = "gpt-4o";
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 500;

    // Prompt configuration
    public string SystemPrompt { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;      // e.g. "Schedule a demo call"
    public string CompanyContext { get; set; } = string.Empty;  // e.g. "You work for Acme Corp..."
    public string EscalationKeywords { get; set; } = string.Empty; // comma-separated: "angry,refund,cancel"
    public bool AutoEscalate { get; set; } = true;

    // Stats(computed)
    public int TotalSessions { get; set; } = 0;
    public float AvgResolutionRate { get; set; } = 0;

    // Relations
    public Guid CreatedByUserId { get; set; }
    public User? CreatedBy { get; set; }
    public ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();

}
