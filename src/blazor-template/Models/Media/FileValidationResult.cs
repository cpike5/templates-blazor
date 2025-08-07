namespace BlazorTemplate.Models.Media;

/// <summary>
/// Result of file validation operations.
/// </summary>
public class FileValidationResult
{
    /// <summary>
    /// Indicates if the file passed validation.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// List of validation warnings that don't prevent upload.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>Valid file validation result.</returns>
    public static FileValidationResult Success()
    {
        return new FileValidationResult { IsValid = true };
    }

    /// <summary>
    /// Creates a failed validation result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing why validation failed.</param>
    /// <returns>Invalid file validation result.</returns>
    public static FileValidationResult Failure(string errorMessage)
    {
        return new FileValidationResult 
        { 
            IsValid = false, 
            ErrorMessage = errorMessage 
        };
    }

    /// <summary>
    /// Creates a successful validation result with warnings.
    /// </summary>
    /// <param name="warnings">List of warning messages.</param>
    /// <returns>Valid file validation result with warnings.</returns>
    public static FileValidationResult SuccessWithWarnings(List<string> warnings)
    {
        return new FileValidationResult 
        { 
            IsValid = true, 
            Warnings = warnings 
        };
    }
}