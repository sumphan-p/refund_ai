using imp_api.Models;

namespace imp_api.Repositories;

public interface IUserMenuPermissionRepository
{
    Task<IEnumerable<UserMenuPermission>> GetByUserIdAsync(Guid userId);
    Task SetPermissionsAsync(Guid userId, IEnumerable<UserMenuPermission> permissions);
}
