using System.Data;
using Dapper;
using imp_api.Models;

namespace imp_api.Repositories;

public class PasswordResetRepository : IPasswordResetRepository
{
    private readonly IDbConnection _db;

    public PasswordResetRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task CreateAsync(PasswordResetToken token)
    {
        await _db.ExecuteAsync(
            @"INSERT INTO imp.PasswordResetTokens (Id, UserId, Token, ExpiresAt, CreatedAt)
              VALUES (@Id, @UserId, @Token, @ExpiresAt, SYSUTCDATETIME())",
            new { Id = Guid.NewGuid(), token.UserId, token.Token, token.ExpiresAt });
    }

    public async Task<PasswordResetToken?> GetByTokenAsync(string token)
    {
        return await _db.QuerySingleOrDefaultAsync<PasswordResetToken>(
            @"SELECT Id, UserId, Token, ExpiresAt, CreatedAt, UsedAt
              FROM imp.PasswordResetTokens
              WHERE Token = @Token",
            new { Token = token });
    }

    public async Task MarkUsedAsync(string token)
    {
        await _db.ExecuteAsync(
            "UPDATE imp.PasswordResetTokens SET UsedAt = SYSUTCDATETIME() WHERE Token = @Token AND UsedAt IS NULL",
            new { Token = token });
    }

    public async Task InvalidateAllByUserIdAsync(Guid userId)
    {
        await _db.ExecuteAsync(
            "UPDATE imp.PasswordResetTokens SET UsedAt = SYSUTCDATETIME() WHERE UserId = @UserId AND UsedAt IS NULL",
            new { UserId = userId });
    }
}
