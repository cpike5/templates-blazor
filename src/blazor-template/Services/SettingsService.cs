using BlazorTemplate.Data;
using BlazorTemplate.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Reflection;
using System.Text.Json;

namespace BlazorTemplate.Services
{
    /// <summary>
    /// Service for managing application settings with database persistence, caching, and encryption
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISettingsEncryptionService _encryptionService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SettingsService> _logger;

        private const string CACHE_KEY = "ApplicationSettings";
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);

        public SettingsService(
            ApplicationDbContext context,
            ISettingsEncryptionService encryptionService,
            IMemoryCache cache,
            ILogger<SettingsService> logger)
        {
            _context = context;
            _encryptionService = encryptionService;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ApplicationSettings> GetSettingsAsync()
        {
            if (_cache.TryGetValue(CACHE_KEY, out ApplicationSettings? cachedSettings) && cachedSettings != null)
            {
                return cachedSettings;
            }

            var settings = new ApplicationSettings();
            var dbSettings = await _context.ApplicationSettings.ToListAsync();

            if (!dbSettings.Any())
            {
                // No settings in database, return defaults and seed them
                await SeedDefaultSettingsAsync(settings);
                _cache.Set(CACHE_KEY, settings, CacheExpiration);
                return settings;
            }

            // Map database values to settings object
            MapDatabaseToSettings(dbSettings, settings);

            _cache.Set(CACHE_KEY, settings, CacheExpiration);
            return settings;
        }

        public async Task<bool> SaveSettingsAsync(ApplicationSettings settings, string? userId = null)
        {
            try
            {
                // Validate settings first
                var validationResult = await ValidateSettingsAsync(settings);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Settings validation failed: {Errors}", string.Join(", ", validationResult.Errors));
                    return false;
                }

                // Convert settings object to key-value pairs
                var settingsDict = SettingsToKeyValuePairs(settings);
                var timestamp = DateTime.UtcNow;

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    foreach (var kvp in settingsDict)
                    {
                        var existingSetting = await _context.ApplicationSettings
                            .FirstOrDefaultAsync(s => s.Key == kvp.Key);

                        var shouldEncrypt = _encryptionService.ShouldEncrypt(kvp.Key);
                        var valueToStore = shouldEncrypt && !string.IsNullOrEmpty(kvp.Value) 
                            ? _encryptionService.Encrypt(kvp.Value) 
                            : kvp.Value;

                        if (existingSetting != null)
                        {
                            // Log change for audit
                            await LogSettingsChangeAsync(kvp.Key, existingSetting.Value, kvp.Value, userId, "Updated");

                            // Update existing setting
                            existingSetting.Value = valueToStore;
                            existingSetting.IsEncrypted = shouldEncrypt;
                            existingSetting.LastModified = timestamp;
                            existingSetting.ModifiedBy = userId;
                        }
                        else
                        {
                            // Create new setting
                            var newSetting = new ApplicationSetting
                            {
                                Key = kvp.Key,
                                Value = valueToStore,
                                IsEncrypted = shouldEncrypt,
                                Category = GetSettingCategory(kvp.Key),
                                LastModified = timestamp,
                                ModifiedBy = userId
                            };

                            _context.ApplicationSettings.Add(newSetting);
                            await LogSettingsChangeAsync(kvp.Key, null, kvp.Value, userId, "Created");
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Clear cache to force reload
                    _cache.Remove(CACHE_KEY);

                    _logger.LogInformation("Settings saved successfully by user {UserId}", userId ?? "System");
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Failed to save settings");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings");
                return false;
            }
        }

        public async Task<bool> ResetSettingsAsync(string? userId = null)
        {
            try
            {
                var defaultSettings = new ApplicationSettings();
                var result = await SaveSettingsAsync(defaultSettings, userId);
                
                if (result)
                {
                    _logger.LogInformation("Settings reset to defaults by user {UserId}", userId ?? "System");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting settings");
                return false;
            }
        }

        public async Task<bool> TestEmailSettingsAsync(EmailSettings emailSettings, string testRecipient)
        {
            try
            {
                using var client = new SmtpClient(emailSettings.SmtpServer, emailSettings.SmtpPort)
                {
                    EnableSsl = emailSettings.UseSsl,
                    Credentials = new System.Net.NetworkCredential(emailSettings.Username, emailSettings.Password)
                };

                var message = new MailMessage
                {
                    From = new MailAddress(emailSettings.FromAddress, emailSettings.FromName),
                    Subject = "Email Settings Test",
                    Body = "This is a test email to verify your email settings configuration.",
                    IsBodyHtml = false
                };

                message.To.Add(testRecipient);

                await client.SendMailAsync(message);
                _logger.LogInformation("Test email sent successfully to {Recipient}", testRecipient);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test email to {Recipient}", testRecipient);
                return false;
            }
        }

        public async Task<string?> GetSettingAsync(string key)
        {
            var setting = await _context.ApplicationSettings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting?.Value == null)
                return null;

            return setting.IsEncrypted 
                ? _encryptionService.Decrypt(setting.Value)
                : setting.Value;
        }

        public async Task<bool> SetSettingAsync(string key, string? value, string? userId = null)
        {
            try
            {
                var existingSetting = await _context.ApplicationSettings
                    .FirstOrDefaultAsync(s => s.Key == key);

                var shouldEncrypt = _encryptionService.ShouldEncrypt(key);
                var valueToStore = shouldEncrypt && !string.IsNullOrEmpty(value)
                    ? _encryptionService.Encrypt(value)
                    : value;

                if (existingSetting != null)
                {
                    var oldValue = existingSetting.IsEncrypted && !string.IsNullOrEmpty(existingSetting.Value)
                        ? _encryptionService.Decrypt(existingSetting.Value)
                        : existingSetting.Value;

                    await LogSettingsChangeAsync(key, oldValue, value, userId, "Updated");

                    existingSetting.Value = valueToStore;
                    existingSetting.IsEncrypted = shouldEncrypt;
                    existingSetting.LastModified = DateTime.UtcNow;
                    existingSetting.ModifiedBy = userId;
                }
                else
                {
                    var newSetting = new ApplicationSetting
                    {
                        Key = key,
                        Value = valueToStore,
                        IsEncrypted = shouldEncrypt,
                        Category = GetSettingCategory(key),
                        LastModified = DateTime.UtcNow,
                        ModifiedBy = userId
                    };

                    _context.ApplicationSettings.Add(newSetting);
                    await LogSettingsChangeAsync(key, null, value, userId, "Created");
                }

                await _context.SaveChangesAsync();
                _cache.Remove(CACHE_KEY);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting value for key {Key}", key);
                return false;
            }
        }

        public async Task<SystemHealth> GetSystemHealthAsync()
        {
            var health = new SystemHealth();

            try
            {
                // Check database connectivity
                var canConnect = await _context.Database.CanConnectAsync();
                health.Metrics["Database"] = canConnect ? "Healthy" : "Unhealthy";

                // Check settings count
                var settingsCount = await _context.ApplicationSettings.CountAsync();
                health.Metrics["Settings Count"] = settingsCount.ToString();

                // Check recent activity
                var recentActivity = await _context.SettingsAuditLogs
                    .Where(s => s.Timestamp > DateTime.UtcNow.AddHours(-24))
                    .CountAsync();
                health.Metrics["Recent Changes (24h)"] = recentActivity.ToString();

                // Overall status
                health.Status = canConnect ? "Healthy" : "Degraded";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health");
                health.Status = "Unhealthy";
                health.Metrics["Error"] = ex.Message;
            }

            return health;
        }

        public async Task<List<SettingsAuditEntry>> GetRecentAuditEntriesAsync(int count = 50)
        {
            return await _context.SettingsAuditLogs
                .Include(s => s.User)
                .OrderByDescending(s => s.Timestamp)
                .Take(count)
                .Select(s => new SettingsAuditEntry
                {
                    Id = s.Id,
                    SettingsKey = s.SettingsKey,
                    OldValue = s.OldValue,
                    NewValue = s.NewValue,
                    UserId = s.UserId,
                    UserEmail = s.User != null ? s.User.Email : null,
                    Timestamp = s.Timestamp,
                    Action = s.Action
                })
                .ToListAsync();
        }

        public async Task<SettingsValidationResult> ValidateSettingsAsync(ApplicationSettings settings)
        {
            var result = new SettingsValidationResult();
            var context = new ValidationContext(settings);
            var validationResults = new List<ValidationResult>();

            // Validate the entire settings object
            if (!Validator.TryValidateObject(settings, context, validationResults, true))
            {
                result.IsValid = false;
                result.Errors.AddRange(validationResults.Select(v => v.ErrorMessage ?? "Unknown validation error"));
            }

            // Custom business rule validations
            await ValidateBusinessRulesAsync(settings, result);

            return result;
        }

        #region Private Methods

        private async Task SeedDefaultSettingsAsync(ApplicationSettings defaultSettings)
        {
            _logger.LogInformation("Seeding default settings to database");
            
            var settingsDict = SettingsToKeyValuePairs(defaultSettings);
            
            foreach (var kvp in settingsDict)
            {
                var shouldEncrypt = _encryptionService.ShouldEncrypt(kvp.Key);
                var valueToStore = shouldEncrypt && !string.IsNullOrEmpty(kvp.Value)
                    ? _encryptionService.Encrypt(kvp.Value)
                    : kvp.Value;

                var setting = new ApplicationSetting
                {
                    Key = kvp.Key,
                    Value = valueToStore,
                    IsEncrypted = shouldEncrypt,
                    Category = GetSettingCategory(kvp.Key),
                    LastModified = DateTime.UtcNow,
                    ModifiedBy = null
                };

                _context.ApplicationSettings.Add(setting);
            }

            await _context.SaveChangesAsync();
        }

        private void MapDatabaseToSettings(List<ApplicationSetting> dbSettings, ApplicationSettings settings)
        {
            var settingsDict = new Dictionary<string, string?>();

            foreach (var dbSetting in dbSettings)
            {
                var value = dbSetting.IsEncrypted && !string.IsNullOrEmpty(dbSetting.Value)
                    ? _encryptionService.Decrypt(dbSetting.Value)
                    : dbSetting.Value;
                
                settingsDict[dbSetting.Key] = value;
            }

            KeyValuePairsToSettings(settingsDict, settings);
        }

        private static Dictionary<string, string?> SettingsToKeyValuePairs(ApplicationSettings settings)
        {
            var result = new Dictionary<string, string?>();

            // General Settings
            result["General.SiteTitle"] = settings.General.SiteTitle;
            result["General.AdminEmail"] = settings.General.AdminEmail;
            result["General.DefaultTimeZone"] = settings.General.DefaultTimeZone;
            result["General.DateFormat"] = settings.General.DateFormat;
            result["General.MaintenanceMode"] = settings.General.MaintenanceMode.ToString();
            result["General.UserRegistration"] = settings.General.UserRegistration.ToString();
            result["General.GuestAccess"] = settings.General.GuestAccess.ToString();

            // Security Settings
            result["Security.PasswordMinLength"] = settings.Security.PasswordMinLength.ToString();
            result["Security.SessionTimeoutMinutes"] = settings.Security.SessionTimeoutMinutes.ToString();
            result["Security.RequireEmailConfirmation"] = settings.Security.RequireEmailConfirmation.ToString();
            result["Security.RequireTwoFactor"] = settings.Security.RequireTwoFactor.ToString();
            result["Security.AccountLockout"] = settings.Security.AccountLockout.ToString();
            result["Security.PasswordComplexity"] = settings.Security.PasswordComplexity.ToString();
            result["Security.MaxLoginAttempts"] = settings.Security.MaxLoginAttempts.ToString();
            result["Security.LockoutTimeoutMinutes"] = settings.Security.LockoutTimeoutMinutes.ToString();

            // Appearance Settings
            result["Appearance.ColorScheme"] = settings.Appearance.ColorScheme;
            result["Appearance.DarkMode"] = settings.Appearance.DarkMode.ToString();
            result["Appearance.CompactLayout"] = settings.Appearance.CompactLayout.ToString();
            result["Appearance.LogoPath"] = settings.Appearance.LogoPath;
            result["Appearance.FaviconPath"] = settings.Appearance.FaviconPath;

            // Email Settings
            result["Email.SmtpServer"] = settings.Email.SmtpServer;
            result["Email.SmtpPort"] = settings.Email.SmtpPort.ToString();
            result["Email.Username"] = settings.Email.Username;
            result["Email.Password"] = settings.Email.Password;
            result["Email.UseSsl"] = settings.Email.UseSsl.ToString();
            result["Email.FromAddress"] = settings.Email.FromAddress;
            result["Email.FromName"] = settings.Email.FromName;
            result["Email.EnableWelcomeEmail"] = settings.Email.EnableWelcomeEmail.ToString();
            result["Email.EnablePasswordResetEmail"] = settings.Email.EnablePasswordResetEmail.ToString();
            result["Email.EnableEmailVerification"] = settings.Email.EnableEmailVerification.ToString();

            // Backup Settings
            result["Backup.AutoBackupEnabled"] = settings.Backup.AutoBackupEnabled.ToString();
            result["Backup.BackupTime"] = settings.Backup.BackupTime;
            result["Backup.RetentionDays"] = settings.Backup.RetentionDays.ToString();
            result["Backup.BackupDays"] = JsonSerializer.Serialize(settings.Backup.BackupDays);
            result["Backup.BackupPath"] = settings.Backup.BackupPath;
            result["Backup.BackupUserData"] = settings.Backup.BackupUserData.ToString();
            result["Backup.BackupSystemSettings"] = settings.Backup.BackupSystemSettings.ToString();
            result["Backup.BackupLogs"] = settings.Backup.BackupLogs.ToString();

            // Integration Settings
            result["Integrations.GoogleOAuthEnabled"] = settings.Integrations.GoogleOAuthEnabled.ToString();
            result["Integrations.MicrosoftOAuthEnabled"] = settings.Integrations.MicrosoftOAuthEnabled.ToString();
            result["Integrations.AnalyticsEnabled"] = settings.Integrations.AnalyticsEnabled.ToString();
            result["Integrations.GoogleClientId"] = settings.Integrations.GoogleClientId;
            result["Integrations.GoogleClientSecret"] = settings.Integrations.GoogleClientSecret;
            result["Integrations.MicrosoftClientId"] = settings.Integrations.MicrosoftClientId;
            result["Integrations.MicrosoftClientSecret"] = settings.Integrations.MicrosoftClientSecret;
            result["Integrations.AnalyticsId"] = settings.Integrations.AnalyticsId;
            result["Integrations.EnableApiAccess"] = settings.Integrations.EnableApiAccess.ToString();
            result["Integrations.EnableRateLimiting"] = settings.Integrations.EnableRateLimiting.ToString();
            result["Integrations.ApiRequestsPerMinute"] = settings.Integrations.ApiRequestsPerMinute.ToString();

            return result;
        }

        private static void KeyValuePairsToSettings(Dictionary<string, string?> dict, ApplicationSettings settings)
        {
            // General Settings
            if (dict.TryGetValue("General.SiteTitle", out var siteTitle) && !string.IsNullOrEmpty(siteTitle))
                settings.General.SiteTitle = siteTitle;
            if (dict.TryGetValue("General.AdminEmail", out var adminEmail) && !string.IsNullOrEmpty(adminEmail))
                settings.General.AdminEmail = adminEmail;
            if (dict.TryGetValue("General.DefaultTimeZone", out var timeZone) && !string.IsNullOrEmpty(timeZone))
                settings.General.DefaultTimeZone = timeZone;
            if (dict.TryGetValue("General.DateFormat", out var dateFormat) && !string.IsNullOrEmpty(dateFormat))
                settings.General.DateFormat = dateFormat;
            if (dict.TryGetValue("General.MaintenanceMode", out var maintenance) && bool.TryParse(maintenance, out var maintenanceValue))
                settings.General.MaintenanceMode = maintenanceValue;
            if (dict.TryGetValue("General.UserRegistration", out var userReg) && bool.TryParse(userReg, out var userRegValue))
                settings.General.UserRegistration = userRegValue;
            if (dict.TryGetValue("General.GuestAccess", out var guestAccess) && bool.TryParse(guestAccess, out var guestAccessValue))
                settings.General.GuestAccess = guestAccessValue;

            // Security Settings
            if (dict.TryGetValue("Security.PasswordMinLength", out var pwdLength) && int.TryParse(pwdLength, out var pwdLengthValue))
                settings.Security.PasswordMinLength = pwdLengthValue;
            if (dict.TryGetValue("Security.SessionTimeoutMinutes", out var sessionTimeout) && int.TryParse(sessionTimeout, out var sessionTimeoutValue))
                settings.Security.SessionTimeoutMinutes = sessionTimeoutValue;
            if (dict.TryGetValue("Security.RequireEmailConfirmation", out var requireEmail) && bool.TryParse(requireEmail, out var requireEmailValue))
                settings.Security.RequireEmailConfirmation = requireEmailValue;
            if (dict.TryGetValue("Security.RequireTwoFactor", out var require2FA) && bool.TryParse(require2FA, out var require2FAValue))
                settings.Security.RequireTwoFactor = require2FAValue;
            if (dict.TryGetValue("Security.AccountLockout", out var lockout) && bool.TryParse(lockout, out var lockoutValue))
                settings.Security.AccountLockout = lockoutValue;
            if (dict.TryGetValue("Security.PasswordComplexity", out var complexity) && bool.TryParse(complexity, out var complexityValue))
                settings.Security.PasswordComplexity = complexityValue;
            if (dict.TryGetValue("Security.MaxLoginAttempts", out var maxAttempts) && int.TryParse(maxAttempts, out var maxAttemptsValue))
                settings.Security.MaxLoginAttempts = maxAttemptsValue;
            if (dict.TryGetValue("Security.LockoutTimeoutMinutes", out var lockoutTimeout) && int.TryParse(lockoutTimeout, out var lockoutTimeoutValue))
                settings.Security.LockoutTimeoutMinutes = lockoutTimeoutValue;

            // Appearance Settings
            if (dict.TryGetValue("Appearance.ColorScheme", out var colorScheme) && !string.IsNullOrEmpty(colorScheme))
                settings.Appearance.ColorScheme = colorScheme;
            if (dict.TryGetValue("Appearance.DarkMode", out var darkMode) && bool.TryParse(darkMode, out var darkModeValue))
                settings.Appearance.DarkMode = darkModeValue;
            if (dict.TryGetValue("Appearance.CompactLayout", out var compact) && bool.TryParse(compact, out var compactValue))
                settings.Appearance.CompactLayout = compactValue;
            if (dict.TryGetValue("Appearance.LogoPath", out var logoPath) && !string.IsNullOrEmpty(logoPath))
                settings.Appearance.LogoPath = logoPath;
            if (dict.TryGetValue("Appearance.FaviconPath", out var faviconPath) && !string.IsNullOrEmpty(faviconPath))
                settings.Appearance.FaviconPath = faviconPath;

            // Email Settings  
            if (dict.TryGetValue("Email.SmtpServer", out var smtpServer) && !string.IsNullOrEmpty(smtpServer))
                settings.Email.SmtpServer = smtpServer;
            if (dict.TryGetValue("Email.SmtpPort", out var smtpPort) && int.TryParse(smtpPort, out var smtpPortValue))
                settings.Email.SmtpPort = smtpPortValue;
            if (dict.TryGetValue("Email.Username", out var username) && !string.IsNullOrEmpty(username))
                settings.Email.Username = username;
            if (dict.TryGetValue("Email.Password", out var password) && !string.IsNullOrEmpty(password))
                settings.Email.Password = password;
            if (dict.TryGetValue("Email.UseSsl", out var useSsl) && bool.TryParse(useSsl, out var useSslValue))
                settings.Email.UseSsl = useSslValue;
            if (dict.TryGetValue("Email.FromAddress", out var fromAddress) && !string.IsNullOrEmpty(fromAddress))
                settings.Email.FromAddress = fromAddress;
            if (dict.TryGetValue("Email.FromName", out var fromName) && !string.IsNullOrEmpty(fromName))
                settings.Email.FromName = fromName;
            if (dict.TryGetValue("Email.EnableWelcomeEmail", out var enableWelcome) && bool.TryParse(enableWelcome, out var enableWelcomeValue))
                settings.Email.EnableWelcomeEmail = enableWelcomeValue;
            if (dict.TryGetValue("Email.EnablePasswordResetEmail", out var enableReset) && bool.TryParse(enableReset, out var enableResetValue))
                settings.Email.EnablePasswordResetEmail = enableResetValue;
            if (dict.TryGetValue("Email.EnableEmailVerification", out var enableVerification) && bool.TryParse(enableVerification, out var enableVerificationValue))
                settings.Email.EnableEmailVerification = enableVerificationValue;

            // Backup Settings
            if (dict.TryGetValue("Backup.AutoBackupEnabled", out var autoBackup) && bool.TryParse(autoBackup, out var autoBackupValue))
                settings.Backup.AutoBackupEnabled = autoBackupValue;
            if (dict.TryGetValue("Backup.BackupTime", out var backupTime) && !string.IsNullOrEmpty(backupTime))
                settings.Backup.BackupTime = backupTime;
            if (dict.TryGetValue("Backup.RetentionDays", out var retention) && int.TryParse(retention, out var retentionValue))
                settings.Backup.RetentionDays = retentionValue;
            if (dict.TryGetValue("Backup.BackupDays", out var backupDays) && !string.IsNullOrEmpty(backupDays))
            {
                try
                {
                    var days = JsonSerializer.Deserialize<List<string>>(backupDays);
                    if (days != null) settings.Backup.BackupDays = days;
                }
                catch { /* ignore invalid JSON */ }
            }
            if (dict.TryGetValue("Backup.BackupPath", out var backupPath) && !string.IsNullOrEmpty(backupPath))
                settings.Backup.BackupPath = backupPath;
            if (dict.TryGetValue("Backup.BackupUserData", out var backupUsers) && bool.TryParse(backupUsers, out var backupUsersValue))
                settings.Backup.BackupUserData = backupUsersValue;
            if (dict.TryGetValue("Backup.BackupSystemSettings", out var backupSettings) && bool.TryParse(backupSettings, out var backupSettingsValue))
                settings.Backup.BackupSystemSettings = backupSettingsValue;
            if (dict.TryGetValue("Backup.BackupLogs", out var backupLogs) && bool.TryParse(backupLogs, out var backupLogsValue))
                settings.Backup.BackupLogs = backupLogsValue;

            // Integration Settings
            if (dict.TryGetValue("Integrations.GoogleOAuthEnabled", out var googleOAuth) && bool.TryParse(googleOAuth, out var googleOAuthValue))
                settings.Integrations.GoogleOAuthEnabled = googleOAuthValue;
            if (dict.TryGetValue("Integrations.MicrosoftOAuthEnabled", out var msOAuth) && bool.TryParse(msOAuth, out var msOAuthValue))
                settings.Integrations.MicrosoftOAuthEnabled = msOAuthValue;
            if (dict.TryGetValue("Integrations.AnalyticsEnabled", out var analytics) && bool.TryParse(analytics, out var analyticsValue))
                settings.Integrations.AnalyticsEnabled = analyticsValue;
            if (dict.TryGetValue("Integrations.GoogleClientId", out var googleId) && !string.IsNullOrEmpty(googleId))
                settings.Integrations.GoogleClientId = googleId;
            if (dict.TryGetValue("Integrations.GoogleClientSecret", out var googleSecret) && !string.IsNullOrEmpty(googleSecret))
                settings.Integrations.GoogleClientSecret = googleSecret;
            if (dict.TryGetValue("Integrations.MicrosoftClientId", out var msId) && !string.IsNullOrEmpty(msId))
                settings.Integrations.MicrosoftClientId = msId;
            if (dict.TryGetValue("Integrations.MicrosoftClientSecret", out var msSecret) && !string.IsNullOrEmpty(msSecret))
                settings.Integrations.MicrosoftClientSecret = msSecret;
            if (dict.TryGetValue("Integrations.AnalyticsId", out var analyticsId) && !string.IsNullOrEmpty(analyticsId))
                settings.Integrations.AnalyticsId = analyticsId;
            if (dict.TryGetValue("Integrations.EnableApiAccess", out var apiAccess) && bool.TryParse(apiAccess, out var apiAccessValue))
                settings.Integrations.EnableApiAccess = apiAccessValue;
            if (dict.TryGetValue("Integrations.EnableRateLimiting", out var rateLimit) && bool.TryParse(rateLimit, out var rateLimitValue))
                settings.Integrations.EnableRateLimiting = rateLimitValue;
            if (dict.TryGetValue("Integrations.ApiRequestsPerMinute", out var apiRequests) && int.TryParse(apiRequests, out var apiRequestsValue))
                settings.Integrations.ApiRequestsPerMinute = apiRequestsValue;
        }

        private static string GetSettingCategory(string key)
        {
            return key.Split(".").FirstOrDefault() ?? "General";
        }

        private async Task LogSettingsChangeAsync(string key, string? oldValue, string? newValue, string? userId, string action)
        {
            try
            {
                var auditEntry = new SettingsAuditLog
                {
                    SettingsKey = key,
                    OldValue = oldValue,
                    NewValue = newValue,
                    UserId = userId,
                    Timestamp = DateTime.UtcNow,
                    Action = action
                };

                _context.SettingsAuditLogs.Add(auditEntry);
                // Note: SaveChanges will be called by the caller
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log settings change for key {Key}", key);
            }
        }

        private async Task ValidateBusinessRulesAsync(ApplicationSettings settings, SettingsValidationResult result)
        {
            // Email validation
            if (!string.IsNullOrEmpty(settings.Email.Username) && 
                !string.IsNullOrEmpty(settings.Email.Password) &&
                string.IsNullOrEmpty(settings.Email.FromAddress))
            {
                result.Warnings.Add("Email username and password are configured but no from address is set");
            }

            // Security validation
            if (settings.Security.AccountLockout && settings.Security.MaxLoginAttempts < 3)
            {
                result.Warnings.Add("Account lockout is enabled with very few login attempts allowed");
            }

            // Integration validation
            if (settings.Integrations.GoogleOAuthEnabled && 
                (string.IsNullOrEmpty(settings.Integrations.GoogleClientId) || string.IsNullOrEmpty(settings.Integrations.GoogleClientSecret)))
            {
                result.Errors.Add("Google OAuth is enabled but client ID or secret is missing");
                result.IsValid = false;
            }

            if (settings.Integrations.MicrosoftOAuthEnabled && 
                (string.IsNullOrEmpty(settings.Integrations.MicrosoftClientId) || string.IsNullOrEmpty(settings.Integrations.MicrosoftClientSecret)))
            {
                result.Errors.Add("Microsoft OAuth is enabled but client ID or secret is missing");
                result.IsValid = false;
            }

            await Task.CompletedTask; // For future async validations
        }

        #endregion
    }
}