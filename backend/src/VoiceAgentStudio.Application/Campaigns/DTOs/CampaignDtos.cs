using System.ComponentModel.DataAnnotations;
using VoiceAgentStudio.Domain.Entities;

namespace VoiceAgentStudio.Application.Campaigns.DTOs;

// ── Campaign DTOs ─────────────────────────────────────────────────────

public class CampaignDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalContacts { get; set; }
    public int PendingSessions { get; set; }
    public int ActiveSessions { get; set; }
    public int CompletedSessions { get; set; }
    public int TransferredSessions { get; set; }
    public int FailedSessions { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CampaignSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public int TotalContacts { get; set; }
    public int CompletedSessions { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCampaignDto
{
    [Required, MinLength(3), MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public Guid AgentId { get; set; }

    public DateTime? ScheduledAt { get; set; }
}

// ── Contact DTOs ──────────────────────────────────────────────────────

public class ContactDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CustomContext { get; set; } = string.Empty;
    public string? SessionStatus { get; set; }
    public string? DetectedIntention { get; set; }
}

public class CreateContactDto
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CustomContext { get; set; } = string.Empty;
}

// ── Session monitor DTOs ──────────────────────────────────────────────

public class SessionMonitorDto
{
    public Guid Id { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string DetectedIntention { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public bool WasEscalated { get; set; }
    public string EscalationReason { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public List<MessageDto> Messages { get; set; } = new();
}

public class MessageDto
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ── Monitor hub events ────────────────────────────────────────────────

public static class MonitorHubEvents
{
    public const string SessionUpdated = "SessionUpdated";
    public const string CampaignStatusChanged = "CampaignStatusChanged";
    public const string CampaignCompleted = "CampaignCompleted";
}

