using Microsoft.EntityFrameworkCore;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using Identity.Infrastructure.Data;

namespace Identity.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;

    public UserRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant(), cancellationToken);
    }

    public async Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var normalizedPhoneNumber = User.NormalizePhoneNumber(phoneNumber);

        return await _context.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhoneNumber, cancellationToken);
    }

    public async Task<User?> GetByNicknameAsync(string nickname, CancellationToken cancellationToken = default)
    {
        var normalizedNickname = nickname.Trim().ToLowerInvariant();

        return await _context.Users
            .FirstOrDefaultAsync(u => u.Nickname.ToLower() == normalizedNickname, cancellationToken);
    }

    public async Task<User?> GetByEmailOrPhoneAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var trimmedIdentifier = identifier.Trim();

        return trimmedIdentifier.Contains('@')
            ? await GetByEmailAsync(trimmedIdentifier, cancellationToken)
            : await GetByPhoneNumberAsync(trimmedIdentifier, cancellationToken);
    }

    public async Task<List<User>> GetBannedUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.IsBanned)
            .OrderByDescending(u => u.BannedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
