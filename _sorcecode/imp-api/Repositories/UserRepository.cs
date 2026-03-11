using System.Data;
using Dapper;
using imp_api.Models;

namespace imp_api.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnection _db;

    public UserRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _db.QuerySingleOrDefaultAsync<User>(
            "SELECT Id, UserName, PasswordHash, DisplayName, Email, IsActive, CreatedAt, UpdatedAt FROM imp.Users WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<User?> GetByUserNameAsync(string userName)
    {
        return await _db.QuerySingleOrDefaultAsync<User>(
            "SELECT Id, UserName, PasswordHash, DisplayName, Email, IsActive, CreatedAt, UpdatedAt FROM imp.Users WHERE UserName = @UserName",
            new { UserName = userName });
    }

    public async Task<User> CreateAsync(User user)
    {
        user.Id = Guid.NewGuid();
        await _db.ExecuteAsync(
            @"INSERT INTO imp.Users (Id, UserName, PasswordHash, DisplayName, Email)
              VALUES (@Id, @UserName, @PasswordHash, @DisplayName, @Email)",
            new { user.Id, user.UserName, user.PasswordHash, user.DisplayName, user.Email });
        return user;
    }

    public async Task UpdatePasswordAsync(Guid userId, string passwordHash)
    {
        await _db.ExecuteAsync(
            "UPDATE imp.Users SET PasswordHash = @PasswordHash, UpdatedAt = SYSUTCDATETIME() WHERE Id = @UserId",
            new { UserId = userId, PasswordHash = passwordHash });
    }

    public async Task UpdateAsync(User user)
    {
        await _db.ExecuteAsync(
            @"UPDATE imp.Users
              SET DisplayName = @DisplayName, Email = @Email, IsActive = @IsActive, UpdatedAt = SYSUTCDATETIME()
              WHERE Id = @Id",
            new { user.Id, user.DisplayName, user.Email, user.IsActive });
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _db.QueryAsync<User>(
            "SELECT Id, UserName, DisplayName, Email, IsActive, CreatedAt, UpdatedAt FROM imp.Users ORDER BY CreatedAt DESC");
    }

    public async Task ToggleActiveAsync(Guid userId, bool isActive)
    {
        await _db.ExecuteAsync(
            "UPDATE imp.Users SET IsActive = @IsActive, UpdatedAt = SYSUTCDATETIME() WHERE Id = @UserId",
            new { UserId = userId, IsActive = isActive });
    }

    public async Task<int> GetActiveAdminCountAsync()
    {
        return await _db.QuerySingleAsync<int>(
            @"SELECT COUNT(DISTINCT u.Id)
              FROM imp.Users u
              INNER JOIN imp.UserRoles ur ON u.Id = ur.UserId
              INNER JOIN imp.Roles r ON ur.RoleId = r.Id
              WHERE u.IsActive = 1 AND r.RoleName = 'Admin' AND r.IsActive = 1");
    }
}
