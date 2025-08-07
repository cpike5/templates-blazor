using BlazorTemplate.Data;
using BlazorTemplate.Models.Media;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorTemplate.Services.Media;

/// <summary>
/// Provides security services for file validation and access control.
/// </summary>
public interface IFileSecurityService
{
    /// <summary>
    /// Validates an uploaded file against security policies.
    /// </summary>
    /// <param name="file">The file to validate.</param>
    /// <returns>The validation result indicating success or failure with details.</returns>
    Task<FileValidationResult> ValidateFileAsync(IBrowserFile file);

    /// <summary>
    /// Checks if a user has permission to access a file.
    /// </summary>
    /// <param name="fileId">The ID of the file.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="permission">The type of access permission required.</param>
    /// <returns>True if the user has permission, otherwise false.</returns>
    Task<bool> CanUserAccessFileAsync(int fileId, string userId, MediaAccessPermission permission);

    /// <summary>
    /// Sanitizes a filename to remove dangerous characters.
    /// </summary>
    /// <param name="fileName">The original filename.</param>
    /// <returns>A sanitized filename safe for storage.</returns>
    Task<string> SanitizeFileName(string fileName);

    /// <summary>
    /// Scans a file for viruses or malware.
    /// </summary>
    /// <param name="fileStream">The file stream to scan.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the file is clean, false if threats are detected.</returns>
    Task<bool> ScanForVirusesAsync(Stream fileStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a secure filename for storage.
    /// </summary>
    /// <param name="originalFileName">The original filename.</param>
    /// <returns>A secure filename with UUID and sanitized extension.</returns>
    string GenerateSecureFileName(string originalFileName);

    /// <summary>
    /// Calculates the SHA-256 hash of a file stream.
    /// </summary>
    /// <param name="fileStream">The file stream to hash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The SHA-256 hash as a hexadecimal string.</returns>
    Task<string> CalculateFileHashAsync(Stream fileStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines the media category based on the file's MIME type.
    /// </summary>
    /// <param name="contentType">The MIME type of the file.</param>
    /// <returns>The appropriate media file category.</returns>
    MediaFileCategory DetermineFileCategory(string contentType);
}