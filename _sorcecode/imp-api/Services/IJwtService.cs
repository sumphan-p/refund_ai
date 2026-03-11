using imp_api.Models;

namespace imp_api.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    int GetAccessTokenExpirationMinutes();
    int GetRefreshTokenExpirationDays();
    int GetRememberMeExpirationDays();
}
