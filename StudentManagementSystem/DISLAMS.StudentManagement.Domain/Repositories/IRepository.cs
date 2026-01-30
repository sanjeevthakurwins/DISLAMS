using DISLAMS.StudentManagement.Domain.Entities;

namespace DISLAMS.StudentManagement.Domain.Repositories
{
    /// <summary>
    /// Generic repository interface for data access
    /// </summary>
    public interface IRepository<T> where T : Entity
    {
        Task<T> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> AddAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task DeleteAsync(Guid id);
        Task SaveChangesAsync();
    }
}
