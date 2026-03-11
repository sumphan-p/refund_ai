using imp_api.Models;

namespace imp_api.Repositories;

public interface IRoleMenuPermissionRepository
{
    Task<IEnumerable<RoleMenuPermission>> GetByRoleIdAsync(int roleId);
    Task<IEnumerable<RoleMenuPermission>> GetMergedPermissionsByUserIdAsync(Guid userId);
    Task SetPermissionsAsync(int roleId, IEnumerable<RoleMenuPermission> permissions);
}
