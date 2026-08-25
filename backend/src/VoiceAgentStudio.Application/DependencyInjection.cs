using Microsoft.Extensions.DependencyInjection;
using VoiceAgentStudio.Application.Agents;
using VoiceAgentStudio.Application.Analytics;
using VoiceAgentStudio.Application.Auth;
using VoiceAgentStudio.Application.Campaigns;
using VoiceAgentStudio.Application.Chat;

namespace VoiceAgentStudio.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAiOrchestrator, AiOrchestrator>();
        services.AddScoped<ICampaignService, Campaigns.CampaignService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        return services;
    }
}
