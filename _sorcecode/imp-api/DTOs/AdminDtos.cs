using System.ComponentModel.DataAnnotations;

namespace imp_api.DTOs;

// =============================================
// Admin — User Management
// =============================================

public class CreateUserRequest
{
    [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้")]
    [MinLength(3, ErrorMessage = "ชื่อผู้ใช้ต้องมีอย่างน้อย 3 ตัวอักษร")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
    [MinLength(6, ErrorMessage = "รหัสผ่านต้องมีอย่างน้อย 6 ตัวอักษร")]
    [MaxLength(128, ErrorMessage = "รหัสผ่านต้องไม่เกิน 128 ตัวอักษร")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกชื่อที่แสดง")]
    public string DisplayName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    public string? Email { get; set; }

    public List<int>? RoleIds { get; set; }
}

public class UpdateUserRequest
{
    [Required(ErrorMessage = "กรุณากรอกชื่อที่แสดง")]
    public string DisplayName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    public string? Email { get; set; }
}

public class AssignRoleRequest
{
    [Required]
    public List<int> RoleIds { get; set; } = new();
}

public class UserListItem
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

// =============================================
// Admin — Role Management
// =============================================

public class CreateRoleRequest
{
    [Required(ErrorMessage = "กรุณากรอกชื่อบทบาท")]
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateRoleRequest
{
    [Required(ErrorMessage = "กรุณากรอกชื่อบทบาท")]
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class RoleListItem
{
    public int Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

// =============================================
// Admin — Permission Management
// =============================================

public class SetPermissionsRequest
{
    [Required]
    public List<PermissionEntry> Permissions { get; set; } = new();
}

public class PermissionEntry
{
    public int MenuId { get; set; }
    public bool Visible { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanReadOnly { get; set; }
    public bool CanDelete { get; set; }
}

public class PermissionItem
{
    public int MenuId { get; set; }
    public string MenuCode { get; set; } = string.Empty;
    public string MenuName { get; set; } = string.Empty;
    public bool Visible { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanReadOnly { get; set; }
    public bool CanDelete { get; set; }
}

// =============================================
// Admin — User-specific Permissions
// =============================================

public class SetUserPermissionsRequest
{
    [Required]
    public List<PermissionEntry> Permissions { get; set; } = new();
}

// =============================================
// Admin — Menu Management
// =============================================

public class CreateMenuRequest
{
    public int? ParentId { get; set; }

    [Required(ErrorMessage = "กรุณากรอกรหัสเมนู")]
    public string MenuCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกชื่อเมนู")]
    public string MenuName { get; set; } = string.Empty;

    public string? Icon { get; set; }
    public string? Route { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateMenuRequest
{
    public int? ParentId { get; set; }

    [Required(ErrorMessage = "กรุณากรอกรหัสเมนู")]
    public string MenuCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกชื่อเมนู")]
    public string MenuName { get; set; } = string.Empty;

    public string? Icon { get; set; }
    public string? Route { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MenuListItem
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string MenuCode { get; set; } = string.Empty;
    public string MenuName { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Route { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

// =============================================
// Menu with Permissions (for current user)
// =============================================

public class MenuWithPermission
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string MenuCode { get; set; } = string.Empty;
    public string MenuName { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Route { get; set; }
    public int SortOrder { get; set; }
    public bool Visible { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanReadOnly { get; set; }
    public bool CanDelete { get; set; }
    public List<MenuWithPermission> Children { get; set; } = new();
}
