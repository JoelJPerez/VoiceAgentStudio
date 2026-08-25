using Microsoft.AspNetCore.SignalR;
using VoiceAgentStudio.Application.Common.Interfaces;

namespace VoiceAgentStudio.API.Hubs;

public class CampaignMonitorNotifier : IRealtimeNotifier
{
    private readonly IHubContext<CampaignMonitorHub> _hub;

    public CampaignMonitorNotifier(IHubContext<CampaignMonitorHub> hub)
        => _hub = hub;

    public Task NotifySessionUpdatedAsync(object payload, Guid campaignId, CancellationToken ct = default)
        => _hub.Clients.Group(campaignId.ToString())
               .SendAsync("SessionUpdated", payload, ct);

    public Task NotifyCampaignCompletedAsync(object payload, Guid campaignId, CancellationToken ct = default)
        => _hub.Clients.Group(campaignId.ToString())
               .SendAsync("CampaignCompleted", payload, ct);
}