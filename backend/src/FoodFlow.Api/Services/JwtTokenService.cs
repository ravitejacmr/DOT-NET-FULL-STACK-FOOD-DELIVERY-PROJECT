using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FoodFlow.Api.Data;
using FoodFlow.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace FoodFlow.Api.Services;

public class JwtTokenService(IConfiguration configuration, FoodFlowDbContext db)
{
    public string CreateAccessToken(AppUser user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(ClaimTypes.Name, user.Email)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"], audience: configuration["Jwt:Audience"],
            claims: claims, expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> CreateRefreshTokenAsync(AppUser user)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id, TokenHash = Hash(token), ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await db.SaveChangesAsync();
        return token;
    }

    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
