//using Microsoft.EntityFrameworkCore;
using VoiceAgentStudio.Application.Analytics.DTOs;
using VoiceAgentStudio.Application.Common.Interfaces;
using VoiceAgentStudio.Domain.Enums;

namespace VoiceAgentStudio.Application.Analytics;

public interface IAnalyticsService
{
    Task<FullAnalyticsDto> GetFullAnalyticsAsync(Guid userId, CancellationToken ct = default);
}

public class AnalyticsService : IAnalyticsService
{
    private readonly IUnitOfWork _uow;

    public AnalyticsService(IUnitOfWork uow) => _uow = uow;

    public async Task<FullAnalyticsDto> GetFullAnalyticsAsync(Guid userId, CancellationToken ct = default)
    {
        // ── Agents ────────────────────────────────────────────────────
        var agents = (await _uow.Agents.GetByUserIdAsync(userId, ct)).ToList();

        // ── Campaigns ─────────────────────────────────────────────────
        var campaigns = (await _uow.Campaigns.GetByUserIdAsync(userId, ct)).ToList();

        // ── Sessions (across all user campaigns) ──────────────────────
        var allSessions = new List<Domain.Entities.Session>();
        foreach (var c in campaigns)
        {
            var sessions = await _uow.Sessions.GetByCampaignIdAsync(c.Id, ct);
            allSessions.AddRange(sessions);
        }

        // ── Stats ──────────────────────────────────────────────────────
        var completed = allSessions.Where(s => s.Status == SessionStatus.Completed).ToList();
        var escalated = allSessions.Where(s => s.WasEscalated).ToList();

        var avgDuration = completed.Any()
            ? completed
                .Where(s => s.StartedAt.HasValue && s.EndedAt.HasValue)
                .Select(s => (s.EndedAt!.Value - s.StartedAt!.Value).TotalSeconds)
                .DefaultIfEmpty(0).Average()
            : 0;

        var stats = new DashboardStatsDto
        {
            TotalAgents = agents.Count,
            ActiveAgents = agents.Count(a => a.Status == AgentStatus.Active),
            TotalCampaigns = campaigns.Count,
            TotalSessions = allSessions.Count,
            CompletedSessions = completed.Count,
            EscalatedSessions = escalated.Count,
            AvgResolutionRate = allSessions.Count > 0
                ? Math.Round((double)completed.Count / allSessions.Count * 100, 1)
                : 0,
            AvgSessionDurationSeconds = Math.Round(avgDuration, 0)
        };

        // ── By status ──────────────────────────────────────────────────
        var byStatus = allSessions
            .GroupBy(s => s.Status.ToString())
            .Select(g => new SessionsByStatusDto { Status = g.Key, Count = g.Count() })
            .ToList();

        // ── By intention ───────────────────────────────────────────────
        var byIntention = allSessions
            .Where(s => s.DetectedIntention != IntentionType.Unknown)
            .GroupBy(s => s.DetectedIntention.ToString())
            .Select(g => new SessionsByIntentionDto { Intention = g.Key, Count = g.Count() })
            .ToList();

        // ── By day (last 7 days) ───────────────────────────────────────
        var last7 = Enumerable.Range(0, 7)
            .Select(i => DateTime.UtcNow.Date.AddDays(-i))
            .OrderBy(d => d)
            .ToList();

        var byDay = last7.Select(day =>
        {
            var daySessions = allSessions.Where(s =>
                s.CreatedAt.Date == day.Date).ToList();
            return new SessionsByDayDto
            {
                Date = day.ToString("dd/MM"),
                Total = daySessions.Count,
                Completed = daySessions.Count(s => s.Status == SessionStatus.Completed),
                Escalated = daySessions.Count(s => s.WasEscalated)
            };
        }).ToList();

        // ── Agent performance ─────────────────────────────────────────
        var agentPerformance = agents.Select(agent =>
        {
            var agentCampaignIds = campaigns
                .Where(c => c.AgentId == agent.Id)
                .Select(c => c.Id).ToHashSet();

            var agentSessions = allSessions
                .Where(s => agentCampaignIds.Contains(s.CampaignId)).ToList();

            var agentCompleted = agentSessions.Count(s => s.Status == SessionStatus.Completed);

            return new AgentPerformanceDto
            {
                Id = agent.Id,
                Name = agent.Name,
                ModelName = agent.ModelName,
                TotalSessions = agentSessions.Count,
                CompletedSessions = agentCompleted,
                EscalatedSessions = agentSessions.Count(s => s.WasEscalated),
                ResolutionRate = agentSessions.Count > 0
                    ? Math.Round((double)agentCompleted / agentSessions.Count * 100, 1)
                    : 0
            };
        }).OrderByDescending(a => a.TotalSessions).ToList();

        // ── Campaign stats ────────────────────────────────────────────
        var campaignStats = campaigns.Select(c =>
        {
            var cSessions = allSessions.Where(s => s.CampaignId == c.Id).ToList();
            var cCompleted = cSessions.Count(s => s.Status == SessionStatus.Completed);

            return new CampaignSummaryStatsDto
            {
                Id = c.Id,
                Name = c.Name,
                Status = c.Status.ToString(),
                TotalContacts = c.Contacts?.Count ?? 0,
                CompletedSessions = cCompleted,
                EscalatedSessions = cSessions.Count(s => s.WasEscalated),
                CompletionRate = cSessions.Count > 0
                    ? Math.Round((double)cCompleted / cSessions.Count * 100, 1)
                    : 0
            };
        }).OrderByDescending(c => c.TotalContacts).ToList();

        return new FullAnalyticsDto
        {
            Stats = stats,
            ByStatus = byStatus,
            ByIntention = byIntention,
            ByDay = byDay,
            AgentPerformance = agentPerformance,
            CampaignStats = campaignStats
        };
    }
}
