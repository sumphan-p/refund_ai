using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using imp_api.Models;

namespace imp_api.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _config;
    private readonly byte[] _keyBytes;

    public JwtService(IConfiguration config)
    {
        _config = config;

        var secretKey = config["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Configuration 'Jwt:SecretKey' is required.");

        if (secretKey.Length < 32)
            throw new InvalidOperationException("Configuration 'Jwt:SecretKey' must be at least 32 characters.");

        _keyBytes = Encoding.UTF8.GetBytes(secretKey);
    }

    public string GenerateAccessToken(User user, IEnumerable<string> roles)
    {
        var key = new SymmetricSecurityKey(_keyBytes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("userName", user.UserName),
            new("displayName", user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes()),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public int GetAccessTokenExpirationMinutes()
        => _config.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 15);

    public int GetRefreshTokenExpirationDays()
        => _config.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7);

    public int GetRememberMeExpirationDays()
        => _config.GetValue<int>("Jwt:RememberMeExpirationDays", 30);
}
