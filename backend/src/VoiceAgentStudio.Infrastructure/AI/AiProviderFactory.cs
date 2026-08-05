using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VoiceAgentStudio.Application.Common.Interfaces;

namespace VoiceAgentStudio.Infrastructure.AI;

/// <summary>
/// Resolves the correct IAiProvider at runtime.
/// To switch provider, change "DefaultAiProvider" in appsettings.json
/// or override per-agent via Agent.LlmProvider.
/// </summary>
public class AiProviderFactory : IAiProviderFactory
{
    private readonly IServiceProvider _services;
    private readonly string _defaultProvider;

    public AiProviderFactory(IServiceProvider services, IConfiguration config)
    {
        _services = services;
        _defaultProvider = config["DefaultAiProvider"] ?? "Gemini";
    }

    public IAiProvider GetProvider(string providerName)
    {
        // Map enum string → provider name (e.g. "OpenAI" → OpenAiProvider)
        var normalized = providerName.ToLowerInvariant() switch
        {
            "openai" => "OpenAI",
            "anthropic" => "Anthropic",
            "gemini" => "Gemini",
            _ => _defaultProvider
        };

        return normalized switch
        {
            "OpenAI" => _services.GetRequiredService<OpenAiProvider>(),
            "Gemini" => _services.GetRequiredService<GeminiProvider>(),
            _ => _services.GetRequiredService<GeminiProvider>() // safe default
        };
    }

    public IAiProvider GetDefault()
        => GetProvider(_defaultProvider);
}
