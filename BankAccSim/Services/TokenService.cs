using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BankAccSim.Models;
using Microsoft.IdentityModel.Tokens;

namespace BankAccSim.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(Customer customer);
}

public class TokenService(IConfiguration config) : ITokenService
{
    public (string Token, DateTime ExpiresAt) CreateToken(Customer customer)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, customer.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, customer.Email),
        };

        var expiresAt = DateTime.UtcNow.AddMinutes(config.GetValue("Jwt:ExpiresMinutes", 60));

        var jwt = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expiresAt);
    }
}