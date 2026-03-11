using System.Data;
using Dapper;

namespace imp_api.Services;

public class TokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TokenCleanupService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(6);

    public TokenCleanupService(IServiceProvider services, ILogger<TokenCleanupService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_interval, stoppingToken);
            await CleanupAsync();
        }
    }

    private async Task CleanupAsync()
    {
        try
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();

            var refreshDeleted = await db.ExecuteAsync(
                "DELETE FROM imp.RefreshTokens WHERE ExpiresAt < SYSUTCDATETIME() OR RevokedAt IS NOT NULL");

            var resetDeleted = await db.ExecuteAsync(
                "DELETE FROM imp.PasswordResetTokens WHERE ExpiresAt < SYSUTCDATETIME() OR UsedAt IS NOT NULL");

            if (refreshDeleted > 0 || resetDeleted > 0)
                _logger.LogInformation("Token cleanup: deleted {RefreshCount} refresh tokens, {ResetCount} reset tokens",
                    refreshDeleted, resetDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token cleanup failed");
        }
    }
}
