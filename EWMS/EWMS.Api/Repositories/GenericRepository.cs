using EWMS.Api.Data;
using EWMS.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Api.Repositories;

public class GenericRepository<T>(EWMSDbContext context) : IGenericRepository<T> where T : class
{
    protected readonly EWMSDbContext Context = context;
    public IQueryable<T> Query => Context.Set<T>();
    public virtual Task<List<T>> GetAllAsync() => Query.ToListAsync();
    public virtual async Task<T?> GetByIdAsync(int id) => await Context.Set<T>().FindAsync(id);
    public void Add(T entity) => Context.Set<T>().Add(entity);
    public void Update(T entity) => Context.Set<T>().Update(entity);
    public Task<int> SaveChangesAsync() => Context.SaveChangesAsync();
}
