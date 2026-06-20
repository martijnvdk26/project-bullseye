using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BullseyeAPI.Application.DTOs;
using BullseyeAPI.Application.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace BullseyeAPI.Application.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(PlayerDto player)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured. Set it via `dotnet user-secrets set \"Jwt:Key\" \"...\"`.");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, player.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, player.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, player.Name),
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expiryMinutes = _configuration.GetValue<int?>("Jwt:ExpiryMinutes") ?? 60;

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
