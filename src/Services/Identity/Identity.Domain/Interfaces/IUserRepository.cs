using Identity.Domain.Entities;

namespace Identity.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<User?> GetByNicknameAsync(string nickname, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailOrPhoneAsync(string identifier, CancellationToken cancellationToken = default);
    Task<List<User>> GetBannedUsersAsync(CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
