using imp_api.DTOs;
using imp_api.Helpers;
using imp_api.Models;
using imp_api.Repositories;

namespace imp_api.Services;

public class AdminService : IAdminService
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IMenuRepository _menuRepo;
    private readonly IRoleMenuPermissionRepository _permissionRepo;
    private readonly IUserMenuPermissionRepository _userPermRepo;

    public AdminService(
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IMenuRepository menuRepo,
        IRoleMenuPermissionRepository permissionRepo,
        IUserMenuPermissionRepository userPermRepo)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _menuRepo = menuRepo;
        _permissionRepo = permissionRepo;
        _userPermRepo = userPermRepo;
    }

    // =============================================
    // Users
    // =============================================

    public async Task<IEnumerable<UserListItem>> GetAllUsersAsync()
    {
        var users = (await _userRepo.GetAllAsync()).ToList();
        var roleMap = await _roleRepo.GetRoleNamesByUserIdsAsync(users.Select(u => u.Id));

        return users.Select(user => new UserListItem
        {
            Id = user.Id,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            Email = user.Email,
            IsActive = user.IsActive,
            Roles = roleMap.TryGetValue(user.Id, out var roles) ? roles : [],
            CreatedAt = user.CreatedAt
        });
    }

    public async Task<UserListItem> CreateUserAsync(CreateUserRequest request)
    {
        var normalizedUserName = request.UserName.Trim().ToLowerInvariant();

        var existing = await _userRepo.GetByUserNameAsync(normalizedUserName);
        if (existing is not null)
            throw new AuthException("USERNAME_EXISTS", "An account with this username already exists.");

        var user = new User
        {
            UserName = normalizedUserName,
            PasswordHash = PasswordHasher.Hash(request.Password),
            DisplayName = request.DisplayName.Trim(),
            Email = request.Email?.Trim()
        };

        user = await _userRepo.CreateAsync(user);

        if (request.RoleIds?.Any() == true)
        {
            await _roleRepo.SetUserRolesAsync(user.Id, request.RoleIds);
        }

        // Copy role-based permissions → UserMenuPermissions as defaults
        await CopyRolePermissionsToUserAsync(user.Id);

        var roles = await _roleRepo.GetRolesByUserIdAsync(user.Id);

        return new UserListItem
        {
            Id = user.Id,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            Email = user.Email,
            IsActive = user.IsActive,
            Roles = roles.Select(r => r.RoleName).ToList(),
            CreatedAt = user.CreatedAt
        };
    }

    public async Task UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new AuthException("USER_NOT_FOUND", "User not found.");

        user.DisplayName = request.DisplayName.Trim();
        user.Email = request.Email?.Trim();

        await _userRepo.UpdateAsync(user);
    }

    public async Task ToggleUserActiveAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new AuthException("USER_NOT_FOUND", "User not found.");

        // Prevent deactivating the last active admin
        if (user.IsActive)
        {
            var userRoles = await _roleRepo.GetRolesByUserIdAsync(userId);
            if (userRoles.Any(r => r.RoleName == "Admin"))
            {
                var activeAdminCount = await _userRepo.GetActiveAdminCountAsync();
                if (activeAdminCount <= 1)
                    throw new AppException("LAST_ADMIN", "ไม่สามารถปิดใช้งานผู้ดูแลระบบคนสุดท้ายได้");
            }
        }

        await _userRepo.ToggleActiveAsync(userId, !user.IsActive);
    }

    public async Task AssignRolesAsync(Guid userId, AssignRoleRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new AuthException("USER_NOT_FOUND", "User not found.");

        if (request.RoleIds is null || !request.RoleIds.Any())
            throw new AppException("INVALID_REQUEST", "กรุณาระบุบทบาทอย่างน้อย 1 รายการ");

        // Validate all role IDs exist
        var allRoles = (await _roleRepo.GetAllAsync()).ToList();
        var validRoleIds = allRoles.Select(r => r.Id).ToHashSet();
        var invalidIds = request.RoleIds.Where(id => !validRoleIds.Contains(id)).ToList();
        if (invalidIds.Any())
            throw new AppException("INVALID_ROLES", $"ไม่พบบทบาท ID: {string.Join(", ", invalidIds)}");

        // Prevent removing Admin role from the last active admin
        var currentRoles = await _roleRepo.GetRolesByUserIdAsync(userId);
        var hadAdmin = currentRoles.Any(r => r.RoleName == "Admin");
        var willHaveAdmin = request.RoleIds.Any(id =>
            allRoles.Any(r => r.Id == id && r.RoleName == "Admin"));

        if (hadAdmin && !willHaveAdmin)
        {
            var activeAdminCount = await _userRepo.GetActiveAdminCountAsync();
            if (activeAdminCount <= 1)
                throw new AppException("LAST_ADMIN", "ไม่สามารถลบบทบาท Admin จากผู้ดูแลระบบคนสุดท้ายได้");
        }

        await _roleRepo.SetUserRolesAsync(userId, request.RoleIds);
    }

    // =============================================
    // Roles
    // =============================================

    public async Task<IEnumerable<RoleListItem>> GetAllRolesAsync()
    {
        var roles = await _roleRepo.GetAllAsync();
        return roles.Select(r => new RoleListItem
        {
            Id = r.Id,
            RoleName = r.RoleName,
            Description = r.Description,
            IsActive = r.IsActive
        });
    }

    public async Task<RoleListItem> CreateRoleAsync(CreateRoleRequest request)
    {
        var role = new Role
        {
            RoleName = request.RoleName.Trim(),
            Description = request.Description?.Trim()
        };

        role = await _roleRepo.CreateAsync(role);

        return new RoleListItem
        {
            Id = role.Id,
            RoleName = role.RoleName,
            Description = role.Description,
            IsActive = role.IsActive
        };
    }

    public async Task UpdateRoleAsync(int roleId, UpdateRoleRequest request)
    {
        var role = await _roleRepo.GetByIdAsync(roleId)
            ?? throw new AuthException("ROLE_NOT_FOUND", "Role not found.");

        role.RoleName = request.RoleName.Trim();
        role.Description = request.Description?.Trim();
        role.IsActive = request.IsActive;

        await _roleRepo.UpdateAsync(role);
    }

    // =============================================
    // Permissions
    // =============================================

    public async Task<IEnumerable<PermissionItem>> GetRolePermissionsAsync(int roleId)
    {
        var menus = await _menuRepo.GetAllAsync();
        var permissions = await _permissionRepo.GetByRoleIdAsync(roleId);
        var permDict = permissions.ToDictionary(p => p.MenuId);

        return menus.Select(m =>
        {
            permDict.TryGetValue(m.Id, out var perm);
            return new PermissionItem
            {
                MenuId = m.Id,
                MenuCode = m.MenuCode,
                MenuName = m.MenuName,
                Visible = perm?.Visible ?? false,
                CanCreate = perm?.CanCreate ?? false,
                CanEdit = perm?.CanEdit ?? false,
                CanReadOnly = perm?.CanReadOnly ?? false,
                CanDelete = perm?.CanDelete ?? false
            };
        });
    }

    public async Task SetRolePermissionsAsync(int roleId, SetPermissionsRequest request)
    {
        _ = await _roleRepo.GetByIdAsync(roleId)
            ?? throw new AuthException("ROLE_NOT_FOUND", "Role not found.");

        if (request.Permissions is null || !request.Permissions.Any())
            throw new AppException("INVALID_REQUEST", "กรุณาระบุสิทธิ์อย่างน้อย 1 รายการ");

        // Validate all menu IDs exist
        var allMenus = await _menuRepo.GetAllAsync();
        var validMenuIds = allMenus.Select(m => m.Id).ToHashSet();
        var invalidMenuIds = request.Permissions
            .Select(p => p.MenuId)
            .Where(id => !validMenuIds.Contains(id))
            .Distinct()
            .ToList();
        if (invalidMenuIds.Any())
            throw new AppException("INVALID_MENUS", $"ไม่พบเมนู ID: {string.Join(", ", invalidMenuIds)}");

        var permissions = request.Permissions.Select(p => new RoleMenuPermission
        {
            RoleId = roleId,
            MenuId = p.MenuId,
            Visible = p.Visible,
            CanCreate = p.CanCreate,
            CanEdit = p.CanEdit,
            CanReadOnly = p.CanReadOnly,
            CanDelete = p.CanDelete
        });

        await _permissionRepo.SetPermissionsAsync(roleId, permissions);
    }

    // =============================================
    // User-specific Permissions
    // =============================================

    public async Task<IEnumerable<PermissionItem>> GetUserPermissionsAsync(Guid userId)
    {
        _ = await _userRepo.GetByIdAsync(userId)
            ?? throw new AuthException("USER_NOT_FOUND", "User not found.");

        var menus = (await _menuRepo.GetAllAsync()).ToList();
        var userPerms = (await _userPermRepo.GetByUserIdAsync(userId)).ToList();

        // First time: copy role-based permissions → save to UserMenuPermissions
        if (!userPerms.Any())
        {
            var rolePerms = (await _permissionRepo.GetMergedPermissionsByUserIdAsync(userId)).ToList();
            var roleDict = rolePerms.ToDictionary(p => p.MenuId);

            var newPerms = menus.Select(m =>
            {
                roleDict.TryGetValue(m.Id, out var rp);
                return new UserMenuPermission
                {
                    UserId = userId,
                    MenuId = m.Id,
                    Visible = rp?.Visible ?? false,
                    CanCreate = rp?.CanCreate ?? false,
                    CanEdit = rp?.CanEdit ?? false,
                    CanReadOnly = rp?.CanReadOnly ?? false,
                    CanDelete = rp?.CanDelete ?? false
                };
            }).ToList();

            await _userPermRepo.SetPermissionsAsync(userId, newPerms);
            userPerms = newPerms;
        }

        var permDict = userPerms.ToDictionary(p => p.MenuId);
        return menus.Select(m =>
        {
            permDict.TryGetValue(m.Id, out var perm);
            return new PermissionItem
            {
                MenuId = m.Id,
                MenuCode = m.MenuCode,
                MenuName = m.MenuName,
                Visible = perm?.Visible ?? false,
                CanCreate = perm?.CanCreate ?? false,
                CanEdit = perm?.CanEdit ?? false,
                CanReadOnly = perm?.CanReadOnly ?? false,
                CanDelete = perm?.CanDelete ?? false
            };
        });
    }

    public async Task SetUserPermissionsAsync(Guid userId, SetUserPermissionsRequest request)
    {
        _ = await _userRepo.GetByIdAsync(userId)
            ?? throw new AuthException("USER_NOT_FOUND", "User not found.");

        if (request.Permissions is null || !request.Permissions.Any())
            throw new AppException("INVALID_REQUEST", "กรุณาระบุสิทธิ์อย่างน้อย 1 รายการ");

        var allMenus = await _menuRepo.GetAllAsync();
        var validMenuIds = allMenus.Select(m => m.Id).ToHashSet();
        var invalidMenuIds = request.Permissions
            .Select(p => p.MenuId)
            .Where(id => !validMenuIds.Contains(id))
            .Distinct()
            .ToList();
        if (invalidMenuIds.Any())
            throw new AppException("INVALID_MENUS", $"ไม่พบเมนู ID: {string.Join(", ", invalidMenuIds)}");

        var permissions = request.Permissions.Select(p => new UserMenuPermission
        {
            UserId = userId,
            MenuId = p.MenuId,
            Visible = p.Visible,
            CanCreate = p.CanCreate,
            CanEdit = p.CanEdit,
            CanReadOnly = p.CanReadOnly,
            CanDelete = p.CanDelete
        });

        await _userPermRepo.SetPermissionsAsync(userId, permissions);
    }

    // =============================================
    // Menus
    // =============================================

    public async Task<IEnumerable<MenuListItem>> GetAllMenusAsync()
    {
        var menus = await _menuRepo.GetAllAsync();
        return menus.Select(m => new MenuListItem
        {
            Id = m.Id,
            ParentId = m.ParentId,
            MenuCode = m.MenuCode,
            MenuName = m.MenuName,
            Icon = m.Icon,
            Route = m.Route,
            SortOrder = m.SortOrder,
            IsActive = m.IsActive
        });
    }

    public async Task<MenuListItem> CreateMenuAsync(CreateMenuRequest request)
    {
        if (request.ParentId.HasValue)
        {
            _ = await _menuRepo.GetByIdAsync(request.ParentId.Value)
                ?? throw new AppException("INVALID_PARENT", "ไม่พบเมนูหลักที่ระบุ");
        }

        // Validate MenuCode uniqueness
        var existingMenu = await _menuRepo.GetByMenuCodeAsync(request.MenuCode.Trim());
        if (existingMenu is not null)
            throw new AppException("MENU_CODE_EXISTS", "รหัสเมนูนี้มีอยู่แล้ว");

        var menu = new Menu
        {
            ParentId = request.ParentId,
            MenuCode = request.MenuCode.Trim(),
            MenuName = request.MenuName.Trim(),
            Icon = request.Icon?.Trim(),
            Route = request.Route?.Trim(),
            SortOrder = request.SortOrder
        };

        menu = await _menuRepo.CreateAsync(menu);

        return new MenuListItem
        {
            Id = menu.Id,
            ParentId = menu.ParentId,
            MenuCode = menu.MenuCode,
            MenuName = menu.MenuName,
            Icon = menu.Icon,
            Route = menu.Route,
            SortOrder = menu.SortOrder,
            IsActive = menu.IsActive
        };
    }

    public async Task UpdateMenuAsync(int menuId, UpdateMenuRequest request)
    {
        var menu = await _menuRepo.GetByIdAsync(menuId)
            ?? throw new AuthException("MENU_NOT_FOUND", "Menu not found.");

        if (request.ParentId.HasValue)
        {
            if (request.ParentId.Value == menuId)
                throw new AppException("INVALID_PARENT", "เมนูไม่สามารถเป็นเมนูหลักของตัวเองได้");

            _ = await _menuRepo.GetByIdAsync(request.ParentId.Value)
                ?? throw new AppException("INVALID_PARENT", "ไม่พบเมนูหลักที่ระบุ");

            // Prevent circular reference — check ancestor chain
            var allMenus = (await _menuRepo.GetAllAsync()).ToList();
            var currentParentId = request.ParentId.Value;
            var visited = new HashSet<int> { menuId };
            while (true)
            {
                if (!visited.Add(currentParentId))
                    throw new AppException("CIRCULAR_REFERENCE", "การตั้งค่าเมนูหลักนี้จะทำให้เกิดการอ้างอิงวนรอบ");

                var parentMenu = allMenus.FirstOrDefault(m => m.Id == currentParentId);
                if (parentMenu?.ParentId is null) break;
                currentParentId = parentMenu.ParentId.Value;
            }
        }

        // Validate MenuCode uniqueness (exclude self)
        var existingMenu = await _menuRepo.GetByMenuCodeAsync(request.MenuCode.Trim());
        if (existingMenu is not null && existingMenu.Id != menuId)
            throw new AppException("MENU_CODE_EXISTS", "รหัสเมนูนี้มีอยู่แล้ว");

        menu.ParentId = request.ParentId;
        menu.MenuCode = request.MenuCode.Trim();
        menu.MenuName = request.MenuName.Trim();
        menu.Icon = request.Icon?.Trim();
        menu.Route = request.Route?.Trim();
        menu.SortOrder = request.SortOrder;
        menu.IsActive = request.IsActive;

        await _menuRepo.UpdateAsync(menu);
    }

    public async Task<List<MenuWithPermission>> GetMenuTreeForUserAsync(Guid userId)
    {
        var menus = await _menuRepo.GetAllAsync();

        // If user has user-specific permissions → use those exclusively
        // Otherwise fallback to role-based merged permissions
        var userPerms = (await _userPermRepo.GetByUserIdAsync(userId)).ToList();
        var hasUserPerms = userPerms.Any();

        Dictionary<int, dynamic> permDict;
        if (hasUserPerms)
        {
            permDict = userPerms.ToDictionary(
                p => p.MenuId,
                p => (dynamic)p);
        }
        else
        {
            var rolePerms = await _permissionRepo.GetMergedPermissionsByUserIdAsync(userId);
            permDict = rolePerms.ToDictionary(
                p => p.MenuId,
                p => (dynamic)p);
        }

        var flatList = menus
            .Where(m => m.IsActive)
            .Select(m =>
            {
                permDict.TryGetValue(m.Id, out var perm);
                return new MenuWithPermission
                {
                    Id = m.Id,
                    ParentId = m.ParentId,
                    MenuCode = m.MenuCode,
                    MenuName = m.MenuName,
                    Icon = m.Icon,
                    Route = m.Route,
                    SortOrder = m.SortOrder,
                    Visible = perm?.Visible ?? false,
                    CanCreate = perm?.CanCreate ?? false,
                    CanEdit = perm?.CanEdit ?? false,
                    CanReadOnly = perm?.CanReadOnly ?? false,
                    CanDelete = perm?.CanDelete ?? false
                };
            })
            .Where(m => m.Visible)
            .ToList();

        return BuildMenuTree(flatList, null);
    }

    public async Task CopyRolePermissionsToUserAsync(Guid userId)
    {
        var menus = (await _menuRepo.GetAllAsync()).ToList();
        var rolePerms = (await _permissionRepo.GetMergedPermissionsByUserIdAsync(userId)).ToList();
        var roleDict = rolePerms.ToDictionary(p => p.MenuId);

        var newPerms = menus.Select(m =>
        {
            roleDict.TryGetValue(m.Id, out var rp);
            return new UserMenuPermission
            {
                UserId = userId,
                MenuId = m.Id,
                Visible = rp?.Visible ?? false,
                CanCreate = rp?.CanCreate ?? false,
                CanEdit = rp?.CanEdit ?? false,
                CanReadOnly = rp?.CanReadOnly ?? false,
                CanDelete = rp?.CanDelete ?? false
            };
        }).ToList();

        await _userPermRepo.SetPermissionsAsync(userId, newPerms);
    }

    private static List<MenuWithPermission> BuildMenuTree(List<MenuWithPermission> allMenus, int? parentId, int depth = 0)
    {
        // Guard against circular references — max 10 levels deep
        if (depth > 10) return [];

        return allMenus
            .Where(m => m.ParentId == parentId)
            .OrderBy(m => m.SortOrder)
            .Select(m =>
            {
                m.Children = BuildMenuTree(allMenus, m.Id, depth + 1);
                return m;
            })
            .ToList();
    }
}
