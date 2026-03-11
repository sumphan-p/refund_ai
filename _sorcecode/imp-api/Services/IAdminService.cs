using imp_api.DTOs;

namespace imp_api.Services;

public interface IAdminService
{
    // Users
    Task<IEnumerable<UserListItem>> GetAllUsersAsync();
    Task<UserListItem> CreateUserAsync(CreateUserRequest request);
    Task UpdateUserAsync(Guid userId, UpdateUserRequest request);
    Task ToggleUserActiveAsync(Guid userId);
    Task AssignRolesAsync(Guid userId, AssignRoleRequest request);

    // Roles
    Task<IEnumerable<RoleListItem>> GetAllRolesAsync();
    Task<RoleListItem> CreateRoleAsync(CreateRoleRequest request);
    Task UpdateRoleAsync(int roleId, UpdateRoleRequest request);

    // Role Permissions
    Task<IEnumerable<PermissionItem>> GetRolePermissionsAsync(int roleId);
    Task SetRolePermissionsAsync(int roleId, SetPermissionsRequest request);

    // User-specific Permissions
    Task<IEnumerable<PermissionItem>> GetUserPermissionsAsync(Guid userId);
    Task SetUserPermissionsAsync(Guid userId, SetUserPermissionsRequest request);

    // Copy role permissions to user (for first-time setup)
    Task CopyRolePermissionsToUserAsync(Guid userId);

    // Menus
    Task<IEnumerable<MenuListItem>> GetAllMenusAsync();
    Task<MenuListItem> CreateMenuAsync(CreateMenuRequest request);
    Task UpdateMenuAsync(int menuId, UpdateMenuRequest request);
    Task<List<MenuWithPermission>> GetMenuTreeForUserAsync(Guid userId);
}
