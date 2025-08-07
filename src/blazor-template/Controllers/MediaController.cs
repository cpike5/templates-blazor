using BlazorTemplate.Extensions;
using BlazorTemplate.Services.Media;
using Microsoft.AspNetCore.Mvc;

namespace BlazorTemplate.Controllers;

/// <summary>
/// Controller for serving media files with proper security and access control.
/// </summary>
[Route("media")]
public class MediaController : Controller
{
    private readonly IMediaManagementService _mediaService;
    private readonly ILogger<MediaController> _logger;

    /// <summary>
    /// Initializes a new instance of the MediaController.
    /// </summary>
    /// <param name="mediaService">Media management service.</param>
    /// <param name="logger">Logger instance.</param>
    public MediaController(
        IMediaManagementService mediaService,
        ILogger<MediaController> logger)
    {
        _mediaService = mediaService;
        _logger = logger;
    }

    /// <summary>
    /// Serves a file by its ID with proper access control.
    /// </summary>
    /// <param name="fileId">The ID of the file to serve.</param>
    /// <returns>The file content or NotFound if inaccessible.</returns>
    [HttpGet("{fileId:int}")]
    public async Task<IActionResult> GetFile(int fileId)
    {
        try
        {
            var userId = User.IsAuthenticated() ? User.GetUserId() : null;
            var file = await _mediaService.GetFileByIdAsync(fileId, userId);
            
            if (file == null)
            {
                _logger.LogWarning("File {FileId} not found or access denied for user {UserId}", fileId, userId);
                return NotFound();
            }

            var stream = await _mediaService.GetFileStreamAsync(fileId, userId);
            if (stream == null)
            {
                _logger.LogWarning("File stream not available for file {FileId}", fileId);
                return NotFound();
            }

            // Set security headers
            Response.Headers.Append("X-Content-Type-Options", "nosniff");
            Response.Headers.Append("X-Frame-Options", "DENY");
            
            // Set appropriate cache headers
            Response.Headers.Append("Cache-Control", "private, max-age=3600");
            
            _logger.LogDebug("Serving file {FileId} ({FileName}) to user {UserId}", fileId, file.FileName, userId);

            return File(stream, file.ContentType, file.FileName, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving file {FileId}", fileId);
            return StatusCode(500, "An error occurred while retrieving the file.");
        }
    }

    /// <summary>
    /// Serves a file directly by storage path (for local storage URLs).
    /// </summary>
    /// <param name="path">The URL-encoded storage path.</param>
    /// <returns>The file content or NotFound if inaccessible.</returns>
    [HttpGet("file/{*path}")]
    public async Task<IActionResult> GetFileByPath(string path)
    {
        try
        {
            var decodedPath = Uri.UnescapeDataString(path);
            _logger.LogDebug("Direct file access request for path: {Path}", decodedPath);
            
            // This endpoint should be secured to prevent direct access bypass
            // For now, we'll return NotFound to force access through the ID-based endpoint
            return NotFound("Direct file access is not allowed. Use file ID instead.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in direct file access for path {Path}", path);
            return StatusCode(500, "An error occurred while retrieving the file.");
        }
    }

    /// <summary>
    /// Serves a thumbnail for an image file.
    /// </summary>
    /// <param name="fileId">The ID of the file.</param>
    /// <param name="size">The desired thumbnail size (default: 150).</param>
    /// <returns>The thumbnail image or NotFound if unavailable.</returns>
    [HttpGet("{fileId:int}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(int fileId, int size = 150)
    {
        try
        {
            var userId = User.IsAuthenticated() ? User.GetUserId() : null;
            
            // Validate size parameter
            if (size < 50 || size > 500)
            {
                size = 150; // Default to safe size
            }

            var thumbnailStream = await _mediaService.GetThumbnailStreamAsync(fileId, userId, size);
            
            if (thumbnailStream == null)
            {
                _logger.LogDebug("Thumbnail not available for file {FileId}", fileId);
                return NotFound();
            }

            // Set cache headers for thumbnails (can be cached longer)
            Response.Headers.Append("Cache-Control", "public, max-age=7200");
            Response.Headers.Append("X-Content-Type-Options", "nosniff");
            
            return File(thumbnailStream, "image/jpeg", enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving thumbnail for file {FileId}", fileId);
            return StatusCode(500, "An error occurred while retrieving the thumbnail.");
        }
    }

    /// <summary>
    /// Forces download of a file (sets Content-Disposition header).
    /// </summary>
    /// <param name="fileId">The ID of the file to download.</param>
    /// <returns>The file content as an attachment or NotFound if inaccessible.</returns>
    [HttpGet("{fileId:int}/download")]
    public async Task<IActionResult> DownloadFile(int fileId)
    {
        try
        {
            var userId = User.IsAuthenticated() ? User.GetUserId() : null;
            var file = await _mediaService.GetFileByIdAsync(fileId, userId);
            
            if (file == null)
            {
                _logger.LogWarning("File {FileId} not found or access denied for download by user {UserId}", fileId, userId);
                return NotFound();
            }

            var stream = await _mediaService.GetFileStreamAsync(fileId, userId);
            if (stream == null)
            {
                _logger.LogWarning("File stream not available for download of file {FileId}", fileId);
                return NotFound();
            }

            // Set security headers
            Response.Headers.Append("X-Content-Type-Options", "nosniff");
            Response.Headers.Append("X-Frame-Options", "DENY");
            
            _logger.LogInformation("File {FileId} ({FileName}) downloaded by user {UserId}", fileId, file.FileName, userId);

            return File(stream, file.ContentType, file.FileName, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileId}", fileId);
            return StatusCode(500, "An error occurred while downloading the file.");
        }
    }
}