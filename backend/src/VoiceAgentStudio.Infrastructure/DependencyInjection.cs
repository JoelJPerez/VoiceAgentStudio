using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VoiceAgentStudio.Application.Common.Interfaces;
using VoiceAgentStudio.Infrastructure.AI;
using VoiceAgentStudio.Infrastructure.Campaigns;
using VoiceAgentStudio.Infrastructure.Persistence;
using VoiceAgentStudio.Infrastructure.Services;

namespace VoiceAgentStudio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── EF Core ──────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
            ));

        // ── Repositories ─────────────────────────────────────────────
        services.AddScoped<IAgentRepository, AgentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── Services ─────────────────────────────────────────────────
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICsvContactParser, CsvContactParser>();
        services.AddHttpContextAccessor();

        // ── AI Providers ──────────────────────────────────────────────
        services.AddHttpClient("Gemini", client => client.Timeout = TimeSpan.FromMinutes(3));
        services.AddHttpClient("OpenAI", client => client.Timeout = TimeSpan.FromMinutes(3));
        services.AddTransient<GeminiProvider>();
        services.AddTransient<OpenAiProvider>();
        services.AddSingleton<IAiProviderFactory, AiProviderFactory>();

        // ── Campaign execution (background service + queue) ───────────
        services.AddSingleton<CampaignExecutionQueue>();
        services.AddSingleton<ICampaignExecutionQueue>(sp =>
            sp.GetRequiredService<CampaignExecutionQueue>());
        services.AddHostedService<CampaignExecutionService>();

        return services;
    }
}
