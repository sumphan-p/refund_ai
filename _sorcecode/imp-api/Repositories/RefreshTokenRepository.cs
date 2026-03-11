using System.Data;
using Dapper;
using imp_api.Models;

namespace imp_api.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDbConnection _db;

    public RefreshTokenRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task CreateAsync(RefreshToken token)
    {
        await _db.ExecuteAsync(
            @"INSERT INTO imp.RefreshTokens (Id, UserId, Token, ExpiresAt, CreatedAt)
              VALUES (@Id, @UserId, @Token, @ExpiresAt, SYSUTCDATETIME())",
            new { Id = Guid.NewGuid(), token.UserId, token.Token, token.ExpiresAt });
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _db.QuerySingleOrDefaultAsync<RefreshToken>(
            @"SELECT Id, UserId, Token, ExpiresAt, CreatedAt, RevokedAt
              FROM imp.RefreshTokens
              WHERE Token = @Token",
            new { Token = token });
    }

    public async Task RevokeAsync(string token)
    {
        await _db.ExecuteAsync(
            "UPDATE imp.RefreshTokens SET RevokedAt = SYSUTCDATETIME() WHERE Token = @Token AND RevokedAt IS NULL",
            new { Token = token });
    }

    public async Task RevokeAllByUserIdAsync(Guid userId)
    {
        await _db.ExecuteAsync(
            "UPDATE imp.RefreshTokens SET RevokedAt = SYSUTCDATETIME() WHERE UserId = @UserId AND RevokedAt IS NULL",
            new { UserId = userId });
    }
}
