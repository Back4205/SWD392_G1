using System.ComponentModel.DataAnnotations;
using EWMS.Api.Common;
using EWMS.Api.DTOs.Auth;
using EWMS.Api.Entities;
using EWMS.Api.Interfaces;
using EWMS.Api.Services;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace EWMS.Api.Tests;

public class AuthSequenceTests
{
    private readonly PasswordHasher<User> _hasher = new();

    [Fact]
    public async Task DeactivatedBranchRequiresCorrectPasswordAndNeverCreatesSession()
    {
        var users = Users("Inactive");
        var service = Service(users);
        var wrong = await Assert.ThrowsAsync<GoodsReceiptException>(() => service.LoginAsync(
            new LoginDto { UserName = "demo", Password = "incorrect" }));
        Assert.Equal("AUTH_INVALID_CREDENTIALS", wrong.Code);
        var inactive = await Assert.ThrowsAsync<GoodsReceiptException>(() => service.LoginAsync(
            new LoginDto { UserName = "demo", Password = "Old-Password!" }));
        Assert.Equal("AUTH_ACCOUNT_DEACTIVATED", inactive.Code);
        Assert.Equal(401, inactive.StatusCode);
        Assert.Empty(users.Sessions);
    }

    [Fact]
    public async Task ActiveLoginUsesRepositoryUserAndStoresRealSessionBeforeReturningToken()
    {
        var users = Users();
        var result = await Service(users).LoginAsync(new LoginDto { UserName = " demo ", Password = "Old-Password!" });
        Assert.Equal(users.Account.Id, result.User.UserId);
        Assert.Equal("Manager", result.User.Role);
        Assert.Equal(new[] { 4 }, result.User.WarehouseIds);
        Assert.NotEmpty(result.Token);
        Assert.Equal(users.Account.Id, Assert.Single(users.Sessions).UserId);
        Assert.Equal(new[] { "find:demo", "add-session", "permissions", "warehouses" }, users.Calls);
    }

    [Theory]
    [InlineData("Abc123")]
    [InlineData("Eight123")]
    public async Task PasswordsAtSixCharacterBoundaryAreHashedAndPersistedThroughRepository(string password)
    {
        var dto = new ChangePasswordDto { CurrentPassword = "Old-Password!", NewPassword = password, ConfirmPassword = password };
        Assert.True(Validator.TryValidateObject(dto, new ValidationContext(dto), new List<ValidationResult>(), true));
        var users = Users();
        await Service(users).ChangePasswordAsync(users.Account.Id, dto);
        Assert.Equal(new[] { "get:7", "update-password" }, users.Calls);
        Assert.NotEqual(password, users.Account.PasswordHash);
        Assert.True(users.Account.VerifyPassword(password, _hasher));
        Assert.False(users.Account.VerifyPassword("Old-Password!", _hasher));
    }

    [Theory]
    [InlineData("Old-Password!", "12345", "12345", "AUTH_PASSWORD_POLICY")]
    [InlineData("Old-Password!", "New123", "Other123", "AUTH_PASSWORD_POLICY")]
    [InlineData("wrong-current", "New123", "New123", "AUTH_CURRENT_PASSWORD")]
    [InlineData("Old-Password!", "Old-Password!", "Old-Password!", "AUTH_PASSWORD_POLICY")]
    [InlineData("", "New123", "New123", "AUTH_CURRENT_PASSWORD_REQUIRED")]
    public async Task InvalidPasswordBranchesNeverPersist(string current, string password, string confirmation, string code)
    {
        var users = Users();
        string originalHash = users.Account.PasswordHash;
        var error = await Assert.ThrowsAsync<GoodsReceiptException>(() => Service(users).ChangePasswordAsync(7,
            new ChangePasswordDto { CurrentPassword = current, NewPassword = password, ConfirmPassword = confirmation }));
        Assert.Equal(code, error.Code);
        Assert.Equal(originalHash, users.Account.PasswordHash);
        Assert.DoesNotContain("update-password", users.Calls);
    }

    private FakeUsers Users(string status = "Active")
    {
        var account = new User { Id = 7, UserName = "demo", FullName = "Demo Manager", Status = status,
            RoleId = 2, Role = new Role { Id = 2, RoleName = "Manager" } };
        account.SetPasswordHash(_hasher.HashPassword(account, "Old-Password!"));
        return new FakeUsers(account);
    }

    private AuthService Service(IUserRepository users)
    {
        var settings = new AuthTokenSettings { SigningKey = "Tests-Only-Signing-Key-At-Least-32-Bytes-42!" };
        return new AuthService(users, _hasher, new TokenService(settings), settings);
    }

    private sealed class FakeUsers(User account) : IUserRepository
    {
        public User Account { get; } = account;
        public List<string> Calls { get; } = new();
        public List<AuthSession> Sessions { get; } = new();
        public Task<User?> FindByUsernameAsync(string username)
        { Calls.Add("find:" + username); return Task.FromResult<User?>(username == Account.UserName ? Account : null); }
        public Task<User?> GetByIdAsync(int id)
        { Calls.Add("get:" + id); return Task.FromResult<User?>(id == Account.Id ? Account : null); }
        public Task<List<int>> GetWarehouseIdsAsync(int userId)
        { Calls.Add("warehouses"); return Task.FromResult(new List<int> { 4 }); }
        public Task<List<string>> GetPermissionsAsync(int roleId)
        { Calls.Add("permissions"); return Task.FromResult(new List<string> { "Inbound.Approve" }); }
        public Task AddSessionAsync(AuthSession session)
        { Calls.Add("add-session"); Sessions.Add(session); return Task.CompletedTask; }
        public Task EndSessionAsync(Guid sessionId) => Task.CompletedTask;
        public Task UpdatePasswordHashAsync(int userId, string newHash)
        { Calls.Add("update-password"); Account.SetPasswordHash(newHash); return Task.CompletedTask; }
    }
}
