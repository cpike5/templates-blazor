namespace BlazorTemplate.Services.Media;

/// <summary>
/// Provides storage services for media files across different storage providers.
/// </summary>
public interface IMediaStorageService
{
    /// <summary>
    /// Stores a file in the configured storage provider.
    /// </summary>
    /// <param name="fileStream">The file stream to store.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="category">The file category for organization.</param>
    /// <param name="userId">The ID of the user uploading the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The storage path where the file was stored.</returns>
    Task<string> StoreFileAsync(Stream fileStream, string fileName, string category, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a file stream from the storage provider.
    /// </summary>
    /// <param name="storagePath">The storage path of the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file stream if found, otherwise null.</returns>
    Task<Stream?> GetFileStreamAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from the storage provider.
    /// </summary>
    /// <param name="storagePath">The storage path of the file to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the file was deleted, otherwise false.</returns>
    Task<bool> DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a URL for accessing the file.
    /// </summary>
    /// <param name="storagePath">The storage path of the file.</param>
    /// <param name="expiration">Optional expiration time for the URL.</param>
    /// <returns>The URL for accessing the file.</returns>
    Task<string> GetFileUrlAsync(string storagePath, TimeSpan? expiration = null);

    /// <summary>
    /// Checks if a file exists in the storage provider.
    /// </summary>
    /// <param name="storagePath">The storage path to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the file exists, otherwise false.</returns>
    Task<bool> FileExistsAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the size of a file in bytes.
    /// </summary>
    /// <param name="storagePath">The storage path of the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file size in bytes.</returns>
    Task<long> GetFileSizeAsync(string storagePath, CancellationToken cancellationToken = default);
}