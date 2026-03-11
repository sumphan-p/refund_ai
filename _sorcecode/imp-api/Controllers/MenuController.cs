using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using imp_api.DTOs;
using imp_api.Services;

namespace imp_api.Controllers;

[Authorize]
[ApiController]
[Route("api/menu")]
public class MenuController : ControllerBase
{
    private readonly IAdminService _adminService;

    public MenuController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMenusWithPermissions()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new ErrorResponse { Error = "INVALID_TOKEN", Message = "Invalid token." });

        try
        {
            var tree = await _adminService.GetMenuTreeForUserAsync(userId);
            return Ok(tree);
        }
        catch (AppException ex)
        {
            return BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }
}
