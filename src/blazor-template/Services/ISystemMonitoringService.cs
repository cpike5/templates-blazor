using BlazorTemplate.Models.Dashboard;

namespace BlazorTemplate.Services
{
    /// <summary>
    /// Service interface for system monitoring and health metrics collection.
    /// </summary>
    public interface ISystemMonitoringService
    {
        /// <summary>
        /// Gets the current memory usage information.
        /// </summary>
        /// <returns>Memory usage details including percentage and formatted display.</returns>
        Task<(double UsagePercentage, string FormattedUsage, long TotalMemory, long UsedMemory)> GetMemoryUsageAsync();

        /// <summary>
        /// Gets the current storage usage information for the system and database.
        /// </summary>
        /// <returns>Storage usage details including percentage and formatted display.</returns>
        Task<(double UsagePercentage, string FormattedUsage, long TotalSpace, long UsedSpace, long DatabaseSize)> GetStorageUsageAsync();

        /// <summary>
        /// Gets the current CPU usage percentage.
        /// </summary>
        /// <returns>CPU usage percentage.</returns>
        Task<double> GetCpuUsageAsync();

        /// <summary>
        /// Gets the application startup time for uptime calculations.
        /// </summary>
        /// <returns>Application startup timestamp.</returns>
        DateTime GetApplicationStartTime();

        /// <summary>
        /// Gets the uptime percentage based on expected vs actual runtime.
        /// </summary>
        /// <returns>Uptime percentage and formatted uptime string.</returns>
        Task<(string UptimePercentage, string FormattedUptime, TimeSpan Uptime)> GetUptimeAsync();

        /// <summary>
        /// Gets the current database size in bytes.
        /// </summary>
        /// <returns>Database size in bytes and formatted display.</returns>
        Task<(long SizeBytes, string FormattedSize)> GetDatabaseSizeAsync();

        /// <summary>
        /// Records a system health metric to the database for historical tracking.
        /// </summary>
        /// <param name="metricType">The type of metric (e.g., "Memory", "Storage", "CPU").</param>
        /// <param name="metricValue">The metric value as a string.</param>
        Task RecordHealthMetricAsync(string metricType, string metricValue);

        /// <summary>
        /// Gets historical health metrics from the database.
        /// </summary>
        /// <param name="metricType">The type of metric to retrieve.</param>
        /// <param name="hoursBack">Number of hours back to retrieve data.</param>
        /// <returns>List of historical metric values.</returns>
        Task<List<(DateTime Timestamp, string Value)>> GetHistoricalMetricsAsync(string metricType, int hoursBack = 24);
    }
}