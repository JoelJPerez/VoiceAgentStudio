using VoiceAgentStudio.Domain.Common;
using VoiceAgentStudio.Domain.Enums;
using static System.Collections.Specialized.BitVector32;

namespace VoiceAgentStudio.Domain.Entities;

public class Campaign : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Guid AgentId { get; set; }
    public Agent? Agent { get; set; }

    public Guid CreatedByUserId { get; set; }
    public User? CreatedBy { get; set; }

    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}


public class Contact : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CustomContext { get; set; } = string.Empty; // personalisation per contact

    public Guid CampaignId { get; set; }
    public Campaign? Campaign { get; set; }

    public Session? Session { get; set; }
}

public class Session : BaseEntity
{
    public SessionStatus Status { get; set; } = SessionStatus.Pending;
    public IntentionType DetectedIntention { get; set; } = IntentionType.Unknown;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int MessageCount { get; set; } = 0;
    public bool WasEscalated { get; set; } = false;
    public string EscalationReason { get; set; } = string.Empty;

    public Guid CampaignId { get; set; }
    public Campaign? Campaign { get; set; }

    public Guid ContactId { get; set; }
    public Contact? Contact { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

public class Message : BaseEntity
{
    public string Role { get; set; } = "user"; // "user" | "assistant"
    public string Content { get; set; } = string.Empty;
    public int TokensUsed { get; set; } = 0;

    public Guid SessionId { get; set; }
    public Session? Session { get; set; }
}

public enum CampaignStatus
{
    Draft = 1,
    Scheduled = 2,
    Running = 3,
    Paused = 4,
    Completed = 5,
    Cancelled = 6
}
