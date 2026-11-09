using EWMS.Api.Data;
using EWMS.Api.Entities;
using EWMS.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Api.Repositories;

public class UserRepository : IUserRepository
{
    private readonly EWMSDbContext _context;
    public UserRepository(EWMSDbContext context) { _context = context; }

    public Task<User?> FindByUsernameAsync(string username) =>
        _context.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.UserName == username);

    public Task<User?> GetByIdAsync(int id) =>
        _context.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.Id == id);

    public Task<List<int>> GetWarehouseIdsAsync(int userId) =>
        _context.UserWarehouses.Where(x => x.UserId == userId).Select(x => x.WarehouseId).ToListAsync();

    public Task<List<string>> GetPermissionsAsync(int roleId) =>
        _context.RolePermissions.Where(x => x.RoleId == roleId).Select(x => x.Permission.PermissionName).ToListAsync();

    public async Task AddSessionAsync(AuthSession session)
    {
        _context.AuthSessions.Add(session);
        await _context.SaveChangesAsync();
    }

    public async Task EndSessionAsync(Guid sessionId)
    {
        var session = await _context.AuthSessions.SingleOrDefaultAsync(x => x.Id == sessionId);
        if (session == null) return;
        session.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task UpdatePasswordHashAsync(int userId, string newHash)
    {
        var user = await _context.Users.SingleAsync(x => x.Id == userId);
        user.SetPasswordHash(newHash);
        var sessions = await _context.AuthSessions.Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync();
        foreach (var session in sessions) session.RevokedAt = DateTime.UtcNow;
        // Password and revocation remain atomic in one EF SaveChanges transaction.
        await _context.SaveChangesAsync();
    }
}
