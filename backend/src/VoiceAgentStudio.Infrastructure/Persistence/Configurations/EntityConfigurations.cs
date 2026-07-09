using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoiceAgentStudio.Domain.Entities;

namespace VoiceAgentStudio.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.FullName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(200).IsRequired();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.Role).HasMaxLength(50).IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();

        // Global soft-delete filter
        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}

public class AgentConfiguration : IEntityTypeConfiguration<Agent>
{
    public void Configure(EntityTypeBuilder<Agent> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Name).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Description).HasMaxLength(500);
        builder.Property(a => a.ModelName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.SystemPrompt).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(a => a.Objective).HasMaxLength(300);
        builder.Property(a => a.CompanyContext).HasColumnType("nvarchar(max)");
        builder.Property(a => a.EscalationKeywords).HasMaxLength(500);

        builder.Property(a => a.Status).HasConversion<string>();
        builder.Property(a => a.Tone).HasConversion<string>();
        builder.Property(a => a.LlmProvider).HasConversion<string>();

        builder.HasOne(a => a.CreatedBy)
               .WithMany(u => u.Agents)
               .HasForeignKey(a => a.CreatedByUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}

public class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.Status).HasConversion<string>();

        builder.HasOne(c => c.Agent)
               .WithMany(a => a.Campaigns)
               .HasForeignKey(c => c.AgentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.FullName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.PhoneNumber).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(200);
        builder.Property(c => c.CustomContext).HasColumnType("nvarchar(max)");

        builder.HasOne(c => c.Campaign)
               .WithMany(cp => cp.Contacts)
               .HasForeignKey(c => c.CampaignId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Status).HasConversion<string>();
        builder.Property(s => s.DetectedIntention).HasConversion<string>();
        builder.Property(s => s.EscalationReason).HasMaxLength(500);

        builder.HasOne(s => s.Campaign)
               .WithMany(c => c.Sessions)
               .HasForeignKey(s => s.CampaignId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Contact)
               .WithOne(c => c.Session)
               .HasForeignKey<Session>(s => s.ContactId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Role).HasMaxLength(20).IsRequired();
        builder.Property(m => m.Content).HasColumnType("nvarchar(max)").IsRequired();

        builder.HasOne(m => m.Session)
               .WithMany(s => s.Messages)
               .HasForeignKey(m => m.SessionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}
