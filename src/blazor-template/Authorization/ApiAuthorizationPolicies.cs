using Microsoft.AspNetCore.Authorization;

namespace BlazorTemplate.Authorization
{
    /// <summary>
    /// Contains authorization policy constants for API endpoints
    /// </summary>
    public static class ApiPolicies
    {
        // Role-based policies
        public const string AdminOnly = "AdminOnly";
        public const string UserOrAdmin = "UserOrAdmin";
        public const string GuestOrHigher = "GuestOrHigher";

        // Resource-based policies  
        public const string ManageOwnAccount = "ManageOwnAccount";
        public const string ViewUsers = "ViewUsers";
        public const string ManageUsers = "ManageUsers";
        public const string ViewSystemStats = "ViewSystemStats";
        public const string ManageSystem = "ManageSystem";

        // API-specific policies
        public const string ApiAccess = "ApiAccess";
        public const string ApiReadOnly = "ApiReadOnly";
        public const string ApiFullAccess = "ApiFullAccess";

        /// <summary>
        /// Configure all authorization policies
        /// </summary>
        public static void ConfigurePolicies(AuthorizationOptions options)
        {
            // Role-based policies
            options.AddPolicy(AdminOnly, policy => 
                policy.RequireRole("Administrator"));

            options.AddPolicy(UserOrAdmin, policy => 
                policy.RequireRole("User", "Administrator"));

            options.AddPolicy(GuestOrHigher, policy => 
                policy.RequireRole("Guest", "User", "Administrator"));

            // API access policies
            options.AddPolicy(ApiAccess, policy => 
                policy.RequireAuthenticatedUser()
                      .RequireRole("Guest", "User", "Administrator"));

            options.AddPolicy(ApiReadOnly, policy => 
                policy.RequireAuthenticatedUser()
                      .RequireRole("Guest", "User", "Administrator")
                      .RequireClaim("api:read"));

            options.AddPolicy(ApiFullAccess, policy => 
                policy.RequireAuthenticatedUser()
                      .RequireRole("User", "Administrator")
                      .RequireClaim("api:write"));

            // Resource management policies
            options.AddPolicy(ViewUsers, policy => 
                policy.RequireRole("Administrator"));

            options.AddPolicy(ManageUsers, policy => 
                policy.RequireRole("Administrator")
                      .RequireClaim("manage:users"));

            options.AddPolicy(ViewSystemStats, policy => 
                policy.RequireRole("Administrator"));

            options.AddPolicy(ManageSystem, policy => 
                policy.RequireRole("Administrator")
                      .RequireClaim("manage:system"));

            // Self-management policy
            options.AddPolicy(ManageOwnAccount, policy => 
                policy.RequireAuthenticatedUser()
                      .Requirements.Add(new ManageOwnAccountRequirement()));
        }
    }

    /// <summary>
    /// Requirement for managing own account
    /// </summary>
    public class ManageOwnAccountRequirement : IAuthorizationRequirement
    {
    }

    /// <summary>
    /// Handler for own account management authorization
    /// </summary>
    public class ManageOwnAccountHandler : AuthorizationHandler<ManageOwnAccountRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ManageOwnAccountRequirement requirement)
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            // Check if user is admin (can manage any account)
            if (context.User.IsInRole("Administrator"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Check if user is accessing their own account
            if (context.Resource is string targetUserId)
            {
                if (userId == targetUserId)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
            }
            else
            {
                // If no specific resource is being accessed, allow self-management
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// API security requirements attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class ApiSecurityAttribute : Attribute
    {
        public string[] RequiredRoles { get; set; } = Array.Empty<string>();
        public string[] RequiredClaims { get; set; } = Array.Empty<string>();
        public bool RequireAuthentication { get; set; } = true;
        public string? PolicyName { get; set; }

        public ApiSecurityAttribute(params string[] requiredRoles)
        {
            RequiredRoles = requiredRoles;
        }
    }
}