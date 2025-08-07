using BlazorTemplate.Data;
using BlazorTemplate.Models.Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BlazorTemplate.Services.Monitoring
{
    /// <summary>
    /// Service implementation for system monitoring and health metrics collection.
    /// Provides real-time system monitoring capabilities including memory, storage, CPU, and uptime tracking.
    /// </summary>
    public class SystemMonitoringService : ISystemMonitoringService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<SystemMonitoringService> _logger;
        private readonly IConfiguration _configuration;
        private static readonly DateTime _applicationStartTime = DateTime.UtcNow;
        private static readonly object _cpuCounterLock = new object();
        private static PerformanceCounter? _cpuCounter;

        /// <summary>
        /// Initializes a new instance of the SystemMonitoringService.
        /// </summary>
        /// <param name="contextFactory">The database context factory for creating context instances.</param>
        /// <param name="logger">Logger instance for error logging.</param>
        /// <param name="configuration">Application configuration.</param>
        public SystemMonitoringService(
            IDbContextFactory<ApplicationDbContext> contextFactory, 
            ILogger<SystemMonitoringService> logger,
            IConfiguration configuration)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _configuration = configuration;
            
            // Initialize CPU counter on Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    lock (_cpuCounterLock)
                    {
                        _cpuCounter ??= new PerformanceCounter("Processor", "% Processor Time", "_Total");
                        // First call returns 0, so call it once to initialize
                        _cpuCounter.NextValue();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to initialize CPU performance counter. CPU monitoring will use alternative method.");
                }
            }
        }

        /// <summary>
        /// Gets the current memory usage information.
        /// </summary>
        /// <returns>Memory usage details including percentage and formatted display.</returns>
        public async Task<(double UsagePercentage, string FormattedUsage, long TotalMemory, long UsedMemory)> GetMemoryUsageAsync()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var usedMemory = process.WorkingSet64;
                
                // Get total system memory (cross-platform approach)
                var totalMemory = GC.GetTotalMemory(false);
                
                // For more accurate system memory on Windows
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try
                    {
                        var memoryCounters = new PerformanceCounterCategory("Memory");
                        var availableBytes = new PerformanceCounter("Memory", "Available Bytes");
                        var committedBytes = new PerformanceCounter("Memory", "Committed Bytes");
                        
                        var available = availableBytes.NextValue();
                        var committed = committedBytes.NextValue();
                        totalMemory = (long)(available + committed);
                        
                        availableBytes.Dispose();
                        committedBytes.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to get system memory info, using process memory");
                        // Fallback: estimate total memory based on process usage
                        totalMemory = Math.Max(totalMemory, usedMemory * 4); // Conservative estimate
                    }
                }
                else
                {
                    // On Linux/macOS, try to read /proc/meminfo or use estimation
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        try
                        {
                            var memInfo = await File.ReadAllTextAsync("/proc/meminfo");
                            var memTotalLine = memInfo.Split("\n").FirstOrDefault(line => line.StartsWith("MemTotal:"));
                            if (memTotalLine != null)
                            {
                                var parts = memTotalLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 2 && long.TryParse(parts[1], out var totalKb))
                                {
                                    totalMemory = totalKb * 1024; // Convert from KB to bytes
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Failed to read /proc/meminfo");
                        }
                    }
                    
                    // Fallback estimation if we couldn't get system memory
                    if (totalMemory < usedMemory)
                    {
                        totalMemory = usedMemory * 4; // Conservative estimate
                    }
                }

                var usagePercentage = totalMemory > 0 ? (double)usedMemory / totalMemory * 100 : 0;
                var formattedUsage = $"{usagePercentage:F1}%";

                // Record metric for historical tracking
                await RecordHealthMetricAsync("Memory", usagePercentage.ToString("F1"));

                return (usagePercentage, formattedUsage, totalMemory, usedMemory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting memory usage");
                return (0, "0%", 0, 0);
            }
        }

        /// <summary>
        /// Gets the current storage usage information for the system and database.
        /// </summary>
        /// <returns>Storage usage details including percentage and formatted display.</returns>
        public async Task<(double UsagePercentage, string FormattedUsage, long TotalSpace, long UsedSpace, long DatabaseSize)> GetStorageUsageAsync()
        {
            try
            {
                // Get database connection string to determine database location
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var databasePath = ExtractDatabasePath(connectionString);
                
                // Determine the drive/mount point to check
                var driveToCheck = GetDriveFromPath(databasePath);
                
                long totalSpace = 0;
                long availableSpace = 0;
                long databaseSize = 0;

                // Get drive information (cross-platform)
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var driveInfo = new DriveInfo(driveToCheck);
                    if (driveInfo.IsReady)
                    {
                        totalSpace = driveInfo.TotalSize;
                        availableSpace = driveInfo.AvailableFreeSpace;
                    }
                }
                else
                {
                    // For Linux/macOS, use statvfs equivalent
                    try
                    {
                        var drives = DriveInfo.GetDrives();
                        var targetDrive = drives.FirstOrDefault(d => driveToCheck.StartsWith(d.Name)) 
                                        ?? drives.FirstOrDefault(d => d.DriveType == DriveType.Fixed);
                        
                        if (targetDrive != null && targetDrive.IsReady)
                        {
                            totalSpace = targetDrive.TotalSize;
                            availableSpace = targetDrive.AvailableFreeSpace;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to get drive info on non-Windows platform");
                    }
                }

                // Get database size
                databaseSize = await GetDatabaseSizeBytesAsync();

                var usedSpace = totalSpace - availableSpace;
                var usagePercentage = totalSpace > 0 ? (double)usedSpace / totalSpace * 100 : 0;
                var formattedUsage = $"{usagePercentage:F1}%";

                // Record metric for historical tracking
                await RecordHealthMetricAsync("Storage", usagePercentage.ToString("F1"));

                return (usagePercentage, formattedUsage, totalSpace, usedSpace, databaseSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting storage usage");
                return (0, "0%", 0, 0, 0);
            }
        }

        /// <summary>
        /// Gets the current CPU usage percentage.
        /// </summary>
        /// <returns>CPU usage percentage.</returns>
        public async Task<double> GetCpuUsageAsync()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && _cpuCounter != null)
                {
                    // Windows: Use PerformanceCounter
                    lock (_cpuCounterLock)
                    {
                        var cpuUsage = _cpuCounter.NextValue();
                        // Record metric for historical tracking
                        _ = Task.Run(async () => await RecordHealthMetricAsync("CPU", cpuUsage.ToString("F1")));
                        return cpuUsage;
                    }
                }
                else
                {
                    // Cross-platform alternative: Use Process CPU time
                    var startTime = DateTime.UtcNow;
                    var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
                    
                    await Task.Delay(1000); // Wait 1 second for measurement
                    
                    var endTime = DateTime.UtcNow;
                    var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
                    
                    var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                    var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                    var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed) * 100;
                    
                    // Record metric for historical tracking
                    await RecordHealthMetricAsync("CPU", cpuUsageTotal.ToString("F1"));
                    
                    return Math.Min(100, Math.Max(0, cpuUsageTotal));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting CPU usage");
                return 0;
            }
        }

        /// <summary>
        /// Gets the application startup time for uptime calculations.
        /// </summary>
        /// <returns>Application startup timestamp.</returns>
        public DateTime GetApplicationStartTime()
        {
            return _applicationStartTime;
        }

        /// <summary>
        /// Gets the uptime percentage based on expected vs actual runtime.
        /// </summary>
        /// <returns>Uptime percentage and formatted uptime string.</returns>
        public async Task<(string UptimePercentage, string FormattedUptime, TimeSpan Uptime)> GetUptimeAsync()
        {
            try
            {
                var uptime = DateTime.UtcNow - _applicationStartTime;
                
                // Calculate uptime percentage (assuming we expect 99.9% uptime)
                // This could be enhanced to track actual downtime events
                var totalHours = uptime.TotalHours;
                var expectedDowntime = totalHours * 0.001; // 0.1% expected downtime
                var actualDowntime = 0; // TODO: Track actual downtime events
                
                var uptimePercentage = totalHours > 0 
                    ? Math.Min(100, Math.Max(0, (totalHours - actualDowntime) / totalHours * 100))
                    : 100;

                var formattedUptime = uptime.Days > 0 
                    ? $"{uptime.Days} days, {uptime.Hours} hours, {uptime.Minutes} minutes"
                    : uptime.Hours > 0 
                        ? $"{uptime.Hours} hours, {uptime.Minutes} minutes"
                        : $"{uptime.Minutes} minutes";

                var formattedPercentage = uptimePercentage >= 99.9 ? "99.9%" : $"{uptimePercentage:F1}%";

                // Record metric for historical tracking
                await RecordHealthMetricAsync("Uptime", uptimePercentage.ToString("F2"));

                return (formattedPercentage, formattedUptime, uptime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating uptime");
                return ("0%", "0 minutes", TimeSpan.Zero);
            }
        }

        /// <summary>
        /// Gets the current database size in bytes.
        /// </summary>
        /// <returns>Database size in bytes and formatted display.</returns>
        public async Task<(long SizeBytes, string FormattedSize)> GetDatabaseSizeAsync()
        {
            try
            {
                var sizeBytes = await GetDatabaseSizeBytesAsync();
                var formattedSize = FormatBytes(sizeBytes);
                return (sizeBytes, formattedSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting database size");
                return (0, "0 B");
            }
        }

        /// <summary>
        /// Records a system health metric to the database for historical tracking.
        /// </summary>
        /// <param name="metricType">The type of metric (e.g., "Memory", "Storage", "CPU").</param>
        /// <param name="metricValue">The metric value as a string.</param>
        public async Task RecordHealthMetricAsync(string metricType, string metricValue)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                
                var metric = new SystemHealthMetric
                {
                    MetricType = metricType,
                    MetricValue = metricValue,
                    Timestamp = DateTime.UtcNow
                };

                context.SystemHealthMetrics.Add(metric);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording health metric: {MetricType} = {MetricValue}", metricType, metricValue);
            }
        }

        /// <summary>
        /// Gets historical health metrics from the database.
        /// </summary>
        /// <param name="metricType">The type of metric to retrieve.</param>
        /// <param name="hoursBack">Number of hours back to retrieve data.</param>
        /// <returns>List of historical metric values.</returns>
        public async Task<List<(DateTime Timestamp, string Value)>> GetHistoricalMetricsAsync(string metricType, int hoursBack = 24)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                
                var cutoffTime = DateTime.UtcNow.AddHours(-hoursBack);
                
                var metrics = await context.SystemHealthMetrics
                    .Where(m => m.MetricType == metricType && m.Timestamp >= cutoffTime)
                    .OrderBy(m => m.Timestamp)
                    .Select(m => new { m.Timestamp, m.MetricValue })
                    .ToListAsync();

                return metrics.Select(m => (m.Timestamp, m.MetricValue ?? "0")).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving historical metrics for {MetricType}", metricType);
                return new List<(DateTime, string)>();
            }
        }

        /// <summary>
        /// Gets the database size in bytes using SQL Server specific queries or file size estimation.
        /// </summary>
        /// <returns>Database size in bytes.</returns>
        private async Task<long> GetDatabaseSizeBytesAsync()
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                
                // Try SQL Server specific approach first
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (connectionString?.Contains("Server=", StringComparison.OrdinalIgnoreCase) == true)
                {
                    try
                    {
                        var result = await context.Database.ExecuteSqlRawAsync(
                            "SELECT SUM(CAST(FILEPROPERTY(name, 'SpaceUsed') AS bigint) * 8192) FROM sys.database_files");
                        // Note: ExecuteSqlRawAsync doesn't return scalar values, so we'll use a different approach
                        
                        // Alternative approach using DbConnection
                        using var command = context.Database.GetDbConnection().CreateCommand();
                        command.CommandText = "SELECT SUM(CAST(FILEPROPERTY(name, 'SpaceUsed') AS bigint) * 8192) FROM sys.database_files";
                        
                        await context.Database.OpenConnectionAsync();
                        var sizeResult = await command.ExecuteScalarAsync();
                        
                        if (sizeResult != null && sizeResult != DBNull.Value)
                        {
                            return Convert.ToInt64(sizeResult);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to get SQL Server database size, trying file-based approach");
                    }
                }

                // Fallback: Try to get database file size
                var dbPath = ExtractDatabasePath(connectionString);
                if (!string.IsNullOrEmpty(dbPath) && File.Exists(dbPath))
                {
                    var fileInfo = new FileInfo(dbPath);
                    return fileInfo.Length;
                }

                // Last resort: Use table count estimation
                var tableCount = await context.Users.CountAsync() + 
                               await context.UserActivities.CountAsync() +
                               await context.SystemHealthMetrics.CountAsync();
                
                // Rough estimation: 1KB per record
                return tableCount * 1024;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating database size");
                return 0;
            }
        }

        /// <summary>
        /// Extracts the database file path from a connection string.
        /// </summary>
        /// <param name="connectionString">Database connection string.</param>
        /// <returns>Database file path or empty string if not found.</returns>
        private string ExtractDatabasePath(string? connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return string.Empty;

            // Look for LocalDB or file-based database paths
            var attachDbFilenameIndex = connectionString.IndexOf("AttachDbFilename=", StringComparison.OrdinalIgnoreCase);
            if (attachDbFilenameIndex >= 0)
            {
                var start = attachDbFilenameIndex + "AttachDbFilename=".Length;
                var end = connectionString.IndexOf(";", start);
                if (end == -1) end = connectionString.Length;
                
                var path = connectionString[start..end].Trim('\'', '"');
                return Environment.ExpandEnvironmentVariables(path);
            }

            // For SQLite databases
            var dataSourceIndex = connectionString.IndexOf("Data Source=", StringComparison.OrdinalIgnoreCase);
            if (dataSourceIndex >= 0)
            {
                var start = dataSourceIndex + "Data Source=".Length;
                var end = connectionString.IndexOf(";", start);
                if (end == -1) end = connectionString.Length;
                
                var path = connectionString[start..end].Trim('\'', '"');
                return Environment.ExpandEnvironmentVariables(path);
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the drive letter or mount point from a file path.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <returns>Drive letter or root path.</returns>
        private string GetDriveFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C:\\" : "/";
            }

            try
            {
                var fullPath = Path.GetFullPath(path);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return Path.GetPathRoot(fullPath) ?? "C:\\";
                }
                else
                {
                    return Path.GetPathRoot(fullPath) ?? "/";
                }
            }
            catch
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C:\\" : "/";
            }
        }

        /// <summary>
        /// Formats bytes into human-readable format.
        /// </summary>
        /// <param name="bytes">Bytes to format.</param>
        /// <returns>Formatted string (e.g., "1.5 MB").</returns>
        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            if (bytes == 0) return "0 B";
            
            var order = 0;
            var size = (double)bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:F1} {sizes[order]}";
        }
    }
}