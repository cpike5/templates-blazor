using Microsoft.EntityFrameworkCore;

namespace BlazorTemplate.Services.Monitoring
{
    /// <summary>
    /// Background service that periodically collects and stores system health metrics.
    /// Runs every 5 minutes to record CPU, memory, storage, and uptime metrics for historical tracking.
    /// </summary>
    public class SystemHealthBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SystemHealthBackgroundService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5); // Record metrics every 5 minutes

        /// <summary>
        /// Initializes a new instance of the SystemHealthBackgroundService.
        /// </summary>
        /// <param name="scopeFactory">Service scope factory for creating scoped services.</param>
        /// <param name="logger">Logger instance for background service logging.</param>
        public SystemHealthBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<SystemHealthBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// Executes the background service, collecting system metrics at regular intervals.
        /// </summary>
        /// <param name="stoppingToken">Token to signal service shutdown.</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("System Health Background Service started - collecting metrics every {Interval} minutes", _interval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CollectSystemMetricsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while collecting system metrics");
                }

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
            }

            _logger.LogInformation("System Health Background Service stopped");
        }

        /// <summary>
        /// Collects and stores current system metrics using the SystemMonitoringService.
        /// </summary>
        private async Task CollectSystemMetricsAsync()
        {
            try
            {
                _logger.LogTrace("Collecting system health metrics");

                // Collect all metrics in parallel with separate scopes for each task
                // to avoid DbContext concurrency issues
                var tasks = new[]
                {
                    Task.Run(async () =>
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var systemMonitoring = scope.ServiceProvider.GetRequiredService<ISystemMonitoringService>();
                        var memory = await systemMonitoring.GetMemoryUsageAsync();
                        await systemMonitoring.RecordHealthMetricAsync("Memory", memory.UsagePercentage.ToString("F1"));
                    }),
                    Task.Run(async () =>
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var systemMonitoring = scope.ServiceProvider.GetRequiredService<ISystemMonitoringService>();
                        var storage = await systemMonitoring.GetStorageUsageAsync();
                        await systemMonitoring.RecordHealthMetricAsync("Storage", storage.UsagePercentage.ToString("F1"));
                    }),
                    Task.Run(async () =>
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var systemMonitoring = scope.ServiceProvider.GetRequiredService<ISystemMonitoringService>();
                        var cpu = await systemMonitoring.GetCpuUsageAsync();
                        await systemMonitoring.RecordHealthMetricAsync("CPU", cpu.ToString("F1"));
                    }),
                    Task.Run(async () =>
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var systemMonitoring = scope.ServiceProvider.GetRequiredService<ISystemMonitoringService>();
                        var uptime = await systemMonitoring.GetUptimeAsync();
                        await systemMonitoring.RecordHealthMetricAsync("Uptime", uptime.Uptime.TotalHours.ToString("F1"));
                    }),
                    Task.Run(async () =>
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var systemMonitoring = scope.ServiceProvider.GetRequiredService<ISystemMonitoringService>();
                        var database = await systemMonitoring.GetDatabaseSizeAsync();
                        await systemMonitoring.RecordHealthMetricAsync("DatabaseSize", database.SizeBytes.ToString());
                    })
                };

                await Task.WhenAll(tasks);

                _logger.LogTrace("System health metrics collected and stored successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect system metrics");
                throw;
            }
        }

        /// <summary>
        /// Cleanup old metrics to prevent database growth.
        /// Called when the service is stopping.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("System Health Background Service is stopping, cleaning up old metrics");

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

                // Keep only the last 7 days of metrics
                var cutoffDate = DateTime.UtcNow.AddDays(-7);
                var oldMetrics = context.SystemHealthMetrics.Where(m => m.Timestamp < cutoffDate);
                
                var deleteCount = await oldMetrics.CountAsync();
                if (deleteCount > 0)
                {
                    context.SystemHealthMetrics.RemoveRange(oldMetrics);
                    await context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Cleaned up {Count} old system health metrics", deleteCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup old system health metrics during shutdown");
            }

            await base.StopAsync(cancellationToken);
        }
    }
}