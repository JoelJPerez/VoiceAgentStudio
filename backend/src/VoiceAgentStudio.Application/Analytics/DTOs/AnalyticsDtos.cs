namespace VoiceAgentStudio.Application.Analytics.DTOs;

public class DashboardStatsDto
{
    public int TotalAgents { get; set; }
    public int ActiveAgents { get; set; }
    public int TotalCampaigns { get; set; }
    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }
    public int EscalatedSessions { get; set; }
    public double AvgResolutionRate { get; set; }
    public double AvgSessionDurationSeconds { get; set; }
}

public class SessionsByStatusDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class SessionsByIntentionDto
{
    public string Intention { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class SessionsByDayDto
{
    public string Date { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Escalated { get; set; }
}

public class AgentPerformanceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }
    public int EscalatedSessions { get; set; }
    public double ResolutionRate { get; set; }
}

public class CampaignSummaryStatsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalContacts { get; set; }
    public int CompletedSessions { get; set; }
    public int EscalatedSessions { get; set; }
    public double CompletionRate { get; set; }
}

public class FullAnalyticsDto
{
    public DashboardStatsDto Stats { get; set; } = new();
    public List<SessionsByStatusDto> ByStatus { get; set; } = new();
    public List<SessionsByIntentionDto> ByIntention { get; set; } = new();
    public List<SessionsByDayDto> ByDay { get; set; } = new();
    public List<AgentPerformanceDto> AgentPerformance { get; set; } = new();
    public List<CampaignSummaryStatsDto> CampaignStats { get; set; } = new();
}
