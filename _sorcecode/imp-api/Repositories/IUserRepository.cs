using imp_api.Models;

namespace imp_api.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUserNameAsync(string userName);
    Task<User> CreateAsync(User user);
    Task UpdatePasswordAsync(Guid userId, string passwordHash);
    Task UpdateAsync(User user);
    Task<IEnumerable<User>> GetAllAsync();
    Task ToggleActiveAsync(Guid userId, bool isActive);
    Task<int> GetActiveAdminCountAsync();
}
