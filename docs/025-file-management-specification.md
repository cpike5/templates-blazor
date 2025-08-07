# File Management & Media System Specification

## Overview

The File Management & Media System provides secure file upload, storage, processing, and serving capabilities for the Blazor Server template. The system supports local file storage with future extensibility to cloud providers (Azure Blob Storage, AWS S3), comprehensive file metadata tracking, role-based access control, and media processing capabilities including thumbnail generation.

Built on the existing ASP.NET Core Identity and role system, the file management system follows established service patterns and integrates seamlessly with the current architecture.

## Core Requirements

### System Architecture Goals
- **Security First**: Role-based access control, secure file serving, and comprehensive validation
- **Performance Optimized**: Efficient storage, caching strategies, and optimized database queries
- **Scalable Design**: Extensible storage backends and configurable resource limits
- **User-Friendly**: Intuitive upload interface with drag-and-drop support
- **Enterprise Ready**: Audit logging, virus scanning capability, and administrative controls

### Integration Points
- ASP.NET Core Identity and role system integration
- Entity Framework with ApplicationDbContext extension
- Existing service layer patterns (UserRoleService, UserManagementService)
- Blazor Server components with interactive rendering
- Structured logging with Serilog
- Navigation system integration
- Configuration system using existing ConfigurationOptions pattern

## Database Schema

### MediaFile Entity

Primary entity for storing file metadata and system information.

```csharp
public class MediaFile
{
    public int Id { get; set; }
    
    // File Identity
    public string FileName { get; set; }                    // Original filename
    public string StoredFileName { get; set; }              // Internal storage filename (UUID-based)
    public string ContentType { get; set; }                // MIME type
    public long FileSize { get; set; }                      // Size in bytes
    public string FileHash { get; set; }                    // SHA-256 hash for deduplication
    
    // Storage Information
    public string StorageProvider { get; set; }             // "Local", "AzureBlob", "S3"
    public string StoragePath { get; set; }                 // Relative path within storage
    public string? StorageContainer { get; set; }           // Container/bucket name (cloud storage)
    
    // Metadata
    public string? Title { get; set; }                      // User-defined title
    public string? Description { get; set; }               // User description
    public string? TagsJson { get; set; }                  // JSON array of tags
    
    // Classification
    public MediaFileCategory Category { get; set; }
    public MediaFileVisibility Visibility { get; set; }
    public MediaProcessingStatus ProcessingStatus { get; set; }
    
    // Media-specific Properties
    public int? ImageWidth { get; set; }
    public int? ImageHeight { get; set; }
    public TimeSpan? Duration { get; set; }                 // For video/audio files
    
    // Thumbnail Information
    public bool HasThumbnail { get; set; }
    public string? ThumbnailPath { get; set; }
    public string? ThumbnailSizes { get; set; }            // JSON array of available sizes
    
    // User Association
    public string UploadedByUserId { get; set; }
    public ApplicationUser UploadedBy { get; set; }
    
    // Access Control
    public string? SharedWithRoles { get; set; }           // JSON array of role names
    
    // Audit Information
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public int AccessCount { get; set; }
    
    // Computed Properties
    public List<string> Tags => 
        string.IsNullOrEmpty(TagsJson) 
            ? new List<string>() 
            : JsonSerializer.Deserialize<List<string>>(TagsJson) ?? new List<string>();

    public List<string> SharedRoles => 
        string.IsNullOrEmpty(SharedWithRoles) 
            ? new List<string>() 
            : JsonSerializer.Deserialize<List<string>>(SharedWithRoles) ?? new List<string>();
}

public enum MediaFileCategory
{
    Document,
    Image, 
    Video,
    Audio,
    Archive,
    Other
}

public enum MediaFileVisibility
{
    Private,
    Public, 
    Shared
}

public enum MediaProcessingStatus
{
    Pending,
    Processing,
    Complete,
    Error,
    Deleted
}
```

### MediaFileAccess Entity

Tracks detailed access permissions for files.

```csharp
public class MediaFileAccess
{
    public int Id { get; set; }
    
    // File Reference
    public int MediaFileId { get; set; }
    public MediaFile MediaFile { get; set; }
    
    // Access Grant Information
    public string? UserId { get; set; }                     // Specific user access
    public ApplicationUser? User { get; set; }
    public string? Role { get; set; }                       // Role-based access
    
    // Permission Details
    public MediaAccessPermission Permission { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }                // Optional expiration
    
    // Audit Information
    public string GrantedByUserId { get; set; }
    public ApplicationUser GrantedByUser { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByUserId { get; set; }
    public ApplicationUser? RevokedByUser { get; set; }
    
    public bool IsActive => RevokedAt == null && (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);
}

public enum MediaAccessPermission
{
    View,
    Download,
    Edit,
    Delete,
    Share
}
```

### Entity Framework Configuration

```csharp
// Add to ApplicationDbContext.cs
public DbSet<MediaFile> MediaFiles { get; set; }
public DbSet<MediaFileAccess> MediaFileAccess { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // MediaFile Configuration
    modelBuilder.Entity<MediaFile>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
        entity.Property(e => e.StoredFileName).IsRequired().HasMaxLength(255);
        entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
        entity.Property(e => e.FileHash).IsRequired().HasMaxLength(64);
        entity.Property(e => e.StorageProvider).IsRequired().HasMaxLength(50);
        entity.Property(e => e.StoragePath).IsRequired().HasMaxLength(500);
        entity.Property(e => e.Title).HasMaxLength(200);
        entity.Property(e => e.Description).HasMaxLength(1000);
        
        // Relationships
        entity.HasOne(e => e.UploadedBy)
              .WithMany()
              .HasForeignKey(e => e.UploadedByUserId)
              .OnDelete(DeleteBehavior.Restrict);

        // Indexes for Performance
        entity.HasIndex(e => e.UploadedByUserId).HasDatabaseName("IX_MediaFiles_UploadedByUserId");
        entity.HasIndex(e => e.Category).HasDatabaseName("IX_MediaFiles_Category");
        entity.HasIndex(e => e.Visibility).HasDatabaseName("IX_MediaFiles_Visibility");
        entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_MediaFiles_CreatedAt");
        entity.HasIndex(e => e.ProcessingStatus).HasDatabaseName("IX_MediaFiles_ProcessingStatus");
        entity.HasIndex(e => e.FileHash).HasDatabaseName("IX_MediaFiles_FileHash");
        entity.HasIndex(e => new { e.UploadedByUserId, e.Category }).HasDatabaseName("IX_MediaFiles_User_Category");
    });

    // MediaFileAccess Configuration  
    modelBuilder.Entity<MediaFileAccess>(entity =>
    {
        entity.HasKey(e => e.Id);
        
        // Relationships
        entity.HasOne(e => e.MediaFile)
              .WithMany()
              .HasForeignKey(e => e.MediaFileId)
              .OnDelete(DeleteBehavior.Cascade);
              
        entity.HasOne(e => e.User)
              .WithMany()
              .HasForeignKey(e => e.UserId)
              .OnDelete(DeleteBehavior.Restrict);
              
        entity.HasOne(e => e.GrantedByUser)
              .WithMany()
              .HasForeignKey(e => e.GrantedByUserId)
              .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.RevokedByUser)
              .WithMany()
              .HasForeignKey(e => e.RevokedByUserId)
              .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        entity.HasIndex(e => e.MediaFileId).HasDatabaseName("IX_MediaFileAccess_MediaFileId");
        entity.HasIndex(e => e.UserId).HasDatabaseName("IX_MediaFileAccess_UserId");
        entity.HasIndex(e => e.Role).HasDatabaseName("IX_MediaFileAccess_Role");
        entity.HasIndex(e => new { e.MediaFileId, e.UserId }).HasDatabaseName("IX_MediaFileAccess_File_User");
    });
}
```

## Service Layer Architecture

### Core Service Interfaces

```csharp
public interface IMediaStorageService
{
    Task<string> StoreFileAsync(Stream fileStream, string fileName, string category, string userId, CancellationToken cancellationToken = default);
    Task<Stream?> GetFileStreamAsync(string storagePath, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default);
    Task<string> GetFileUrlAsync(string storagePath, TimeSpan? expiration = null);
    Task<bool> FileExistsAsync(string storagePath, CancellationToken cancellationToken = default);
    Task<long> GetFileSizeAsync(string storagePath, CancellationToken cancellationToken = default);
}

public interface IImageProcessingService
{
    Task<string> GenerateThumbnailAsync(string originalPath, int width, int height, CancellationToken cancellationToken = default);
    Task<(int width, int height)> GetImageDimensionsAsync(string filePath, CancellationToken cancellationToken = default);
    Task<bool> IsImageFileAsync(string contentType);
    Task<string> OptimizeImageAsync(string originalPath, int quality = 85, CancellationToken cancellationToken = default);
}

public interface IMediaManagementService  
{
    Task<MediaFile> UploadFileAsync(IFormFile file, string userId, MediaFileCategory category, string? title = null, string? description = null, CancellationToken cancellationToken = default);
    Task<MediaFile?> GetFileByIdAsync(int fileId, string? userId = null, CancellationToken cancellationToken = default);
    Task<PagedResult<MediaFile>> GetUserFilesAsync(string userId, MediaFileCategory? category = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<PagedResult<MediaFile>> GetPublicFilesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(int fileId, string userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateFileMetadataAsync(int fileId, string userId, string? title, string? description, List<string>? tags, CancellationToken cancellationToken = default);
    Task<Stream?> GetFileStreamAsync(int fileId, string? userId = null, CancellationToken cancellationToken = default);
    Task<Stream?> GetThumbnailStreamAsync(int fileId, string? userId = null, int size = 150, CancellationToken cancellationToken = default);
}

public interface IFileSecurityService
{
    Task<FileValidationResult> ValidateFileAsync(IFormFile file);
    Task<bool> CanUserAccessFileAsync(int fileId, string userId, MediaAccessPermission permission);
    Task<string> SanitizeFileName(string fileName);
    Task<bool> ScanForVirusesAsync(Stream fileStream, CancellationToken cancellationToken = default);
    string GenerateSecureFileName(string originalFileName);
}
```

### Service Implementations

#### LocalFileStorageService

```csharp
public class LocalFileStorageService : IMediaStorageService
{
    private readonly IOptions<ConfigurationOptions> _config;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _basePath;

    public LocalFileStorageService(IOptions<ConfigurationOptions> config, ILogger<LocalFileStorageService> logger)
    {
        _config = config;
        _logger = logger;
        _basePath = Path.GetFullPath(_config.Value.FileManagement.LocalStorage.RootPath);
        
        // Ensure directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> StoreFileAsync(Stream fileStream, string fileName, string category, string userId, CancellationToken cancellationToken = default)
    {
        var dateFolder = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var categoryFolder = category.ToLowerInvariant();
        var userFolder = userId;
        
        var relativePath = Path.Combine(userFolder, categoryFolder, dateFolder, fileName);
        var fullPath = Path.Combine(_basePath, relativePath);
        
        var directory = Path.GetDirectoryName(fullPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory!);
        }

        using var fileStream2 = new FileStream(fullPath, FileMode.Create);
        await fileStream.CopyToAsync(fileStream2, cancellationToken);
        
        _logger.LogInformation("File stored at {FilePath} for user {UserId}", relativePath, userId);
        return relativePath;
    }

    public async Task<Stream?> GetFileStreamAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        
        if (!File.Exists(fullPath))
        {
            return null;
        }

        return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public async Task<bool> DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        
        if (!File.Exists(fullPath))
        {
            return false;
        }

        File.Delete(fullPath);
        _logger.LogInformation("File deleted at {StoragePath}", storagePath);
        return true;
    }

    public async Task<string> GetFileUrlAsync(string storagePath, TimeSpan? expiration = null)
    {
        // For local storage, return a relative URL that will be handled by MediaController
        return $"/media/file/{Uri.EscapeDataString(storagePath)}";
    }

    public async Task<bool> FileExistsAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        return File.Exists(fullPath);
    }

    public async Task<long> GetFileSizeAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        return new FileInfo(fullPath).Length;
    }
}
```

#### MediaManagementService

```csharp
public class MediaManagementService : IMediaManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly IMediaStorageService _storageService;
    private readonly IImageProcessingService _imageProcessing;
    private readonly IFileSecurityService _securityService;
    private readonly ILogger<MediaManagementService> _logger;

    public MediaManagementService(
        ApplicationDbContext context,
        IMediaStorageService storageService,
        IImageProcessingService imageProcessing,
        IFileSecurityService securityService,
        ILogger<MediaManagementService> logger)
    {
        _context = context;
        _storageService = storageService;
        _imageProcessing = imageProcessing;
        _securityService = securityService;
        _logger = logger;
    }

    public async Task<MediaFile> UploadFileAsync(IFormFile file, string userId, MediaFileCategory category, string? title = null, string? description = null, CancellationToken cancellationToken = default)
    {
        // Validate file
        var validationResult = await _securityService.ValidateFileAsync(file);
        if (!validationResult.IsValid)
        {
            throw new FileValidationException(validationResult.ErrorMessage!);
        }

        // Generate secure filename
        var secureFileName = _securityService.GenerateSecureFileName(file.FileName);
        
        // Calculate file hash
        string fileHash;
        using (var stream = file.OpenReadStream())
        {
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
            fileHash = Convert.ToHexString(hashBytes);
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
            FileName = file.FileName,
            StoredFileName = secureFileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            FileHash = fileHash,
            StorageProvider = "Local", // TODO: Get from config
            StoragePath = storagePath,
            Title = title,
            Description = description,
            Category = category,
            Visibility = MediaFileVisibility.Private,
            ProcessingStatus = MediaProcessingStatus.Pending,
            UploadedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            AccessCount = 0
        };

        // Get image dimensions if applicable
        if (await _imageProcessing.IsImageFileAsync(file.ContentType))
        {
            try
            {
                var dimensions = await _imageProcessing.GetImageDimensionsAsync(storagePath, cancellationToken);
                mediaFile.ImageWidth = dimensions.width;
                mediaFile.ImageHeight = dimensions.height;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get image dimensions for file {FileId}", mediaFile.Id);
            }
        }

        _context.MediaFiles.Add(mediaFile);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("File uploaded successfully: {FileId} by user {UserId}", mediaFile.Id, userId);

        // Queue for background processing
        // TODO: Add background job for thumbnail generation

        return mediaFile;
    }

    public async Task<MediaFile?> GetFileByIdAsync(int fileId, string? userId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.MediaFiles
            .Include(f => f.UploadedBy)
            .Where(f => f.Id == fileId && f.ProcessingStatus != MediaProcessingStatus.Deleted);

        if (userId != null)
        {
            // Apply access control
            query = query.Where(f => 
                f.UploadedByUserId == userId || 
                f.Visibility == MediaFileVisibility.Public ||
                (f.Visibility == MediaFileVisibility.Shared && 
                 _context.MediaFileAccess.Any(a => a.MediaFileId == fileId && 
                    (a.UserId == userId || _context.UserRoles.Any(ur => ur.UserId == userId && ur.Role.Name == a.Role)) &&
                    a.IsActive)));
        }
        else
        {
            // Public access only
            query = query.Where(f => f.Visibility == MediaFileVisibility.Public);
        }

        var file = await query.FirstOrDefaultAsync(cancellationToken);
        
        if (file != null)
        {
            // Update access statistics
            file.LastAccessedAt = DateTime.UtcNow;
            file.AccessCount++;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return file;
    }

    public async Task<PagedResult<MediaFile>> GetUserFilesAsync(string userId, MediaFileCategory? category = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
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

    public async Task<PagedResult<MediaFile>> GetPublicFilesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = _context.MediaFiles
            .Where(f => f.Visibility == MediaFileVisibility.Public && f.ProcessingStatus == MediaProcessingStatus.Complete);

        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(f => f.UploadedBy)
            .ToListAsync(cancellationToken);

        return new PagedResult<MediaFile>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> DeleteFileAsync(int fileId, string userId, CancellationToken cancellationToken = default)
    {
        var file = await _context.MediaFiles
            .FirstOrDefaultAsync(f => f.Id == fileId && f.UploadedByUserId == userId, cancellationToken);

        if (file == null)
        {
            return false;
        }

        // Soft delete
        file.ProcessingStatus = MediaProcessingStatus.Deleted;
        file.LastModifiedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync(cancellationToken);

        // Queue for physical deletion in background job
        _logger.LogInformation("File {FileId} marked for deletion by user {UserId}", fileId, userId);
        
        return true;
    }

    public async Task<bool> UpdateFileMetadataAsync(int fileId, string userId, string? title, string? description, List<string>? tags, CancellationToken cancellationToken = default)
    {
        var file = await _context.MediaFiles
            .FirstOrDefaultAsync(f => f.Id == fileId && f.UploadedByUserId == userId, cancellationToken);

        if (file == null)
        {
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

    public async Task<Stream?> GetFileStreamAsync(int fileId, string? userId = null, CancellationToken cancellationToken = default)
    {
        var file = await GetFileByIdAsync(fileId, userId, cancellationToken);
        if (file == null)
        {
            return null;
        }

        return await _storageService.GetFileStreamAsync(file.StoragePath, cancellationToken);
    }

    public async Task<Stream?> GetThumbnailStreamAsync(int fileId, string? userId = null, int size = 150, CancellationToken cancellationToken = default)
    {
        var file = await GetFileByIdAsync(fileId, userId, cancellationToken);
        if (file == null || !file.HasThumbnail)
        {
            return null;
        }

        var thumbnailPath = $"{file.ThumbnailPath}_{size}.jpg";
        return await _storageService.GetFileStreamAsync(thumbnailPath, cancellationToken);
    }
}
```

## File Serving Controller

### MediaController (File Serving Only)

Simple controller for secure file serving - no full REST API.

```csharp
[Route("media")]
public class MediaController : Controller
{
    private readonly IMediaManagementService _mediaService;
    private readonly ILogger<MediaController> _logger;

    public MediaController(
        IMediaManagementService mediaService,
        ILogger<MediaController> logger)
    {
        _mediaService = mediaService;
        _logger = logger;
    }

    [HttpGet("{fileId:int}")]
    public async Task<IActionResult> GetFile(int fileId)
    {
        var userId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
        var file = await _mediaService.GetFileByIdAsync(fileId, userId);
        
        if (file == null)
        {
            return NotFound();
        }

        var stream = await _mediaService.GetFileStreamAsync(fileId, userId);
        if (stream == null)
        {
            return NotFound();
        }

        Response.Headers.Add("X-Content-Type-Options", "nosniff");
        return File(stream, file.ContentType, file.FileName, enableRangeProcessing: true);
    }

    [HttpGet("{fileId:int}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(int fileId, int size = 150)
    {
        var userId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
        var thumbnailStream = await _mediaService.GetThumbnailStreamAsync(fileId, userId, size);
        
        if (thumbnailStream == null)
        {
            return NotFound();
        }

        Response.Headers.Add("Cache-Control", "public, max-age=3600");
        Response.Headers.Add("X-Content-Type-Options", "nosniff");
        return File(thumbnailStream, "image/jpeg", enableRangeProcessing: true);
    }
}
```

## Configuration System

### ConfigurationOptions Extension

Integrates with the existing ConfigurationOptions pattern:

```csharp
// Add to existing ConfigurationOptions.cs
public class ConfigurationOptions
{
    public static readonly string SectionName = "Site";
    public AdministrationOptions Administration { get; set; } = new AdministrationOptions();
    public SetupOptions Setup { get; set; } = new SetupOptions();
    public SettingsOptions Settings { get; set; } = new SettingsOptions();
    public FileManagementOptions FileManagement { get; set; } = new FileManagementOptions(); // NEW
}

public class FileManagementOptions
{
    public LocalStorageOptions LocalStorage { get; set; } = new();
    public SecurityOptions Security { get; set; } = new();
    public ProcessingOptions Processing { get; set; } = new();
    public CacheOptions Cache { get; set; } = new();
}

public class LocalStorageOptions
{
    public string RootPath { get; set; } = "App_Data/uploads";
    public bool OrganizeByDate { get; set; } = true;
    public long MaxStorageBytes { get; set; } = 10_737_418_240; // 10GB
}

public class SecurityOptions
{
    public long MaxFileSizeBytes { get; set; } = 104_857_600; // 100MB
    public string[] AllowedMimeTypes { get; set; } = {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf", "text/plain", "application/zip"
    };
    public string[] AllowedExtensions { get; set; } = {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".txt", ".zip"
    };
    public bool EnableVirusScanning { get; set; } = false;
    public bool RequireAuthentication { get; set; } = true;
}

public class ProcessingOptions
{
    public bool EnableThumbnailGeneration { get; set; } = true;
    public int ThumbnailMaxWidth { get; set; } = 300;
    public int ThumbnailMaxHeight { get; set; } = 300;
    public int ThumbnailQuality { get; set; } = 85;
}

public class CacheOptions
{
    public bool EnableFileMetadataCache { get; set; } = true;
    public int MetadataCacheExpirationMinutes { get; set; } = 60;
    public bool EnableThumbnailCache { get; set; } = true;
}
```

### appsettings.json Configuration

```json
{
  "Site": {
    "Administration": {
      // existing config...
    },
    "Setup": {
      // existing config...
    },
    "FileManagement": {
      "LocalStorage": {
        "RootPath": "App_Data/uploads",
        "OrganizeByDate": true,
        "MaxStorageBytes": 10737418240
      },
      "Security": {
        "MaxFileSizeBytes": 104857600,
        "AllowedMimeTypes": [
          "image/jpeg", "image/png", "image/gif", "image/webp",
          "application/pdf", "text/plain", "application/zip"
        ],
        "AllowedExtensions": [
          ".jpg", ".jpeg", ".png", ".gif", ".webp", 
          ".pdf", ".txt", ".zip"
        ],
        "EnableVirusScanning": false,
        "RequireAuthentication": true
      },
      "Processing": {
        "EnableThumbnailGeneration": true,
        "ThumbnailMaxWidth": 300,
        "ThumbnailMaxHeight": 300,
        "ThumbnailQuality": 85
      },
      "Cache": {
        "EnableFileMetadataCache": true,
        "MetadataCacheExpirationMinutes": 60,
        "EnableThumbnailCache": true
      }
    }
  }
}
```

## Blazor Components

### FileUploadComponent

Interactive file upload component with drag-and-drop support.

```razor
@rendermode InteractiveServer
@using Microsoft.AspNetCore.Authorization
@using BlazorTemplate.Services
@inject IMediaManagementService MediaService
@inject IJSRuntime JS
@inject ILogger<FileUploadComponent> Logger

<div class="file-upload-container">
    <div class="drop-zone @(isDragging ? "drag-over" : "")" 
         @ondragenter="HandleDragEnter" 
         @ondragleave="HandleDragLeave" 
         @ondragover:preventDefault="true"
         @ondrop="HandleDrop">
        
        <div class="upload-content">
            <i class="fas fa-cloud-upload-alt upload-icon"></i>
            <h4>Drop files here or click to browse</h4>
            <p>Supported formats: @string.Join(", ", AllowedExtensions)</p>
            <p>Maximum file size: @FormatFileSize(MaxFileSize)</p>
            
            <InputFile OnChange="HandleFileSelection" 
                      multiple="@AllowMultiple" 
                      accept="@string.Join(",", AllowedExtensions)"
                      class="file-input" />
        </div>
    </div>

    @if (uploadQueue.Any())
    {
        <div class="upload-progress">
            <h5>Upload Progress</h5>
            @foreach (var upload in uploadQueue)
            {
                <div class="progress-item">
                    <div class="progress-header">
                        <span class="file-name">@upload.FileName</span>
                        <span class="file-size">@FormatFileSize(upload.FileSize)</span>
                    </div>
                    <div class="progress-bar">
                        <div class="progress-fill" style="width: @(upload.Progress)%"></div>
                    </div>
                    <div class="progress-status">
                        @if (upload.IsComplete)
                        {
                            <span class="text-success"><i class="fas fa-check"></i> Complete</span>
                        }
                        else if (upload.HasError)
                        {
                            <span class="text-danger"><i class="fas fa-exclamation-triangle"></i> @upload.ErrorMessage</span>
                        }
                        else
                        {
                            <span class="text-info">@upload.Progress% - @upload.Status</span>
                        }
                    </div>
                </div>
            }
        </div>
    }
</div>

@code {
    [Parameter] public MediaFileCategory Category { get; set; } = MediaFileCategory.Document;
    [Parameter] public bool AllowMultiple { get; set; } = true;
    [Parameter] public long MaxFileSize { get; set; } = 104857600; // 100MB
    [Parameter] public string[] AllowedExtensions { get; set; } = { ".pdf", ".jpg", ".jpeg", ".png", ".gif" };
    [Parameter] public EventCallback<List<MediaFile>> OnFilesUploaded { get; set; }

    private bool isDragging = false;
    private List<FileUpload> uploadQueue = new();

    private class FileUpload
    {
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int Progress { get; set; }
        public string Status { get; set; } = "Preparing...";
        public bool IsComplete { get; set; }
        public bool HasError { get; set; }
        public string? ErrorMessage { get; set; }
        public MediaFile? Result { get; set; }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JS.InvokeVoidAsync("fileUpload.initialize", DotNetObjectReference.Create(this));
        }
    }

    private void HandleDragEnter() => isDragging = true;
    private void HandleDragLeave() => isDragging = false;

    private async Task HandleDrop()
    {
        isDragging = false;
        // File handling will be done through HandleFileSelection
    }

    private async Task HandleFileSelection(InputFileChangeEventArgs e)
    {
        var files = e.GetMultipleFiles(AllowMultiple ? 10 : 1);
        var uploadedFiles = new List<MediaFile>();

        foreach (var file in files)
        {
            var upload = new FileUpload
            {
                FileName = file.Name,
                FileSize = file.Size,
                Progress = 0,
                Status = "Validating..."
            };
            
            uploadQueue.Add(upload);
            StateHasChanged();

            try
            {
                // Validate file
                if (file.Size > MaxFileSize)
                {
                    upload.HasError = true;
                    upload.ErrorMessage = $"File size exceeds maximum of {FormatFileSize(MaxFileSize)}";
                    StateHasChanged();
                    continue;
                }

                var extension = Path.GetExtension(file.Name).ToLowerInvariant();
                if (!AllowedExtensions.Contains(extension))
                {
                    upload.HasError = true;
                    upload.ErrorMessage = $"File type not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}";
                    StateHasChanged();
                    continue;
                }

                upload.Status = "Uploading...";
                upload.Progress = 10;
                StateHasChanged();

                // Upload file
                var result = await MediaService.UploadFileAsync(file, GetCurrentUserId(), Category);
                
                upload.Progress = 100;
                upload.IsComplete = true;
                upload.Status = "Complete";
                upload.Result = result;
                
                uploadedFiles.Add(result);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                upload.HasError = true;
                upload.ErrorMessage = ex.Message;
                Logger.LogError(ex, "Error uploading file {FileName}", file.Name);
                StateHasChanged();
            }
        }

        if (uploadedFiles.Any())
        {
            await OnFilesUploaded.InvokeAsync(uploadedFiles);
        }
    }

    private string GetCurrentUserId()
    {
        // Implementation depends on authentication context
        // This would typically come from a cascading parameter or service
        return "current-user-id"; // Placeholder
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size = size / 1024;
        }
        
        return $"{size:0.##} {sizes[order]}";
    }
}

<style>
.file-upload-container {
    margin: 1rem 0;
}

.drop-zone {
    border: 2px dashed #ccc;
    border-radius: 8px;
    padding: 2rem;
    text-align: center;
    background: #fafafa;
    transition: all 0.3s ease;
    cursor: pointer;
}

.drop-zone:hover, .drop-zone.drag-over {
    border-color: #007bff;
    background: #f0f8ff;
}

.upload-icon {
    font-size: 3rem;
    color: #007bff;
    margin-bottom: 1rem;
}

.file-input {
    position: absolute;
    opacity: 0;
    width: 100%;
    height: 100%;
    cursor: pointer;
}

.upload-progress {
    margin-top: 1rem;
    padding: 1rem;
    background: #f8f9fa;
    border-radius: 4px;
}

.progress-item {
    margin-bottom: 1rem;
}

.progress-header {
    display: flex;
    justify-content: space-between;
    margin-bottom: 0.5rem;
}

.progress-bar {
    width: 100%;
    height: 8px;
    background: #e9ecef;
    border-radius: 4px;
    overflow: hidden;
}

.progress-fill {
    height: 100%;
    background: #28a745;
    transition: width 0.3s ease;
}

.progress-status {
    margin-top: 0.25rem;
    font-size: 0.875rem;
}
</style>
```

### MediaGalleryComponent

Display uploaded files in a gallery format.

```razor
@rendermode InteractiveServer
@using BlazorTemplate.Services
@inject IMediaManagementService MediaService

<div class="media-gallery">
    <div class="gallery-controls">
        <div class="view-controls">
            <button class="btn btn-outline-secondary @(viewMode == "grid" ? "active" : "")" 
                    @onclick="() => SetViewMode(\"grid\")">
                <i class="fas fa-th"></i> Grid
            </button>
            <button class="btn btn-outline-secondary @(viewMode == "list" ? "active" : "")" 
                    @onclick="() => SetViewMode(\"list\")">
                <i class="fas fa-list"></i> List
            </button>
        </div>
        
        <div class="filter-controls">
            <select class="form-select" @onchange="HandleCategoryFilter">
                <option value="">All Categories</option>
                @foreach (var category in Enum.GetValues<MediaFileCategory>())
                {
                    <option value="@category">@category</option>
                }
            </select>
        </div>
    </div>

    @if (isLoading)
    {
        <div class="loading-spinner">
            <div class="spinner-border" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
        </div>
    }
    else if (!files.Any())
    {
        <div class="empty-state">
            <i class="fas fa-folder-open empty-icon"></i>
            <h4>No files found</h4>
            <p>Upload some files to get started.</p>
        </div>
    }
    else
    {
        <div class="gallery-content @viewMode">
            @foreach (var file in files)
            {
                <div class="media-item" @onclick="() => SelectFile(file)">
                    <div class="media-thumbnail">
                        @if (IsImage(file.ContentType))
                        {
                            <img src="/media/@file.Id/thumbnail" alt="@file.FileName" loading="lazy" />
                        }
                        else
                        {
                            <div class="file-icon">
                                <i class="@GetFileIcon(file.ContentType)"></i>
                            </div>
                        }
                    </div>
                    
                    <div class="media-info">
                        <h6 class="file-name" title="@file.FileName">@file.FileName</h6>
                        <p class="file-details">
                            @FormatFileSize(file.FileSize) • @file.CreatedAt.ToString("MMM dd, yyyy")
                        </p>
                        @if (!string.IsNullOrEmpty(file.Description))
                        {
                            <p class="file-description">@file.Description</p>
                        }
                    </div>
                    
                    <div class="media-actions">
                        <button class="btn btn-sm btn-outline-primary" @onclick:stopPropagation="true" 
                                @onclick="() => DownloadFile(file)">
                            <i class="fas fa-download"></i>
                        </button>
                        @if (CanEdit)
                        {
                            <button class="btn btn-sm btn-outline-secondary" @onclick:stopPropagation="true"
                                    @onclick="() => EditFile(file)">
                                <i class="fas fa-edit"></i>
                            </button>
                            <button class="btn btn-sm btn-outline-danger" @onclick:stopPropagation="true"
                                    @onclick="() => DeleteFile(file)">
                                <i class="fas fa-trash"></i>
                            </button>
                        }
                    </div>
                </div>
            }
        </div>

        @if (hasMorePages)
        {
            <div class="pagination-controls">
                <button class="btn btn-outline-primary" @onclick="LoadMoreFiles" disabled="@isLoadingMore">
                    @if (isLoadingMore)
                    {
                        <span class="spinner-border spinner-border-sm" role="status"></span>
                        <span>Loading...</span>
                    }
                    else
                    {
                        <span>Load More</span>
                    }
                </button>
            </div>
        }
    }
</div>

@code {
    [Parameter] public string UserId { get; set; } = string.Empty;
    [Parameter] public bool CanEdit { get; set; } = false;
    [Parameter] public EventCallback<MediaFile> OnFileSelected { get; set; }
    [Parameter] public EventCallback<MediaFile> OnFileDeleted { get; set; }

    private List<MediaFile> files = new();
    private string viewMode = "grid";
    private MediaFileCategory? selectedCategory = null;
    private bool isLoading = false;
    private bool isLoadingMore = false;
    private bool hasMorePages = false;
    private int currentPage = 1;
    private const int PageSize = 20;

    protected override async Task OnInitializedAsync()
    {
        await LoadFiles();
    }

    private async Task LoadFiles(bool append = false)
    {
        if (!append)
        {
            isLoading = true;
            currentPage = 1;
            files.Clear();
        }
        else
        {
            isLoadingMore = true;
        }

        StateHasChanged();

        try
        {
            var result = await MediaService.GetUserFilesAsync(UserId, selectedCategory, currentPage, PageSize);
            
            if (append)
            {
                files.AddRange(result.Items);
            }
            else
            {
                files = result.Items.ToList();
            }

            hasMorePages = currentPage < result.TotalPages;
        }
        catch (Exception ex)
        {
            // Handle error
            Console.WriteLine($"Error loading files: {ex.Message}");
        }
        finally
        {
            isLoading = false;
            isLoadingMore = false;
            StateHasChanged();
        }
    }

    private async Task LoadMoreFiles()
    {
        currentPage++;
        await LoadFiles(append: true);
    }

    private void SetViewMode(string mode)
    {
        viewMode = mode;
        StateHasChanged();
    }

    private async Task HandleCategoryFilter(ChangeEventArgs e)
    {
        var value = e.Value?.ToString();
        selectedCategory = string.IsNullOrEmpty(value) ? null : Enum.Parse<MediaFileCategory>(value);
        await LoadFiles();
    }

    private async Task SelectFile(MediaFile file)
    {
        await OnFileSelected.InvokeAsync(file);
    }

    private async Task DownloadFile(MediaFile file)
    {
        // Implementation for file download
        var url = $"/media/{file.Id}";
        await JS.InvokeVoidAsync("window.open", url, "_blank");
    }

    private async Task EditFile(MediaFile file)
    {
        // Implementation for file editing
        // This could open a modal or navigate to edit page
    }

    private async Task DeleteFile(MediaFile file)
    {
        if (await JS.InvokeAsync<bool>("confirm", $"Are you sure you want to delete '{file.FileName}'?"))
        {
            var success = await MediaService.DeleteFileAsync(file.Id, UserId);
            if (success)
            {
                files.Remove(file);
                await OnFileDeleted.InvokeAsync(file);
                StateHasChanged();
            }
        }
    }

    private bool IsImage(string contentType)
    {
        return contentType.StartsWith("image/");
    }

    private string GetFileIcon(string contentType)
    {
        return contentType switch
        {
            var ct when ct.StartsWith("image/") => "fas fa-file-image",
            "application/pdf" => "fas fa-file-pdf",
            var ct when ct.StartsWith("text/") => "fas fa-file-alt",
            "application/zip" => "fas fa-file-archive",
            var ct when ct.StartsWith("video/") => "fas fa-file-video",
            var ct when ct.StartsWith("audio/") => "fas fa-file-audio",
            _ => "fas fa-file"
        };
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size = size / 1024;
        }
        
        return $"{size:0.##} {sizes[order]}";
    }
}

<style>
.media-gallery {
    width: 100%;
}

.gallery-controls {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1rem;
    padding-bottom: 1rem;
    border-bottom: 1px solid #dee2e6;
}

.gallery-content.grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
    gap: 1rem;
}

.gallery-content.list .media-item {
    display: flex;
    align-items: center;
    padding: 0.75rem;
    margin-bottom: 0.5rem;
    border: 1px solid #dee2e6;
    border-radius: 4px;
}

.media-item {
    border: 1px solid #dee2e6;
    border-radius: 8px;
    overflow: hidden;
    transition: all 0.3s ease;
    cursor: pointer;
    background: white;
}

.media-item:hover {
    border-color: #007bff;
    box-shadow: 0 2px 8px rgba(0,123,255,0.15);
}

.media-thumbnail {
    width: 100%;
    height: 150px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: #f8f9fa;
    overflow: hidden;
}

.media-thumbnail img {
    width: 100%;
    height: 100%;
    object-fit: cover;
}

.file-icon {
    font-size: 2rem;
    color: #6c757d;
}

.media-info {
    padding: 0.75rem;
}

.file-name {
    margin: 0 0 0.25rem 0;
    font-size: 0.875rem;
    font-weight: 600;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.file-details {
    margin: 0;
    font-size: 0.75rem;
    color: #6c757d;
}

.file-description {
    margin: 0.25rem 0 0 0;
    font-size: 0.75rem;
    color: #495057;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
}

.media-actions {
    padding: 0.5rem 0.75rem;
    border-top: 1px solid #dee2e6;
    display: flex;
    gap: 0.25rem;
}

.empty-state {
    text-align: center;
    padding: 3rem;
    color: #6c757d;
}

.empty-icon {
    font-size: 3rem;
    margin-bottom: 1rem;
}

.loading-spinner {
    text-align: center;
    padding: 2rem;
}

.pagination-controls {
    text-align: center;
    margin-top: 2rem;
}
</style>
```

## Service Registration

### ServiceCollectionExtensions Integration

Following the established pattern, add the file management services via extension method.

```csharp
// Add to Extensions/ServiceCollectionExtensions.cs

public static IServiceCollection AddFileManagementServices(this IServiceCollection services, IConfiguration configuration)
{
    // Storage Services - determine provider from config
    var siteConfig = configuration.GetSection(ConfigurationOptions.SectionName).Get<ConfigurationOptions>();
    var storageProvider = siteConfig?.FileManagement?.LocalStorage?.RootPath != null ? "Local" : "Local";
    
    switch (storageProvider.ToLower())
    {
        case "local":
            services.AddSingleton<IMediaStorageService, LocalFileStorageService>();
            break;
        case "azureblob":
            services.AddSingleton<IMediaStorageService, AzureBlobStorageService>();
            break;
        case "aws":
        case "s3":
            services.AddSingleton<IMediaStorageService, AwsS3StorageService>();
            break;
        default:
            services.AddSingleton<IMediaStorageService, LocalFileStorageService>();
            break;
    }

    // Core Services
    services.AddScoped<IMediaManagementService, MediaManagementService>();
    services.AddScoped<IImageProcessingService, ImageProcessingService>();
    services.AddScoped<IFileSecurityService, FileSecurityService>();
    services.AddScoped<IVirusScanner, NoOpVirusScanner>(); // Default to no-op, can be configured

    // Background Services
    services.AddHostedService<MediaProcessingBackgroundService>();
    services.AddHostedService<FileCleanupBackgroundService>();
    
    return services;
}
```

### Program.cs Integration

```csharp
// Add to Program.cs after existing service registrations
builder.Services.AddFileManagementServices(builder.Configuration);
```

## Navigation Integration

Add file management to the navigation system:

```json
// Add to appsettings.json Navigation section
{
  "Navigation": {
    "Items": [
      // existing items...
      {
        "Id": "media",
        "Title": "Media",
        "Icon": "fas fa-images",
        "RequiredRoles": ["User", "Administrator"],
        "Order": 25,
        "Children": [
          {
            "Id": "my-files",
            "Title": "My Files",
            "Href": "/Media/MyFiles",
            "Icon": "fas fa-folder-open",
            "Order": 1
          },
          {
            "Id": "upload",
            "Title": "Upload Files",
            "Href": "/Media/Upload",
            "Icon": "fas fa-cloud-upload-alt",
            "Order": 2
          }
        ]
      }
    ]
  }
}
```

## Implementation Phases

### Phase 1: Core Upload System (Week 1-2)
**Deliverables:**
- Database schema and migrations
- Local file storage service
- Basic file upload service
- Simple upload component
- File serving controller

**Success Criteria:**
- Users can upload files through Blazor component
- Files are stored securely in local file system
- Basic file listing and download works
- Database tracks file metadata correctly

### Phase 2: Security & Processing (Week 3-4)
**Deliverables:**
- File validation and security service
- Image processing service with thumbnail generation
- Background processing service
- Access control implementation
- Enhanced upload component with progress

**Success Criteria:**
- File validation prevents malicious uploads
- Thumbnails generate automatically for images
- Role-based access control works correctly
- Background processing handles thumbnails

### Phase 3: Advanced Features (Week 5-6)
**Deliverables:**
- Media gallery component with grid/list views
- File metadata editing
- Search and filtering capabilities
- Cloud storage service implementations (Azure/AWS)
- Advanced file management features

**Success Criteria:**
- Gallery displays files with thumbnails
- Users can edit file metadata
- Search and filtering work correctly
- Cloud storage provider can be configured

### Phase 4: Enterprise Features (Week 7-8)
**Deliverables:**
- Caching implementation
- Analytics and reporting
- Bulk operations
- Advanced security features
- Performance optimization

**Success Criteria:**
- System handles large file volumes efficiently
- Admin has visibility into file usage
- Performance meets production requirements
- Security audit passes

## Support Classes

### Helper Classes and Extensions

```csharp
public class FileValidationException : Exception
{
    public FileValidationException(string message) : base(message) { }
}

public class FileValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

// Extension methods for user context
public static class UserExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
```

## JavaScript Support

### File Upload JavaScript (wwwroot/js/fileUpload.js)

```javascript
window.fileUpload = {
    initialize: function(dotnetRef) {
        // Initialize drag and drop handlers
        const dropZone = document.querySelector('.drop-zone');
        if (!dropZone) return;

        dropZone.addEventListener('dragover', function(e) {
            e.preventDefault();
            e.stopPropagation();
            this.classList.add('drag-over');
        });

        dropZone.addEventListener('dragleave', function(e) {
            e.preventDefault();
            e.stopPropagation();
            this.classList.remove('drag-over');
        });

        dropZone.addEventListener('drop', function(e) {
            e.preventDefault();
            e.stopPropagation();
            this.classList.remove('drag-over');
            
            const files = e.dataTransfer.files;
            if (files.length > 0) {
                // Trigger Blazor file input change event
                const fileInput = this.querySelector('.file-input');
                fileInput.files = files;
                
                // Manually trigger the change event
                const event = new Event('change', { bubbles: true });
                fileInput.dispatchEvent(event);
            }
        });
    }
};
```

## Testing Strategy

### Unit Tests Example

```csharp
[TestClass]
public class MediaManagementServiceTests
{
    private ApplicationDbContext _context;
    private IMediaStorageService _storageService;
    private MediaManagementService _service;

    [TestInitialize]
    public void Setup()
    {
        // Setup in-memory database and mock services
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _context = new ApplicationDbContext(options);
        _storageService = new Mock<IMediaStorageService>().Object;
        
        _service = new MediaManagementService(
            _context,
            _storageService,
            Mock.Of<IImageProcessingService>(),
            Mock.Of<IFileSecurityService>(),
            Mock.Of<ILogger<MediaManagementService>>());
    }

    [TestMethod]
    public async Task UploadFileAsync_ValidFile_CreatesFileRecord()
    {
        // Arrange
        var mockFile = CreateMockFile("test.pdf", "application/pdf", 1024);
        var userId = "user123";

        // Act
        var result = await _service.UploadFileAsync(mockFile, userId, MediaFileCategory.Document);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test.pdf", result.FileName);
        Assert.AreEqual(userId, result.UploadedByUserId);
        
        var dbFile = await _context.MediaFiles.FirstOrDefaultAsync();
        Assert.IsNotNull(dbFile);
    }

    private IFormFile CreateMockFile(string fileName, string contentType, long size)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.Length).Returns(size);
        mock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[size]));
        return mock.Object;
    }
}
```

This specification provides a comprehensive blueprint for implementing a production-ready file management system that integrates seamlessly with the existing Blazor Server template architecture. Each component is designed to follow established patterns while providing the flexibility needed for future enhancements.