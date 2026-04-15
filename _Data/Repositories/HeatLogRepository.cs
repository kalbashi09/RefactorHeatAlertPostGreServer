using Microsoft.EntityFrameworkCore;
using RefactorHeatAlertPostGre.Models.Entities;

namespace RefactorHeatAlertPostGre.Data.Repositories
{
    public class HeatLogRepository : IHeatLogRepository
    {
        private readonly AppDbContext _context;

        public HeatLogRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<HeatLog> CreateAsync(HeatLog heatLog, CancellationToken cancellationToken = default)
        {
            _context.HeatLogs.Add(heatLog);
            await _context.SaveChangesAsync(cancellationToken);
            return heatLog;
        }

        public async Task<List<HeatLog>> GetHistoryAsync(int limit = 100, int offset = 0, CancellationToken cancellationToken = default)
        {
            return await _context.HeatLogs
                .Include(h => h.Sensor)
                .Where(h => h.Sensor.IsActive)
                .OrderByDescending(h => h.RecordedAt)
                .Skip(offset)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<HeatLog>> GetHistoryBySensorAsync(int sensorId, int limit = 50, CancellationToken cancellationToken = default)
        {
            return await _context.HeatLogs
                .Where(h => h.SensorId == sensorId)
                .OrderByDescending(h => h.RecordedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
        {
            var oldLogs = await _context.HeatLogs
                .Where(h => h.RecordedAt < cutoffDate)
                .ToListAsync(cancellationToken);

            if (oldLogs.Any())
            {
                _context.HeatLogs.RemoveRange(oldLogs);
                return await _context.SaveChangesAsync(cancellationToken);
            }

            return 0;
        }

        public async Task<int> DeleteBySensorIdAsync(int sensorId, CancellationToken cancellationToken = default)
        {
            return await _context.HeatLogs
                .Where(h => h.SensorId == sensorId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.HeatLogs.CountAsync(cancellationToken);
        }

        public async Task<HeatLog?> GetLatestAsync(CancellationToken cancellationToken = default)
        {
            return await _context.HeatLogs
                .Include(h => h.Sensor)
                .OrderByDescending(h => h.RecordedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // Maintain only the most recent logs (auto-cleanup)
        public async Task<int> PruneOldLogsAsync(int keepCount = 300, CancellationToken cancellationToken = default)
        {
            var idsToKeep = await _context.HeatLogs
                .OrderByDescending(h => h.RecordedAt)
                .Take(keepCount)
                .Select(h => h.Id)
                .ToListAsync(cancellationToken);

            return await _context.HeatLogs
                .Where(h => !idsToKeep.Contains(h.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}