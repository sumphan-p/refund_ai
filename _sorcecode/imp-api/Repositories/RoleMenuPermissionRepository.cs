using System.Data;
using Dapper;
using imp_api.Models;

namespace imp_api.Repositories;

public class RoleMenuPermissionRepository : IRoleMenuPermissionRepository
{
    private readonly IDbConnection _db;

    public RoleMenuPermissionRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<IEnumerable<RoleMenuPermission>> GetByRoleIdAsync(int roleId)
    {
        return await _db.QueryAsync<RoleMenuPermission>(
            @"SELECT Id, RoleId, MenuId, Visible, CanCreate, CanEdit, CanReadOnly, CanDelete
              FROM imp.RoleMenuPermissions
              WHERE RoleId = @RoleId",
            new { RoleId = roleId });
    }

    /// <summary>
    /// Merges permissions across all roles for a user.
    /// If a user has multiple roles, permissions are OR-merged (most permissive wins).
    /// </summary>
    public async Task<IEnumerable<RoleMenuPermission>> GetMergedPermissionsByUserIdAsync(Guid userId)
    {
        return await _db.QueryAsync<RoleMenuPermission>(
            @"SELECT
                0 AS Id,
                0 AS RoleId,
                rmp.MenuId,
                MAX(CAST(rmp.Visible AS INT)) AS Visible,
                MAX(CAST(rmp.CanCreate AS INT)) AS CanCreate,
                MAX(CAST(rmp.CanEdit AS INT)) AS CanEdit,
                MAX(CAST(rmp.CanReadOnly AS INT)) AS CanReadOnly,
                MAX(CAST(rmp.CanDelete AS INT)) AS CanDelete
              FROM imp.RoleMenuPermissions rmp
              INNER JOIN imp.UserRoles ur ON rmp.RoleId = ur.RoleId
              WHERE ur.UserId = @UserId
              GROUP BY rmp.MenuId",
            new { UserId = userId });
    }

    public async Task SetPermissionsAsync(int roleId, IEnumerable<RoleMenuPermission> permissions)
    {
        if (_db.State != ConnectionState.Open)
            await ((System.Data.Common.DbConnection)_db).OpenAsync();

        using var tx = _db.BeginTransaction();
        try
        {
            await _db.ExecuteAsync(
                "DELETE FROM imp.RoleMenuPermissions WHERE RoleId = @RoleId",
                new { RoleId = roleId }, transaction: tx);

            await _db.ExecuteAsync(
                @"INSERT INTO imp.RoleMenuPermissions (RoleId, MenuId, Visible, CanCreate, CanEdit, CanReadOnly, CanDelete)
                  VALUES (@RoleId, @MenuId, @Visible, @CanCreate, @CanEdit, @CanReadOnly, @CanDelete)",
                permissions, transaction: tx);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
