# File Management & Media System

## Overview

The File Management & Media System provides comprehensive file upload, storage, and management capabilities with role-based access control. The system is designed for security, scalability, and ease of use, supporting multiple file types with metadata management and thumbnail generation.

## Architecture

### Core Components

The file management system consists of several key components:

- **MediaManagementService** - Primary service for file operations
- **FileSecurityService** - Security validation and access control
- **LocalFileStorageService** - Storage provider implementation
- **MediaController** - Secure file serving endpoint
- **MediaFile Entity** - Database model for file metadata
- **MediaFileAccess Entity** - Access permissions tracking
- **FileUploadComponent** - Blazor component for file uploads
- **Media.razor** - File management page

### Service Layer Architecture

```
MediaManagementService
├── File Upload & Processing
├── Metadata Management
├── Access Control Integration
└── Storage Provider Abstraction
    └── LocalFileStorageService
        ├── Physical File Storage
        ├── Folder Organization
        └── Thumbnail Generation

FileSecurityService
├── File Validation
├── MIME Type Checking
├── Size Limit Enforcement
├── Malware Scanning (Basic)
└── Access Authorization
```

## Database Schema

### MediaFile Entity

The primary entity for storing file metadata:

```csharp
public class MediaFile
{
    public int Id { get; set; }
    public string FileName { get; set; }          // Original filename
    public string StoredFileName { get; set; }    // Internal UUID-based name
    public string ContentType { get; set; }       // MIME type
    public long FileSize { get; set; }            // Size in bytes
    public string FileHash { get; set; }          // SHA-256 hash
    public string StorageProvider { get; set; }   // "Local", "Azure", "S3"
    public string StoragePath { get; set; }       // Relative path
    public string? StorageContainer { get; set; } // Container/bucket name
    
    // Metadata
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? TagsJson { get; set; }         // JSON array of tags
    
    // Classification
    public MediaFileCategory Category { get; set; }
    public MediaFileVisibility Visibility { get; set; }
    public MediaProcessingStatus ProcessingStatus { get; set; }
    
    // Media Properties
    public int? ImageWidth { get; set; }
    public int? ImageHeight { get; set; }
    public TimeSpan? Duration { get; set; }
    public bool HasThumbnail { get; set; }
    public string? ThumbnailPath { get; set; }
    
    // Access & Tracking
    public string UploadedByUserId { get; set; }
    public string? SharedWithRoles { get; set; }   // JSON array of roles
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public int AccessCount { get; set; }
}
```

### MediaFileAccess Entity

Tracks individual file access permissions:

```csharp
public class MediaFileAccess
{
    public int Id { get; set; }
    public int MediaFileId { get; set; }
    public string UserId { get; set; }
    public string? RoleName { get; set; }
    public MediaAccessLevel AccessLevel { get; set; }
    public DateTime GrantedAt { get; set; }
    public string GrantedByUserId { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
```

### Enumerations

```csharp
public enum MediaFileCategory
{
    Document, Image, Video, Audio, Archive, Other
}

public enum MediaFileVisibility
{
    Private,  // Only uploader can access
    Public,   // All authenticated users
    Shared    // Specific users/roles
}

public enum MediaProcessingStatus
{
    Pending, Processing, Complete, Error, Deleted
}

public enum MediaAccessLevel
{
    Read, ReadWrite, FullControl
}
```

## Configuration

### File Management Options

Configuration is defined in `ConfigurationOptions.cs`:

```csharp
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
    public string[] BlockedExtensions { get; set; } = {
        ".exe", ".bat", ".cmd", ".scr", ".pif"
    };
    public bool EnableVirusScanning { get; set; } = false;
    public bool ValidateFileHeaders { get; set; } = true;
}

public class ProcessingOptions
{
    public bool GenerateThumbnails { get; set; } = true;
    public int[] ThumbnailSizes { get; set; } = { 150, 300, 600 };
    public int MaxImageWidth { get; set; } = 4000;
    public int MaxImageHeight { get; set; } = 4000;
    public bool OptimizeImages { get; set; } = true;
    public int JpegQuality { get; set; } = 85;
}
```

### Configuration in appsettings.json

```json
{
  "Site": {
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
        "BlockedExtensions": [".exe", ".bat", ".cmd", ".scr", ".pif"],
        "EnableVirusScanning": false,
        "ValidateFileHeaders": true
      },
      "Processing": {
        "GenerateThumbnails": true,
        "ThumbnailSizes": [150, 300, 600],
        "MaxImageWidth": 4000,
        "MaxImageHeight": 4000,
        "OptimizeImages": true,
        "JpegQuality": 85
      }
    }
  }
}
```

## Features

### File Upload

- **Multiple File Support**: Upload multiple files simultaneously
- **Drag & Drop Interface**: Modern file upload experience
- **Progress Tracking**: Real-time upload progress indication
- **Client-Side Validation**: Immediate feedback on file restrictions
- **Server-Side Validation**: Comprehensive security checks

### Security Features

- **MIME Type Validation**: Checks file headers against MIME types
- **File Size Limits**: Configurable per-file and total storage limits
- **Extension Blocking**: Prevents upload of dangerous file types
- **Hash-Based Deduplication**: SHA-256 hashing prevents duplicate storage
- **Secure File Serving**: Files served through protected endpoints
- **Access Control**: Role-based and user-specific permissions

### Storage Management

- **Organized Structure**: Files organized by date (YYYY/MM/DD)
- **UUID Naming**: Internal files use UUID names for security
- **Thumbnail Generation**: Automatic thumbnails for image files
- **Storage Abstraction**: Pluggable storage providers (Local, Azure, S3)
- **Cleanup Operations**: Scheduled cleanup of deleted files

### Access Control

- **Visibility Levels**: Private, Public, and Shared access modes
- **Role-Based Sharing**: Share files with specific roles
- **User Permissions**: Grant access to individual users
- **Access Tracking**: Monitor file access patterns
- **Expiring Permissions**: Time-limited access grants

### Metadata Management

- **Rich Metadata**: Titles, descriptions, and tags
- **File Categorization**: Document, Image, Video, Audio, Archive, Other
- **Search Capabilities**: Search by filename, title, tags, or content type
- **Version Tracking**: Track file modifications and access history

## API Endpoints

### MediaController

The system provides secure file access through the MediaController:

```csharp
[Route("api/media")]
public class MediaController : ControllerBase
{
    [HttpGet("{fileId}")]
    public async Task<IActionResult> GetFile(int fileId)
    
    [HttpGet("{fileId}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(int fileId, int size = 150)
}
```

### Usage Examples

```csharp
// Serve file with access control
GET /api/media/123
Authorization: Bearer {jwt-token}

// Get thumbnail
GET /api/media/123/thumbnail?size=300
Authorization: Bearer {jwt-token}
```

## Integration with Navigation

The file management system integrates with the existing navigation system:

```json
{
  "Navigation": {
    "Items": [
      {
        "Id": "media-files",
        "Title": "Media Files",
        "Href": "/media",
        "Icon": "fas fa-file",
        "RequiredRoles": [],
        "Order": 30
      }
    ]
  }
}
```

## Usage Guide

### For End Users

1. **Upload Files**:
   - Navigate to the Media Files page
   - Drag files onto the upload area or click to browse
   - Add titles, descriptions, and tags as needed
   - Set visibility (Private, Public, or Shared)

2. **Manage Files**:
   - View all uploaded files in a paginated list
   - Edit metadata (title, description, tags)
   - Change visibility settings
   - Delete files (soft delete)

3. **Share Files**:
   - Set files to Public for all users to access
   - Share with specific roles using Shared visibility
   - Copy direct links to files (for authorized access)

### For Developers

1. **Service Registration**:
   ```csharp
   // In Program.cs
   builder.Services.AddScoped<IMediaManagementService, MediaManagementService>();
   builder.Services.AddScoped<IFileSecurityService, FileSecurityService>();
   builder.Services.AddScoped<IMediaStorageService, LocalFileStorageService>();
   ```

2. **Upload Files Programmatically**:
   ```csharp
   var mediaFile = await _mediaService.UploadFileAsync(
       file, userId, MediaFileCategory.Document, 
       title: "My Document", 
       description: "Important document"
   );
   ```

3. **Retrieve Files**:
   ```csharp
   var file = await _mediaService.GetFileByIdAsync(fileId, userId);
   var stream = await _mediaService.GetFileStreamAsync(fileId, userId);
   ```

4. **Custom Storage Providers**:
   ```csharp
   // Implement IMediaStorageService for cloud storage
   public class AzureBlobStorageService : IMediaStorageService
   {
       // Implementation for Azure Blob Storage
   }
   ```

## Security Considerations

### File Validation

- All files are validated against allowed MIME types
- File headers are checked to prevent MIME type spoofing
- Dangerous file extensions are blocked by default
- File size limits prevent storage exhaustion

### Access Control

- Files are served through protected endpoints only
- User authentication is required for private files
- Role-based access control for shared files
- Access logging for audit purposes

### Storage Security

- Files stored with UUID names to prevent enumeration
- Original filenames stored separately in database
- Physical files organized in date-based folders
- Regular cleanup of orphaned files

## Maintenance

### Database Maintenance

- Regular cleanup of deleted files (soft deletes)
- Archive old access logs to maintain performance
- Monitor storage usage and apply limits

### File System Maintenance

- Scheduled cleanup of physically deleted files
- Thumbnail regeneration for corrupted images
- Storage space monitoring and alerts

### Performance Optimization

- Database indexing on frequently queried fields
- Thumbnail caching for improved performance
- File stream caching for repeated access

## Future Enhancements

### Phase 2 Planned Features

- **Cloud Storage Integration**: Azure Blob Storage, AWS S3
- **Advanced Image Processing**: Automatic resizing, format conversion
- **Video Processing**: Thumbnail generation from videos
- **Full-Text Search**: Search within document contents
- **File Versioning**: Track multiple versions of files
- **Bulk Operations**: Upload multiple files with metadata
- **API Endpoints**: RESTful API for programmatic access
- **Admin Dashboard**: File management and monitoring tools

### Possible Extensions

- **CDN Integration**: Content delivery network support
- **Virus Scanning**: Integration with malware detection services
- **Watermarking**: Automatic watermark application
- **File Conversion**: Convert between file formats
- **Archive Management**: Extract and manage archive contents

## Troubleshooting

### Common Issues

1. **Upload Failures**:
   - Check file size limits in configuration
   - Verify MIME type is in allowed list
   - Ensure storage directory has write permissions

2. **Access Denied**:
   - Verify user authentication
   - Check file visibility settings
   - Confirm role-based permissions

3. **Thumbnail Generation**:
   - Ensure image processing libraries are installed
   - Check file format support
   - Verify storage permissions for thumbnail directory

4. **Storage Issues**:
   - Monitor disk space usage
   - Check storage path configuration
   - Verify file system permissions

### Logging

The system uses Serilog for structured logging:

```csharp
// File upload logging
_logger.LogInformation("File uploaded: {FileName} by user {UserId}", 
    fileName, userId);

// Access control logging
_logger.LogWarning("Unauthorized file access attempt: {FileId} by user {UserId}", 
    fileId, userId);

// Error logging
_logger.LogError(ex, "Failed to upload file: {FileName}", fileName);
```

## Configuration Examples

### Development Configuration

```json
{
  "Site": {
    "FileManagement": {
      "LocalStorage": {
        "RootPath": "wwwroot/uploads",
        "OrganizeByDate": false,
        "MaxStorageBytes": 1073741824
      },
      "Security": {
        "MaxFileSizeBytes": 10485760,
        "EnableVirusScanning": false,
        "ValidateFileHeaders": false
      },
      "Processing": {
        "GenerateThumbnails": true,
        "OptimizeImages": false
      }
    }
  }
}
```

### Production Configuration

```json
{
  "Site": {
    "FileManagement": {
      "LocalStorage": {
        "RootPath": "/var/app_data/uploads",
        "OrganizeByDate": true,
        "MaxStorageBytes": 107374182400
      },
      "Security": {
        "MaxFileSizeBytes": 104857600,
        "EnableVirusScanning": true,
        "ValidateFileHeaders": true
      },
      "Processing": {
        "GenerateThumbnails": true,
        "OptimizeImages": true,
        "JpegQuality": 75
      }
    }
  }
}
```

This documentation covers the complete implementation of Phase 1 of the File Management & Media System, providing a solid foundation for file upload, storage, and management within the Blazor template application.