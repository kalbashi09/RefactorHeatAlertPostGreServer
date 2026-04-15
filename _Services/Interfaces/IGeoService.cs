namespace RefactorHeatAlertPostGre.Services.Interfaces
{
    public interface IGeoService
    {
        /// <summary>
        /// Determines the barangay name from latitude/longitude coordinates
        /// </summary>
        string GetBarangay(double latitude, double longitude);

        /// <summary>
        /// Validates if coordinates are within valid range
        /// </summary>
        bool IsValidCoordinate(double latitude, double longitude);

        /// <summary>
        /// Calculates distance between two points in kilometers
        /// </summary>
        double CalculateDistance(double lat1, double lon1, double lat2, double lon2);

        /// <summary>
        /// Gets all barangay names in the system
        /// </summary>
        List<string> GetAllBarangays();
    }
}