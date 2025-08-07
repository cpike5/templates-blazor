namespace BlazorTemplate.Models.Dashboard
{
    /// <summary>
    /// Dashboard statistics data model containing key metrics for the admin dashboard.
    /// </summary>
    public class DashboardStatistics
    {
        /// <summary>
        /// Total number of users in the system.
        /// </summary>
        public int TotalUsers { get; set; }
        
        /// <summary>
        /// Total number of roles configured in the system.
        /// </summary>
        public int TotalRoles { get; set; }
        
        /// <summary>
        /// Number of unique user login sessions today.
        /// </summary>
        public int TodaysSessions { get; set; }
        
        /// <summary>
        /// Number of security alerts (failed logins, lockouts) in the last 24 hours.
        /// </summary>
        public int SecurityAlerts { get; set; }
        
        // Trend calculations (optional, for future enhancement)
        /// <summary>
        /// User growth percentage compared to previous period.
        /// </summary>
        public double UserGrowthPercentage { get; set; }
        
        /// <summary>
        /// Session growth percentage compared to previous period.
        /// </summary>
        public double SessionGrowthPercentage { get; set; }
        
        /// <summary>
        /// Security alerts trend compared to previous period.
        /// </summary>
        public int SecurityAlertsTrend { get; set; }
    }
}