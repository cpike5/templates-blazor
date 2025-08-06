using Microsoft.AspNetCore.Mvc;

namespace BlazorTemplate.Middleware
{
    /// <summary>
    /// Represents an API version
    /// </summary>
    public class ApiVersion : IEquatable<ApiVersion>, IComparable<ApiVersion>
    {
        public int MajorVersion { get; }
        public int MinorVersion { get; }

        public ApiVersion(int majorVersion, int minorVersion = 0)
        {
            MajorVersion = majorVersion;
            MinorVersion = minorVersion;
        }

        public override string ToString()
        {
            return MinorVersion == 0 ? $"{MajorVersion}" : $"{MajorVersion}.{MinorVersion}";
        }

        public bool Equals(ApiVersion? other)
        {
            return other is not null && MajorVersion == other.MajorVersion && MinorVersion == other.MinorVersion;
        }

        public override bool Equals(object? obj)
        {
            return obj is ApiVersion other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MajorVersion, MinorVersion);
        }

        public int CompareTo(ApiVersion? other)
        {
            if (other is null) return 1;
            var majorCompare = MajorVersion.CompareTo(other.MajorVersion);
            return majorCompare != 0 ? majorCompare : MinorVersion.CompareTo(other.MinorVersion);
        }

        public static bool operator ==(ApiVersion? left, ApiVersion? right)
        {
            return left?.Equals(right) ?? right is null;
        }

        public static bool operator !=(ApiVersion? left, ApiVersion? right)
        {
            return !(left == right);
        }
    }

    /// <summary>
    /// Middleware to handle API versioning through headers, query parameters, or URL segments
    /// </summary>
    public class ApiVersioningMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiVersioningMiddleware> _logger;
        private readonly ApiVersioningOptions _options;

        public ApiVersioningMiddleware(
            RequestDelegate next,
            ILogger<ApiVersioningMiddleware> logger,
            ApiVersioningOptions options)
        {
            _next = next;
            _logger = logger;
            _options = options;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only process API requests
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            var requestedVersion = GetRequestedVersion(context);
            var resolvedVersion = ResolveVersion(requestedVersion);

            // Add version information to request headers for controllers
            context.Items["ApiVersion"] = resolvedVersion;
            context.Request.Headers["X-Resolved-Api-Version"] = resolvedVersion.ToString();

            // Validate version support
            if (!_options.SupportedVersions.Contains(resolvedVersion))
            {
                await WriteVersionNotSupportedResponse(context, requestedVersion);
                return;
            }

            // Add version information to response headers
            context.Response.Headers["X-Api-Version"] = resolvedVersion.ToString();
            context.Response.Headers["X-Supported-Versions"] = string.Join(", ", _options.SupportedVersions);

            _logger.LogDebug("API request processed with version {Version} for path {Path}", 
                resolvedVersion, context.Request.Path);

            await _next(context);
        }

        private ApiVersion GetRequestedVersion(HttpContext context)
        {
            // 1. Check URL path (e.g., /api/v1/users, /api/v2/users)
            var pathSegments = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathSegments?.Length > 1 && pathSegments[1].StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseVersion(pathSegments[1], out var pathVersion))
                {
                    return pathVersion;
                }
            }

            // 2. Check query parameter (e.g., ?version=1.0, ?api-version=v1)
            if (context.Request.Query.ContainsKey("version"))
            {
                if (TryParseVersion(context.Request.Query["version"], out var queryVersion))
                {
                    return queryVersion;
                }
            }

            if (context.Request.Query.ContainsKey("api-version"))
            {
                if (TryParseVersion(context.Request.Query["api-version"], out var apiQueryVersion))
                {
                    return apiQueryVersion;
                }
            }

            // 3. Check custom header (e.g., X-API-Version: 1.0)
            if (context.Request.Headers.ContainsKey("X-API-Version"))
            {
                if (TryParseVersion(context.Request.Headers["X-API-Version"], out var headerVersion))
                {
                    return headerVersion;
                }
            }

            // 4. Check Accept header versioning (e.g., Accept: application/vnd.blazortemplate.v1+json)
            var acceptHeader = context.Request.Headers.Accept.ToString();
            if (!string.IsNullOrEmpty(acceptHeader))
            {
                var versionMatch = System.Text.RegularExpressions.Regex.Match(acceptHeader, @"\.v(\d+)(?:\.(\d+))?");
                if (versionMatch.Success)
                {
                    var major = int.Parse(versionMatch.Groups[1].Value);
                    var minor = versionMatch.Groups[2].Success ? int.Parse(versionMatch.Groups[2].Value) : 0;
                    return new ApiVersion(major, minor);
                }
            }

            // Return default version if none specified
            return _options.DefaultVersion;
        }

        private ApiVersion ResolveVersion(ApiVersion requestedVersion)
        {
            // Check if exact version is supported
            if (_options.SupportedVersions.Contains(requestedVersion))
            {
                return requestedVersion;
            }

            // Find closest supported version (backward compatibility)
            var compatibleVersion = _options.SupportedVersions
                .Where(v => v.MajorVersion == requestedVersion.MajorVersion && v.MinorVersion <= requestedVersion.MinorVersion)
                .OrderByDescending(v => v.MinorVersion)
                .FirstOrDefault();

            return compatibleVersion ?? requestedVersion;
        }

        private static bool TryParseVersion(string? versionString, out ApiVersion version)
        {
            version = new ApiVersion(1, 0);
            
            if (string.IsNullOrWhiteSpace(versionString))
                return false;

            // Remove 'v' prefix if present
            versionString = versionString.TrimStart('v', 'V');

            // Try different formats
            if (double.TryParse(versionString, out var doubleVersion))
            {
                var major = (int)Math.Floor(doubleVersion);
                var minor = (int)((doubleVersion - major) * 10);
                version = new ApiVersion(major, minor);
                return true;
            }

            if (versionString.Contains('.'))
            {
                var parts = versionString.Split('.');
                if (parts.Length >= 2 && 
                    int.TryParse(parts[0], out var major) && 
                    int.TryParse(parts[1], out var minor))
                {
                    version = new ApiVersion(major, minor);
                    return true;
                }
            }

            if (int.TryParse(versionString, out var intVersion))
            {
                version = new ApiVersion(intVersion, 0);
                return true;
            }

            return false;
        }

        private async Task WriteVersionNotSupportedResponse(HttpContext context, ApiVersion requestedVersion)
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                success = false,
                message = $"API version '{requestedVersion}' is not supported",
                errors = new[] 
                { 
                    $"Requested version: {requestedVersion}",
                    $"Supported versions: {string.Join(", ", _options.SupportedVersions)}"
                },
                timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));
            
            _logger.LogWarning("Unsupported API version {RequestedVersion} requested for path {Path}. Supported versions: {SupportedVersions}",
                requestedVersion, context.Request.Path, string.Join(", ", _options.SupportedVersions));
        }
    }

    /// <summary>
    /// Configuration options for API versioning
    /// </summary>
    public class ApiVersioningOptions
    {
        public ApiVersion DefaultVersion { get; set; } = new ApiVersion(1, 0);
        public List<ApiVersion> SupportedVersions { get; set; } = new() { new ApiVersion(1, 0) };
        public bool AssumeDefaultVersionWhenUnspecified { get; set; } = true;
    }

    /// <summary>
    /// Extension methods for API versioning
    /// </summary>
    public static class ApiVersioningExtensions
    {
        /// <summary>
        /// Add API versioning middleware to the pipeline
        /// </summary>
        public static IServiceCollection AddApiVersioning(this IServiceCollection services, Action<ApiVersioningOptions>? configure = null)
        {
            var options = new ApiVersioningOptions();
            configure?.Invoke(options);

            services.AddSingleton(options);
            return services;
        }

        /// <summary>
        /// Use API versioning middleware
        /// </summary>
        public static IApplicationBuilder UseApiVersioning(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ApiVersioningMiddleware>();
        }

        /// <summary>
        /// Get the resolved API version from the current request
        /// </summary>
        public static ApiVersion? GetApiVersion(this HttpContext context)
        {
            return context.Items["ApiVersion"] as ApiVersion;
        }

        /// <summary>
        /// Get the resolved API version from the current controller context
        /// </summary>
        public static ApiVersion? GetApiVersion(this ControllerBase controller)
        {
            return controller.HttpContext.GetApiVersion();
        }
    }
}