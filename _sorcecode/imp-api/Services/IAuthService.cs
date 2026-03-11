using imp_api.DTOs;

namespace imp_api.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<string?> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<AuthResponse> RefreshAsync(RefreshRequest request);
    Task LogoutAsync(LogoutRequest request);
}
