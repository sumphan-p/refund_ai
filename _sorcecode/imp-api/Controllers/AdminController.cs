using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using imp_api.DTOs;
using imp_api.Services;

namespace imp_api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    // =============================================
    // Users
    // =============================================

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _adminService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var user = await _adminService.CreateUserAsync(request);
            return StatusCode(201, user);
        }
        catch (AppException ex)
        {
            return ex.Error == "USERNAME_EXISTS"
                ? Conflict(new ErrorResponse { Error = ex.Error, Message = ex.Message })
                : BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }

    [HttpPut("users/{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            await _adminService.UpdateUserAsync(id, request);
            return NoContent();
        }
        catch (AppException ex)
        {
            return ex.Error == "USER_NOT_FOUND"
                ? NotFound(new ErrorResponse { Error = ex.Error, Message = ex.Message })
                : BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }

    [HttpPut("users/{id:guid}/toggle-active")]
    public async Task<IActionResult> ToggleUserActive(Guid id)
    {
        try
        {
            await _adminService.ToggleUserActiveAsync(id);
            return NoContent();
        }
        catch (AppException ex)
        {
            return ex.Error == "USER_NOT_FOUND"
                ? NotFound(new ErrorResponse { Error = ex.Error, Message = ex.Message })
                : BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }

    [HttpPost("users/{id:guid}/roles")]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignRoleRequest request)
    {
        try
        {
            await _adminService.AssignRolesAsync(id, request);
            return NoContent();
        }
        catch (AppException ex)
        {
            return ex.Error == "USER_NOT_FOUND"
                ? NotFound(new ErrorResponse { Error = ex.Error, Message = ex.Message })
                : BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }

    // =============================================
    // Roles
    // =============================================

    [HttpGet("roles")]
    public async Task<IActionResult> GetAllRoles()
    {
        var roles = await _adminService.GetAllRolesAsync();
        return Ok(roles);
    }

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        try
        {
            var role = await _adminService.CreateRoleAsync(request);
            return StatusCode(201, role);
        }
        catch (AppException ex)
        {
            return BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }

    [HttpPut("roles/{id:int}")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            await _adminService.UpdateRoleAsync(id, request);
            return NoContent();
        }
        catch (AppException ex)
        {
            return ex.Error == "ROLE_NOT_FOUND"
                ? NotFound(new ErrorResponse { Error = ex.Error, Message = ex.Message })
                : BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }

    [HttpGet("roles/{id:int}/permissions")]
    public async Task<IActionResult> GetRolePermissions(int id)
    {
        var permissions = await _adminService.GetRolePermissionsAsync(id);
        return Ok(permissions);
    }

    [HttpPut("roles/{id:int}/permissions")]
    public async Task<IActionResult> SetRolePermissions(int id, [FromBody] SetPermissionsRequest request)
    {
        try
        {
            await _adminService.SetRolePermissionsAsync(id, request);
            return NoContent();
        }
        catch (AppException ex)
        {
            return ex.Error == "ROLE_NOT_FOUND"
                ? NotFound(new ErrorResponse { Error = ex.Error, Message = ex.Message })
                : BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }

    // =============================================
    // User-specific Permissions
    // =============================================

    [HttpGet("users/{id:guid}/permissions")]
    public async Task<IActionResult> GetUserPermissions(Guid id)
    {
        try
        {
            var permissions = await _adminService.GetUserPermissionsAsync(id);
            return Ok(permissions);
        }
        catch (AppException ex)
        {
            return ex.Error == "USER_NOT_FOUND"
                ? NotFound(new ErrorResponse { Error = ex.Error, Message = ex.Message })
                : BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }

    [HttpPut("users/{id:guid}/permissions")]
    public async Task<IActionResult> SetUserPermissions(Guid id, [FromBody] SetUserPermissionsRequest request)
    {
        try
        {
            await _adminService.SetUserPermissionsAsync(id, request);
            return NoContent();
        }
        catch (AppException ex)
        {
            return ex.Error == "USER_NOT_FOUND"
                ? NotFound(new ErrorResponse { Error = ex.Error, Message = ex.Message })
                : BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }

    // =============================================
    // Menus
    // =============================================

    [HttpGet("menus")]
    public async Task<IActionResult> GetAllMenus()
    {
        var menus = await _adminService.GetAllMenusAsync();
        return Ok(menus);
    }

    [HttpPost("menus")]
    public async Task<IActionResult> CreateMenu([FromBody] CreateMenuRequest request)
    {
        try
        {
            var menu = await _adminService.CreateMenuAsync(request);
            return StatusCode(201, menu);
        }
        catch (AppException ex)
        {
            return BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }

    [HttpPut("menus/{id:int}")]
    public async Task<IActionResult> UpdateMenu(int id, [FromBody] UpdateMenuRequest request)
    {
        try
        {
            await _adminService.UpdateMenuAsync(id, request);
            return NoContent();
        }
        catch (AppException ex)
        {
            return ex.Error == "MENU_NOT_FOUND"
                ? NotFound(new ErrorResponse { Error = ex.Error, Message = ex.Message })
                : BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }
}
