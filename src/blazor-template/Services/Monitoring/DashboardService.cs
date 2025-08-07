using BlazorTemplate.Data;
using BlazorTemplate.Models.Dashboard;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BlazorTemplate.Services.Monitoring
{
    /// <summary>
    /// Service implementation for retrieving dashboard data and system metrics.
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<DashboardService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISystemMonitoringService _systemMonitoring;

        /// <summary>
        /// Initializes a new instance of the DashboardService.
        /// </summary>
        /// <param name="context">The application database context.</param>
        /// <param name="userManager">ASP.NET Core Identity user manager.</param>
        /// <param name="roleManager">ASP.NET Core Identity role manager.</param>
        /// <param name="logger">Logger instance for error logging.</param>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="systemMonitoring">System monitoring service for real-time metrics.</param>
        public DashboardService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<DashboardService> logger,
            IConfiguration configuration,
            ISystemMonitoringService systemMonitoring)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _configuration = configuration;
            _systemMonitoring = systemMonitoring;
        }

        /// <summary>
        /// Retrieves the main dashboard statistics including user counts, sessions, and security alerts.
        /// </summary>
        /// <returns>Dashboard statistics data.</returns>
        public async Task<DashboardStatistics> GetStatisticsAsync()
        {
            try
            {
                _logger.LogDebug("Retrieving dashboard statistics");

                // Get user statistics
                var totalUsers = await _userManager.Users.CountAsync();
                var totalRoles = await _roleManager.Roles.CountAsync();
                
                // Get today's sessions (activities)
                var today = DateTime.UtcNow.Date;
                var todaysSessions = await _context.UserActivities
                    .Where(a => a.Timestamp >= today && a.Action.Contains("Login"))
                    .Select(a => a.UserId)
                    .Distinct()
                    .CountAsync();

                // Get security alerts (failed login attempts in last 24 hours)
                var yesterday = DateTime.UtcNow.AddDays(-1);
                var securityAlerts = await _context.UserActivities
                    .Where(a => a.Timestamp >= yesterday && 
                           (a.Action.Contains("Failed") || a.Action.Contains("Lockout")))
                    .CountAsync();

                _logger.LogDebug("Retrieved statistics: {TotalUsers} users, {TotalRoles} roles, {TodaysSessions} sessions, {SecurityAlerts} alerts", 
                    totalUsers, totalRoles, todaysSessions, securityAlerts);

                return new DashboardStatistics
                {
                    TotalUsers = totalUsers,
                    TotalRoles = totalRoles,
                    TodaysSessions = todaysSessions,
                    SecurityAlerts = securityAlerts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard statistics");
                return new DashboardStatistics(); // Return default values
            }
        }

        /// <summary>
        /// Retrieves the most recent user activities for display in the activity feed.
        /// </summary>
        /// <param name="count">Number of recent activities to retrieve (default: 10).</param>
        /// <returns>List of recent activity items.</returns>
        public async Task<List<RecentActivityItem>> GetRecentActivitiesAsync(int count = 10)
        {
            try
            {
                _logger.LogDebug("Retrieving {Count} recent activities", count);

                var activities = await _context.UserActivities
                    .Include(a => a.User)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(count)
                    .Select(a => new RecentActivityItem
                    {
                        UserId = a.UserId,
                        UserName = a.User != null ? a.User.UserName ?? "Unknown" : "Unknown",
                        UserEmail = a.User != null ? a.User.Email ?? "" : "",
                        Action = a.Action,
                        Details = a.Details,
                        Timestamp = a.Timestamp,
                        IpAddress = a.IpAddress
                    })
                    .ToListAsync();

                _logger.LogDebug("Retrieved {Count} activities", activities.Count);
                return activities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent activities");
                return new List<RecentActivityItem>();
            }
        }

        /// <summary>
        /// Checks the status of various system components and returns their health status.
        /// </summary>
        /// <returns>List of system status items.</returns>
        public async Task<List<SystemStatusItem>> GetSystemStatusAsync()
        {
            _logger.LogDebug("Checking system status");
            var statusItems = new List<SystemStatusItem>();

            // Database Status
            var dbStatus = await CheckDatabaseStatusAsync();
            statusItems.Add(new SystemStatusItem
            {
                Name = "Database",
                Description = dbStatus.IsHealthy ? "SQL Server connection active" : "Connection issues detected",
                Status = dbStatus.IsHealthy ? SystemStatus.Healthy : SystemStatus.Error,
                Icon = "fas fa-database"
            });

            // Email Service Status (placeholder - implement based on your email service)
            statusItems.Add(new SystemStatusItem
            {
                Name = "Email Service",
                Description = "SMTP server responding",
                Status = SystemStatus.Healthy, // TODO: Implement actual check
                Icon = "fas fa-envelope"
            });

            // Security Status (based on recent failed logins)
            var securityStatus = await CheckSecurityStatusAsync();
            statusItems.Add(new SystemStatusItem
            {
                Name = "Security",
                Description = securityStatus.FailedLoginCount > 5 ? 
                    $"{securityStatus.FailedLoginCount} failed login attempts detected" : 
                    "No security issues detected",
                Status = securityStatus.FailedLoginCount > 5 ? SystemStatus.Warning : SystemStatus.Healthy,
                Icon = "fas fa-shield-alt"
            });

            // Memory Usage (using real system monitoring)
            var memoryInfo = await _systemMonitoring.GetMemoryUsageAsync();
            statusItems.Add(new SystemStatusItem
            {
                Name = "Memory Usage",
                Description = $"{memoryInfo.FormattedUsage} of available memory",
                Status = memoryInfo.UsagePercentage > 80 ? SystemStatus.Warning : SystemStatus.Healthy,
                Icon = "fas fa-memory"
            });

            // Storage Usage (using real system monitoring)
            var storageInfo = await _systemMonitoring.GetStorageUsageAsync();
            statusItems.Add(new SystemStatusItem
            {
                Name = "Storage Usage",
                Description = $"{storageInfo.FormattedUsage} storage used",
                Status = storageInfo.UsagePercentage > 85 ? SystemStatus.Warning : SystemStatus.Healthy,
                Icon = "fas fa-hdd"
            });

            _logger.LogDebug("System status check completed with {Count} items", statusItems.Count);
            return statusItems;
        }

        /// <summary>
        /// Retrieves key health metrics for system monitoring.
        /// </summary>
        /// <returns>List of health metric items.</returns>
        public async Task<List<HealthMetricItem>> GetHealthMetricsAsync()
        {
            _logger.LogDebug("Retrieving health metrics");
            var metrics = new List<HealthMetricItem>();

            // Application Uptime (using real monitoring)
            var uptimeInfo = await _systemMonitoring.GetUptimeAsync();
            metrics.Add(new HealthMetricItem
            {
                Name = "Uptime",
                Description = $"System running for {uptimeInfo.FormattedUptime}",
                Value = uptimeInfo.UptimePercentage,
                Status = uptimeInfo.Uptime.TotalHours < 1 ? SystemStatus.Warning : SystemStatus.Healthy
            });

            // Average Response Time (placeholder)
            metrics.Add(new HealthMetricItem
            {
                Name = "Response Time",
                Description = "Average server response",
                Value = "120ms", // TODO: Implement actual response time tracking
                Status = SystemStatus.Healthy
            });

            // Active Sessions
            var activeSessions = await GetActiveSessionsCountAsync();
            metrics.Add(new HealthMetricItem
            {
                Name = "Active Sessions",
                Description = "Currently logged in users",
                Value = activeSessions.ToString(),
                Status = SystemStatus.Healthy
            });

            // Storage Usage (using real monitoring)
            var storageUsage = await _systemMonitoring.GetStorageUsageAsync();
            metrics.Add(new HealthMetricItem
            {
                Name = "Storage",
                Description = $"Database and file storage usage",
                Value = storageUsage.FormattedUsage,
                Status = storageUsage.UsagePercentage > 85 ? SystemStatus.Error : 
                        storageUsage.UsagePercentage > 75 ? SystemStatus.Warning : SystemStatus.Healthy
            });

            // CPU Usage (using real monitoring)
            var cpuUsage = await _systemMonitoring.GetCpuUsageAsync();
            metrics.Add(new HealthMetricItem
            {
                Name = "CPU Usage",
                Description = "Current processor utilization",
                Value = $"{cpuUsage:F1}%",
                Status = cpuUsage > 80 ? SystemStatus.Warning : SystemStatus.Healthy
            });

            // Database Size (using real monitoring)
            var databaseSize = await _systemMonitoring.GetDatabaseSizeAsync();
            metrics.Add(new HealthMetricItem
            {
                Name = "Database Size",
                Description = "Current database file size",
                Value = databaseSize.FormattedSize,
                Status = databaseSize.SizeBytes > 1_000_000_000 ? SystemStatus.Warning : SystemStatus.Healthy // Warn if > 1GB
            });

            _logger.LogDebug("Retrieved {Count} health metrics", metrics.Count);
            return metrics;
        }

        /// <summary>
        /// Checks the database connection and returns health status.
        /// </summary>
        /// <returns>Database health status and message.</returns>
        private async Task<(bool IsHealthy, string Message)> CheckDatabaseStatusAsync()
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SELECT 1");
                return (true, "Connection successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Checks for recent security events and returns security status.
        /// </summary>
        /// <returns>Security status with failed login count.</returns>
        private async Task<(int FailedLoginCount, DateTime LastCheck)> CheckSecurityStatusAsync()
        {
            try
            {
                var last24Hours = DateTime.UtcNow.AddHours(-24);
                var failedLogins = await _context.UserActivities
                    .Where(a => a.Timestamp >= last24Hours && 
                           (a.Action.Contains("Failed") || a.Action.Contains("Invalid")))
                    .CountAsync();

                return (failedLogins, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Security status check failed");
                return (0, DateTime.UtcNow);
            }
        }


        /// <summary>
        /// Counts the number of users with recent activity to determine active sessions.
        /// </summary>
        /// <returns>Number of active sessions.</returns>
        private async Task<int> GetActiveSessionsCountAsync()
        {
            try
            {
                // Count unique users with activity in the last 30 minutes
                var thirtyMinutesAgo = DateTime.UtcNow.AddMinutes(-30);
                var activeSessions = await _context.UserActivities
                    .Where(a => a.Timestamp >= thirtyMinutesAgo)
                    .Select(a => a.UserId)
                    .Distinct()
                    .CountAsync();

                return activeSessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Active sessions count failed");
                return 0;
            }
        }
    }
}