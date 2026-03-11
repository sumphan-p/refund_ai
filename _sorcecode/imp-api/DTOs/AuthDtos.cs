using System.ComponentModel.DataAnnotations;

namespace imp_api.DTOs;

// =============================================
// Auth Request DTOs
// =============================================

public class LoginRequest
{
    [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
    [MaxLength(128, ErrorMessage = "รหัสผ่านต้องไม่เกิน 128 ตัวอักษร")]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
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
}

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้")]
    public string UserName { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "กรุณาระบุ Token")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่านใหม่")]
    [MinLength(6, ErrorMessage = "รหัสผ่านต้องมีอย่างน้อย 6 ตัวอักษร")]
    [MaxLength(128, ErrorMessage = "รหัสผ่านต้องไม่เกิน 128 ตัวอักษร")]
    public string Password { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "กรุณากรอกรหัสผ่านปัจจุบัน")]
    [MaxLength(128, ErrorMessage = "รหัสผ่านต้องไม่เกิน 128 ตัวอักษร")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่านใหม่")]
    [MinLength(6, ErrorMessage = "รหัสผ่านต้องมีอย่างน้อย 6 ตัวอักษร")]
    [MaxLength(128, ErrorMessage = "รหัสผ่านต้องไม่เกิน 128 ตัวอักษร")]
    public string NewPassword { get; set; } = string.Empty;
}

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

// =============================================
// Auth Response DTOs
// =============================================

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public UserDto User { get; set; } = null!;
}

public class UserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string>? FieldErrors { get; set; }
}

public class MessageResponse
{
    public string Message { get; set; } = string.Empty;
    public string? ResetLink { get; set; }
}
