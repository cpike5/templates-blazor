using System.ComponentModel.DataAnnotations;

namespace BlazorTemplate.Data
{
    /// <summary>
    /// Database entity for storing application settings as key-value pairs
    /// </summary>
    public class ApplicationSetting
    {
        [Key]
        [StringLength(255)]
        public required string Key { get; set; }

        public string? Value { get; set; }

        public bool IsEncrypted { get; set; } = false;

        [StringLength(100)]
        public string Category { get; set; } = "General";

        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? ModifiedBy { get; set; }

        // Navigation property
        public ApplicationUser? ModifiedByUser { get; set; }
    }

    /// <summary>
    /// Database entity for tracking system health metrics
    /// </summary>
    public class SystemHealthMetric
    {
        public long Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string MetricType { get; set; }

        public string? MetricValue { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Database entity for auditing settings changes
    /// </summary>
    public class SettingsAuditLog
    {
        public long Id { get; set; }

        [Required]
        [StringLength(255)]
        public required string SettingsKey { get; set; }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string Action { get; set; } = "Updated"; // Created, Updated, Deleted

        // Navigation property
        public ApplicationUser? User { get; set; }
    }
}