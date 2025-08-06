using System.Collections.Concurrent;
using System.Net;

namespace BlazorTemplate.Middleware
{
    /// <summary>
    /// Rate limiting middleware for API endpoints
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly RateLimitingOptions _options;
        private static readonly ConcurrentDictionary<string, ClientRateLimit> _clients = new();

        public RateLimitingMiddleware(
            RequestDelegate next,
            ILogger<RateLimitingMiddleware> logger,
            RateLimitingOptions options)
        {
            _next = next;
            _logger = logger;
            _options = options;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip rate limiting if disabled or not an API request
            if (!_options.EnableRateLimiting || !context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            var clientId = GetClientIdentifier(context);
            var endpoint = GetEndpointKey(context);
            var rateLimitRule = GetRateLimitRule(context);

            if (rateLimitRule == null)
            {
                await _next(context);
                return;
            }

            var clientLimit = _clients.GetOrAdd(clientId, _ => new ClientRateLimit());
            
            // Clean up expired entries
            clientLimit.CleanupExpiredRequests();

            // Check rate limit
            if (clientLimit.IsRateLimited(endpoint, rateLimitRule))
            {
                await WriteRateLimitExceededResponse(context, rateLimitRule, clientLimit.GetRetryAfterSeconds(endpoint, rateLimitRule));
                return;
            }

            // Record the request
            clientLimit.RecordRequest(endpoint, rateLimitRule);

            // Add rate limit headers
            AddRateLimitHeaders(context, rateLimitRule, clientLimit.GetRemainingRequests(endpoint, rateLimitRule));

            await _next(context);
        }

        private string GetClientIdentifier(HttpContext context)
        {
            // Priority: User ID > IP Address
            var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                return $"user:{userId}";
            }

            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            // Check for forwarded IP in case of proxy/load balancer
            if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    ipAddress = forwardedFor.Split(',')[0].Trim();
                }
            }
            else if (context.Request.Headers.ContainsKey("X-Real-IP"))
            {
                var realIp = context.Request.Headers["X-Real-IP"].ToString();
                if (!string.IsNullOrEmpty(realIp))
                {
                    ipAddress = realIp;
                }
            }

            return $"ip:{ipAddress}";
        }

        private string GetEndpointKey(HttpContext context)
        {
            var method = context.Request.Method;
            var path = context.Request.Path.Value ?? "";
            
            // Normalize path to remove specific IDs for grouping
            // e.g., /api/v1/users/123 -> /api/v1/users/{id}
            var normalizedPath = NormalizePath(path);
            
            return $"{method}:{normalizedPath}";
        }

        private string NormalizePath(string path)
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < segments.Length; i++)
            {
                // Replace GUID-like segments and numeric IDs with placeholders
                if (IsGuidLike(segments[i]) || IsNumeric(segments[i]))
                {
                    segments[i] = "{id}";
                }
            }
            return "/" + string.Join("/", segments);
        }

        private static bool IsGuidLike(string value)
        {
            return Guid.TryParse(value, out _);
        }

        private static bool IsNumeric(string value)
        {
            return int.TryParse(value, out _) || long.TryParse(value, out _);
        }

        private RateLimitRule? GetRateLimitRule(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";
            var method = context.Request.Method;

            // Check for auth endpoints (stricter limits)
            if (path.StartsWith("/api/v1/auth"))
            {
                return _options.AuthEndpointLimit;
            }

            // Check for admin endpoints (moderate limits)
            if (context.User?.IsInRole("Administrator") == true)
            {
                return _options.AdminUserLimit;
            }

            // Check for authenticated users
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                return _options.AuthenticatedUserLimit;
            }

            // Default for anonymous users
            return _options.AnonymousUserLimit;
        }

        private void AddRateLimitHeaders(HttpContext context, RateLimitRule rule, int remaining)
        {
            context.Response.Headers["X-RateLimit-Limit"] = rule.RequestsPerWindow.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
            context.Response.Headers["X-RateLimit-Window"] = rule.WindowSeconds.ToString();
            context.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddSeconds(rule.WindowSeconds).ToUnixTimeSeconds().ToString();
        }

        private async Task WriteRateLimitExceededResponse(HttpContext context, RateLimitRule rule, int retryAfterSeconds)
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";
            context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
            context.Response.Headers["X-RateLimit-Limit"] = rule.RequestsPerWindow.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = "0";
            context.Response.Headers["X-RateLimit-Window"] = rule.WindowSeconds.ToString();

            var errorResponse = new
            {
                success = false,
                message = "Rate limit exceeded",
                errors = new[] 
                { 
                    $"Too many requests. Limit: {rule.RequestsPerWindow} requests per {rule.WindowSeconds} seconds",
                    $"Retry after {retryAfterSeconds} seconds"
                },
                timestamp = DateTime.UtcNow,
                retryAfter = retryAfterSeconds
            };

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));

            _logger.LogWarning("Rate limit exceeded for client {ClientId} on endpoint {Endpoint}. Limit: {Limit}/{Window}s", 
                GetClientIdentifier(context), GetEndpointKey(context), rule.RequestsPerWindow, rule.WindowSeconds);
        }
    }

    /// <summary>
    /// Rate limiting rule configuration
    /// </summary>
    public class RateLimitRule
    {
        public int RequestsPerWindow { get; set; }
        public int WindowSeconds { get; set; }
        public string Name { get; set; } = string.Empty;

        public RateLimitRule() { }

        public RateLimitRule(int requestsPerWindow, int windowSeconds, string name = "")
        {
            RequestsPerWindow = requestsPerWindow;
            WindowSeconds = windowSeconds;
            Name = name;
        }

        /// <summary>
        /// Parse rate limit from string format: "100:60s" or "50:1m"
        /// </summary>
        public static RateLimitRule Parse(string rateLimit, string name = "")
        {
            var parts = rateLimit.Split(':');
            if (parts.Length != 2)
            {
                throw new ArgumentException("Invalid rate limit format. Use 'requests:duration' (e.g., '100:60s' or '50:1m')");
            }

            if (!int.TryParse(parts[0], out var requests))
            {
                throw new ArgumentException("Invalid requests count in rate limit");
            }

            var durationStr = parts[1];
            var windowSeconds = ParseDurationToSeconds(durationStr);

            return new RateLimitRule(requests, windowSeconds, name);
        }

        private static int ParseDurationToSeconds(string duration)
        {
            if (string.IsNullOrEmpty(duration))
                throw new ArgumentException("Invalid duration");

            var unit = duration.Last();
            var valueStr = duration.Substring(0, duration.Length - 1);
            
            if (!int.TryParse(valueStr, out var value))
                throw new ArgumentException("Invalid duration value");

            return unit switch
            {
                's' => value,
                'm' => value * 60,
                'h' => value * 3600,
                'd' => value * 86400,
                _ => throw new ArgumentException("Invalid duration unit. Use s, m, h, or d")
            };
        }
    }

    /// <summary>
    /// Configuration options for rate limiting
    /// </summary>
    public class RateLimitingOptions
    {
        public bool EnableRateLimiting { get; set; } = false;
        
        public RateLimitRule AnonymousUserLimit { get; set; } = new(10, 60, "Anonymous");
        public RateLimitRule AuthenticatedUserLimit { get; set; } = new(100, 60, "Authenticated");
        public RateLimitRule AdminUserLimit { get; set; } = new(500, 60, "Admin");
        public RateLimitRule AuthEndpointLimit { get; set; } = new(5, 300, "Auth"); // Stricter for auth endpoints
        
        public int CleanupIntervalSeconds { get; set; } = 300; // 5 minutes
    }

    /// <summary>
    /// Client rate limit tracking
    /// </summary>
    public class ClientRateLimit
    {
        private readonly ConcurrentDictionary<string, List<DateTime>> _requests = new();
        private readonly object _lock = new();

        public void RecordRequest(string endpoint, RateLimitRule rule)
        {
            lock (_lock)
            {
                var requests = _requests.GetOrAdd(endpoint, _ => new List<DateTime>());
                requests.Add(DateTime.UtcNow);
            }
        }

        public bool IsRateLimited(string endpoint, RateLimitRule rule)
        {
            lock (_lock)
            {
                if (!_requests.TryGetValue(endpoint, out var requests))
                    return false;

                var windowStart = DateTime.UtcNow.AddSeconds(-rule.WindowSeconds);
                var requestsInWindow = requests.Count(r => r > windowStart);
                
                return requestsInWindow >= rule.RequestsPerWindow;
            }
        }

        public int GetRemainingRequests(string endpoint, RateLimitRule rule)
        {
            lock (_lock)
            {
                if (!_requests.TryGetValue(endpoint, out var requests))
                    return rule.RequestsPerWindow;

                var windowStart = DateTime.UtcNow.AddSeconds(-rule.WindowSeconds);
                var requestsInWindow = requests.Count(r => r > windowStart);
                
                return Math.Max(0, rule.RequestsPerWindow - requestsInWindow);
            }
        }

        public int GetRetryAfterSeconds(string endpoint, RateLimitRule rule)
        {
            lock (_lock)
            {
                if (!_requests.TryGetValue(endpoint, out var requests))
                    return 0;

                var windowStart = DateTime.UtcNow.AddSeconds(-rule.WindowSeconds);
                var requestsInWindow = requests.Where(r => r > windowStart).ToList();
                
                if (requestsInWindow.Count < rule.RequestsPerWindow)
                    return 0;

                var oldestRequest = requestsInWindow.Min();
                var secondsUntilReset = (int)(oldestRequest.AddSeconds(rule.WindowSeconds) - DateTime.UtcNow).TotalSeconds;
                
                return Math.Max(1, secondsUntilReset);
            }
        }

        public void CleanupExpiredRequests()
        {
            var cutoff = DateTime.UtcNow.AddHours(-1); // Keep last hour of data
            
            lock (_lock)
            {
                foreach (var endpoint in _requests.Keys.ToList())
                {
                    if (_requests.TryGetValue(endpoint, out var requests))
                    {
                        requests.RemoveAll(r => r < cutoff);
                        
                        // Remove empty endpoint entries
                        if (requests.Count == 0)
                        {
                            _requests.TryRemove(endpoint, out _);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Extension methods for rate limiting
    /// </summary>
    public static class RateLimitingExtensions
    {
        /// <summary>
        /// Add rate limiting services
        /// </summary>
        public static IServiceCollection AddRateLimiting(this IServiceCollection services, Action<RateLimitingOptions>? configure = null)
        {
            var options = new RateLimitingOptions();
            configure?.Invoke(options);

            services.AddSingleton(options);
            return services;
        }

        /// <summary>
        /// Use rate limiting middleware
        /// </summary>
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RateLimitingMiddleware>();
        }
    }
}