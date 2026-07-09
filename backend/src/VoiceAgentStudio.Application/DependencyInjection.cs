using Microsoft.Extensions.DependencyInjection;
using VoiceAgentStudio.Application.Agents;
using VoiceAgentStudio.Application.Auth;

namespace VoiceAgentStudio.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
