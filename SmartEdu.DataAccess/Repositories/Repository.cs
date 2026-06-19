using Microsoft.EntityFrameworkCore;
using SmartEdu.DataAccess.EntityModels;
using SmartEdu.RazorWeb.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.DataAccess.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity is null) return null;
            // If the entity derives from BaseEntity and is soft-deleted, treat as not found
            if (entity is BaseEntity be && be.IsDeleted) return null;
            return entity;
        }

        public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null)
        {
            IQueryable<T> query = _dbSet;
            if (predicate != null) query = query.Where(predicate);
            return await query.ToListAsync();
        }

        public async Task AddAsync(T entity)
            => await _dbSet.AddAsync(entity);

        public void Update(T entity)
            => _dbSet.Update(entity);

        public void Delete(T entity)
            => _dbSet.Remove(entity);

        public async Task SaveChangesAsync()
            => await _context.SaveChangesAsync();

        public async Task<IEnumerable<T>> GetAllWithIncludeAsync(
        Expression<Func<T, bool>>? predicate = null,
        params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;
            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (typeof(BaseEntity).IsAssignableFrom(typeof(T)))
            {
                query = query.Where(e => !EF.Property<bool>(e, nameof(BaseEntity.IsDeleted)));
            }

            return await query.ToListAsync();
        }

        public async Task<T?> GetAsync(Expression<Func<T, bool>> predicate)
        {
            IQueryable<T> query = _dbSet;

            // Tự động lọc IsDeleted nếu là BaseEntity
            if (typeof(BaseEntity).IsAssignableFrom(typeof(T)))
            {
                query = query.Where(e => !EF.Property<bool>(e, nameof(BaseEntity.IsDeleted)));
            }

            return await query.FirstOrDefaultAsync(predicate);
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }
    }
}
