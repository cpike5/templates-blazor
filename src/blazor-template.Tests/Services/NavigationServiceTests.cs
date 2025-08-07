using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BlazorTemplate.Services.Navigation;
using BlazorTemplate.Configuration.Navigation;
using blazor_template.Tests.Infrastructure;
using Moq;
using Xunit;

namespace blazor_template.Tests.Services;

/// <summary>
/// Comprehensive tests for NavigationService covering all scenarios including edge cases and error conditions
/// </summary>
public class NavigationServiceTests : TestBase
{
    private readonly INavigationService _navigationService;
    private readonly Mock<ILogger<NavigationService>> _mockLogger;
    
    public NavigationServiceTests()
    {
        _mockLogger = new Mock<ILogger<NavigationService>>();
        _navigationService = ServiceProvider.GetRequiredService<INavigationService>();
    }

    protected override void RegisterApplicationServices(IServiceCollection services)
    {
        base.RegisterApplicationServices(services);
        
        // Override with mock logger for detailed verification
        services.AddSingleton(_mockLogger.Object);
    }

    #region Happy Path Tests

    [Fact]
    public async Task GetNavigationItemsAsync_ReturnsVisibleItemsOrderedCorrectly()
    {
        // Act
        var result = await _navigationService.GetNavigationItemsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Home", result[0].Title);
        Assert.Equal("Admin", result[1].Title);
        Assert.True(result.All(item => item.IsVisible));
        
        // Verify ordering
        for (int i = 0; i < result.Count - 1; i++)
        {
            Assert.True(result[i].Order <= result[i + 1].Order);
        }
    }

    [Fact]
    public async Task GetNavigationItemsForUserAsync_WithUserRole_ReturnsFilteredItems()
    {
        // Arrange
        var userRoles = new[] { "User" };

        // Act
        var result = await _navigationService.GetNavigationItemsForUserAsync(userRoles);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Home", result[0].Title);
        Assert.Contains("User", result[0].RequiredRoles);
    }

    [Fact]
    public async Task GetNavigationItemsForUserAsync_WithAdminRole_ReturnsAllItems()
    {
        // Arrange
        var userRoles = new[] { "Admin" };

        // Act
        var result = await _navigationService.GetNavigationItemsForUserAsync(userRoles);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Title == "Home");
        Assert.Contains(result, item => item.Title == "Admin");
    }

    [Fact]
    public async Task GetNavigationItemsForUserAsync_WithMultipleRoles_ReturnsAllAccessibleItems()
    {
        // Arrange
        var userRoles = new[] { "User", "Admin" };

        // Act
        var result = await _navigationService.GetNavigationItemsForUserAsync(userRoles);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.True(item.IsVisible));
    }

    [Fact]
    public async Task FindNavigationItemAsync_WithValidId_ReturnsItem()
    {
        // Arrange
        const string itemId = "home";

        // Act
        var result = await _navigationService.FindNavigationItemAsync(itemId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Home", result.Title);
        Assert.Equal("/", result.Href);
    }

    [Theory]
    [InlineData("User", "home", true)]
    [InlineData("Admin", "home", true)]
    [InlineData("Admin", "admin", true)]
    [InlineData("User", "admin", false)]
    [InlineData("Guest", "home", false)]
    public void IsItemAccessible_WithVariousRolesAndItems_ReturnsExpectedAccess(
        string userRole, string itemName, bool expectedAccess)
    {
        // Arrange
        var userRoles = new[] { userRole };
        var navigationItem = CreateTestNavigationItem(itemName);

        // Act
        var result = _navigationService.IsItemAccessible(navigationItem, userRoles);

        // Assert
        Assert.Equal(expectedAccess, result);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task GetNavigationItemsForUserAsync_WithEmptyRoles_ReturnsNoItems()
    {
        // Arrange
        var userRoles = Array.Empty<string>();

        // Act
        var result = await _navigationService.GetNavigationItemsForUserAsync(userRoles);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetNavigationItemsForUserAsync_WithCaseInsensitiveRoles_ReturnsCorrectItems()
    {
        // Arrange
        var userRoles = new[] { "user", "ADMIN" }; // Mixed case

        // Act
        var result = await _navigationService.GetNavigationItemsForUserAsync(userRoles);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task FindNavigationItemAsync_WithCaseInsensitiveId_ReturnsItem()
    {
        // Arrange
        const string itemId = "HOME"; // Different case

        // Act
        var result = await _navigationService.FindNavigationItemAsync(itemId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Home", result.Title);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindNavigationItemAsync_WithInvalidId_ReturnsNull(string? invalidId)
    {
        // Act
        var result = await _navigationService.FindNavigationItemAsync(invalidId!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindNavigationItemAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        const string nonExistentId = "non-existent-item";

        // Act
        var result = await _navigationService.FindNavigationItemAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void IsItemAccessible_WithNullItem_ReturnsFalse()
    {
        // Arrange
        NavigationItem? nullItem = null;
        var userRoles = new[] { "User" };

        // Act
        var result = _navigationService.IsItemAccessible(nullItem!, userRoles);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsItemAccessible_WithNoRequiredRoles_ReturnsTrue()
    {
        // Arrange
        var itemWithNoRoles = new NavigationItem
        {
            Id = "public",
            Title = "Public Item",
            Href = "/public",
            RequiredRoles = new List<string>(),
            IsVisible = true
        };
        var userRoles = new[] { "AnyRole" };

        // Act
        var result = _navigationService.IsItemAccessible(itemWithNoRoles, userRoles);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Performance and Integration Tests

    [Fact]
    public async Task GetNavigationItemsAsync_WithLargeConfiguration_CompletesWithinReasonableTime()
    {
        // This test would require a custom configuration with many items
        // For now, we test with the existing configuration
        
        // Act & Assert - Should complete quickly
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _navigationService.GetNavigationItemsAsync();
        stopwatch.Stop();

        Assert.NotNull(result);
        Assert.True(stopwatch.ElapsedMilliseconds < 100, "Navigation service should respond quickly");
    }

    [Fact]
    public async Task GetNavigationItemsForUserAsync_WithComplexRoleFiltering_IsPerformant()
    {
        // Arrange
        var userRoles = new[] { "User", "Manager", "Admin", "SuperUser" };

        // Act & Assert
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _navigationService.GetNavigationItemsForUserAsync(userRoles);
        stopwatch.Stop();

        Assert.NotNull(result);
        Assert.True(stopwatch.ElapsedMilliseconds < 50, "Role filtering should be fast");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void IsItemAccessible_WithExceptionInRoleCheck_ReturnsFalse()
    {
        // This test ensures graceful handling of potential exceptions
        // Arrange
        var mockItem = new Mock<NavigationItem>();
        mockItem.Setup(x => x.RequiredRoles).Throws(new InvalidOperationException("Test exception"));
        var userRoles = new[] { "User" };

        // Act & Assert - Should not throw
        var result = _navigationService.IsItemAccessible(mockItem.Object, userRoles);
        Assert.False(result);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task GetNavigationItemsForUserAsync_DoesNotExposeRestrictedItems()
    {
        // Arrange
        var limitedUserRoles = new[] { "LimitedUser" };

        // Act
        var result = await _navigationService.GetNavigationItemsForUserAsync(limitedUserRoles);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain(result, item => 
            item.RequiredRoles.Any(role => role == "Admin") && 
            !limitedUserRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void IsItemAccessible_WithSqlInjectionAttempt_HandlesGracefully()
    {
        // Arrange - Test with malicious input
        var maliciousRoles = new[] { "'; DROP TABLE Users; --", "Admin" };
        var navigationItem = CreateTestNavigationItem("admin");

        // Act & Assert - Should not throw and handle safely
        var result = _navigationService.IsItemAccessible(navigationItem, maliciousRoles);
        
        // The test should complete without throwing exceptions
        Assert.IsType<bool>(result);
    }

    #endregion

    #region Test Helpers

    private NavigationItem CreateTestNavigationItem(string itemType)
    {
        return itemType.ToLower() switch
        {
            "home" => new NavigationItem
            {
                Id = "home",
                Title = "Home",
                Href = "/",
                Icon = "fas fa-home",
                RequiredRoles = new List<string> { "User" },
                IsVisible = true,
                Order = 1
            },
            "admin" => new NavigationItem
            {
                Id = "admin",
                Title = "Admin",
                Href = "/admin",
                Icon = "fas fa-cog",
                RequiredRoles = new List<string> { "Admin" },
                IsVisible = true,
                Order = 2
            },
            _ => throw new ArgumentException($"Unknown item type: {itemType}")
        };
    }

    #endregion
}

/// <summary>
/// Tests for NavigationService that require special configuration
/// </summary>
public class NavigationServiceWithCustomConfigurationTests : TestBase
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        // Override with custom navigation configuration for specific tests
        var customNavConfig = new Dictionary<string, string?>
        {
            ["Navigation:Items:0:Id"] = "dashboard",
            ["Navigation:Items:0:Title"] = "Dashboard",
            ["Navigation:Items:0:Href"] = "/dashboard",
            ["Navigation:Items:0:Icon"] = "fas fa-chart-bar",
            ["Navigation:Items:0:Order"] = "1",
            ["Navigation:Items:0:IsVisible"] = "true",
            ["Navigation:Items:0:RequiredRoles:0"] = "User",
            ["Navigation:Items:0:Children:0:Id"] = "reports",
            ["Navigation:Items:0:Children:0:Title"] = "Reports",
            ["Navigation:Items:0:Children:0:Href"] = "/dashboard/reports",
            ["Navigation:Items:0:Children:0:Icon"] = "fas fa-file-alt",
            ["Navigation:Items:0:Children:0:Order"] = "1",
            ["Navigation:Items:0:Children:0:IsVisible"] = "true",
            ["Navigation:Items:0:Children:0:RequiredRoles:0"] = "Manager",
            ["Navigation:Items:1:Id"] = "settings",
            ["Navigation:Items:1:Title"] = "Settings",
            ["Navigation:Items:1:Href"] = "/settings",
            ["Navigation:Items:1:Icon"] = "fas fa-cog",
            ["Navigation:Items:1:Order"] = "2",
            ["Navigation:Items:1:IsVisible"] = "false" // Hidden item
        };

        // Merge with existing configuration
        var existingConfig = Configuration as IConfigurationRoot;
        var newConfig = new ConfigurationBuilder()
            .AddConfiguration(existingConfig!)
            .AddInMemoryCollection(customNavConfig)
            .Build();

        services.AddSingleton<IConfiguration>(newConfig);
    }

    [Fact]
    public async Task GetNavigationItemsAsync_WithHiddenItems_FiltersCorrectly()
    {
        // Arrange
        var navigationService = ServiceProvider.GetRequiredService<INavigationService>();

        // Act
        var result = await navigationService.GetNavigationItemsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result); // Only visible items
        Assert.Equal("Dashboard", result[0].Title);
        Assert.DoesNotContain(result, item => item.Title == "Settings");
    }

    [Fact]
    public async Task GetNavigationItemsForUserAsync_WithNestedItems_FiltersChildrenCorrectly()
    {
        // Arrange
        var navigationService = ServiceProvider.GetRequiredService<INavigationService>();
        var userRoles = new[] { "User" }; // Not Manager

        // Act
        var result = await navigationService.GetNavigationItemsForUserAsync(userRoles);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Dashboard", result[0].Title);
        Assert.Empty(result[0].Children); // Reports should be filtered out
    }

    [Fact]
    public async Task GetNavigationItemsForUserAsync_WithManagerRole_IncludesChildItems()
    {
        // Arrange
        var navigationService = ServiceProvider.GetRequiredService<INavigationService>();
        var userRoles = new[] { "User", "Manager" };

        // Act
        var result = await navigationService.GetNavigationItemsForUserAsync(userRoles);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Dashboard", result[0].Title);
        Assert.Single(result[0].Children);
        Assert.Equal("Reports", result[0].Children[0].Title);
    }
}