namespace EWMS.Api.Interfaces;

// Repositories share the request's DbContext; saving never commits an outer transaction.
public interface IGenericRepository<T> where T : class
{
    IQueryable<T> Query { get; }
    Task<List<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    void Add(T entity);
    void Update(T entity);
    Task<int> SaveChangesAsync();
}
