using System.Data;
using Dapper;
using imp_api.Models;

namespace imp_api.Repositories;

public class UserMenuPermissionRepository : IUserMenuPermissionRepository
{
    private readonly IDbConnection _db;

    public UserMenuPermissionRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<IEnumerable<UserMenuPermission>> GetByUserIdAsync(Guid userId)
    {
        return await _db.QueryAsync<UserMenuPermission>(
            @"SELECT Id, UserId, MenuId, Visible, CanCreate, CanEdit, CanReadOnly, CanDelete
              FROM imp.UserMenuPermissions
              WHERE UserId = @UserId",
            new { UserId = userId });
    }

    public async Task SetPermissionsAsync(Guid userId, IEnumerable<UserMenuPermission> permissions)
    {
        if (_db.State != ConnectionState.Open)
            await ((System.Data.Common.DbConnection)_db).OpenAsync();

        using var tx = _db.BeginTransaction();
        try
        {
            await _db.ExecuteAsync(
                "DELETE FROM imp.UserMenuPermissions WHERE UserId = @UserId",
                new { UserId = userId }, transaction: tx);

            await _db.ExecuteAsync(
                @"INSERT INTO imp.UserMenuPermissions (UserId, MenuId, Visible, CanCreate, CanEdit, CanReadOnly, CanDelete)
                  VALUES (@UserId, @MenuId, @Visible, @CanCreate, @CanEdit, @CanReadOnly, @CanDelete)",
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
