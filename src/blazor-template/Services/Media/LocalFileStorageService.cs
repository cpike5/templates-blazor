using BlazorTemplate.Configuration;
using BlazorTemplate.Models.Media;
using Microsoft.Extensions.Options;

namespace BlazorTemplate.Services.Media;

/// <summary>
/// Implementation of IMediaStorageService for local file system storage.
/// </summary>
public class LocalFileStorageService : IMediaStorageService
{
    private readonly IOptions<ConfigurationOptions> _config;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _basePath;

    /// <summary>
    /// Initializes a new instance of the LocalFileStorageService.
    /// </summary>
    /// <param name="config">Configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public LocalFileStorageService(IOptions<ConfigurationOptions> config, ILogger<LocalFileStorageService> logger)
    {
        _config = config;
        _logger = logger;
        _basePath = Path.GetFullPath(_config.Value.FileManagement.LocalStorage.RootPath);
        
        // Ensure directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation("Created upload directory at {BasePath}", _basePath);
        }
    }

    /// <inheritdoc />
    public async Task<string> StoreFileAsync(Stream fileStream, string fileName, string category, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var relativePath = GenerateStoragePath(fileName, category, userId);
            var fullPath = Path.Combine(_basePath, relativePath);
            
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogDebug("Created directory {Directory}", directory);
            }

            using var fileStreamDestination = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
            await fileStream.CopyToAsync(fileStreamDestination, cancellationToken);
            
            _logger.LogInformation("File stored at {RelativePath} for user {UserId}", relativePath, userId);
            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store file {FileName} for user {UserId}", fileName, userId);
            throw new FileStorageException($"Failed to store file: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Stream?> GetFileStreamAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, storagePath);
            
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found at {StoragePath}", storagePath);
                return null;
            }

            // Use FileShare.Read to allow concurrent reads
            return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve file stream for {StoragePath}", storagePath);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, storagePath);
            
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found for deletion at {StoragePath}", storagePath);
                return false;
            }

            File.Delete(fullPath);
            _logger.LogInformation("File deleted at {StoragePath}", storagePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file at {StoragePath}", storagePath);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<string> GetFileUrlAsync(string storagePath, TimeSpan? expiration = null)
    {
        // For local storage, return a relative URL that will be handled by MediaController
        // URL encode the storage path to handle special characters
        return $"/media/file/{Uri.EscapeDataString(storagePath)}";
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, storagePath);
            return File.Exists(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if file exists at {StoragePath}", storagePath);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<long> GetFileSizeAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, storagePath);
            
            if (!File.Exists(fullPath))
            {
                return 0;
            }

            return new FileInfo(fullPath).Length;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file size for {StoragePath}", storagePath);
            return 0;
        }
    }

    /// <summary>
    /// Generates the storage path for a file based on configuration and user context.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="category">The file category.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>A relative storage path.</returns>
    private string GenerateStoragePath(string fileName, string category, string userId)
    {
        var pathSegments = new List<string> { userId, category.ToLowerInvariant() };
        
        // Add date organization if enabled
        if (_config.Value.FileManagement.LocalStorage.OrganizeByDate)
        {
            var dateFolder = DateTime.UtcNow.ToString("yyyy/MM/dd");
            pathSegments.Add(dateFolder);
        }
        
        pathSegments.Add(fileName);
        
        return Path.Combine(pathSegments.ToArray());
    }
}