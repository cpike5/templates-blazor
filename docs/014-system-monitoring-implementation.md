# System Monitoring Implementation

This document describes the implementation of real system monitoring for uptime checks and storage usage in the Dashboard.

## Overview

The system monitoring implementation replaces mock data with real system metrics collection for:

1. **Uptime Monitoring** - Tracks application startup and calculates uptime percentage
2. **Storage Usage Monitoring** - Real disk space monitoring for database and application storage  
3. **Enhanced System Metrics** - Memory usage, CPU usage, and database size monitoring
4. **Historical Tracking** - Stores metrics in the database for trending analysis

## Architecture

### Core Services

#### `SystemMonitoringService`
- **Location**: `Services/SystemMonitoringService.cs`
- **Interface**: `Services/ISystemMonitoringService.cs`
- **Purpose**: Provides real-time system monitoring capabilities

**Key Methods**:
- `GetMemoryUsageAsync()` - Cross-platform memory usage monitoring
- `GetStorageUsageAsync()` - Disk space monitoring with database path detection
- `GetCpuUsageAsync()` - CPU usage monitoring (Windows PerformanceCounter + cross-platform fallback)
- `GetUptimeAsync()` - Application uptime calculation with percentage tracking
- `GetDatabaseSizeAsync()` - Database size monitoring (SQL Server specific + file-based fallback)
- `RecordHealthMetricAsync()` - Stores metrics to SystemHealthMetric table
- `GetHistoricalMetricsAsync()` - Retrieves historical metric data

#### `SystemHealthBackgroundService`
- **Location**: `Services/SystemHealthBackgroundService.cs`
- **Purpose**: Periodically collects system metrics for historical tracking

**Features**:
- Runs every 5 minutes to collect metrics
- Stores CPU, memory, storage, uptime, and database size metrics
- Automatic cleanup of metrics older than 7 days
- Parallel metric collection for efficiency

### Updated Services

#### `DashboardService`
- **Updated**: To use real system monitoring instead of mock data
- **Changes**: 
  - Integrated with `ISystemMonitoringService`
  - Real uptime calculation using application startup time
  - Real storage usage monitoring with disk space detection
  - Real memory and CPU usage monitoring
  - Real database size monitoring

## Database Schema

### SystemHealthMetric Entity
```csharp
public class SystemHealthMetric
{
    public long Id { get; set; }
    public required string MetricType { get; set; }  // "Memory", "Storage", "CPU", etc.
    public string? MetricValue { get; set; }         // Metric value as string
    public DateTime Timestamp { get; set; }          // When metric was recorded
}
```

**Indexes**:
- `Timestamp` - For time-based queries
- `MetricType` - For filtering by metric type
- `(MetricType, Timestamp)` - Composite index for efficient historical queries

## Cross-Platform Compatibility

### Windows Support
- **CPU Monitoring**: Uses `PerformanceCounter` for accurate CPU usage
- **Memory Monitoring**: System memory via performance counters with process fallback
- **Storage Monitoring**: `DriveInfo` for disk space information
- **Database Size**: SQL Server specific queries when available

### Linux/macOS Support  
- **CPU Monitoring**: Process-based CPU time calculation
- **Memory Monitoring**: `/proc/meminfo` parsing on Linux, estimation fallback
- **Storage Monitoring**: `DriveInfo` cross-platform support
- **Database Size**: File-based size calculation for SQLite/file databases

## Configuration

### Service Registration
```csharp
// In ServiceCollectionExtensions.cs
services.AddScoped<ISystemMonitoringService, SystemMonitoringService>();

// In Program.cs  
builder.Services.AddHostedService<SystemHealthBackgroundService>();
```

### Background Service Settings
- **Collection Interval**: 5 minutes
- **Data Retention**: 7 days
- **Metric Types**: Memory, Storage, CPU, Uptime, DatabaseSize

## Real Metrics Implementation

### Uptime Monitoring
- **Before**: Used `Process.GetCurrentProcess().StartTime` (process uptime only)
- **Now**: Uses static application startup timestamp for accurate application uptime
- **Calculation**: Tracks uptime percentage assuming 99.9% expected availability

### Storage Monitoring
- **Before**: Hardcoded 45% storage usage
- **Now**: Real disk space monitoring with:
  - Database path extraction from connection string
  - Drive space calculation (total vs available)
  - Database file size monitoring
  - Cross-platform drive detection

### Memory Monitoring
- **Enhanced**: Cross-platform memory usage with system memory detection
- **Windows**: Performance counters for accurate system memory
- **Linux**: `/proc/meminfo` parsing for system memory information
- **Fallback**: Process memory usage with estimation

### CPU Monitoring
- **Windows**: Performance counter for real-time CPU usage
- **Cross-platform**: Process CPU time sampling over 1-second intervals
- **Accuracy**: Real CPU utilization percentage calculation

### Database Size Monitoring
- **SQL Server**: Direct database queries using `sys.database_files`
- **File-based**: File size monitoring for LocalDB/SQLite
- **Fallback**: Record count estimation for size approximation

## Usage Examples

### Getting Real-Time Metrics
```csharp
public async Task<SystemStatus> CheckSystemHealth()
{
    var memory = await _systemMonitoring.GetMemoryUsageAsync();
    var storage = await _systemMonitoring.GetStorageUsageAsync();
    var cpu = await _systemMonitoring.GetCpuUsageAsync();
    var uptime = await _systemMonitoring.GetUptimeAsync();
    var dbSize = await _systemMonitoring.GetDatabaseSizeAsync();
    
    return new SystemStatus
    {
        MemoryUsage = memory.UsagePercentage,
        StorageUsage = storage.UsagePercentage,
        CpuUsage = cpu,
        Uptime = uptime.FormattedUptime,
        DatabaseSize = dbSize.FormattedSize
    };
}
```

### Historical Data Analysis
```csharp
public async Task<List<MetricTrend>> GetMemoryTrend()
{
    var historicalData = await _systemMonitoring.GetHistoricalMetricsAsync("Memory", 24);
    return historicalData.Select(h => new MetricTrend 
    { 
        Timestamp = h.Timestamp, 
        Value = double.Parse(h.Value) 
    }).ToList();
}
```

## Monitoring and Alerting

### Dashboard Integration
- **System Status**: Real-time health indicators with proper status levels
- **Health Metrics**: Live system metrics with historical context
- **Alert Thresholds**:
  - Memory > 80%: Warning status
  - Storage > 75%: Warning, > 85%: Error
  - CPU > 80%: Warning status
  - Database > 1GB: Warning status

### Background Monitoring
- **Automatic Collection**: Metrics collected every 5 minutes
- **Data Retention**: 7-day sliding window with automatic cleanup
- **Error Handling**: Graceful degradation with logging for monitoring failures
- **Performance**: Parallel metric collection to minimize impact

## Benefits

1. **Accurate Monitoring**: Real system metrics replace mock data
2. **Historical Tracking**: Database storage enables trend analysis
3. **Cross-Platform**: Works on Windows, Linux, and macOS
4. **Performance**: Efficient background collection with minimal overhead
5. **Maintenance**: Automatic cleanup prevents database growth
6. **Observability**: Better system insights for operations and debugging

## Future Enhancements

1. **Response Time Tracking**: Implement actual HTTP response time monitoring
2. **Alerting System**: Add configurable thresholds with notifications  
3. **Metric Aggregation**: Hourly/daily rollups for long-term storage
4. **Performance Optimization**: Caching for frequently accessed metrics
5. **Network Monitoring**: Network I/O and connectivity monitoring
6. **External Dependencies**: Health checks for external services