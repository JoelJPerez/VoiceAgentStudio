using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using VoiceAgentStudio.Application.Chat;
using VoiceAgentStudio.Application.Chat.DTOs;
using VoiceAgentStudio.Application.Common.Interfaces;

namespace VoiceAgentStudio.API.Hubs;

/// <summary>
/// SignalR hub for real-time AI conversation streaming.
///
/// Client → Server: SendMessage(agentId, userMessage, history[])
/// Server → Client:
///   ReceiveToken(string token)       — one chunk at a time
///   StreamComplete(string fullText)  — full assembled response when done
///   EscalationTriggered(object info) — escalation detected
///   StreamError(string message)      — something went wrong
///   AgentInfo(object info)           — agent metadata on connect
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly IUnitOfWork _uow;
    private readonly IAiOrchestrator _orchestrator;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IUnitOfWork uow, IAiOrchestrator orchestrator, ILogger<ChatHub> logger)
    {
        _uow = uow;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Called by the Angular client to send a message and receive streamed tokens.
    /// </summary>
    public async Task SendMessage(SendMessageDto dto)
    {
        var ct = Context.ConnectionAborted;

        try
        {
            // 1. Load agent and validate ownership
            var agent = await _uow.Agents.GetByIdAsync(dto.AgentId, ct)
                ?? throw new HubException($"Agent {dto.AgentId} not found.");

            // 2. Check for escalation BEFORE calling the AI
            var escalation = _orchestrator.CheckEscalation(agent, dto.UserMessage);
            if (escalation.ShouldEscalate)
            {
                await Clients.Caller.SendAsync(ChatHubEvents.Escalation, new
                {
                    reason = escalation.Reason,
                    matchedKeyword = escalation.MatchedKeyword,
                    agentName = agent.Name
                }, ct);
                return;
            }

            // 3. Stream tokens back to the caller
            var fullText = new System.Text.StringBuilder();

            await foreach (var token in _orchestrator.StreamResponseAsync(
                agent, dto.History, dto.UserMessage, ct))
            {
                fullText.Append(token);
                await Clients.Caller.SendAsync(ChatHubEvents.Token, token, ct);
            }

            // 4. Signal stream complete with full assembled text
            await Clients.Caller.SendAsync(
                ChatHubEvents.StreamEnd, fullText.ToString(), ct);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected mid-stream — normal, no need to log as error
            _logger.LogInformation("Stream cancelled for connection {Id}", Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming response for agent {AgentId}", dto.AgentId);
            await Clients.Caller.SendAsync(ChatHubEvents.Error, ex.Message, ct);
        }
    }

    /// <summary>
    /// Called when the client connects to a specific agent chat session.
    /// Sends back agent metadata so the UI can display the agent's name and tone.
    /// </summary>
    public async Task JoinAgentSession(Guid agentId)
    {
        var agent = await _uow.Agents.GetByIdAsync(agentId, Context.ConnectionAborted);
        if (agent is null) return;

        await Clients.Caller.SendAsync(ChatHubEvents.AgentInfo, new
        {
            id = agent.Id,
            name = agent.Name,
            tone = agent.Tone.ToString(),
            objective = agent.Objective,
            modelName = agent.ModelName
        });
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("ChatHub connected: {Id}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("ChatHub disconnected: {Id}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
