using EWMS.Api.Common;
using EWMS.Api.DTOs.Auth;
using EWMS.Api.Entities;
using Microsoft.AspNetCore.Identity;
using EWMS.Api.Interfaces;

namespace EWMS.Api.Services
{
    public class AuthService
    {
        private readonly IUserRepository _users;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly TokenService _tokenService;
        private readonly AuthTokenSettings _settings;

        public AuthService(IUserRepository users, IPasswordHasher<User> passwordHasher,
            TokenService tokenService, AuthTokenSettings settings)
        {
            _users = users;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _settings = settings;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
        {
            string userName = dto.UserName.Trim();
            var user = await _users.FindByUsernameAsync(userName);
            if (user == null || !user.VerifyPassword(dto.Password, _passwordHasher))
                throw new GoodsReceiptException(401, "AUTH_INVALID_CREDENTIALS", "Username or password is incorrect.");
            if (!string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
                throw new GoodsReceiptException(401, "AUTH_ACCOUNT_DEACTIVATED", "Your account is deactivated. Contact the administrator.");
            var session = new AuthSession
            {
                Id = Guid.NewGuid(), UserId = user.Id,
                CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddHours(_settings.SessionHours)
            };
            await _users.AddSessionAsync(session);
            var permissions = await _users.GetPermissionsAsync(user.RoleId);
            return new LoginResponseDto
            {
                User = await MapUserAsync(user), ExpiresAt = session.ExpiresAt,
                Token = _tokenService.GenerateToken(user, session, permissions)
            };
        }

        public async Task<AuthUserDto> GetUserAsync(int userId)
        {
            var user = await _users.GetByIdAsync(userId)
                ?? throw new GoodsReceiptException(404, "AUTH_USER_NOT_FOUND", "User account was not found.");
            return await MapUserAsync(user);
        }

        private async Task<AuthUserDto> MapUserAsync(User user)
        {
            return new AuthUserDto
            {
                UserId = user.Id, UserName = user.UserName, FullName = user.FullName, Role = user.Role.RoleName,
                WarehouseIds = await _users.GetWarehouseIdsAsync(user.Id)
            };
        }

        public Task LogoutAsync(Guid sessionId) => _users.EndSessionAsync(sessionId);

        public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                throw new GoodsReceiptException(400, "AUTH_CURRENT_PASSWORD_REQUIRED", "Current password is required.");
            if (dto.NewPassword == null || dto.NewPassword.Length < 6 || dto.NewPassword.Length > 128 || dto.NewPassword != dto.ConfirmPassword)
                throw new GoodsReceiptException(400, "AUTH_PASSWORD_POLICY", "New password must contain 6-128 characters and match confirmation.");
            var user = await _users.GetByIdAsync(userId)
                ?? throw new GoodsReceiptException(404, "AUTH_USER_NOT_FOUND", "User account was not found.");
            if (!user.VerifyPassword(dto.CurrentPassword, _passwordHasher))
                throw new GoodsReceiptException(400, "AUTH_CURRENT_PASSWORD", "Current password is incorrect.");
            if (dto.CurrentPassword == dto.NewPassword)
                throw new GoodsReceiptException(400, "AUTH_PASSWORD_POLICY", "Choose a different new password.");
            await _users.UpdatePasswordHashAsync(userId, _passwordHasher.HashPassword(user, dto.NewPassword));
        }
    }
}
