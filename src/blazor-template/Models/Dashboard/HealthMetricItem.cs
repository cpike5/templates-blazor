namespace BlazorTemplate.Models.Dashboard
{
    /// <summary>
    /// Represents a health metric item for monitoring system performance and health.
    /// </summary>
    public class HealthMetricItem
    {
        /// <summary>
        /// Name of the health metric being monitored.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Description providing context about the metric.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Current value of the metric (formatted as string for display).
        /// </summary>
        public string Value { get; set; } = string.Empty;
        
        /// <summary>
        /// Status indicating whether the metric value is within acceptable ranges.
        /// </summary>
        public SystemStatus Status { get; set; }
        
        /// <summary>
        /// Additional metadata about the health metric.
        /// </summary>
        public Dictionary<string, object>? AdditionalData { get; set; }
    }
}