using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VoiceAgentStudio.Application.Campaigns.DTOs;
using VoiceAgentStudio.Application.Chat;
using VoiceAgentStudio.Application.Chat.DTOs;
using VoiceAgentStudio.Application.Common.Interfaces;
using VoiceAgentStudio.Domain.Entities;
using VoiceAgentStudio.Domain.Enums;

namespace VoiceAgentStudio.Infrastructure.Campaigns;

// ── Execution queue ───────────────────────────────────────────────────

public class CampaignExecutionQueue : ICampaignExecutionQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true });

    public void Enqueue(Guid campaignId)
        => _channel.Writer.TryWrite(campaignId);

    public ChannelReader<Guid> Reader => _channel.Reader;
}

// ── Background service ────────────────────────────────────────────────

/// <summary>
/// Processes queued campaigns in the background.
/// For each pending session: calls the AI, builds a simulated conversation,
/// persists messages, and broadcasts updates via SignalR.
/// </summary>
public class CampaignExecutionService : BackgroundService
{
    private readonly CampaignExecutionQueue _queue;
    private readonly IServiceProvider _services;
    private readonly ILogger<CampaignExecutionService> _logger;

    // Delay between sessions to simulate real pacing (not overwhelm free-tier API)
    private static readonly TimeSpan SessionDelay = TimeSpan.FromSeconds(2);

    public CampaignExecutionService(
        CampaignExecutionQueue queue,
        IServiceProvider services,
        ILogger<CampaignExecutionService> logger)
    {
        _queue = queue;
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var campaignId in _queue.Reader.ReadAllAsync(ct))
        {
            try
            {
                await ProcessCampaignAsync(campaignId, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Campaign execution failed: {Id}", campaignId);
            }
        }
    }

    private async Task ProcessCampaignAsync(Guid campaignId, CancellationToken ct)
    {
        _logger.LogInformation("Starting execution for campaign {Id}", campaignId);

        using var scope = _services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var orchestrator = scope.ServiceProvider.GetRequiredService<IAiOrchestrator>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<API.Hubs.CampaignMonitorHub>>();

        while (!ct.IsCancellationRequested)
        {
            // Re-check campaign status on each iteration (supports Pause)
            var campaign = await uow.Campaigns.GetWithDetailsAsync(campaignId, ct);
            if (campaign is null || campaign.Status != CampaignStatus.Running) break;

            var pendingSessions = await uow.Sessions.GetPendingAsync(campaignId, ct);
            var pending = pendingSessions.ToList();

            if (pending.Count == 0)
            {
                // All sessions processed — mark campaign complete
                campaign.Status = CampaignStatus.Completed;
                campaign.CompletedAt = DateTime.UtcNow;
                campaign.UpdatedAt = DateTime.UtcNow;
                await uow.Campaigns.UpdateAsync(campaign, ct);
                await uow.SaveChangesAsync(ct);

                await hubContext.Clients.Group(campaignId.ToString())
                    .SendAsync(MonitorHubEvents.CampaignCompleted, new
                    {
                        campaignId,
                        completedAt = campaign.CompletedAt
                    }, ct);
                break;
            }

            // Process one session at a time
            var session = pending.First();
            await ProcessSessionAsync(session, campaign.Agent!, uow, orchestrator, hubContext, ct);

            await Task.Delay(SessionDelay, ct);
        }
    }

    private async Task ProcessSessionAsync(
        Session session,
        Agent agent,
        IUnitOfWork uow,
        IAiOrchestrator orchestrator,
        IHubContext<API.Hubs.CampaignMonitorHub> hubContext,
        CancellationToken ct)
    {
        // Load contact for personalization
        var contact = await uow.Contacts.GetByIdAsync(session.ContactId, ct);
        if (contact is null) return;

        // ── 1. Mark session as Active ─────────────────────────────────
        session.Status = SessionStatus.Active;
        session.StartedAt = DateTime.UtcNow;
        await uow.Sessions.UpdateAsync(session, ct);
        await uow.SaveChangesAsync(ct);
        await BroadcastSessionUpdate(hubContext, session, contact, ct);

        var conversationHistory = new List<ChatMessageDto>();
        var messagesToSave = new List<Message>();

        try
        {
            // ── 2. Generate opening user message (simulates customer pickup) ──
            var openingMessage = BuildOpeningMessage(contact, agent);
            conversationHistory.Add(new ChatMessageDto { Role = "user", Content = openingMessage });
            messagesToSave.Add(new Message
            {
                SessionId = session.Id,
                Role = "user",
                Content = openingMessage
            });

            // ── 3. Check for escalation before calling AI ─────────────
            var escalation = orchestrator.CheckEscalation(agent, openingMessage);
            if (escalation.ShouldEscalate)
            {
                session.WasEscalated = true;
                session.EscalationReason = escalation.Reason;
                session.Status = SessionStatus.Transferred;
                session.DetectedIntention = IntentionType.NeedsHuman;
            }
            else
            {
                // ── 4. Get AI response (collect streamed tokens) ──────────
                var aiResponseText = await CollectStreamAsync(
                    orchestrator, agent, conversationHistory, openingMessage, ct);

                conversationHistory.Add(new ChatMessageDto { Role = "assistant", Content = aiResponseText });
                messagesToSave.Add(new Message
                {
                    SessionId = session.Id,
                    Role = "assistant",
                    Content = aiResponseText
                });

                // ── 5. Determine session outcome ──────────────────────────
                session.DetectedIntention = DetectIntention(aiResponseText);
                session.Status = session.DetectedIntention == IntentionType.NeedsHuman
                    ? SessionStatus.Transferred
                    : SessionStatus.Completed;
            }

            session.MessageCount = messagesToSave.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Session {Id} failed: {Msg}", session.Id, ex.Message);
            session.Status = SessionStatus.Failed;
            session.EscalationReason = ex.Message;
        }

        // ── 6. Persist messages and finalize session ──────────────────
        session.EndedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        await uow.Messages.AddRangeAsync(messagesToSave, ct);
        await uow.Sessions.UpdateAsync(session, ct);
        await uow.SaveChangesAsync(ct);

        await BroadcastSessionUpdate(hubContext, session, contact, ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static async Task<string> CollectStreamAsync(
        IAiOrchestrator orchestrator,
        Agent agent,
        List<ChatMessageDto> history,
        string userMessage,
        CancellationToken ct)
    {
        var sb = new System.Text.StringBuilder();
        await foreach (var token in orchestrator.StreamResponseAsync(agent, history, userMessage, ct))
            sb.Append(token);
        return sb.ToString();
    }

    private static string BuildOpeningMessage(Contact contact, Agent agent)
    {
        var base64 = !string.IsNullOrWhiteSpace(contact.CustomContext)
            ? $" Additional context: {contact.CustomContext}."
            : string.Empty;

        return $"Hello, my name is {contact.FullName}.{base64} I'm available to talk.";
    }

    private static IntentionType DetectIntention(string response)
    {
        var lower = response.ToLowerInvariant();

        if (lower.Contains("transfer") || lower.Contains("human agent") || lower.Contains("supervisor"))
            return IntentionType.NeedsHuman;
        if (lower.Contains("thank") || lower.Contains("perfect") || lower.Contains("great"))
            return IntentionType.Satisfied;
        if (lower.Contains("not interested") || lower.Contains("cancel") || lower.Contains("no, thank"))
            return IntentionType.Objection;
        if (lower.Contains("schedule") || lower.Contains("appointment") || lower.Contains("demo"))
            return IntentionType.Closed;

        return IntentionType.Interested;
    }

    private static async Task BroadcastSessionUpdate(
        IHubContext<API.Hubs.CampaignMonitorHub> hub,
        Session session,
        Contact contact,
        CancellationToken ct)
    {
        await hub.Clients.Group(session.CampaignId.ToString())
            .SendAsync(MonitorHubEvents.SessionUpdated, new
            {
                id = session.Id,
                contactName = contact.FullName,
                phoneNumber = contact.PhoneNumber,
                status = session.Status.ToString(),
                detectedIntention = session.DetectedIntention.ToString(),
                messageCount = session.MessageCount,
                wasEscalated = session.WasEscalated,
                startedAt = session.StartedAt,
                endedAt = session.EndedAt
            }, ct);
    }
}
