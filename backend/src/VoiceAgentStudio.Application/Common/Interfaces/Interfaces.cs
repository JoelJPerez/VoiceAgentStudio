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

// ── Unit of Work ─────────────────────────────────────────────────────
public interface IUnitOfWork
{
    IAgentRepository Agents { get; }
    IUserRepository Users { get; }
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

