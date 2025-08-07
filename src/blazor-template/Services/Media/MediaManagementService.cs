using System.Text.Json;
using BlazorTemplate.Data;
using BlazorTemplate.Models.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorTemplate.Services.Media;

/// <summary>
/// Implementation of IMediaManagementService for comprehensive media file management.
/// </summary>
public class MediaManagementService : IMediaManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly IMediaStorageService _storageService;
    private readonly IFileSecurityService _securityService;
    private readonly ILogger<MediaManagementService> _logger;

    /// <summary>
    /// Initializes a new instance of the MediaManagementService.
    /// </summary>
    /// <param name="context">Database context.</param>
    /// <param name="storageService">Storage service for file operations.</param>
    /// <param name="securityService">Security service for validation and access control.</param>
    /// <param name="logger">Logger instance.</param>
    public MediaManagementService(
        ApplicationDbContext context,
        IMediaStorageService storageService,
        IFileSecurityService securityService,
        ILogger<MediaManagementService> logger)
    {
        _context = context;
        _storageService = storageService;
        _securityService = securityService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MediaFile> UploadFileAsync(IBrowserFile file, string userId, MediaFileCategory category, string? title = null, string? description = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate file
            var validationResult = await _securityService.ValidateFileAsync(file);
            if (!validationResult.IsValid)
            {
                throw new FileValidationException(validationResult.ErrorMessage!);
            }

            // Generate secure filename and calculate hash
            var secureFileName = _securityService.GenerateSecureFileName(file.Name);
            string fileHash;
            
            using (var stream = file.OpenReadStream())
            {
                fileHash = await _securityService.CalculateFileHashAsync(stream, cancellationToken);
            }

            // Check for duplicates
            var existingFile = await _context.MediaFiles
                .FirstOrDefaultAsync(f => f.FileHash == fileHash && f.UploadedByUserId == userId, cancellationToken);
            
            if (existingFile != null)
            {
                _logger.LogInformation("Duplicate file detected for user {UserId}, returning existing file {FileId}", userId, existingFile.Id);
                return existingFile;
            }

            // Store file
            string storagePath;
            using (var stream = file.OpenReadStream())
            {
                storagePath = await _storageService.StoreFileAsync(stream, secureFileName, category.ToString(), userId, cancellationToken);
            }

            // Create database record
            var mediaFile = new MediaFile
            {
                FileName = await _securityService.SanitizeFileName(file.Name),
                StoredFileName = secureFileName,
                ContentType = file.ContentType,
                FileSize = file.Size,
                FileHash = fileHash,
                StorageProvider = "Local",
                StoragePath = storagePath,
                Title = title,
                Description = description,
                Category = category != MediaFileCategory.Other ? category : _securityService.DetermineFileCategory(file.ContentType),
                Visibility = MediaFileVisibility.Private,
                ProcessingStatus = MediaProcessingStatus.Complete, // For Phase 1, mark as complete immediately
                UploadedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                AccessCount = 0
            };

            _context.MediaFiles.Add(mediaFile);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("File uploaded successfully: {FileId} ({FileName}) by user {UserId}", 
                mediaFile.Id, mediaFile.FileName, userId);

            return mediaFile;
        }
        catch (FileValidationException)
        {
            // Re-throw validation exceptions as-is
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file {FileName} for user {UserId}", file.Name, userId);
            throw new FileStorageException($"Failed to upload file: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<MediaFile?> GetFileByIdAsync(int fileId, string? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.MediaFiles
                .Include(f => f.UploadedBy)
                .Where(f => f.Id == fileId && f.ProcessingStatus != MediaProcessingStatus.Deleted);

            var file = await query.FirstOrDefaultAsync(cancellationToken);
            if (file == null)
            {
                return null;
            }

            // Apply access control
            if (userId != null)
            {
                // Check if user can access this file
                if (file.UploadedByUserId != userId && 
                    file.Visibility != MediaFileVisibility.Public &&
                    !await _securityService.CanUserAccessFileAsync(fileId, userId, MediaAccessPermission.View))
                {
                    _logger.LogWarning("User {UserId} denied access to file {FileId}", userId, fileId);
                    return null;
                }
            }
            else if (file.Visibility != MediaFileVisibility.Public)
            {
                // Anonymous access only allowed for public files
                return null;
            }

            // Update access statistics
            file.LastAccessedAt = DateTime.UtcNow;
            file.AccessCount++;
            await _context.SaveChangesAsync(cancellationToken);

            return file;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file {FileId} for user {UserId}", fileId, userId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<PagedResult<MediaFile>> GetUserFilesAsync(string userId, MediaFileCategory? category = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.MediaFiles
                .Where(f => f.UploadedByUserId == userId && f.ProcessingStatus != MediaProcessingStatus.Deleted);

            if (category.HasValue)
            {
                query = query.Where(f => f.Category == category.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            
            var items = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<MediaFile>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user files for user {UserId}", userId);
            return PagedResult<MediaFile>.Empty(page, pageSize);
        }
    }

    /// <inheritdoc />
    public async Task<PagedResult<MediaFile>> GetPublicFilesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.MediaFiles
                .Include(f => f.UploadedBy)
                .Where(f => f.Visibility == MediaFileVisibility.Public && f.ProcessingStatus == MediaProcessingStatus.Complete);

            var totalCount = await query.CountAsync(cancellationToken);
            
            var items = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<MediaFile>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving public files");
            return PagedResult<MediaFile>.Empty(page, pageSize);
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteFileAsync(int fileId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var file = await _context.MediaFiles
                .FirstOrDefaultAsync(f => f.Id == fileId && f.UploadedByUserId == userId, cancellationToken);

            if (file == null)
            {
                _logger.LogWarning("File {FileId} not found or access denied for user {UserId}", fileId, userId);
                return false;
            }

            // Soft delete
            file.ProcessingStatus = MediaProcessingStatus.Deleted;
            file.LastModifiedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("File {FileId} marked for deletion by user {UserId}", fileId, userId);
            
            // TODO: Queue for physical deletion in background job
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileId} for user {UserId}", fileId, userId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateFileMetadataAsync(int fileId, string userId, string? title, string? description, List<string>? tags, CancellationToken cancellationToken = default)
    {
        try
        {
            var file = await _context.MediaFiles
                .FirstOrDefaultAsync(f => f.Id == fileId && f.UploadedByUserId == userId, cancellationToken);

            if (file == null)
            {
                _logger.LogWarning("File {FileId} not found or access denied for user {UserId}", fileId, userId);
                return false;
            }

            file.Title = title;
            file.Description = description;
            file.TagsJson = tags != null && tags.Count > 0 ? JsonSerializer.Serialize(tags) : null;
            file.LastModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("File metadata updated for file {FileId} by user {UserId}", fileId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating file metadata for file {FileId} and user {UserId}", fileId, userId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<Stream?> GetFileStreamAsync(int fileId, string? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var file = await GetFileByIdAsync(fileId, userId, cancellationToken);
            if (file == null)
            {
                return null;
            }

            return await _storageService.GetFileStreamAsync(file.StoragePath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file stream for file {FileId} and user {UserId}", fileId, userId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<Stream?> GetThumbnailStreamAsync(int fileId, string? userId = null, int size = 150, CancellationToken cancellationToken = default)
    {
        try
        {
            var file = await GetFileByIdAsync(fileId, userId, cancellationToken);
            if (file == null || !file.HasThumbnail)
            {
                return null;
            }

            // For Phase 1, we don't implement thumbnail generation
            // This will be implemented in Phase 2
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving thumbnail stream for file {FileId} and user {UserId}", fileId, userId);
            return null;
        }
    }
}