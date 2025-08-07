using BlazorTemplate.Data;
using BlazorTemplate.Models.Media;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorTemplate.Services.Media;

/// <summary>
/// Provides comprehensive media file management services.
/// </summary>
public interface IMediaManagementService
{
    /// <summary>
    /// Uploads a file to the system with validation and processing.
    /// </summary>
    /// <param name="file">The file to upload.</param>
    /// <param name="userId">The ID of the user uploading the file.</param>
    /// <param name="category">The category of the file.</param>
    /// <param name="title">Optional title for the file.</param>
    /// <param name="description">Optional description for the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created MediaFile entity.</returns>
    Task<MediaFile> UploadFileAsync(IBrowserFile file, string userId, MediaFileCategory category, string? title = null, string? description = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a file by its ID with access control.
    /// </summary>
    /// <param name="fileId">The ID of the file.</param>
    /// <param name="userId">The ID of the user requesting the file (null for public access).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The MediaFile if found and accessible, otherwise null.</returns>
    Task<MediaFile?> GetFileByIdAsync(int fileId, string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list of files uploaded by a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="category">Optional category filter.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated result of the user's files.</returns>
    Task<PagedResult<MediaFile>> GetUserFilesAsync(string userId, MediaFileCategory? category = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list of publicly accessible files.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated result of public files.</returns>
    Task<PagedResult<MediaFile>> GetPublicFilesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a file (marks as deleted without removing from storage).
    /// </summary>
    /// <param name="fileId">The ID of the file to delete.</param>
    /// <param name="userId">The ID of the user requesting deletion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the file was deleted, otherwise false.</returns>
    Task<bool> DeleteFileAsync(int fileId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the metadata of a file.
    /// </summary>
    /// <param name="fileId">The ID of the file.</param>
    /// <param name="userId">The ID of the user updating the file.</param>
    /// <param name="title">The new title.</param>
    /// <param name="description">The new description.</param>
    /// <param name="tags">The new tags.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the metadata was updated, otherwise false.</returns>
    Task<bool> UpdateFileMetadataAsync(int fileId, string userId, string? title, string? description, List<string>? tags, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the file stream for a file.
    /// </summary>
    /// <param name="fileId">The ID of the file.</param>
    /// <param name="userId">The ID of the user requesting the file (null for public access).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file stream if accessible, otherwise null.</returns>
    Task<Stream?> GetFileStreamAsync(int fileId, string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the thumbnail stream for an image file.
    /// </summary>
    /// <param name="fileId">The ID of the file.</param>
    /// <param name="userId">The ID of the user requesting the thumbnail (null for public access).</param>
    /// <param name="size">The desired thumbnail size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The thumbnail stream if available and accessible, otherwise null.</returns>
    Task<Stream?> GetThumbnailStreamAsync(int fileId, string? userId = null, int size = 150, CancellationToken cancellationToken = default);
}