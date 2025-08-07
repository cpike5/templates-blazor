namespace BlazorTemplate.Models.Media;

/// <summary>
/// Exception thrown when file validation fails.
/// </summary>
public class FileValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the FileValidationException class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public FileValidationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the FileValidationException class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public FileValidationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when file storage operations fail.
/// </summary>
public class FileStorageException : Exception
{
    /// <summary>
    /// Initializes a new instance of the FileStorageException class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public FileStorageException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the FileStorageException class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public FileStorageException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when file access is denied.
/// </summary>
public class FileAccessDeniedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the FileAccessDeniedException class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public FileAccessDeniedException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the FileAccessDeniedException class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public FileAccessDeniedException(string message, Exception innerException) : base(message, innerException) { }
}