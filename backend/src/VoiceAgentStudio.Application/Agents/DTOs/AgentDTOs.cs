using VoiceAgentStudio.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace VoiceAgentStudio.Application.Agents.DTOs;

public class AgentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
    public string LlmProvider { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public float Temperature { get; set; }
    public int MaxTokens { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
    public string CompanyContext { get; set; } = string.Empty;
    public string EscalationKeywords { get; set; } = string.Empty;
    public bool AutoEscalate { get; set; }
    public int TotalSessions { get; set; }
    public float AvgResolutionRate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateAgentDto
{
    [Required, MinLength(3), MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public AgentTone Tone { get; set; } = AgentTone.Professional;
    public LlmProvider LlmProvider { get; set; } = LlmProvider.OpenAI;
    public string ModelName { get; set; } = "gpt-4o";

    [Range(0.0, 2.0)]
    public float Temperature { get; set; } = 0.7f;

    [Range(100, 4000)]
    public int MaxTokens { get; set; } = 500;

    [Required, MinLength(20)]
    public string SystemPrompt { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Objective { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string CompanyContext { get; set; } = string.Empty;

    public string EscalationKeywords { get; set; } = "angry,cancel,refund,lawsuit,supervisor";
    public bool AutoEscalate { get; set; } = true;
}

public class UpdateAgentDto : CreateAgentDto
{
    public AgentStatus Status { get; set; } = AgentStatus.Draft;
}

public class AgentSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public float AvgResolutionRate { get; set; }
    public DateTime CreatedAt { get; set; }
}

