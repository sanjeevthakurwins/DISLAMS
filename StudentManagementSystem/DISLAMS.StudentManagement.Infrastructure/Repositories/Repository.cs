using Microsoft.EntityFrameworkCore;
using DISLAMS.StudentManagement.Domain.Entities;
using DISLAMS.StudentManagement.Domain.Enums;
using DISLAMS.StudentManagement.Domain.Repositories;

namespace DISLAMS.StudentManagement.Infrastructure.Repositories
{
    /// <summary>
    /// Generic Repository implementation using EF Core
    /// </summary>
    public class Repository<T> : IRepository<T> where T : Entity
    {
        protected readonly DbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(DbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T> GetByIdAsync(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.ModifiedAt = DateTime.UtcNow;
            await _dbSet.AddAsync(entity);
            return entity;
        }

        public virtual async Task<T> UpdateAsync(T entity)
        {
            entity.ModifiedAt = DateTime.UtcNow;
            _dbSet.Update(entity);
            return entity;
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
            }
        }

        public virtual async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
