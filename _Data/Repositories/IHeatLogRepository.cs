using RefactorHeatAlertPostGre.Models.Entities;

namespace RefactorHeatAlertPostGre.Data.Repositories
{
    public interface IHeatLogRepository
    {
        Task<HeatLog> CreateAsync(HeatLog heatLog, CancellationToken cancellationToken = default);
        Task<List<HeatLog>> GetHistoryAsync(int limit = 100, int offset = 0, CancellationToken cancellationToken = default);
        Task<List<HeatLog>> GetHistoryBySensorAsync(int sensorId, int limit = 50, CancellationToken cancellationToken = default);
        Task<int> DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
        Task<int> DeleteBySensorIdAsync(int sensorId, CancellationToken cancellationToken = default);
        Task<int> GetCountAsync(CancellationToken cancellationToken = default);
        Task<HeatLog?> GetLatestAsync(CancellationToken cancellationToken = default);
        Task<int> PruneOldLogsAsync(int keepCount = 300, CancellationToken cancellationToken = default);
    }
}