using EWMS.Api.Common;
using EWMS.Api.Data;
using EWMS.Api.DTOs.Auth;
using EWMS.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Api.Services
{
    public class AuthService
    {
        private readonly EWMSDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly TokenService _tokenService;
        private readonly AuthTokenSettings _settings;

        public AuthService(EWMSDbContext context, IPasswordHasher<User> passwordHasher,
            TokenService tokenService, AuthTokenSettings settings)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _settings = settings;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
        {
            string userName = dto.UserName.Trim();
            var user = await _context.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.UserName == userName);
            bool passwordValid = user != null && VerifyPassword(user, dto.Password);
            if (!passwordValid || user == null || !string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
                throw new GoodsReceiptException(401, "AUTH_INVALID_CREDENTIALS", "Username or password is incorrect.");
            var session = new AuthSession
            {
                Id = Guid.NewGuid(), UserId = user.Id,
                CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddHours(_settings.SessionHours)
            };
            _context.AuthSessions.Add(session);
            await _context.SaveChangesAsync();
            var permissions = await _context.RolePermissions.Where(x => x.RoleId == user.RoleId)
                .Select(x => x.Permission.PermissionName).ToListAsync();
            return new LoginResponseDto
            {
                User = await MapUserAsync(user), ExpiresAt = session.ExpiresAt,
                Token = _tokenService.GenerateToken(user, session, permissions)
            };
        }

        private bool VerifyPassword(User user, string password)
        {
            try
            {
                return _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password)
                    != PasswordVerificationResult.Failed;
            }
            catch (FormatException) { return false; }
            catch (ArgumentException) { return false; }
        }

        public async Task<AuthUserDto> GetUserAsync(int userId)
        {
            var user = await _context.Users.AsNoTracking().Include(x => x.Role).SingleAsync(x => x.Id == userId);
            return await MapUserAsync(user);
        }

        private async Task<AuthUserDto> MapUserAsync(User user)
        {
            return new AuthUserDto
            {
                UserId = user.Id, UserName = user.UserName, FullName = user.FullName, Role = user.Role.RoleName,
                WarehouseIds = await _context.UserWarehouses.Where(x => x.UserId == user.Id)
                    .Select(x => x.WarehouseId).ToListAsync()
            };
        }

        public async Task LogoutAsync(Guid sessionId)
        {
            var session = await _context.AuthSessions.SingleOrDefaultAsync(x => x.Id == sessionId);
            if (session != null)
            {
                session.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            if (dto.NewPassword.Length < 12 || dto.NewPassword.Length > 128 || dto.NewPassword != dto.ConfirmPassword)
                throw new GoodsReceiptException(400, "AUTH_PASSWORD_POLICY", "New password must contain 12-128 characters and match confirmation.");
            var user = await _context.Users.SingleAsync(x => x.Id == userId);
            if (!VerifyPassword(user, dto.CurrentPassword))
                throw new GoodsReceiptException(400, "AUTH_CURRENT_PASSWORD", "Current password is incorrect.");
            if (dto.CurrentPassword == dto.NewPassword)
                throw new GoodsReceiptException(400, "AUTH_PASSWORD_POLICY", "Choose a different new password.");
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
            var sessions = await _context.AuthSessions.Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync();
            foreach (var session in sessions) session.RevokedAt = DateTime.UtcNow;
            // Password và thu hồi phiên được EF ghi trong cùng SaveChanges transaction.
            await _context.SaveChangesAsync();
        }
    }
}
