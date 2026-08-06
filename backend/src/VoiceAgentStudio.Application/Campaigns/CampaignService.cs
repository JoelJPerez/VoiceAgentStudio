using VoiceAgentStudio.Application.Campaigns.DTOs;
using VoiceAgentStudio.Application.Common.Interfaces;
using VoiceAgentStudio.Domain.Entities;

namespace VoiceAgentStudio.Application.Campaigns;

public interface ICampaignService
{
    Task<IEnumerable<CampaignSummaryDto>> GetAllAsync(CancellationToken ct = default);
    Task<CampaignDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CampaignDto> CreateAsync(CreateCampaignDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<int> ImportContactsAsync(Guid campaignId, Stream csvStream, CancellationToken ct = default);
    Task<CampaignDto> StartAsync(Guid id, CancellationToken ct = default);
    Task<CampaignDto> PauseAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<SessionMonitorDto>> GetSessionsAsync(Guid campaignId, CancellationToken ct = default);
    Task<SessionMonitorDto?> GetSessionDetailAsync(Guid sessionId, CancellationToken ct = default);
}

public class CampaignService : ICampaignService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly ICsvContactParser _csvParser;
    private readonly ICampaignExecutionQueue _executionQueue;

    public CampaignService(
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        ICsvContactParser csvParser,
        ICampaignExecutionQueue executionQueue)
    {
        _uow = uow;
        _currentUser = currentUser;
        _csvParser = csvParser;
        _executionQueue = executionQueue;
    }

    public async Task<IEnumerable<CampaignSummaryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var campaigns = await _uow.Campaigns.GetByUserIdAsync(userId, ct);
        return campaigns.Select(MapToSummary);
    }

    public async Task<CampaignDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var campaign = await _uow.Campaigns.GetWithDetailsAsync(id, ct);
        return campaign is null ? null : MapToDto(campaign);
    }

    public async Task<CampaignDto> CreateAsync(CreateCampaignDto dto, CancellationToken ct = default)
    {
        var userId = RequireUserId();

        // Validate agent exists and belongs to user
        var agent = await _uow.Agents.GetByIdAsync(dto.AgentId, ct)
            ?? throw new KeyNotFoundException("Agent not found.");

        if (agent.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("Agent does not belong to you.");

        var campaign = new Campaign
        {
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            AgentId = dto.AgentId,
            ScheduledAt = dto.ScheduledAt,
            Status = CampaignStatus.Draft,
            CreatedByUserId = userId
        };

        await _uow.Campaigns.AddAsync(campaign, ct);
        await _uow.SaveChangesAsync(ct);

        return MapToDto(campaign);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var campaign = await GetOwnedCampaign(id, ct);

        if (campaign.Status == CampaignStatus.Running)
            throw new InvalidOperationException("Cannot delete a running campaign. Pause it first.");

        campaign.IsDeleted = true;
        campaign.UpdatedAt = DateTime.UtcNow;
        await _uow.Campaigns.UpdateAsync(campaign, ct);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<int> ImportContactsAsync(
        Guid campaignId, Stream csvStream, CancellationToken ct = default)
    {
        var campaign = await GetOwnedCampaign(campaignId, ct);

        if (campaign.Status == CampaignStatus.Running)
            throw new InvalidOperationException("Cannot import contacts to a running campaign.");

        var parsed = _csvParser.Parse(csvStream).Where(p => p.IsValid).ToList();
        if (parsed.Count == 0)
            throw new InvalidOperationException("No valid contacts found in CSV.");

        var contacts = parsed.Select(p => new Contact
        {
            FullName = p.FullName.Trim(),
            PhoneNumber = p.PhoneNumber.Trim(),
            Email = p.Email.Trim(),
            CustomContext = p.CustomContext.Trim(),
            CampaignId = campaignId
        });

        await _uow.Contacts.AddRangeAsync(contacts, ct);
        await _uow.SaveChangesAsync(ct);
        return parsed.Count;
    }

    public async Task<CampaignDto> StartAsync(Guid id, CancellationToken ct = default)
    {
        var campaign = await _uow.Campaigns.GetWithDetailsAsync(id, ct)
            ?? throw new KeyNotFoundException("Campaign not found.");

        if (campaign.Contacts.Count == 0)
            throw new InvalidOperationException("Add contacts before starting the campaign.");

        if (campaign.Status == CampaignStatus.Running)
            throw new InvalidOperationException("Campaign is already running.");

        // Create pending sessions for contacts that don't have one yet
        var existingSessionContactIds = campaign.Sessions.Select(s => s.ContactId).ToHashSet();
        var newSessions = campaign.Contacts
            .Where(c => !existingSessionContactIds.Contains(c.Id))
            .Select(c => new Session
            {
                CampaignId = campaign.Id,
                ContactId = c.Id,
                Status = Domain.Enums.SessionStatus.Pending
            }).ToList();

        foreach (var s in newSessions)
            await _uow.Sessions.AddAsync(s, ct);

        campaign.Status = CampaignStatus.Running;
        campaign.StartedAt ??= DateTime.UtcNow;
        campaign.UpdatedAt = DateTime.UtcNow;

        await _uow.Campaigns.UpdateAsync(campaign, ct);
        await _uow.SaveChangesAsync(ct);

        // Hand off to background execution service
        _executionQueue.Enqueue(campaign.Id);

        return MapToDto(campaign);
    }

    public async Task<CampaignDto> PauseAsync(Guid id, CancellationToken ct = default)
    {
        var campaign = await GetOwnedCampaign(id, ct);

        if (campaign.Status != CampaignStatus.Running)
            throw new InvalidOperationException("Campaign is not running.");

        campaign.Status = CampaignStatus.Paused;
        campaign.UpdatedAt = DateTime.UtcNow;
        await _uow.Campaigns.UpdateAsync(campaign, ct);
        await _uow.SaveChangesAsync(ct);
        return MapToDto(campaign);
    }

    public async Task<IEnumerable<SessionMonitorDto>> GetSessionsAsync(
        Guid campaignId, CancellationToken ct = default)
    {
        var sessions = await _uow.Sessions.GetByCampaignIdAsync(campaignId, ct);
        return sessions.Select(MapSessionToDto);
    }

    public async Task<SessionMonitorDto?> GetSessionDetailAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _uow.Sessions.GetWithMessagesAsync(sessionId, ct);
        return session is null ? null : MapSessionToDetailDto(session);
    }

    // ── Private helpers ───────────────────────────────────────────────

    private async Task<Campaign> GetOwnedCampaign(Guid id, CancellationToken ct)
    {
        var userId = RequireUserId();
        var campaign = await _uow.Campaigns.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("Campaign not found.");
        if (campaign.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("Access denied.");
        return campaign;
    }

    private Guid RequireUserId()
        => _currentUser.UserId ?? throw new UnauthorizedAccessException("Not authenticated.");

    private static CampaignDto MapToDto(Campaign c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        Status = c.Status.ToString(),
        AgentId = c.AgentId,
        AgentName = c.Agent?.Name ?? string.Empty,
        ScheduledAt = c.ScheduledAt,
        StartedAt = c.StartedAt,
        CompletedAt = c.CompletedAt,
        TotalContacts = c.Contacts?.Count ?? 0,
        PendingSessions = c.Sessions?.Count(s => s.Status == Domain.Enums.SessionStatus.Pending) ?? 0,
        ActiveSessions = c.Sessions?.Count(s => s.Status == Domain.Enums.SessionStatus.Active) ?? 0,
        CompletedSessions = c.Sessions?.Count(s => s.Status == Domain.Enums.SessionStatus.Completed) ?? 0,
        TransferredSessions = c.Sessions?.Count(s => s.Status == Domain.Enums.SessionStatus.Transferred) ?? 0,
        FailedSessions = c.Sessions?.Count(s => s.Status == Domain.Enums.SessionStatus.Failed) ?? 0,
        CreatedAt = c.CreatedAt
    };

    private static CampaignSummaryDto MapToSummary(Campaign c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Status = c.Status.ToString(),
        AgentName = c.Agent?.Name ?? string.Empty,
        TotalContacts = c.Contacts?.Count ?? 0,
        CompletedSessions = c.Sessions?.Count(s => s.Status == Domain.Enums.SessionStatus.Completed) ?? 0,
        CreatedAt = c.CreatedAt
    };

    private static SessionMonitorDto MapSessionToDto(Session s) => new()
    {
        Id = s.Id,
        ContactName = s.Contact?.FullName ?? string.Empty,
        PhoneNumber = s.Contact?.PhoneNumber ?? string.Empty,
        Status = s.Status.ToString(),
        DetectedIntention = s.DetectedIntention.ToString(),
        MessageCount = s.MessageCount,
        WasEscalated = s.WasEscalated,
        EscalationReason = s.EscalationReason,
        StartedAt = s.StartedAt,
        EndedAt = s.EndedAt
    };

    private static SessionMonitorDto MapSessionToDetailDto(Session s)
    {
        var dto = MapSessionToDto(s);
        dto.Messages = s.Messages?
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageDto
            {
                Role = m.Role,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            }).ToList() ?? new();
        return dto;
    }
}
