using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VoiceAgentStudio.Application.Common.Interfaces;
using VoiceAgentStudio.Infrastructure.AI;
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
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── Services ─────────────────────────────────────────────────
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddHttpContextAccessor();

        // ── AI Providers ──────────────────────────────────────────────
        // Named HttpClients — one per provider for isolated config/timeouts
        services.AddHttpClient("Gemini", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(3);
        });
        services.AddHttpClient("OpenAI", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(3);
        });

        // Providers registered as Transient (stateless HTTP clients)
        services.AddTransient<GeminiProvider>();
        services.AddTransient<OpenAiProvider>();

        // Factory registered as Singleton (holds no state, resolves from DI)
        services.AddSingleton<IAiProviderFactory, AiProviderFactory>();

        return services;
    }
}
