using imp_api.Models;

namespace imp_api.Repositories;

public interface IPasswordResetRepository
{
    Task CreateAsync(PasswordResetToken token);
    Task<PasswordResetToken?> GetByTokenAsync(string token);
    Task MarkUsedAsync(string token);
    Task InvalidateAllByUserIdAsync(Guid userId);
}
