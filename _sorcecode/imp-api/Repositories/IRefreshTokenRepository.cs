using imp_api.Models;

namespace imp_api.Repositories;

public interface IRefreshTokenRepository
{
    Task CreateAsync(RefreshToken token);
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task RevokeAsync(string token);
    Task RevokeAllByUserIdAsync(Guid userId);
}
