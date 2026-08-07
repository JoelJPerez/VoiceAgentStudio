using Microsoft.EntityFrameworkCore;
using VoiceAgentStudio.Application.Common.Interfaces;
using VoiceAgentStudio.Domain.Common;
using VoiceAgentStudio.Domain.Entities;
using VoiceAgentStudio.Domain.Enums;
using VoiceAgentStudio.Infrastructure.Persistence;

namespace VoiceAgentStudio.Infrastructure.Persistence;

// ── Campaign repository ───────────────────────────────────────────────

public class CampaignRepository : Repository<Campaign>, ICampaignRepository
{
    public CampaignRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Campaign>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.Agent)
            .Include(c => c.Contacts)
            .Include(c => c.Sessions)
            .Where(c => c.CreatedByUserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<Campaign?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.Agent)
            .Include(c => c.Contacts)
            .Include(c => c.Sessions)
                .ThenInclude(s => s.Contact)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
}

// ── Contact repository ────────────────────────────────────────────────

public class ContactRepository : Repository<Contact>, IContactRepository
{
    public ContactRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Contact>> GetByCampaignIdAsync(Guid campaignId, CancellationToken ct = default)
        => await _dbSet
            .Where(c => c.CampaignId == campaignId)
            .OrderBy(c => c.FullName)
            .ToListAsync(ct);

    public async Task AddRangeAsync(IEnumerable<Contact> contacts, CancellationToken ct = default)
        => await _context.Contacts.AddRangeAsync(contacts, ct);
}

// ── Session repository ────────────────────────────────────────────────

public class SessionRepository : Repository<Session>, ISessionRepository
{
    public SessionRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Session>> GetByCampaignIdAsync(Guid campaignId, CancellationToken ct = default)
        => await _dbSet
            .Include(s => s.Contact)
            .Where(s => s.CampaignId == campaignId)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task<Session?> GetWithMessagesAsync(Guid id, CancellationToken ct = default)
        => await _dbSet
            .Include(s => s.Contact)
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IEnumerable<Session>> GetPendingAsync(Guid campaignId, CancellationToken ct = default)
        => await _dbSet
            .Include(s => s.Contact)
            .Where(s => s.CampaignId == campaignId && s.Status == SessionStatus.Pending)
            .ToListAsync(ct);
}

// ── Message repository ────────────────────────────────────────────────

public class MessageRepository : Repository<Message>, IMessageRepository
{
    public MessageRepository(AppDbContext context) : base(context) { }

    public async Task AddRangeAsync(IEnumerable<Message> messages, CancellationToken ct = default)
        => await _context.Messages.AddRangeAsync(messages, ct);
}

// ── Updated Unit of Work ──────────────────────────────────────────────

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IAgentRepository Agents { get; }
    public IUserRepository Users { get; }
    public ICampaignRepository Campaigns { get; }
    public IContactRepository Contacts { get; }
    public ISessionRepository Sessions { get; }
    public IMessageRepository Messages { get; }

    public UnitOfWork(
        AppDbContext context,
        IAgentRepository agents,
        IUserRepository users,
        ICampaignRepository campaigns,
        IContactRepository contacts,
        ISessionRepository sessions,
        IMessageRepository messages)
    {
        _context = context;
        Agents = agents;
        Users = users;
        Campaigns = campaigns;
        Contacts = contacts;
        Sessions = sessions;
        Messages = messages;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
