using Microsoft.EntityFrameworkCore;
using RefactorHeatAlertPostGre.Models.Entities;

namespace RefactorHeatAlertPostGre.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(AppDbContext context)
        {
            // Apply pending migrations (if any - should already be done)
            // await context.Database.MigrateAsync();
            // OPTIONAL: Seed static sensors if none exist (using your real sensor list)
            if (!await context.Sensors.AnyAsync(s => s.SensorCode.StartsWith("TAL-")))
            {
                var staticSensors = new List<Sensor>
                {
                    new() { SensorCode = "TAL-01", DisplayName = "Tabunok Public Market", Barangay = "Tabunok", Latitude = 10.26320000m, Longitude = 123.83850000m, BaselineTemp = 34, EnvironmentType = "Outdoor-Shade", IsActive = true },
                    new() { SensorCode = "TAL-02", DisplayName = "Talisay City Hall", Barangay = "Mohon", Latitude = 10.25337990m, Longitude = 123.82944830m, BaselineTemp = 32, EnvironmentType = "Indoor", IsActive = true },
                    new() { SensorCode = "TAL-03", DisplayName = "Linao Barangay Hall", Barangay = "Linao", Latitude = 10.25620000m, Longitude = 123.81900000m, BaselineTemp = 31, EnvironmentType = "Indoor", IsActive = true },
                    new() { SensorCode = "TAL-04", DisplayName = "Sta. Teresa de Avila Church", Barangay = "Poblacion", Latitude = 10.24400000m, Longitude = 123.84780000m, BaselineTemp = 31, EnvironmentType = "Indoor", IsActive = true },
                    new() { SensorCode = "TAL-05", DisplayName = "Dumlog Sports Complex", Barangay = "Dumlog", Latitude = 10.24480000m, Longitude = 123.83930000m, BaselineTemp = 32, EnvironmentType = "Indoor", IsActive = true },
                    new() { SensorCode = "TAL-06", DisplayName = "Lagtang Elementary School", Barangay = "Lagtang", Latitude = 10.26350000m, Longitude = 123.83220000m, BaselineTemp = 31, EnvironmentType = "Indoor", IsActive = true },
                    new() { SensorCode = "TAL-07", DisplayName = "San Roque Parish", Barangay = "San Roque", Latitude = 10.25450000m, Longitude = 123.84650000m, BaselineTemp = 33, EnvironmentType = "Indoor", IsActive = true },
                    new() { SensorCode = "TAL-08", DisplayName = "Bulacao Central School", Barangay = "Bulacao", Latitude = 10.26750000m, Longitude = 123.84350000m, BaselineTemp = 33, EnvironmentType = "Indoor", IsActive = true },
                    new() { SensorCode = "TAL-09", DisplayName = "Lawaan III Health Center", Barangay = "Lawaan III", Latitude = 10.25220000m, Longitude = 123.83680000m, BaselineTemp = 33, EnvironmentType = "Indoor-AC", IsActive = true },
                    new() { SensorCode = "TAL-10", DisplayName = "Pooc Barangay Hall", Barangay = "Pooc", Latitude = 10.23720000m, Longitude = 123.84250000m, BaselineTemp = 31, EnvironmentType = "Indoor", IsActive = true },
                    new() { SensorCode = "TAL-11", DisplayName = "Cansojong Fish Port", Barangay = "Cansojong", Latitude = 10.24350000m, Longitude = 123.85420000m, BaselineTemp = 32, EnvironmentType = "Outdoor-Sun", IsActive = true },
                    new() { SensorCode = "TAL-12", DisplayName = "Biasong Elementary School", Barangay = "Biasong", Latitude = 10.24180000m, Longitude = 123.82750000m, BaselineTemp = 30, EnvironmentType = "Indoor", IsActive = true },
                    new() { SensorCode = "TAL-13", DisplayName = "Jaclupan Dam Checkpoint", Barangay = "Jaclupan", Latitude = 10.30220000m, Longitude = 123.81670000m, BaselineTemp = 28, EnvironmentType = "Outdoor-Shade", IsActive = true },
                    new() { SensorCode = "TAL-14", DisplayName = "San Isidro Barangay Hall", Barangay = "San Isidro", Latitude = 10.26120000m, Longitude = 123.84620000m, BaselineTemp = 33, EnvironmentType = "Indoor", IsActive = true },
                    new() { SensorCode = "TAL-15", DisplayName = "Tangke Sea Wall", Barangay = "Tangke", Latitude = 10.23550000m, Longitude = 123.85880000m, BaselineTemp = 32, EnvironmentType = "Outdoor-Sun", IsActive = true },
                    new() { SensorCode = "TAL-16", DisplayName = "Maghaway Heights", Barangay = "Maghaway", Latitude = 10.26850000m, Longitude = 123.81220000m, BaselineTemp = 29, EnvironmentType = "Outdoor-Shade", IsActive = true },
                    new() { SensorCode = "TAL-17", DisplayName = "Candulawan Chapel", Barangay = "Candulawan", Latitude = 10.27550000m, Longitude = 123.82850000m, BaselineTemp = 29, EnvironmentType = "Indoor", IsActive = true },
                    new() { SensorCode = "TAL-18", DisplayName = "Camp 4 Barangay Hall", Barangay = "Camp 4", Latitude = 10.32050000m, Longitude = 123.82050000m, BaselineTemp = 27, EnvironmentType = "Indoor", IsActive = true },
                    new() { SensorCode = "TAL-19", DisplayName = "Manipis Road Station", Barangay = "Manipis", Latitude = 10.33420000m, Longitude = 123.80150000m, BaselineTemp = 27, EnvironmentType = "Outdoor-Pavement", IsActive = true },
                    new() { SensorCode = "TAL-20", DisplayName = "Tapul Junction", Barangay = "Tapul", Latitude = 10.27850000m, Longitude = 123.81880000m, BaselineTemp = 28, EnvironmentType = "Indoor", IsActive = true },
                    new() { SensorCode = "TAL-45", DisplayName = "T PARK", Barangay = "Poblacion", Latitude = 10.24214200m, Longitude = 123.84860000m, BaselineTemp = 34, EnvironmentType = "Outdoor-Sun", IsActive = true }
                };

                context.Sensors.AddRange(staticSensors);
                await context.SaveChangesAsync();
                Console.WriteLine($"✅ {staticSensors.Count} static sensors seeded.");
            }
            else
            {
                Console.WriteLine("✅ Static sensors already exist. Skipping sensor seed.");
            }
        }
    }
}