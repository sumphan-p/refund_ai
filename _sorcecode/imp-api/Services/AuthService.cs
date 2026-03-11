using imp_api.DTOs;
using imp_api.Helpers;
using imp_api.Models;
using imp_api.Repositories;

namespace imp_api.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IPasswordResetRepository _passwordResetRepo;
    private readonly IMenuRepository _menuRepo;
    private readonly IRoleMenuPermissionRepository _permissionRepo;
    private readonly IUserMenuPermissionRepository _userPermRepo;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;

    public AuthService(
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IPasswordResetRepository passwordResetRepo,
        IMenuRepository menuRepo,
        IRoleMenuPermissionRepository permissionRepo,
        IUserMenuPermissionRepository userPermRepo,
        IJwtService jwtService,
        IEmailService emailService,
        IConfiguration config)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _passwordResetRepo = passwordResetRepo;
        _menuRepo = menuRepo;
        _permissionRepo = permissionRepo;
        _userPermRepo = userPermRepo;
        _jwtService = jwtService;
        _emailService = emailService;
        _config = config;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var normalizedUserName = request.UserName.Trim().ToLowerInvariant();
        var user = await _userRepo.GetByUserNameAsync(normalizedUserName);

        if (user is null || !user.IsActive || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            throw new AuthException("INVALID_CREDENTIALS", "Invalid username or password.");

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        ValidatePasswordStrength(request.Password);

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

        // Assign default "User" role
        var roles = await _roleRepo.GetAllAsync();
        var userRole = roles.FirstOrDefault(r => r.RoleName == "User");
        if (userRole is not null)
        {
            await _roleRepo.SetUserRolesAsync(user.Id, new[] { userRole.Id });
        }

        // Copy role-based permissions → UserMenuPermissions as defaults
        await CopyRolePermissionsToUserAsync(user.Id);

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<string?> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var normalizedUserName = request.UserName.Trim().ToLowerInvariant();
        var user = await _userRepo.GetByUserNameAsync(normalizedUserName);

        // Always succeed to prevent username enumeration
        if (user is null || !user.IsActive)
            return null;

        var expirationHours = _config.GetValue<int>("ResetPassword:TokenExpirationHours", 1);

        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddHours(expirationHours)
        };

        await _passwordResetRepo.CreateAsync(resetToken);
        var resetLink = await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken.Token);
        return resetLink;
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        ValidatePasswordStrength(request.Password);

        var resetToken = await _passwordResetRepo.GetByTokenAsync(request.Token);

        if (resetToken is null || !resetToken.IsValid)
            throw new AuthException("INVALID_TOKEN", "This reset link is invalid or has expired. Please request a new one.");

        var user = await _userRepo.GetByIdAsync(resetToken.UserId);
        if (user is null || !user.IsActive)
            throw new AuthException("INVALID_TOKEN", "This account is no longer active.");

        var newHash = PasswordHasher.Hash(request.Password);
        await _userRepo.UpdatePasswordAsync(resetToken.UserId, newHash);
        await _passwordResetRepo.MarkUsedAsync(request.Token);

        // Invalidate all outstanding reset tokens and refresh tokens
        await _passwordResetRepo.InvalidateAllByUserIdAsync(resetToken.UserId);
        await _refreshTokenRepo.RevokeAllByUserIdAsync(resetToken.UserId);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        ValidatePasswordStrength(request.NewPassword);

        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null)
            throw new AuthException("USER_NOT_FOUND", "User not found.");

        if (!PasswordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new AuthException("INVALID_CREDENTIALS", "Current password is incorrect.");

        var newHash = PasswordHasher.Hash(request.NewPassword);
        await _userRepo.UpdatePasswordAsync(userId, newHash);

        // Revoke all refresh tokens (force re-login)
        await _refreshTokenRepo.RevokeAllByUserIdAsync(userId);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request)
    {
        var storedToken = await _refreshTokenRepo.GetByTokenAsync(request.RefreshToken);

        if (storedToken is null || !storedToken.IsActive)
            throw new AuthException("INVALID_TOKEN", "Invalid or expired refresh token.");

        // Validate user BEFORE revoking — prevents locking out user if validation fails
        var user = await _userRepo.GetByIdAsync(storedToken.UserId);
        if (user is null || !user.IsActive)
            throw new AuthException("INVALID_TOKEN", "User account is not active.");

        // Revoke old token (rotation) — only after all validation passes
        await _refreshTokenRepo.RevokeAsync(request.RefreshToken);

        return await GenerateAuthResponseAsync(user);
    }

    public async Task LogoutAsync(LogoutRequest request)
    {
        await _refreshTokenRepo.RevokeAsync(request.RefreshToken);
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user)
    {
        var roles = await _roleRepo.GetRolesByUserIdAsync(user.Id);
        var roleNames = roles.Select(r => r.RoleName).ToList();

        var accessToken = _jwtService.GenerateAccessToken(user, roleNames);
        var refreshTokenValue = _jwtService.GenerateRefreshToken();

        var expirationDays = _jwtService.GetRefreshTokenExpirationDays();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays)
        };

        await _refreshTokenRepo.CreateAsync(refreshToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresIn = _jwtService.GetAccessTokenExpirationMinutes() * 60,
            User = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName,
                Roles = roleNames
            }
        };
    }

    private async Task CopyRolePermissionsToUserAsync(Guid userId)
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

    private static void ValidatePasswordStrength(string password)
    {
        if (password.Length < 6)
            throw new AppException("WEAK_PASSWORD", "รหัสผ่านต้องมีอย่างน้อย 6 ตัวอักษร");

        var hasLetter = password.Any(char.IsLetter);
        var hasDigit = password.Any(char.IsDigit);

        if (!hasLetter || !hasDigit)
            throw new AppException("WEAK_PASSWORD", "รหัสผ่านต้องมีทั้งตัวอักษรและตัวเลข");
    }
}
