using System.Security.Claims;

namespace BlazorTemplate.Extensions;

/// <summary>
/// Extension methods for ClaimsPrincipal to simplify user context operations.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the user ID from the ClaimsPrincipal.
    /// </summary>
    /// <param name="user">The ClaimsPrincipal instance.</param>
    /// <returns>The user ID if found, otherwise null.</returns>
    public static string? GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Gets the user email from the ClaimsPrincipal.
    /// </summary>
    /// <param name="user">The ClaimsPrincipal instance.</param>
    /// <returns>The user email if found, otherwise null.</returns>
    public static string? GetUserEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Gets the user name from the ClaimsPrincipal.
    /// </summary>
    /// <param name="user">The ClaimsPrincipal instance.</param>
    /// <returns>The user name if found, otherwise null.</returns>
    public static string? GetUserName(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value;
    }

    /// <summary>
    /// Checks if the user is authenticated.
    /// </summary>
    /// <param name="user">The ClaimsPrincipal instance.</param>
    /// <returns>True if authenticated, otherwise false.</returns>
    public static bool IsAuthenticated(this ClaimsPrincipal user)
    {
        return user.Identity?.IsAuthenticated == true;
    }
}