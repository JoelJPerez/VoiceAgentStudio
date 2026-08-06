using VoiceAgentStudio.Domain.Entities;

namespace VoiceAgentStudio.Application.Common.Interfaces;

// ── Generic repository ──────────────────────────────────────────────
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

// ── Specific repositories ────────────────────────────────────────────
public interface IAgentRepository : IRepository<Agent>
{
    Task<IEnumerable<Agent>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> NameExistsForUserAsync(string name, Guid userId, Guid? excludeId = null, CancellationToken ct = default);
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
}

public interface ICampaignRepository : IRepository<Campaign>
{
    Task<IEnumerable<Campaign>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Campaign?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);
}

public interface IContactRepository : IRepository<Contact>
{
    Task<IEnumerable<Contact>> GetByCampaignIdAsync(Guid campaignId, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Contact> contacts, CancellationToken ct = default);
}

public interface ISessionRepository : IRepository<Session>
{
    Task<IEnumerable<Session>> GetByCampaignIdAsync(Guid campaignId, CancellationToken ct = default);
    Task<Session?> GetWithMessagesAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Session>> GetPendingAsync(Guid campaignId, CancellationToken ct = default);
}

public interface IMessageRepository : IRepository<Message>
{
    Task AddRangeAsync(IEnumerable<Message> messages, CancellationToken ct = default);
}

// ── Unit of Work ─────────────────────────────────────────────────────
public interface IUnitOfWork
{
    IAgentRepository Agents { get; }
    IUserRepository Users { get; }
    ICampaignRepository Campaigns { get; }
    IContactRepository Contacts { get; }
    ISessionRepository Sessions { get; }
    IMessageRepository Messages { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

// ── Auth service ─────────────────────────────────────────────────────
public interface ITokenService
{
    string GenerateToken(User user);
    Guid? GetUserIdFromToken(string token);
}

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

// ── Current user context ─────────────────────────────────────────────
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}

// ── CSV parsing ───────────────────────────────────────────────────────
public interface ICsvContactParser
{
    IEnumerable<ParsedContact> Parse(Stream csvStream);
}

public class ParsedContact
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CustomContext { get; set; } = string.Empty;
    public bool IsValid => !string.IsNullOrWhiteSpace(FullName);
}

// ── Campaign execution queue ──────────────────────────────────────────
public interface ICampaignExecutionQueue
{
    void Enqueue(Guid campaignId);
}

