using System.Text.Json;
using RefactorHeatAlertPostGre.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace RefactorHeatAlertPostGre.Services
{
    public class GeoService : IGeoService
    {
        private readonly List<GeoJsonFeature> _barangayFeatures = new();
        private readonly ILogger<GeoService> _logger;

        public GeoService(ILogger<GeoService> logger)
        {
            _logger = logger;
            LoadGeoJsonData();
        }

        private void LoadGeoJsonData()
        {
            try
            {
                var jsonPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, 
                    "sharedresource", 
                    "talisaycitycebu.json"
                );

                if (File.Exists(jsonPath))
                {
                    var json = File.ReadAllText(jsonPath);
                    var collection = JsonSerializer.Deserialize<FeatureCollection>(json);
                    
                    if (collection?.features != null)
                    {
                        _barangayFeatures.AddRange(collection.features);
                        _logger.LogInformation("Loaded {Count} barangay boundaries", _barangayFeatures.Count);
                    }
                }
                else
                {
                    _logger.LogWarning("GeoJSON file not found at {Path}", jsonPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load GeoJSON data");
            }
        }

        public string GetBarangay(double latitude, double longitude)
        {
            if (!IsValidCoordinate(latitude, longitude))
                return "Invalid Coordinates";

            foreach (var feature in _barangayFeatures)
            {
                if (feature.geometry == null) continue;

                var geomType = feature.geometry.type;
                var coordsElement = feature.geometry.coordinates;

                if (coordsElement.ValueKind == JsonValueKind.Undefined) continue;

                try
                {
                    if (geomType == "Polygon")
                    {
                        // Polygon: coordinates is an array of rings (exterior + holes)
                        // We only check the exterior ring (first element)
                        var rings = coordsElement.EnumerateArray();
                        if (rings.Any())
                        {
                            var exteriorRing = rings.First();
                            var polygon = ParseRingToPolygon(exteriorRing);
                            if (IsPointInPolygon(longitude, latitude, polygon))
                                return feature.properties?.NAME_3 ?? "Unknown Barangay";
                        }
                    }
                    else if (geomType == "MultiPolygon")
                    {
                        // MultiPolygon: array of polygons, each polygon is array of rings
                        var polygons = coordsElement.EnumerateArray();
                        foreach (var polygonElement in polygons)
                        {
                            var rings = polygonElement.EnumerateArray();
                            if (rings.Any())
                            {
                                var exteriorRing = rings.First();
                                var polygon = ParseRingToPolygon(exteriorRing);
                                if (IsPointInPolygon(longitude, latitude, polygon))
                                    return feature.properties?.NAME_3 ?? "Unknown Barangay";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing geometry for feature");
                }
            }

            return "Outside of Talisay City";
        }

        /// <summary>
        /// Converts a JsonElement representing a ring (array of [lon,lat] pairs) to a double[][] polygon.
        /// </summary>
        private double[][] ParseRingToPolygon(JsonElement ringElement)
        {
            var points = new List<double[]>();
            foreach (var pointElement in ringElement.EnumerateArray())
            {
                var coords = pointElement.EnumerateArray().Select(x => x.GetDouble()).ToArray();
                if (coords.Length >= 2)
                    points.Add(new[] { coords[0], coords[1] });
            }
            return points.ToArray();
        }

        private bool IsPointInPolygon(double x, double y, double[][] polygon)
        {
            if (polygon == null || polygon.Length == 0) return false;
            
            int n = polygon.Length;
            bool inside = false;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if (polygon[i] == null || polygon[j] == null) continue;
                if (polygon[i].Length < 2 || polygon[j].Length < 2) continue;

                double xi = polygon[i][0], yi = polygon[i][1];
                double xj = polygon[j][0], yj = polygon[j][1];

                if (((yi > y) != (yj > y)) && (x < (xj - xi) * (y - yi) / (yj - yi) + xi))
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        public bool IsValidCoordinate(double latitude, double longitude)
        {
            return latitude >= -90 && latitude <= 90 && 
                   longitude >= -180 && longitude <= 180;
        }

        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadiusKm = 6371.0;

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return earthRadiusKm * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        public List<string> GetAllBarangays()
        {
            return _barangayFeatures
                .Select(f => f.properties?.NAME_3)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .OrderBy(name => name)
                .ToList()!;
        }
    }

    // GeoJSON deserialization classes (now using JsonElement for flexible coordinates)
    public class FeatureCollection
    {
        public string type { get; set; } = string.Empty;
        public List<GeoJsonFeature> features { get; set; } = new();
    }

    public class GeoJsonFeature
    {
        public string type { get; set; } = string.Empty;
        public Geometry? geometry { get; set; }
        public Properties? properties { get; set; }
    }

    public class Geometry
    {
        public string type { get; set; } = string.Empty;
        public JsonElement coordinates { get; set; }  // Changed from double[][][] to JsonElement
    }

    public class Properties
    {
        public string NAME_3 { get; set; } = string.Empty;
    }
}