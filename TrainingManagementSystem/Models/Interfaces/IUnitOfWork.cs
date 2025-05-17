namespace TrainingManagementSystem.Models.Interfaces
{
    public interface IUnitOfWork<T>
    {
        IGRepository<T> Entity { get; }
        Task SaveAsync();
    }

}
