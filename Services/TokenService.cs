using System.Text;
using Backend.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) Create(Account account, int? cinemaId);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config) => _config = config;

    public (string Token, DateTime ExpiresAt) Create(Account account, int? cinemaId)
    {
        var key = _config["Jwt:Key"]!;
        var expires = DateTime.UtcNow.AddMinutes(_config.GetValue<int>("Jwt:ExpireMinutes", 480));

        var claims = new Dictionary<string, object>
        {
            ["sub"] = account.UserID.ToString(),
            ["role"] = account.Role ?? "",
            ["name"] = account.FullName ?? "",
            ["email"] = account.Email ?? ""
        };
        if (cinemaId.HasValue) claims["cinemaId"] = cinemaId.Value.ToString();

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"],
            Expires = expires,
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return (token, expires);
    }
}
