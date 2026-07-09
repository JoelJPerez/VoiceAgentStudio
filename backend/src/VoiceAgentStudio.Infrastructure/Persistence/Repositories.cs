using Microsoft.EntityFrameworkCore;
using VoiceAgentStudio.Application.Common.Interfaces;
using VoiceAgentStudio.Domain.Common;
using VoiceAgentStudio.Domain.Entities;

namespace VoiceAgentStudio.Infrastructure.Persistence;

// ── Generic repository ───────────────────────────────────────────────

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(e => e.Id == id, ct);

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
        => await _dbSet.ToListAsync(ct);

    public virtual async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
        return entity;
    }

    public virtual Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        _context.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, ct);
        if (entity != null)
        {
            entity.IsDeleted = true;
            _context.Entry(entity).State = EntityState.Modified;
        }
    }
}

// ── Agent repository ─────────────────────────────────────────────────

public class AgentRepository : Repository<Agent>, IAgentRepository
{
    public AgentRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Agent>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _dbSet
            .Where(a => a.CreatedByUserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task<bool> NameExistsForUserAsync(
        string name,
        Guid userId,
        Guid? excludeId = null,
        CancellationToken ct = default)
    {
        var query = _dbSet.Where(a =>
            a.CreatedByUserId == userId &&
            a.Name.ToLower() == name.ToLower().Trim());

        if (excludeId.HasValue)
            query = query.Where(a => a.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }
}

// ── User repository ──────────────────────────────────────────────────

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(u => u.Email == email.ToLower(), ct);
}

// ── Unit of Work ─────────────────────────────────────────────────────

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IAgentRepository Agents { get; }
    public IUserRepository Users { get; }

    public UnitOfWork(AppDbContext context, IAgentRepository agents, IUserRepository users)
    {
        _context = context;
        Agents = agents;
        Users = users;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}

