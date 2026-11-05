using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EWMS.Api.Entities;
using Microsoft.IdentityModel.Tokens;

namespace EWMS.Api.Services
{
    public class AuthTokenSettings
    {
        public string SigningKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = "EWMS";
        public string Audience { get; set; } = "EWMS.Web";
        public int SessionHours { get; set; } = 8;
    }

    public class TokenService
    {
        private readonly AuthTokenSettings _settings;
        public TokenService(AuthTokenSettings settings) { _settings = settings; }

        public string GenerateToken(User user, AuthSession session, List<string> permissions)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("name", user.UserName),
                new Claim("role", user.Role.RoleName),
                new Claim(JwtRegisteredClaimNames.Jti, session.Id.ToString("N"))
            };
            foreach (string permission in permissions)
                claims.Add(new Claim("permission", permission));
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
            var token = new JwtSecurityToken(_settings.Issuer, _settings.Audience, claims,
                session.CreatedAt, session.ExpiresAt, new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static AuthTokenSettings ReadSettings(IConfiguration configuration, IHostEnvironment environment)
        {
            var settings = new AuthTokenSettings();
            settings.SigningKey = configuration["Authentication:SigningKey"] ?? "";
            settings.Issuer = configuration["Authentication:Issuer"] ?? "EWMS";
            settings.Audience = configuration["Authentication:Audience"] ?? "EWMS.Web";
            if (string.IsNullOrEmpty(settings.SigningKey))
            {
                if (!environment.IsDevelopment())
                    throw new InvalidOperationException("Configure Authentication:SigningKey before running outside Development.");
                // Khóa local được giữ ngoài source, dùng lại sau khi restart. Không log khóa.
                string folder = Path.Combine(environment.ContentRootPath, ".local");
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, "jwt-signing-key");
                if (!File.Exists(path))
                    File.WriteAllText(path, Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));
                settings.SigningKey = File.ReadAllText(path).Trim();
            }
            if (Encoding.UTF8.GetByteCount(settings.SigningKey) < 32)
                throw new InvalidOperationException("Authentication:SigningKey must contain at least 32 bytes.");
            return settings;
        }
    }
}
