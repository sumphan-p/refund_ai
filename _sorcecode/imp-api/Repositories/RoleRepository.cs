using System.Data;
using Dapper;
using imp_api.Models;

namespace imp_api.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly IDbConnection _db;

    public RoleRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Role>> GetAllAsync()
    {
        return await _db.QueryAsync<Role>(
            "SELECT Id, RoleName, Description, IsActive, CreatedAt FROM imp.Roles ORDER BY Id");
    }

    public async Task<Role?> GetByIdAsync(int id)
    {
        return await _db.QuerySingleOrDefaultAsync<Role>(
            "SELECT Id, RoleName, Description, IsActive, CreatedAt FROM imp.Roles WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<Role> CreateAsync(Role role)
    {
        role.Id = await _db.QuerySingleAsync<int>(
            @"INSERT INTO imp.Roles (RoleName, Description)
              OUTPUT INSERTED.Id
              VALUES (@RoleName, @Description)",
            new { role.RoleName, role.Description });
        return role;
    }

    public async Task UpdateAsync(Role role)
    {
        await _db.ExecuteAsync(
            @"UPDATE imp.Roles
              SET RoleName = @RoleName, Description = @Description, IsActive = @IsActive
              WHERE Id = @Id",
            new { role.Id, role.RoleName, role.Description, role.IsActive });
    }

    public async Task<IEnumerable<Role>> GetRolesByUserIdAsync(Guid userId)
    {
        return await _db.QueryAsync<Role>(
            @"SELECT r.Id, r.RoleName, r.Description, r.IsActive, r.CreatedAt
              FROM imp.Roles r
              INNER JOIN imp.UserRoles ur ON r.Id = ur.RoleId
              WHERE ur.UserId = @UserId AND r.IsActive = 1",
            new { UserId = userId });
    }

    public async Task<Dictionary<Guid, List<string>>> GetRoleNamesByUserIdsAsync(IEnumerable<Guid> userIds)
    {
        var rows = await _db.QueryAsync<(Guid UserId, string RoleName)>(
            @"SELECT ur.UserId, r.RoleName
              FROM imp.UserRoles ur
              INNER JOIN imp.Roles r ON ur.RoleId = r.Id
              WHERE ur.UserId IN @UserIds AND r.IsActive = 1",
            new { UserIds = userIds });

        return rows
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.RoleName).ToList());
    }

    public async Task SetUserRolesAsync(Guid userId, IEnumerable<int> roleIds)
    {
        if (_db.State != ConnectionState.Open)
            await ((System.Data.Common.DbConnection)_db).OpenAsync();

        using var tx = _db.BeginTransaction();
        try
        {
            await _db.ExecuteAsync(
                "DELETE FROM imp.UserRoles WHERE UserId = @UserId",
                new { UserId = userId }, transaction: tx);

            var parameters = roleIds.Select(roleId => new { UserId = userId, RoleId = roleId });
            await _db.ExecuteAsync(
                "INSERT INTO imp.UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)",
                parameters, transaction: tx);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
