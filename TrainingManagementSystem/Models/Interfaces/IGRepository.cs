using System.Linq.Expressions;

namespace TrainingManagementSystem.Models.Interfaces
{
    public interface IGRepository<T>
    {
        void Insert(T entity);
        void Delete(T entity);
        void Update(T entity);
        IQueryable<T> GetAll();
        IQueryable<T> GetWhere(Expression<Func<T, bool>> filter);
        Task<T> GetByIdAsync(object id);
        IQueryable<T> Include(params Expression<Func<T, object>>[] includes);
    }
}
