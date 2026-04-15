using RefactorHeatAlertPostGre.Models.Entities;

namespace RefactorHeatAlertPostGre.Data.Repositories
{
    public interface ISubscriberRepository
    {
        Task<Subscriber?> GetByIdAsync(long chatId, CancellationToken cancellationToken = default);
        Task<List<Subscriber>> GetAllActiveAsync(CancellationToken cancellationToken = default);
        Task<List<long>> GetAllActiveChatIdsAsync(CancellationToken cancellationToken = default);
        Task<Subscriber> SaveAsync(Subscriber subscriber, CancellationToken cancellationToken = default);
        Task<bool> UnsubscribeAsync(long chatId, CancellationToken cancellationToken = default);
        Task<bool> UpdateLastNotifiedAsync(long chatId, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(long chatId, CancellationToken cancellationToken = default);
    }
}