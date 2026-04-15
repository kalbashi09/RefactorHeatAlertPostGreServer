using RefactorHeatAlertPostGre.Models.Entities;
using RefactorHeatAlertPostGre.Models.Enums;

namespace RefactorHeatAlertPostGre.Services.Interfaces
{
    public interface ISimulationService
    {
        /// <summary>
        /// Generates a realistic heat index based on sensor's baseline temperature
        /// </summary>
        int GenerateReading(Sensor sensor);

        /// <summary>
        /// Runs a complete simulation cycle for all active sensors
        /// </summary>
        Task<List<AlertResult>> RunSimulationCycleAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current danger level for a given heat index
        /// </summary>
        DangerLevel GetDangerLevel(int heatIndex);

        /// <summary>
        /// Formats a heat reading into an AlertResult
        /// </summary>
        AlertResult CreateAlertResult(Sensor sensor, int heatIndex);
    }
}