using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace BlazorTemplate.Data;

/// <summary>
/// Represents a media file uploaded to the system with metadata and storage information.
/// </summary>
public class MediaFile
{
    /// <summary>
    /// Unique identifier for the media file.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Original filename as uploaded by the user.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Internal storage filename (UUID-based) for security.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME type of the file.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// SHA-256 hash of the file content for deduplication.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string FileHash { get; set; } = string.Empty;

    /// <summary>
    /// Storage provider type ("Local", "AzureBlob", "S3").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string StorageProvider { get; set; } = "Local";

    /// <summary>
    /// Relative path within the storage provider.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Container or bucket name for cloud storage providers.
    /// </summary>
    [MaxLength(100)]
    public string? StorageContainer { get; set; }

    /// <summary>
    /// User-defined title for the file.
    /// </summary>
    [MaxLength(200)]
    public string? Title { get; set; }

    /// <summary>
    /// User description of the file.
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// JSON array of tags as a string.
    /// </summary>
    public string? TagsJson { get; set; }

    /// <summary>
    /// Category classification of the file.
    /// </summary>
    public MediaFileCategory Category { get; set; }

    /// <summary>
    /// Visibility setting for the file.
    /// </summary>
    public MediaFileVisibility Visibility { get; set; }

    /// <summary>
    /// Processing status of the file.
    /// </summary>
    public MediaProcessingStatus ProcessingStatus { get; set; }

    /// <summary>
    /// Image width in pixels (for image files).
    /// </summary>
    public int? ImageWidth { get; set; }

    /// <summary>
    /// Image height in pixels (for image files).
    /// </summary>
    public int? ImageHeight { get; set; }

    /// <summary>
    /// Duration for video/audio files.
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// Indicates if the file has a thumbnail generated.
    /// </summary>
    public bool HasThumbnail { get; set; }

    /// <summary>
    /// Path to the thumbnail file.
    /// </summary>
    public string? ThumbnailPath { get; set; }

    /// <summary>
    /// JSON array of available thumbnail sizes.
    /// </summary>
    public string? ThumbnailSizes { get; set; }

    /// <summary>
    /// User ID of the file uploader.
    /// </summary>
    [Required]
    public string UploadedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the user who uploaded the file.
    /// </summary>
    public ApplicationUser UploadedBy { get; set; } = null!;

    /// <summary>
    /// JSON array of role names that can access this file.
    /// </summary>
    public string? SharedWithRoles { get; set; }

    /// <summary>
    /// When the file was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the file metadata was last modified.
    /// </summary>
    public DateTime? LastModifiedAt { get; set; }

    /// <summary>
    /// When the file was last accessed.
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// Number of times the file has been accessed.
    /// </summary>
    public int AccessCount { get; set; }

    /// <summary>
    /// Gets the tags as a list from the JSON string.
    /// </summary>
    public List<string> Tags => 
        string.IsNullOrEmpty(TagsJson) 
            ? new List<string>() 
            : JsonSerializer.Deserialize<List<string>>(TagsJson) ?? new List<string>();

    /// <summary>
    /// Gets the shared roles as a list from the JSON string.
    /// </summary>
    public List<string> SharedRoles => 
        string.IsNullOrEmpty(SharedWithRoles) 
            ? new List<string>() 
            : JsonSerializer.Deserialize<List<string>>(SharedWithRoles) ?? new List<string>();
}

/// <summary>
/// Categories for file classification.
/// </summary>
public enum MediaFileCategory
{
    /// <summary>
    /// Document files (PDF, Word, etc.).
    /// </summary>
    Document,

    /// <summary>
    /// Image files (JPEG, PNG, etc.).
    /// </summary>
    Image, 

    /// <summary>
    /// Video files (MP4, AVI, etc.).
    /// </summary>
    Video,

    /// <summary>
    /// Audio files (MP3, WAV, etc.).
    /// </summary>
    Audio,

    /// <summary>
    /// Archive files (ZIP, RAR, etc.).
    /// </summary>
    Archive,

    /// <summary>
    /// Other file types.
    /// </summary>
    Other
}

/// <summary>
/// Visibility levels for media files.
/// </summary>
public enum MediaFileVisibility
{
    /// <summary>
    /// Only accessible by the uploader.
    /// </summary>
    Private,

    /// <summary>
    /// Accessible by all authenticated users.
    /// </summary>
    Public, 

    /// <summary>
    /// Accessible by specific users or roles.
    /// </summary>
    Shared
}

/// <summary>
/// Processing status for media files.
/// </summary>
public enum MediaProcessingStatus
{
    /// <summary>
    /// File upload complete, processing not started.
    /// </summary>
    Pending,

    /// <summary>
    /// File is being processed (thumbnails, etc.).
    /// </summary>
    Processing,

    /// <summary>
    /// File processing completed successfully.
    /// </summary>
    Complete,

    /// <summary>
    /// Error occurred during processing.
    /// </summary>
    Error,

    /// <summary>
    /// File has been marked for deletion.
    /// </summary>
    Deleted
}