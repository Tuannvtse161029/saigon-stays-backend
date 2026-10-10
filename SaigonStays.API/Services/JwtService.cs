using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SaigonStays.API.Models;

namespace SaigonStays.API.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(User user)
    {
        var secret = _config["JwtSettings:Secret"] ?? "DefaultSaigonStaysSecretKeyMustBeLongEnough2026!@#$%^";
        var issuer = _config["JwtSettings:Issuer"] ?? "SaigonStaysAPI";
        var audience = _config["JwtSettings:Audience"] ?? "SaigonStaysClient";
        var expiryMinutes = int.TryParse(_config["JwtSettings:ExpiryMinutes"], out var minutes) ? minutes : 1440;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public DateTime GetExpirationDate()
    {
        var expiryMinutes = int.TryParse(_config["JwtSettings:ExpiryMinutes"], out var minutes) ? minutes : 1440;
        return DateTime.UtcNow.AddMinutes(expiryMinutes);
    }
}
