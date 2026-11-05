using System.Text;
using System.Threading.RateLimiting;
using EWMS.Api.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

namespace EWMS.Api.Services;

public static class AuthenticationRegistration
{
    // Đăng ký một lần, dùng lại trong API và kiểm thử HTTP thật.
    public static IServiceCollection AddEwmsAuthentication(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment environment)
    {
        var settings = TokenService.ReadSettings(configuration, environment);
        services.AddSingleton(settings);
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<AuthService>();
        services.AddScoped<TokenService>();
        services.AddScoped<JwtSessionEvents>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.EventsType = typeof(JwtSessionEvents);
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = settings.Issuer,
                ValidateAudience = true, ValidAudience = settings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
                ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = "name", RoleClaimType = "role"
            };
        });
        services.AddAuthorization();
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = ".EWMS.Csrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });
        // Cookie tự gửi theo trình duyệt: mọi thao tác ghi phải kèm anti-CSRF token.
        // AddControllersWithViews đăng ký filter antiforgery chuẩn; API vẫn trả JSON.
        services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0,
                    AutoReplenishment = true
                }));
        });
        return services;
    }
}
