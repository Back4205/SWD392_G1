using EWMS.Api.Entities;

namespace EWMS.Api.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByUsernameAsync(string username);
    Task<User?> GetByIdAsync(int id);
    Task<List<int>> GetWarehouseIdsAsync(int userId);
    Task<List<string>> GetPermissionsAsync(int roleId);
    Task AddSessionAsync(AuthSession session);
    Task EndSessionAsync(Guid sessionId);
    Task UpdatePasswordHashAsync(int userId, string newHash);
}
