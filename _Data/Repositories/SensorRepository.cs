using Microsoft.EntityFrameworkCore;
using RefactorHeatAlertPostGre.Models.Entities;

namespace RefactorHeatAlertPostGre.Data.Repositories
{
    public class SensorRepository : ISensorRepository
    {
        private readonly AppDbContext _context;

        public SensorRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Sensor?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Sensors
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task<Sensor?> GetByCodeAsync(string sensorCode, CancellationToken cancellationToken = default)
        {
            return await _context.Sensors
                .FirstOrDefaultAsync(s => s.SensorCode == sensorCode, cancellationToken);
        }

        public async Task<List<Sensor>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = _context.Sensors.AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            return await query
                .OrderBy(s => s.DisplayName)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Sensor>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        {
            return await GetAllAsync(false, cancellationToken);
        }

        public async Task<Sensor> CreateAsync(Sensor sensor, CancellationToken cancellationToken = default)
        {
            _context.Sensors.Add(sensor);
            await _context.SaveChangesAsync(cancellationToken);
            return sensor;
        }

        public async Task<Sensor> UpdateAsync(Sensor sensor, CancellationToken cancellationToken = default)
        {
            _context.Sensors.Update(sensor);
            await _context.SaveChangesAsync(cancellationToken);
            return sensor;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var sensor = await GetByIdAsync(id, cancellationToken);
            if (sensor == null) return false;

            _context.Sensors.Remove(sensor);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Sensors.AnyAsync(s => s.Id == id, cancellationToken);
        }

        public async Task<bool> ExistsByCodeAsync(string sensorCode, CancellationToken cancellationToken = default)
        {
            return await _context.Sensors.AnyAsync(s => s.SensorCode == sensorCode, cancellationToken);
        }
    }
}