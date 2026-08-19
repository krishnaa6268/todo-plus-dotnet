using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TodoPlus.Models;

namespace TodoPlus.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        ClaimsPrincipal? ValidateToken(string token);
    }

    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetSecretKey() =>
            Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
            ?? Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? _configuration["JwtSettings:SecretKey"] 
            ?? "SuperSecretTodoPlusJwtSigningKey2026WithAtLeast256BitsOfEntropy!!";

        private string GetIssuer() =>
            Environment.GetEnvironmentVariable("JWT_ISSUER") 
            ?? _configuration["JwtSettings:Issuer"] 
            ?? "TodoPlusApp";

        private string GetAudience() =>
            Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
            ?? _configuration["JwtSettings:Audience"] 
            ?? "TodoPlusUsers";

        private int GetExpirationMinutes()
        {
            var expEnv = Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES");
            if (!string.IsNullOrEmpty(expEnv) && int.TryParse(expEnv, out var expVal))
            {
                return expVal;
            }
            return int.TryParse(_configuration["JwtSettings:ExpirationInMinutes"], out var exp) ? exp : 1440;
        }

        public string GenerateToken(User user)
        {
            var secretKey = GetSecretKey();
            var issuer = GetIssuer();
            var audience = GetAudience();
            var expirationMinutes = GetExpirationMinutes();

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            var secretKey = GetSecretKey();
            var issuer = GetIssuer();
            var audience = GetAudience();

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var _);
                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}
