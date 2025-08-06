using BlazorTemplate.DTO.Api;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;

namespace BlazorTemplate.Middleware
{
    /// <summary>
    /// Global error handling middleware for API endpoints
    /// </summary>
    public class ApiErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiErrorHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiErrorHandlingMiddleware(
            RequestDelegate next,
            ILogger<ApiErrorHandlingMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Only handle exceptions for API requests
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    await HandleApiExceptionAsync(context, ex);
                }
                else
                {
                    // Let other middleware handle non-API exceptions
                    throw;
                }
            }
        }

        private async Task HandleApiExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "API Exception occurred for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            var (statusCode, error) = MapExceptionToApiResponse(exception);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            // Add correlation ID for tracking
            var correlationId = context.TraceIdentifier;
            context.Response.Headers["X-Correlation-ID"] = correlationId;

            var response = new ApiResponse<object>
            {
                Success = false,
                Message = error.Message,
                Errors = error.Details.ToList(),
                Timestamp = DateTime.UtcNow
            };

            // Add debug information in development
            if (_environment.IsDevelopment())
            {
                response.Errors.Add($"Exception Type: {exception.GetType().Name}");
                if (!string.IsNullOrEmpty(exception.StackTrace))
                {
                    response.Errors.Add($"Stack Trace: {exception.StackTrace}");
                }
                
                // Add inner exception details
                var innerEx = exception.InnerException;
                while (innerEx != null)
                {
                    response.Errors.Add($"Inner Exception: {innerEx.Message}");
                    innerEx = innerEx.InnerException;
                }
            }

            var jsonResponse = JsonSerializer.Serialize(response, _jsonOptions);
            await context.Response.WriteAsync(jsonResponse);

            // Log structured error information
            LogStructuredError(context, exception, statusCode, correlationId);
        }

        private (int statusCode, ErrorDetails error) MapExceptionToApiResponse(Exception exception)
        {
            return exception switch
            {
                ValidationException validationEx => (
                    (int)HttpStatusCode.BadRequest,
                    new ErrorDetails("Validation failed", new[] { validationEx.Message })
                ),
                
                ArgumentNullException argumentNullEx => (
                    (int)HttpStatusCode.BadRequest,
                    new ErrorDetails("Missing required parameter", new[] { argumentNullEx.Message })
                ),
                
                ArgumentException argumentEx => (
                    (int)HttpStatusCode.BadRequest,
                    new ErrorDetails("Invalid argument", new[] { argumentEx.Message })
                ),
                
                UnauthorizedAccessException => (
                    (int)HttpStatusCode.Unauthorized,
                    new ErrorDetails("Unauthorized access", new[] { "Authentication required" })
                ),
                
                InvalidOperationException invalidOpEx => (
                    (int)HttpStatusCode.Conflict,
                    new ErrorDetails("Invalid operation", new[] { invalidOpEx.Message })
                ),
                
                KeyNotFoundException => (
                    (int)HttpStatusCode.NotFound,
                    new ErrorDetails("Resource not found", new[] { "The requested resource was not found" })
                ),
                
                TimeoutException => (
                    (int)HttpStatusCode.RequestTimeout,
                    new ErrorDetails("Request timeout", new[] { "The request timed out" })
                ),
                
                TaskCanceledException => (
                    (int)HttpStatusCode.RequestTimeout,
                    new ErrorDetails("Request canceled", new[] { "The request was canceled" })
                ),
                
                NotImplementedException => (
                    (int)HttpStatusCode.NotImplemented,
                    new ErrorDetails("Feature not implemented", new[] { "This feature is not yet implemented" })
                ),


                // Database-related exceptions
                Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => (
                    (int)HttpStatusCode.Conflict,
                    new ErrorDetails("Concurrency conflict", new[] { "The resource was modified by another user" })
                ),

                Microsoft.EntityFrameworkCore.DbUpdateException dbEx => (
                    (int)HttpStatusCode.Conflict,
                    new ErrorDetails("Database update failed", new[] { GetDatabaseErrorMessage(dbEx) })
                ),

                // Remove non-existent IdentityException

                // JWT exceptions (SecurityTokenExpiredException and SecurityTokenValidationException inherit from SecurityTokenException)
                Microsoft.IdentityModel.Tokens.SecurityTokenExpiredException => (
                    (int)HttpStatusCode.Unauthorized,
                    new ErrorDetails("Token expired", new[] { "The authentication token has expired" })
                ),

                Microsoft.IdentityModel.Tokens.SecurityTokenValidationException tokenValidationEx => (
                    (int)HttpStatusCode.Unauthorized,
                    new ErrorDetails("Token validation failed", new[] { tokenValidationEx.Message })
                ),

                Microsoft.IdentityModel.Tokens.SecurityTokenException tokenEx => (
                    (int)HttpStatusCode.Unauthorized,
                    new ErrorDetails("Invalid token", new[] { tokenEx.Message })
                ),

                // Default for unhandled exceptions
                _ => (
                    (int)HttpStatusCode.InternalServerError,
                    new ErrorDetails(
                        _environment.IsDevelopment() 
                            ? $"Internal server error: {exception.Message}" 
                            : "An unexpected error occurred",
                        _environment.IsDevelopment() 
                            ? new[] { exception.Message } 
                            : new[] { "Please contact support if the problem persists" }
                    )
                )
            };
        }

        private string GetDatabaseErrorMessage(Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            // Check for common database constraint violations
            var innerException = dbEx.InnerException?.Message ?? dbEx.Message;
            
            if (innerException.Contains("UNIQUE constraint", StringComparison.OrdinalIgnoreCase) ||
                innerException.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
            {
                return "A record with this information already exists";
            }
            
            if (innerException.Contains("FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase))
            {
                return "Cannot complete operation due to related data dependencies";
            }
            
            if (innerException.Contains("DELETE statement conflicted", StringComparison.OrdinalIgnoreCase))
            {
                return "Cannot delete record because it is referenced by other data";
            }

            return _environment.IsDevelopment() ? innerException : "A database error occurred";
        }

        private void LogStructuredError(HttpContext context, Exception exception, int statusCode, string correlationId)
        {
            var errorDetails = new
            {
                CorrelationId = correlationId,
                RequestId = context.TraceIdentifier,
                Method = context.Request.Method,
                Path = context.Request.Path.Value,
                QueryString = context.Request.QueryString.Value,
                StatusCode = statusCode,
                Exception = exception.GetType().Name,
                Message = exception.Message,
                UserId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString(),
                Timestamp = DateTime.UtcNow
            };

            if (statusCode >= 500)
            {
                _logger.LogError("API Error {@ErrorDetails}", errorDetails);
            }
            else
            {
                _logger.LogWarning("API Warning {@ErrorDetails}", errorDetails);
            }
        }

        private record ErrorDetails(string Message, string[] Details);
    }

    /// <summary>
    /// Extensions for registering the API error handling middleware
    /// </summary>
    public static class ApiErrorHandlingExtensions
    {
        /// <summary>
        /// Add API error handling middleware to the pipeline
        /// </summary>
        public static IApplicationBuilder UseApiErrorHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ApiErrorHandlingMiddleware>();
        }
    }

    /// <summary>
    /// Custom exceptions for API operations
    /// </summary>
    public class ApiException : Exception
    {
        public int StatusCode { get; }
        public string[] Details { get; }

        public ApiException(int statusCode, string message, params string[] details) : base(message)
        {
            StatusCode = statusCode;
            Details = details ?? Array.Empty<string>();
        }

        public ApiException(HttpStatusCode statusCode, string message, params string[] details) 
            : this((int)statusCode, message, details)
        {
        }
    }

    /// <summary>
    /// Business logic validation exception
    /// </summary>
    public class BusinessValidationException : ApiException
    {
        public BusinessValidationException(string message, params string[] details)
            : base(HttpStatusCode.BadRequest, message, details)
        {
        }
    }

    /// <summary>
    /// Resource not found exception
    /// </summary>
    public class ResourceNotFoundException : ApiException
    {
        public ResourceNotFoundException(string resourceType, string identifier)
            : base(HttpStatusCode.NotFound, $"{resourceType} with identifier '{identifier}' was not found")
        {
        }

        public ResourceNotFoundException(string message)
            : base(HttpStatusCode.NotFound, message)
        {
        }
    }

    /// <summary>
    /// Resource conflict exception
    /// </summary>
    public class ResourceConflictException : ApiException
    {
        public ResourceConflictException(string message, params string[] details)
            : base(HttpStatusCode.Conflict, message, details)
        {
        }
    }
}