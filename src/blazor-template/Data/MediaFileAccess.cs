using System.ComponentModel.DataAnnotations;

namespace BlazorTemplate.Data;

/// <summary>
/// Tracks detailed access permissions for media files.
/// </summary>
public class MediaFileAccess
{
    /// <summary>
    /// Unique identifier for the access record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID of the media file this access record applies to.
    /// </summary>
    public int MediaFileId { get; set; }

    /// <summary>
    /// Navigation property to the media file.
    /// </summary>
    public MediaFile MediaFile { get; set; } = null!;

    /// <summary>
    /// Specific user ID granted access (optional - use for individual user permissions).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Navigation property to the user granted access.
    /// </summary>
    public ApplicationUser? User { get; set; }

    /// <summary>
    /// Role name granted access (optional - use for role-based permissions).
    /// </summary>
    [MaxLength(256)]
    public string? Role { get; set; }

    /// <summary>
    /// Type of permission granted.
    /// </summary>
    public MediaAccessPermission Permission { get; set; }

    /// <summary>
    /// When the access was granted.
    /// </summary>
    public DateTime GrantedAt { get; set; }

    /// <summary>
    /// Optional expiration time for the access.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// ID of the user who granted this access.
    /// </summary>
    [Required]
    public string GrantedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the user who granted the access.
    /// </summary>
    public ApplicationUser GrantedByUser { get; set; } = null!;

    /// <summary>
    /// When the access was revoked (if applicable).
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// ID of the user who revoked this access.
    /// </summary>
    public string? RevokedByUserId { get; set; }

    /// <summary>
    /// Navigation property to the user who revoked the access.
    /// </summary>
    public ApplicationUser? RevokedByUser { get; set; }

    /// <summary>
    /// Indicates if this access grant is currently active.
    /// </summary>
    public bool IsActive => RevokedAt == null && (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);
}

/// <summary>
/// Types of permissions that can be granted for media files.
/// </summary>
public enum MediaAccessPermission
{
    /// <summary>
    /// Permission to view the file.
    /// </summary>
    View,

    /// <summary>
    /// Permission to download the file.
    /// </summary>
    Download,

    /// <summary>
    /// Permission to edit file metadata.
    /// </summary>
    Edit,

    /// <summary>
    /// Permission to delete the file.
    /// </summary>
    Delete,

    /// <summary>
    /// Permission to share the file with others.
    /// </summary>
    Share
}