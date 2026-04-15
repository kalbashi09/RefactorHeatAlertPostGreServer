using RefactorHeatAlertPostGre.Models.Entities;

namespace RefactorHeatAlertPostGre.Data.Repositories
{
    public interface ISensorRepository
    {
        Task<Sensor?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Sensor?> GetByCodeAsync(string sensorCode, CancellationToken cancellationToken = default);
        Task<List<Sensor>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
        Task<List<Sensor>> GetAllActiveAsync(CancellationToken cancellationToken = default);
        Task<Sensor> CreateAsync(Sensor sensor, CancellationToken cancellationToken = default);
        Task<Sensor> UpdateAsync(Sensor sensor, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> ExistsByCodeAsync(string sensorCode, CancellationToken cancellationToken = default);
    }
}