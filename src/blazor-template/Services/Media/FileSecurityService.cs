using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BlazorTemplate.Configuration;
using BlazorTemplate.Data;
using BlazorTemplate.Models.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorTemplate.Services.Media;

/// <summary>
/// Implementation of IFileSecurityService for file validation and security operations.
/// </summary>
public class FileSecurityService : IFileSecurityService
{
    private readonly IOptions<ConfigurationOptions> _config;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FileSecurityService> _logger;
    
    // Regex pattern for dangerous characters in filenames
    private static readonly Regex DangerousCharsRegex = new(@"[<>:""/\\|?*\x00-\x1F]", RegexOptions.Compiled);
    
    // Known dangerous file extensions
    private static readonly HashSet<string> DangerousExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".bat", ".cmd", ".com", ".pif", ".scr", ".vbs", ".js", ".jar", ".app", ".deb", ".pkg", ".dmg"
    };

    /// <summary>
    /// Initializes a new instance of the FileSecurityService.
    /// </summary>
    /// <param name="config">Configuration options.</param>
    /// <param name="context">Database context.</param>
    /// <param name="logger">Logger instance.</param>
    public FileSecurityService(IOptions<ConfigurationOptions> config, ApplicationDbContext context, ILogger<FileSecurityService> logger)
    {
        _config = config;
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FileValidationResult> ValidateFileAsync(IBrowserFile file)
    {
        try
        {
            var warnings = new List<string>();
            var securityOptions = _config.Value.FileManagement.Security;

            // Check file size
            if (file.Size > securityOptions.MaxFileSizeBytes)
            {
                var maxSizeMB = securityOptions.MaxFileSizeBytes / (1024 * 1024);
                return FileValidationResult.Failure($"File size ({file.Size / (1024 * 1024):F1} MB) exceeds the maximum allowed size of {maxSizeMB} MB.");
            }

            if (file.Size == 0)
            {
                return FileValidationResult.Failure("File is empty.");
            }

            // Check MIME type
            if (!securityOptions.AllowedMimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            {
                return FileValidationResult.Failure($"File type '{file.ContentType}' is not allowed.");
            }

            // Check file extension
            var extension = Path.GetExtension(file.Name);
            if (string.IsNullOrEmpty(extension) || 
                !securityOptions.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return FileValidationResult.Failure($"File extension '{extension}' is not allowed.");
            }

            // Check for dangerous extensions
            if (DangerousExtensions.Contains(extension))
            {
                return FileValidationResult.Failure($"File extension '{extension}' is blocked for security reasons.");
            }

            // Validate filename
            if (string.IsNullOrWhiteSpace(file.Name))
            {
                return FileValidationResult.Failure("Filename cannot be empty.");
            }

            // Check for dangerous characters in filename
            if (DangerousCharsRegex.IsMatch(file.Name))
            {
                warnings.Add("Filename contains special characters that will be sanitized.");
            }

            // Virus scanning (if enabled)
            if (securityOptions.EnableVirusScanning)
            {
                using var stream = file.OpenReadStream();
                var isClean = await ScanForVirusesAsync(stream);
                if (!isClean)
                {
                    return FileValidationResult.Failure("File failed virus scan.");
                }
            }

            _logger.LogDebug("File validation successful for {FileName} ({ContentType}, {FileSize} bytes)", 
                file.Name, file.ContentType, file.Size);

            return warnings.Count > 0 
                ? FileValidationResult.SuccessWithWarnings(warnings)
                : FileValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during file validation for {FileName}", file.Name);
            return FileValidationResult.Failure("An error occurred during file validation.");
        }
    }

    /// <inheritdoc />
    public async Task<bool> CanUserAccessFileAsync(int fileId, string userId, MediaAccessPermission permission)
    {
        try
        {
            var file = await _context.MediaFiles
                .Where(f => f.Id == fileId && f.ProcessingStatus != MediaProcessingStatus.Deleted)
                .FirstOrDefaultAsync();

            if (file == null)
            {
                return false;
            }

            // File owner always has full access
            if (file.UploadedByUserId == userId)
            {
                return true;
            }

            // Check visibility settings
            switch (file.Visibility)
            {
                case MediaFileVisibility.Public:
                    // Public files allow view and download for all authenticated users
                    return permission == MediaAccessPermission.View || permission == MediaAccessPermission.Download;
                
                case MediaFileVisibility.Private:
                    // Only owner can access private files
                    return false;
                
                case MediaFileVisibility.Shared:
                    // Check specific access permissions
                    var hasAccess = await _context.MediaFileAccess
                        .Where(a => a.MediaFileId == fileId && 
                                    a.IsActive &&
                                    a.UserId == userId &&
                                    (int)a.Permission >= (int)permission)
                        .AnyAsync();
                    
                    if (!hasAccess)
                    {
                        // Check role-based access
                        var userRoles = await _context.UserRoles
                            .Where(ur => ur.UserId == userId)
                            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                            .ToListAsync();
                            
                        hasAccess = await _context.MediaFileAccess
                            .Where(a => a.MediaFileId == fileId &&
                                        a.IsActive &&
                                        a.Role != null &&
                                        userRoles.Contains(a.Role) &&
                                        (int)a.Permission >= (int)permission)
                            .AnyAsync();
                    }
                    
                    return hasAccess;
                
                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file access for file {FileId} and user {UserId}", fileId, userId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<string> SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "unnamed_file";
        }

        // Remove dangerous characters
        var sanitized = DangerousCharsRegex.Replace(fileName, "_");
        
        // Remove leading/trailing dots and spaces
        sanitized = sanitized.Trim('.', ' ');
        
        // Ensure minimum length
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "unnamed_file";
        }

        // Limit length
        if (sanitized.Length > 200)
        {
            var extension = Path.GetExtension(sanitized);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(sanitized);
            sanitized = nameWithoutExt.Substring(0, Math.Min(nameWithoutExt.Length, 200 - extension.Length)) + extension;
        }

        return sanitized;
    }

    /// <inheritdoc />
    public async Task<bool> ScanForVirusesAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        // Placeholder implementation - in a real system, this would integrate with antivirus software
        // For now, we'll do basic checks for suspicious patterns
        
        try
        {
            if (!fileStream.CanSeek)
            {
                _logger.LogWarning("Cannot scan non-seekable stream for viruses");
                return true; // Allow file if we can't scan
            }

            var originalPosition = fileStream.Position;
            fileStream.Seek(0, SeekOrigin.Begin);
            
            // Read first few bytes to check for suspicious patterns
            var buffer = new byte[1024];
            var bytesRead = await fileStream.ReadAsync(buffer, cancellationToken);
            
            // Reset stream position
            fileStream.Seek(originalPosition, SeekOrigin.Begin);
            
            // Simple check for executable signatures (this is very basic)
            if (bytesRead >= 2)
            {
                // Check for PE executable header (MZ)
                if (buffer[0] == 0x4D && buffer[1] == 0x5A)
                {
                    _logger.LogWarning("Detected executable file signature during virus scan");
                    return false;
                }
            }

            // In a real implementation, you would integrate with:
            // - ClamAV
            // - Windows Defender API
            // - Third-party antivirus solutions
            // - Cloud-based scanning services
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during virus scan");
            // In case of error, allow the file but log the issue
            return true;
        }
    }

    /// <inheritdoc />
    public string GenerateSecureFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var secureId = Guid.NewGuid().ToString("N");
        return $"{secureId}{extension}";
    }

    /// <inheritdoc />
    public async Task<string> CalculateFileHashAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!fileStream.CanSeek)
            {
                throw new ArgumentException("Stream must be seekable to calculate hash", nameof(fileStream));
            }

            var originalPosition = fileStream.Position;
            fileStream.Seek(0, SeekOrigin.Begin);
            
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(fileStream, cancellationToken);
            
            // Reset stream position
            fileStream.Seek(originalPosition, SeekOrigin.Begin);
            
            return Convert.ToHexString(hashBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating file hash");
            throw new FileStorageException("Failed to calculate file hash", ex);
        }
    }

    /// <inheritdoc />
    public MediaFileCategory DetermineFileCategory(string contentType)
    {
        var type = contentType.ToLowerInvariant();
        
        return type switch
        {
            var ct when ct.StartsWith("image/") => MediaFileCategory.Image,
            var ct when ct.StartsWith("video/") => MediaFileCategory.Video,
            var ct when ct.StartsWith("audio/") => MediaFileCategory.Audio,
            "application/pdf" => MediaFileCategory.Document,
            var ct when ct.StartsWith("text/") => MediaFileCategory.Document,
            "application/msword" => MediaFileCategory.Document,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => MediaFileCategory.Document,
            "application/vnd.ms-excel" => MediaFileCategory.Document,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => MediaFileCategory.Document,
            "application/zip" => MediaFileCategory.Archive,
            "application/x-rar-compressed" => MediaFileCategory.Archive,
            "application/x-7z-compressed" => MediaFileCategory.Archive,
            _ => MediaFileCategory.Other
        };
    }
}