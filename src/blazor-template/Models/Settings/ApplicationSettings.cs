using System.ComponentModel.DataAnnotations;

namespace BlazorTemplate.Models.Settings
{
    /// <summary>
    /// Root settings model containing all system settings
    /// </summary>
    public class ApplicationSettings
    {
        public GeneralSettings General { get; set; } = new();
        public SecuritySettings Security { get; set; } = new();
        public AppearanceSettings Appearance { get; set; } = new();
        public EmailSettings Email { get; set; } = new();
        public BackupSettings Backup { get; set; } = new();
        public IntegrationSettings Integrations { get; set; } = new();
    }

    /// <summary>
    /// General application settings
    /// </summary>
    public class GeneralSettings
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string SiteTitle { get; set; } = "Blazor Template Admin";

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string AdminEmail { get; set; } = "admin@test.com";

        [Required]
        [StringLength(50)]
        public string DefaultTimeZone { get; set; } = "America/New_York";

        [Required]
        [StringLength(20)]
        public string DateFormat { get; set; } = "MM/DD/YYYY";

        public bool MaintenanceMode { get; set; } = false;
        public bool UserRegistration { get; set; } = true;
        public bool GuestAccess { get; set; } = false;
    }

    /// <summary>
    /// Security-related settings
    /// </summary>
    public class SecuritySettings
    {
        [Range(6, 128)]
        public int PasswordMinLength { get; set; } = 8;

        [Range(5, 1440)]
        public int SessionTimeoutMinutes { get; set; } = 30;

        public bool RequireEmailConfirmation { get; set; } = true;
        public bool RequireTwoFactor { get; set; } = false;
        public bool AccountLockout { get; set; } = true;
        public bool PasswordComplexity { get; set; } = true;

        [Range(1, 10)]
        public int MaxLoginAttempts { get; set; } = 5;

        [Range(1, 1440)]
        public int LockoutTimeoutMinutes { get; set; } = 30;
    }

    /// <summary>
    /// Appearance and theme settings
    /// </summary>
    public class AppearanceSettings
    {
        [Required]
        [StringLength(20)]
        public string ColorScheme { get; set; } = "primary";

        public bool DarkMode { get; set; } = false;
        public bool CompactLayout { get; set; } = false;

        [StringLength(50)]
        public string LogoPath { get; set; } = string.Empty;

        [StringLength(50)]
        public string FaviconPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Email configuration settings
    /// </summary>
    public class EmailSettings
    {
        [Required]
        [StringLength(100)]
        public string SmtpServer { get; set; } = "smtp.gmail.com";

        [Range(1, 65535)]
        public int SmtpPort { get; set; } = 587;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [StringLength(500)]
        public string Password { get; set; } = string.Empty;

        public bool UseSsl { get; set; } = true;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string FromAddress { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FromName { get; set; } = string.Empty;

        public bool EnableWelcomeEmail { get; set; } = true;
        public bool EnablePasswordResetEmail { get; set; } = true;
        public bool EnableEmailVerification { get; set; } = true;
    }

    /// <summary>
    /// Backup and recovery settings
    /// </summary>
    public class BackupSettings
    {
        public bool AutoBackupEnabled { get; set; } = true;

        [Required]
        [StringLength(10)]
        public string BackupTime { get; set; } = "02:00";

        [Range(1, 365)]
        public int RetentionDays { get; set; } = 30;

        public List<string> BackupDays { get; set; } = new() { "Monday", "Wednesday", "Friday" };

        [StringLength(500)]
        public string BackupPath { get; set; } = string.Empty;

        public bool BackupUserData { get; set; } = true;
        public bool BackupSystemSettings { get; set; } = true;
        public bool BackupLogs { get; set; } = false;
    }

    /// <summary>
    /// Third-party integration settings
    /// </summary>
    public class IntegrationSettings
    {
        public bool GoogleOAuthEnabled { get; set; } = false;
        public bool MicrosoftOAuthEnabled { get; set; } = false;
        public bool AnalyticsEnabled { get; set; } = false;

        [StringLength(200)]
        public string GoogleClientId { get; set; } = string.Empty;

        [StringLength(500)]
        public string GoogleClientSecret { get; set; } = string.Empty;

        [StringLength(200)]
        public string MicrosoftClientId { get; set; } = string.Empty;

        [StringLength(500)]
        public string MicrosoftClientSecret { get; set; } = string.Empty;

        [StringLength(100)]
        public string AnalyticsId { get; set; } = string.Empty;

        public bool EnableApiAccess { get; set; } = false;
        public bool EnableRateLimiting { get; set; } = true;

        [Range(1, 10000)]
        public int ApiRequestsPerMinute { get; set; } = 100;
    }
}