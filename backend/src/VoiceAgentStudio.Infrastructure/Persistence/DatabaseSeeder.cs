using Microsoft.EntityFrameworkCore;
using VoiceAgentStudio.Domain.Entities;
using VoiceAgentStudio.Domain.Enums;

namespace VoiceAgentStudio.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();

        if (await context.Users.AnyAsync()) return; // Already seeded

        // ── Admin user ───────────────────────────────────────────────
        var adminUser = new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            FullName = "Admin VoiceAgent",
            Email = "admin@voiceagent.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin1234!"),
            Role = "Admin",
            IsActive = true
        };

        context.Users.Add(adminUser);

        // ── Demo agents ──────────────────────────────────────────────
        var agentSales = new Agent
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000010"),
            Name = "Agente de Ventas - Telco",
            Description = "Agente especializado en venta consultiva de planes de telefonía móvil.",
            Status = AgentStatus.Active,
            Tone = AgentTone.Friendly,
            LlmProvider = LlmProvider.OpenAI,
            ModelName = "gpt-4o",
            Temperature = 0.75f,
            MaxTokens = 400,
            SystemPrompt = """
                Eres Carlos, un asesor comercial de MovilPlus, amable y empático.
                Tu objetivo es entender las necesidades de comunicación del cliente
                y recomendar el plan de telefonía que mejor se adapte a su uso.
                Siempre saluda por el nombre del cliente si está disponible.
                Usa un lenguaje cercano pero profesional. Máximo 2-3 oraciones por respuesta.
                """,
            Objective = "Cerrar venta de plan postpago premium o agendar cita con asesor",
            CompanyContext = "MovilPlus es operador virtual con cobertura nacional. Planes desde $25.000/mes.",
            EscalationKeywords = "cancelar,demanda,fraude,robo,supervisor,gerente",
            AutoEscalate = true,
            CreatedByUserId = adminUser.Id
        };

        var agentReminder = new Agent
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000011"),
            Name = "Recordatorio de Citas - Clínica",
            Description = "Confirma y reagenda citas médicas de forma proactiva.",
            Status = AgentStatus.Active,
            Tone = AgentTone.Empathetic,
            LlmProvider = LlmProvider.OpenAI,
            ModelName = "gpt-4o",
            Temperature = 0.5f,
            MaxTokens = 300,
            SystemPrompt = """
                Eres Ana, asistente virtual de Clínica Salud Integral.
                Tu objetivo es confirmar la cita médica del paciente para mañana
                y, si no puede asistir, ofrecerle reagendar para otro día disponible.
                Sé muy cordial, breve y empática. No compartas información médica sensible.
                """,
            Objective = "Confirmar asistencia a cita o reagendar para próxima semana",
            CompanyContext = "Clínica Salud Integral. Horarios: Lunes a Viernes 7am-7pm, Sábados 7am-1pm.",
            EscalationKeywords = "urgencia,emergencia,dolor,grave,urgente",
            AutoEscalate = true,
            CreatedByUserId = adminUser.Id
        };

        context.Agents.AddRange(agentSales, agentReminder);

        await context.SaveChangesAsync();
    }
}
