using Microsoft.EntityFrameworkCore;
using RefactorHeatAlertPostGre.Models.Entities;

namespace RefactorHeatAlertPostGre.Data.Repositories
{
    public class SubscriberRepository : ISubscriberRepository
    {
        private readonly AppDbContext _context;

        public SubscriberRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Subscriber?> GetByIdAsync(long chatId, CancellationToken cancellationToken = default)
        {
            return await _context.Subscribers
                .FirstOrDefaultAsync(s => s.ChatId == chatId, cancellationToken);
        }

        public async Task<List<Subscriber>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Subscribers
                .Where(s => s.IsSubscribed)
                .OrderBy(s => s.SubscribedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<long>> GetAllActiveChatIdsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Subscribers
                .Where(s => s.IsSubscribed)
                .Select(s => s.ChatId)
                .ToListAsync(cancellationToken);
        }

        public async Task<Subscriber> SaveAsync(Subscriber subscriber, CancellationToken cancellationToken = default)
        {
            var existing = await GetByIdAsync(subscriber.ChatId, cancellationToken);

            if (existing == null)
            {
                _context.Subscribers.Add(subscriber);
            }
            else
            {
                existing.Username = subscriber.Username;
                existing.IsSubscribed = true;
                existing.SubscribedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return subscriber;
        }

        public async Task<bool> UnsubscribeAsync(long chatId, CancellationToken cancellationToken = default)
        {
            var subscriber = await GetByIdAsync(chatId, cancellationToken);
            if (subscriber == null) return false;

            subscriber.IsSubscribed = false;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> UpdateLastNotifiedAsync(long chatId, CancellationToken cancellationToken = default)
        {
            var subscriber = await GetByIdAsync(chatId, cancellationToken);
            if (subscriber == null) return false;

            subscriber.LastNotifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> ExistsAsync(long chatId, CancellationToken cancellationToken = default)
        {
            return await _context.Subscribers.AnyAsync(s => s.ChatId == chatId, cancellationToken);
        }
    }
}