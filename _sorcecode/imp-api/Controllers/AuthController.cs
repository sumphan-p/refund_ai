using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using imp_api.DTOs;
using imp_api.Services;

namespace imp_api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService authService, IWebHostEnvironment env)
    {
        _authService = authService;
        _env = env;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }
        catch (AuthException ex)
        {
            return Unauthorized(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            return StatusCode(201, result);
        }
        catch (AppException ex)
        {
            return ex.Error == "USERNAME_EXISTS"
                ? Conflict(new ErrorResponse { Error = ex.Error, Message = ex.Message })
                : BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
        catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
        {
            return Conflict(new ErrorResponse
            {
                Error = "USERNAME_EXISTS",
                Message = "An account with this username already exists."
            });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        // Always return success to prevent username enumeration
        var response = new MessageResponse
        {
            Message = "If an account exists, we've sent a password reset link."
        };

        try
        {
            var resetLink = await _authService.ForgotPasswordAsync(request);

            // Only include reset link in Development for easy testing
            if (_env.IsDevelopment() && resetLink is not null)
            {
                response.ResetLink = resetLink;
            }
        }
        catch
        {
            // Swallow errors to prevent username enumeration
        }

        return Ok(response);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            await _authService.ResetPasswordAsync(request);
            return Ok(new MessageResponse { Message = "Password reset successfully." });
        }
        catch (AuthException ex)
        {
            return BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");
            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new ErrorResponse { Error = "INVALID_TOKEN", Message = "Invalid token." });
            await _authService.ChangePasswordAsync(userId, request);
            return Ok(new MessageResponse { Message = "Password changed successfully." });
        }
        catch (AuthException ex)
        {
            return BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        try
        {
            var result = await _authService.RefreshAsync(request);
            return Ok(result);
        }
        catch (AuthException ex)
        {
            return Unauthorized(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        await _authService.LogoutAsync(request);
        return NoContent();
    }
}
