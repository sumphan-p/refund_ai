namespace imp_api.Services;

public interface IEmailService
{
    Task<string> SendPasswordResetEmailAsync(string? email, string token);
}
