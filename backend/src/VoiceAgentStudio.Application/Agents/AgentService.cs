using VoiceAgentStudio.Application.Agents.DTOs;
using VoiceAgentStudio.Application.Common.Interfaces;
using VoiceAgentStudio.Domain.Entities;
using VoiceAgentStudio.Domain.Enums;

namespace VoiceAgentStudio.Application.Agents;

public interface IAgentService
{
    Task<IEnumerable<AgentSummaryDto>> GetAllAsync(CancellationToken ct = default);
    Task<AgentDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AgentDto> CreateAsync(CreateAgentDto dto, CancellationToken ct = default);
    Task<AgentDto> UpdateAsync(Guid id, UpdateAgentDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<AgentDto> ToggleStatusAsync(Guid id, CancellationToken ct = default);
}

public class AgentService : IAgentService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public AgentService(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<AgentSummaryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var agents = await _uow.Agents.GetByUserIdAsync(userId, ct);

        return agents.Select(MapToSummary);
    }

    public async Task<AgentDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var agent = await GetAgentOwnedByCurrentUser(id, ct);
        return MapToDto(agent);
    }

    public async Task<AgentDto> CreateAsync(CreateAgentDto dto, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var nameExists = await _uow.Agents.NameExistsForUserAsync(dto.Name, userId, ct: ct);
        if (nameExists)
            throw new InvalidOperationException($"An agent named '{dto.Name}' already exists.");

        var agent = new Agent
        {
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            Tone = dto.Tone,
            LlmProvider = dto.LlmProvider,
            ModelName = dto.ModelName,
            Temperature = dto.Temperature,
            MaxTokens = dto.MaxTokens,
            SystemPrompt = dto.SystemPrompt.Trim(),
            Objective = dto.Objective.Trim(),
            CompanyContext = dto.CompanyContext.Trim(),
            EscalationKeywords = dto.EscalationKeywords,
            AutoEscalate = dto.AutoEscalate,
            Status = AgentStatus.Draft,
            CreatedByUserId = userId
        };

        await _uow.Agents.AddAsync(agent, ct);
        await _uow.SaveChangesAsync(ct);

        return MapToDto(agent);
    }

    public async Task<AgentDto> UpdateAsync(Guid id, UpdateAgentDto dto, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var agent = await GetAgentOwnedByCurrentUser(id, ct);

        var nameExists = await _uow.Agents.NameExistsForUserAsync(dto.Name, userId, excludeId: id, ct: ct);
        if (nameExists)
            throw new InvalidOperationException($"An agent named '{dto.Name}' already exists.");

        agent.Name = dto.Name.Trim();
        agent.Description = dto.Description.Trim();
        agent.Tone = dto.Tone;
        agent.LlmProvider = dto.LlmProvider;
        agent.ModelName = dto.ModelName;
        agent.Temperature = dto.Temperature;
        agent.MaxTokens = dto.MaxTokens;
        agent.SystemPrompt = dto.SystemPrompt.Trim();
        agent.Objective = dto.Objective.Trim();
        agent.CompanyContext = dto.CompanyContext.Trim();
        agent.EscalationKeywords = dto.EscalationKeywords;
        agent.AutoEscalate = dto.AutoEscalate;
        agent.Status = dto.Status;
        agent.UpdatedAt = DateTime.UtcNow;

        await _uow.Agents.UpdateAsync(agent, ct);
        await _uow.SaveChangesAsync(ct);

        return MapToDto(agent);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var agent = await GetAgentOwnedByCurrentUser(id, ct);

        // Soft delete
        agent.IsDeleted = true;
        agent.UpdatedAt = DateTime.UtcNow;

        await _uow.Agents.UpdateAsync(agent, ct);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<AgentDto> ToggleStatusAsync(Guid id, CancellationToken ct = default)
    {
        var agent = await GetAgentOwnedByCurrentUser(id, ct);

        agent.Status = agent.Status == AgentStatus.Active
            ? AgentStatus.Inactive
            : AgentStatus.Active;
        agent.UpdatedAt = DateTime.UtcNow;

        await _uow.Agents.UpdateAsync(agent, ct);
        await _uow.SaveChangesAsync(ct);

        return MapToDto(agent);
    }

    // ── Private helpers ───────────────────────────────────────────────

    private async Task<Agent> GetAgentOwnedByCurrentUser(Guid id, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var agent = await _uow.Agents.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Agent {id} not found.");

        if (agent.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("Access denied to this agent.");

        return agent;
    }

    private static AgentDto MapToDto(Agent a) => new()
    {
        Id = a.Id,
        Name = a.Name,
        Description = a.Description,
        Status = a.Status.ToString(),
        Tone = a.Tone.ToString(),
        LlmProvider = a.LlmProvider.ToString(),
        ModelName = a.ModelName,
        Temperature = a.Temperature,
        MaxTokens = a.MaxTokens,
        SystemPrompt = a.SystemPrompt,
        Objective = a.Objective,
        CompanyContext = a.CompanyContext,
        EscalationKeywords = a.EscalationKeywords,
        AutoEscalate = a.AutoEscalate,
        TotalSessions = a.TotalSessions,
        AvgResolutionRate = a.AvgResolutionRate,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt
    };

    private static AgentSummaryDto MapToSummary(Agent a) => new()
    {
        Id = a.Id,
        Name = a.Name,
        Status = a.Status.ToString(),
        Tone = a.Tone.ToString(),
        ModelName = a.ModelName,
        TotalSessions = a.TotalSessions,
        AvgResolutionRate = a.AvgResolutionRate,
        CreatedAt = a.CreatedAt
    };
}
