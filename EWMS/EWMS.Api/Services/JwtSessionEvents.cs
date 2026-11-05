using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EWMS.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Api.Services
{
    public class JwtSessionEvents : JwtBearerEvents
    {
        public const string CookieName = ".EWMS.Session";
        private readonly EWMSDbContext _context;
        public JwtSessionEvents(EWMSDbContext context) { _context = context; }

        public override Task MessageReceived(MessageReceivedContext context)
        {
            // Không đọc ID/role từ header giả lập. JWT cookie do máy chủ ký mới là identity.
            if (context.Request.Cookies.TryGetValue(CookieName, out string? token))
                context.Token = token;
            return Task.CompletedTask;
        }

        public override async Task TokenValidated(TokenValidatedContext context)
        {
            string? id = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
            string? userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            string? role = context.Principal?.FindFirstValue("role");
            if (!Guid.TryParseExact(id, "N", out Guid sessionId) || !int.TryParse(userId, out int parsedUserId))
            {
                context.Fail("Invalid session.");
                return;
            }
            var session = await _context.AuthSessions.AsNoTracking().Include(x => x.User).ThenInclude(x => x.Role)
                .SingleOrDefaultAsync(x => x.Id == sessionId);
            // Kiểm tra mỗi request: khóa account, đổi role, logout đều có hiệu lực ngay.
            if (session == null || session.UserId != parsedUserId || session.RevokedAt != null
                || session.ExpiresAt <= DateTime.UtcNow
                || !string.Equals(session.User.Status, "Active", StringComparison.OrdinalIgnoreCase)
                || session.User.Role.RoleName != role)
                context.Fail("Session is no longer valid.");
        }

        public override async Task Challenge(JwtBearerChallengeContext context)
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { code = "AUTH_REQUIRED", message = "Please sign in to continue." });
        }

        public override Task Forbidden(ForbiddenContext context)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return context.Response.WriteAsJsonAsync(new { code = "AUTH_FORBIDDEN", message = "This account cannot access this operation." });
        }
    }
}
