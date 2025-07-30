using BlazorTemplate.Configuration.Navigation;
using Microsoft.Extensions.Options;

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

        public NavigationService(IOptions<NavigationConfiguration> navigationConfig)
        {
            _navigationConfig = navigationConfig.Value;
        }

        public Task<List<NavigationItem>> GetNavigationItemsAsync()
        {
            return Task.FromResult(_navigationConfig.Items
                .Where(item => item.IsVisible)
                .OrderBy(item => item.Order)
                .ToList());
        }

        public Task<List<NavigationItem>> GetNavigationItemsForUserAsync(IEnumerable<string> userRoles)
        {
            var userRolesList = userRoles.ToList();
            var filteredItems = _navigationConfig.Items
                .Where(item => item.IsVisible && IsItemAccessible(item, userRolesList))
                .Select(item => FilterChildrenByRole(item, userRolesList))
                .Where(item => item != null)
                .OrderBy(item => item!.Order)
                .ToList();

            return Task.FromResult(filteredItems!);
        }

        public Task<NavigationItem?> FindNavigationItemAsync(string id)
        {
            var item = FindItemRecursive(_navigationConfig.Items, id);
            return Task.FromResult(item);
        }

        public bool IsItemAccessible(NavigationItem item, IEnumerable<string> userRoles)
        {
            if (!item.RequiredRoles.Any())
                return true; // No role requirement means accessible to all

            return item.RequiredRoles.Any(requiredRole =>
                userRoles.Contains(requiredRole, StringComparer.OrdinalIgnoreCase));
        }

        private NavigationItem? FilterChildrenByRole(NavigationItem item, List<string> userRoles)
        {
            if (!IsItemAccessible(item, userRoles))
                return null;

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
                return null;

            return filteredItem;
        }

        private NavigationItem? FindItemRecursive(List<NavigationItem> items, string id)
        {
            foreach (var item in items)
            {
                if (item.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                    return item;

                if (item.Children.Any())
                {
                    var found = FindItemRecursive(item.Children, id);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }
    }
}
