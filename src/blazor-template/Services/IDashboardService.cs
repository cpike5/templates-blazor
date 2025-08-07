using BlazorTemplate.Models.Dashboard;

namespace BlazorTemplate.Services
{
    /// <summary>
    /// Service interface for retrieving dashboard data and system metrics.
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Retrieves the main dashboard statistics including user counts, sessions, and security alerts.
        /// </summary>
        /// <returns>Dashboard statistics data.</returns>
        Task<DashboardStatistics> GetStatisticsAsync();
        
        /// <summary>
        /// Retrieves the most recent user activities for display in the activity feed.
        /// </summary>
        /// <param name="count">Number of recent activities to retrieve (default: 10).</param>
        /// <returns>List of recent activity items.</returns>
        Task<List<RecentActivityItem>> GetRecentActivitiesAsync(int count = 10);
        
        /// <summary>
        /// Checks the status of various system components and returns their health status.
        /// </summary>
        /// <returns>List of system status items.</returns>
        Task<List<SystemStatusItem>> GetSystemStatusAsync();
        
        /// <summary>
        /// Retrieves key health metrics for system monitoring.
        /// </summary>
        /// <returns>List of health metric items.</returns>
        Task<List<HealthMetricItem>> GetHealthMetricsAsync();
    }
}