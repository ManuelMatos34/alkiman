using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Alkiman.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Alkiman.Infrastructure.Security;

/// <summary>Emite JWT propios firmados con clave simétrica (HMAC-SHA256).</summary>
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAtUtc) GenerateToken(
        Guid userId,
        Guid landlordId,
        string email,
        string fullName,
        string businessName,
        string roleName,
        bool isOwner,
        IEnumerable<string> permissions)
    {
        var secret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret no está configurado.");
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expiryHours = int.TryParse(_configuration["Jwt:ExpiryHours"], out var configuredHours) ? configuredHours : 8;

        var expiresAtUtc = DateTime.UtcNow.AddHours(expiryHours);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new("landlord_id", landlordId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("full_name", fullName),
            new("business_name", businessName),
            new("role", roleName),
            new("is_owner", isOwner ? "true" : "false"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        claims.AddRange(permissions.Select(code => new Claim("permission", code)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
