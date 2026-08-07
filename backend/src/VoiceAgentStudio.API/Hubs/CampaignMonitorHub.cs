using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace VoiceAgentStudio.API.Hubs;

/// <summary>
/// SignalR hub for campaign execution monitoring.
/// Clients join a group per campaign and receive live session updates.
///
/// Client → Server: JoinCampaign(campaignId)  — subscribe to a campaign
/// Server → Client: SessionUpdated(dto)        — session status changed
///                  CampaignCompleted(dto)      — all sessions done
/// </summary>
[Authorize]
public class CampaignMonitorHub : Hub
{
    private readonly ILogger<CampaignMonitorHub> _logger;

    public CampaignMonitorHub(ILogger<CampaignMonitorHub> logger)
        => _logger = logger;

    public async Task JoinCampaign(string campaignId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, campaignId);
        _logger.LogInformation(
            "Connection {ConnId} joined campaign group {CampaignId}",
            Context.ConnectionId, campaignId);
    }

    public async Task LeaveCampaign(string campaignId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, campaignId);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Monitor disconnected: {Id}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
