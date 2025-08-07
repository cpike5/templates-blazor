using BlazorTemplate.Configuration.Navigation;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace BlazorTemplate.Services
{
    public interface INavigationService
    {
        Task<List<NavigationItem>> GetNavigationItemsAsync();
        Task<List<NavigationItem>> GetNavigationItemsForUserAsync(IEnumerable<string> userRoles);
        Task<NavigationItem?> FindNavigationItemAsync(string id);
        bool IsItemAccessible(NavigationItem item, IEnumerable<string> userRoles);
    }

    public class NavigationService : INavigationService
    {
        private readonly NavigationConfiguration _navigationConfig;
        private readonly ILogger<NavigationService> _logger;

        public NavigationService(IOptions<NavigationConfiguration> navigationConfig, ILogger<NavigationService> logger)
        {
            _navigationConfig = navigationConfig.Value;
            _logger = logger;
            
            _logger.LogDebug("NavigationService initialized with {ItemCount} navigation items", 
                _navigationConfig?.Items?.Count ?? 0);
        }

        /// <summary>
        /// Retrieves all visible navigation items ordered by their display order
        /// </summary>
        /// <returns>List of visible navigation items</returns>
        public Task<List<NavigationItem>> GetNavigationItemsAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            
            _logger.LogTrace("Getting all visible navigation items");
            
            try
            {
                var items = _navigationConfig.Items
                    .Where(item => item.IsVisible)
                    .OrderBy(item => item.Order)
                    .ToList();
                
                stopwatch.Stop();
                _logger.LogDebug("Retrieved {ItemCount} visible navigation items in {ElapsedMs}ms",
                    items.Count, stopwatch.ElapsedMilliseconds);
                
                return Task.FromResult(items);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to retrieve navigation items after {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Retrieves navigation items filtered by user roles and visibility
        /// </summary>
        /// <param name='userRoles'>Collection of user roles for filtering</param>
        /// <returns>List of accessible navigation items for the user</returns>
        public Task<List<NavigationItem>> GetNavigationItemsForUserAsync(IEnumerable<string> userRoles)
        {
            var stopwatch = Stopwatch.StartNew();
            
            var userRolesList = userRoles.ToList();
            _logger.LogTrace("Getting navigation items for user with roles: {UserRoles}", 
                string.Join(", ", userRolesList));
            
            try
            {
                var filteredItems = _navigationConfig.Items
                    .Where(item => item.IsVisible && IsItemAccessible(item, userRolesList))
                    .Select(item => FilterChildrenByRole(item, userRolesList))
                    .Where(item => item != null)
                    .OrderBy(item => item!.Order)
                    .ToList();
                
                stopwatch.Stop();
                _logger.LogDebug("Filtered navigation items for user: {AccessibleCount} items accessible from {TotalCount} total items (Roles: {UserRoles}) in {ElapsedMs}ms",
                    filteredItems.Count, _navigationConfig.Items.Count, string.Join(", ", userRolesList), stopwatch.ElapsedMilliseconds);
                
                return Task.FromResult(filteredItems!);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to filter navigation items for user roles {UserRoles} after {ElapsedMs}ms",
                    string.Join(", ", userRolesList), stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Finds a navigation item by its unique identifier
        /// </summary>
        /// <param name='id'>The unique identifier of the navigation item</param>
        /// <returns>The navigation item if found, null otherwise</returns>
        public Task<NavigationItem?> FindNavigationItemAsync(string id)
        {
            var stopwatch = Stopwatch.StartNew();
            
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("FindNavigationItemAsync called with null or empty id");
                return Task.FromResult<NavigationItem?>(null);
            }
            
            _logger.LogTrace("Searching for navigation item with id: {NavigationItemId}", id);
            
            try
            {
                var item = FindItemRecursive(_navigationConfig.Items, id);
                
                stopwatch.Stop();
                if (item != null)
                {
                    _logger.LogDebug("Found navigation item {NavigationItemId} (Title: {Title}) in {ElapsedMs}ms",
                        id, item.Title, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogDebug("Navigation item with id {NavigationItemId} not found after {ElapsedMs}ms search",
                        id, stopwatch.ElapsedMilliseconds);
                }
                
                return Task.FromResult(item);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to find navigation item {NavigationItemId} after {ElapsedMs}ms",
                    id, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Determines if a navigation item is accessible to a user based on their roles
        /// </summary>
        /// <param name='item'>The navigation item to check</param>
        /// <param name='userRoles'>Collection of user roles</param>
        /// <returns>True if the item is accessible, false otherwise</returns>
        public bool IsItemAccessible(NavigationItem item, IEnumerable<string> userRoles)
        {
            if (item == null)
            {
                _logger.LogWarning("IsItemAccessible called with null navigation item");
                return false;
            }
            
            try
            {
                // No role requirement means accessible to all
                if (!item.RequiredRoles.Any())
                {
                    _logger.LogTrace("Navigation item {NavigationItemId} has no role restrictions, allowing access",
                        item.Id);
                    return true;
                }
                
                var userRolesList = userRoles.ToList();
                var hasAccess = item.RequiredRoles.Any(requiredRole =>
                    userRolesList.Contains(requiredRole, StringComparer.OrdinalIgnoreCase));
                
                _logger.LogTrace("Access check for navigation item {NavigationItemId}: {HasAccess} (Required: {RequiredRoles}, User: {UserRoles})",
                    item.Id, hasAccess, string.Join(", ", item.RequiredRoles), string.Join(", ", userRolesList));
                
                return hasAccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking accessibility for navigation item {NavigationItemId}",
                    item.Id);
                return false;
            }
        }

        /// <summary>
        /// Recursively filters navigation items and their children based on user roles
        /// </summary>
        /// <param name='item'>The navigation item to filter</param>
        /// <param name='userRoles'>List of user roles for filtering</param>
        /// <returns>Filtered navigation item or null if not accessible</returns>
        private NavigationItem? FilterChildrenByRole(NavigationItem item, List<string> userRoles)
        {
            try
            {
                if (!IsItemAccessible(item, userRoles))
                {
                    _logger.LogTrace("Navigation item {NavigationItemId} not accessible to user roles: {UserRoles}",
                        item.Id, string.Join(", ", userRoles));
                    return null;
                }

                var originalChildCount = item.Children?.Count ?? 0;
                var filteredItem = new NavigationItem
                {
                    Id = item.Id,
                    Title = item.Title,
                    Href = item.Href,
                    Icon = item.Icon,
                    RequiredRoles = item.RequiredRoles,
                    IsVisible = item.IsVisible,
                    Order = item.Order,
                    Children = item.Children
                        .Select(child => FilterChildrenByRole(child, userRoles))
                        .Where(child => child != null)
                        .OrderBy(child => child!.Order)
                        .ToList()!
                };

                // If it's a group and has no accessible children, don't show it
                if (item.IsGroup && !filteredItem.Children.Any())
                {
                    _logger.LogTrace("Navigation group {NavigationItemId} has no accessible children, excluding from menu",
                        item.Id);
                    return null;
                }
                
                if (originalChildCount > 0 && filteredItem.Children.Count != originalChildCount)
                {
                    _logger.LogTrace("Navigation item {NavigationItemId} children filtered: {AccessibleChildren}/{TotalChildren} accessible",
                        item.Id, filteredItem.Children.Count, originalChildCount);
                }

                return filteredItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering children for navigation item {NavigationItemId}",
                    item.Id);
                return null;
            }
        }

        /// <summary>
        /// Recursively searches for a navigation item by ID in the navigation tree
        /// </summary>
        /// <param name='items'>List of navigation items to search</param>
        /// <param name='id'>The ID to search for</param>
        /// <returns>The found navigation item or null</returns>
        private NavigationItem? FindItemRecursive(List<NavigationItem> items, string id)
        {
            try
            {
                foreach (var item in items)
                {
                    if (string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogTrace("Found navigation item {NavigationItemId} at current level", id);
                        return item;
                    }

                    if (item.Children.Any())
                    {
                        var found = FindItemRecursive(item.Children, id);
                        if (found != null)
                        {
                            _logger.LogTrace("Found navigation item {NavigationItemId} in children of {ParentItemId}",
                                id, item.Id);
                            return found;
                        }
                    }
                }
                
                _logger.LogTrace("Navigation item {NavigationItemId} not found in current branch", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during recursive search for navigation item {NavigationItemId}", id);
                return null;
            }
        }
    }
}
