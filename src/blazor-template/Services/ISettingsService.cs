using BlazorTemplate.Models.Settings;

namespace BlazorTemplate.Services
{
    /// <summary>
    /// Service interface for managing application settings
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Gets all application settings
        /// </summary>
        /// <returns>Complete application settings</returns>
        Task<ApplicationSettings> GetSettingsAsync();

        /// <summary>
        /// Saves all application settings
        /// </summary>
        /// <param name="settings">Settings to save</param>
        /// <param name="userId">ID of user making the changes</param>
        /// <returns>True if successful</returns>
        Task<bool> SaveSettingsAsync(ApplicationSettings settings, string? userId = null);

        /// <summary>
        /// Resets all settings to default values
        /// </summary>
        /// <param name="userId">ID of user performing the reset</param>
        /// <returns>True if successful</returns>
        Task<bool> ResetSettingsAsync(string? userId = null);

        /// <summary>
        /// Tests email settings by attempting to send a test email
        /// </summary>
        /// <param name="emailSettings">Email settings to test</param>
        /// <param name="testRecipient">Email address to send test to</param>
        /// <returns>True if test email was sent successfully</returns>
        Task<bool> TestEmailSettingsAsync(EmailSettings emailSettings, string testRecipient);

        /// <summary>
        /// Gets a specific setting value by key
        /// </summary>
        /// <param name="key">Setting key</param>
        /// <returns>Setting value or null if not found</returns>
        Task<string?> GetSettingAsync(string key);

        /// <summary>
        /// Sets a specific setting value
        /// </summary>
        /// <param name="key">Setting key</param>
        /// <param name="value">Setting value</param>
        /// <param name="userId">ID of user making the change</param>
        /// <returns>True if successful</returns>
        Task<bool> SetSettingAsync(string key, string? value, string? userId = null);

        /// <summary>
        /// Gets system health metrics
        /// </summary>
        /// <returns>Current system health status</returns>
        Task<SystemHealth> GetSystemHealthAsync();

        /// <summary>
        /// Gets recent settings audit log entries
        /// </summary>
        /// <param name="count">Number of entries to retrieve</param>
        /// <returns>Recent audit log entries</returns>
        Task<List<SettingsAuditEntry>> GetRecentAuditEntriesAsync(int count = 50);

        /// <summary>
        /// Validates settings before saving
        /// </summary>
        /// <param name="settings">Settings to validate</param>
        /// <returns>Validation results</returns>
        Task<SettingsValidationResult> ValidateSettingsAsync(ApplicationSettings settings);
    }

    /// <summary>
    /// System health status information
    /// </summary>
    public class SystemHealth
    {
        public string Status { get; set; } = "Healthy";
        public Dictionary<string, string> Metrics { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Settings audit log entry
    /// </summary>
    public class SettingsAuditEntry
    {
        public long Id { get; set; }
        public required string SettingsKey { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? UserId { get; set; }
        public string? UserEmail { get; set; }
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = "Updated";
    }

    /// <summary>
    /// Settings validation result
    /// </summary>
    public class SettingsValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}