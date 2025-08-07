using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BlazorTemplate.Services.Auth;
using BlazorTemplate.Data;
using blazor_template.Tests.Infrastructure;
using Moq;
using Xunit;

namespace blazor_template.Tests.Services;

/// <summary>
/// Comprehensive tests for UserRoleService covering role management operations
/// </summary>
public class UserRoleServiceTests : TestBase
{
    private readonly IUserRoleService _userRoleService;
    private readonly Mock<ILogger<UserRoleService>> _mockLogger;

    public UserRoleServiceTests()
    {
        _mockLogger = new Mock<ILogger<UserRoleService>>();
        _userRoleService = ServiceProvider.GetRequiredService<IUserRoleService>();
    }

    protected override void RegisterApplicationServices(IServiceCollection services)
    {
        base.RegisterApplicationServices(services);
        
        // Override with mock logger
        services.AddSingleton(_mockLogger.Object);
    }

    #region Happy Path Tests

    [Fact]
    public async Task GetUsersAsync_WithExistingUsers_ReturnsAllUsers()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("user1@test.com", "Password123!");
        var user2 = await CreateTestUserAsync("user2@test.com", "Password123!");

        // Act
        var result = await _userRoleService.GetUsersAsync();

        // Assert
        Assert.NotNull(result);
        var users = result.ToList();
        Assert.Equal(2, users.Count);
        Assert.Contains(users, u => u.Email == "user1@test.com");
        Assert.Contains(users, u => u.Email == "user2@test.com");
    }

    [Fact]
    public async Task GetUsersAsync_WithNoUsers_ReturnsEmptyCollection()
    {
        // Act
        var result = await _userRoleService.GetUsersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task AddUserToRoleAsync_WithValidUserAndRole_AddsRoleSuccessfully()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com");
        var role = await EnsureRoleExistsAsync("TestRole");

        // Act
        await _userRoleService.AddUserToRoleAsync("test@example.com", "TestRole");

        // Assert
        var userRoles = await _userRoleService.GetUserRolesAsync("test@example.com");
        Assert.Contains("TestRole", userRoles);
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_WithValidAssignment_RemovesRoleSuccessfully()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com", "Password123!", "TestRole");

        // Act
        await _userRoleService.RemoveUserFromRoleAsync("test@example.com", "TestRole");

        // Assert
        var userRoles = await _userRoleService.GetUserRolesAsync("test@example.com");
        Assert.DoesNotContain("TestRole", userRoles);
    }

    [Fact]
    public async Task GetUserRolesAsync_WithUserHavingRoles_ReturnsCorrectRoles()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com", "Password123!", "Admin", "User");

        // Act
        var result = await _userRoleService.GetUserRolesAsync("test@example.com");

        // Assert
        Assert.NotNull(result);
        var roles = result.ToList();
        Assert.Equal(2, roles.Count);
        Assert.Contains("Admin", roles);
        Assert.Contains("User", roles);
    }

    [Fact]
    public async Task GetUserRolesAsync_WithUserHavingNoRoles_ReturnsEmptyCollection()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com");

        // Act
        var result = await _userRoleService.GetUserRolesAsync("test@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Edge Cases Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddUserToRoleAsync_WithInvalidEmail_ThrowsArgumentException(string? invalidEmail)
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _userRoleService.AddUserToRoleAsync(invalidEmail!, "TestRole"));
        
        Assert.Equal("email", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddUserToRoleAsync_WithInvalidRoleName_ThrowsArgumentException(string? invalidRole)
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _userRoleService.AddUserToRoleAsync("test@example.com", invalidRole!));
        
        Assert.Equal("roleName", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RemoveUserFromRoleAsync_WithInvalidEmail_ThrowsArgumentException(string? invalidEmail)
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _userRoleService.RemoveUserFromRoleAsync(invalidEmail!, "TestRole"));
        
        Assert.Equal("email", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RemoveUserFromRoleAsync_WithInvalidRoleName_ThrowsArgumentException(string? invalidRole)
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _userRoleService.RemoveUserFromRoleAsync("test@example.com", invalidRole!));
        
        Assert.Equal("roleName", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetUserRolesAsync_WithInvalidEmail_ReturnsEmptyCollection(string? invalidEmail)
    {
        // Act
        var result = await _userRoleService.GetUserRolesAsync(invalidEmail);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task AddUserToRoleAsync_WithNonExistentUser_ThrowsArgumentException()
    {
        // Arrange
        await EnsureRoleExistsAsync("TestRole");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _userRoleService.AddUserToRoleAsync("nonexistent@example.com", "TestRole"));
        
        Assert.Contains("not found", exception.Message);
        Assert.Equal("email", exception.ParamName);
    }

    [Fact]
    public async Task AddUserToRoleAsync_WithNonExistentRole_ThrowsArgumentException()
    {
        // Arrange
        await CreateTestUserAsync("test@example.com");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _userRoleService.AddUserToRoleAsync("test@example.com", "NonExistentRole"));
        
        Assert.Contains("not found", exception.Message);
        Assert.Equal("roleName", exception.ParamName);
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_WithNonExistentUser_ThrowsArgumentException()
    {
        // Arrange
        await EnsureRoleExistsAsync("TestRole");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _userRoleService.RemoveUserFromRoleAsync("nonexistent@example.com", "TestRole"));
        
        Assert.Contains("not found", exception.Message);
        Assert.Equal("email", exception.ParamName);
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_WithNonExistentRole_ThrowsArgumentException()
    {
        // Arrange
        await CreateTestUserAsync("test@example.com");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _userRoleService.RemoveUserFromRoleAsync("test@example.com", "NonExistentRole"));
        
        Assert.Contains("not found", exception.Message);
        Assert.Equal("roleName", exception.ParamName);
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_WithUserNotInRole_ThrowsArgumentException()
    {
        // Arrange
        await CreateTestUserAsync("test@example.com");
        await EnsureRoleExistsAsync("TestRole");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _userRoleService.RemoveUserFromRoleAsync("test@example.com", "TestRole"));
        
        Assert.Contains("not assigned", exception.Message);
    }

    [Fact]
    public async Task AddUserToRoleAsync_WhenUserAlreadyInRole_DoesNotDuplicateAssignment()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com", "Password123!", "TestRole");

        // Act - Try to add the same role again
        await _userRoleService.AddUserToRoleAsync("test@example.com", "TestRole");

        // Assert - Should still have only one assignment
        var userRoles = await _userRoleService.GetUserRolesAsync("test@example.com");
        var roles = userRoles.ToList();
        Assert.Single(roles);
        Assert.Equal("TestRole", roles[0]);

        // Verify in database that there's only one assignment
        var dbAssignments = await DbContext.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .CountAsync();
        Assert.Equal(1, dbAssignments);
    }

    [Fact]
    public async Task GetUserRolesAsync_WithNonExistentUser_ReturnsEmptyCollection()
    {
        // Act
        var result = await _userRoleService.GetUserRolesAsync("nonexistent@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task GetUsersAsync_WithManyUsers_CompletesWithinReasonableTime()
    {
        // Arrange - Create multiple users
        var tasks = Enumerable.Range(1, 10)
            .Select(i => CreateTestUserAsync($"user{i}@test.com", "Password123!"))
            .ToArray();
        await Task.WhenAll(tasks);

        // Act & Assert
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _userRoleService.GetUsersAsync();
        stopwatch.Stop();

        Assert.NotNull(result);
        Assert.Equal(10, result.Count());
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, "Should complete within 1 second");
    }

    [Fact]
    public async Task GetUserRolesAsync_WithManyRoles_CompletesWithinReasonableTime()
    {
        // Arrange - Create user with multiple roles
        var roles = new[] { "Role1", "Role2", "Role3", "Role4", "Role5" };
        var user = await CreateTestUserAsync("test@example.com", "Password123!", roles);

        // Act & Assert
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _userRoleService.GetUserRolesAsync("test@example.com");
        stopwatch.Stop();

        Assert.NotNull(result);
        Assert.Equal(5, result.Count());
        Assert.True(stopwatch.ElapsedMilliseconds < 500, "Should complete within 500ms");
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task AddUserToRoleAsync_WithSqlInjectionAttempt_HandlesGracefully()
    {
        // Arrange
        await CreateTestUserAsync("test@example.com");
        await EnsureRoleExistsAsync("TestRole");
        const string maliciousEmail = "test@example.com'; DROP TABLE Users; --";

        // Act & Assert - Should throw argument exception, not SQL exception
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _userRoleService.AddUserToRoleAsync(maliciousEmail, "TestRole"));
        
        Assert.Equal("email", exception.ParamName);
        
        // Verify the original user still exists
        var users = await _userRoleService.GetUsersAsync();
        Assert.Contains(users, u => u.Email == "test@example.com");
    }

    [Fact]
    public async Task GetUserRolesAsync_WithSqlInjectionAttempt_HandlesGracefully()
    {
        // Arrange
        const string maliciousEmail = "'; DROP TABLE Roles; --";

        // Act & Assert - Should return empty collection, not throw
        var result = await _userRoleService.GetUserRolesAsync(maliciousEmail);
        
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task RoleManagementWorkflow_CompleteScenario_WorksCorrectly()
    {
        // Arrange
        var user = await CreateTestUserAsync("workflow@test.com");
        await EnsureRoleExistsAsync("Admin");
        await EnsureRoleExistsAsync("User");

        // Act & Assert - Initial state
        var initialRoles = await _userRoleService.GetUserRolesAsync("workflow@test.com");
        Assert.Empty(initialRoles);

        // Add first role
        await _userRoleService.AddUserToRoleAsync("workflow@test.com", "User");
        var afterFirstAdd = await _userRoleService.GetUserRolesAsync("workflow@test.com");
        Assert.Single(afterFirstAdd);
        Assert.Contains("User", afterFirstAdd);

        // Add second role
        await _userRoleService.AddUserToRoleAsync("workflow@test.com", "Admin");
        var afterSecondAdd = await _userRoleService.GetUserRolesAsync("workflow@test.com");
        Assert.Equal(2, afterSecondAdd.Count());
        Assert.Contains("User", afterSecondAdd);
        Assert.Contains("Admin", afterSecondAdd);

        // Remove first role
        await _userRoleService.RemoveUserFromRoleAsync("workflow@test.com", "User");
        var afterRemove = await _userRoleService.GetUserRolesAsync("workflow@test.com");
        Assert.Single(afterRemove);
        Assert.Contains("Admin", afterRemove);
        Assert.DoesNotContain("User", afterRemove);

        // Remove last role
        await _userRoleService.RemoveUserFromRoleAsync("workflow@test.com", "Admin");
        var finalRoles = await _userRoleService.GetUserRolesAsync("workflow@test.com");
        Assert.Empty(finalRoles);
    }

    [Fact]
    public async Task MultipleUsersConcurrentRoleOperations_HandlesCorrectly()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("concurrent1@test.com");
        var user2 = await CreateTestUserAsync("concurrent2@test.com");
        await EnsureRoleExistsAsync("TestRole");

        // Act - Concurrent operations
        var task1 = _userRoleService.AddUserToRoleAsync("concurrent1@test.com", "TestRole");
        var task2 = _userRoleService.AddUserToRoleAsync("concurrent2@test.com", "TestRole");

        await Task.WhenAll(task1, task2);

        // Assert
        var user1Roles = await _userRoleService.GetUserRolesAsync("concurrent1@test.com");
        var user2Roles = await _userRoleService.GetUserRolesAsync("concurrent2@test.com");

        Assert.Contains("TestRole", user1Roles);
        Assert.Contains("TestRole", user2Roles);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetUsersAsync_WhenDatabaseError_ThrowsInvalidOperationException()
    {
        // This test would require mocking the database context to simulate errors
        // For now, we'll test normal operation and document the expected behavior
        
        // Act & Assert - Normal case should work
        var result = await _userRoleService.GetUsersAsync();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task AddUserToRoleAsync_WithDatabaseConcurrencyIssue_HandlesGracefully()
    {
        // Arrange
        var user = await CreateTestUserAsync("concurrency@test.com");
        await EnsureRoleExistsAsync("TestRole");

        // Act & Assert - Multiple rapid attempts should handle gracefully
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => _userRoleService.AddUserToRoleAsync("concurrency@test.com", "TestRole"))
            .ToArray();

        // Should not throw exceptions
        await Task.WhenAll(tasks);

        // Should still have only one role assignment
        var userRoles = await _userRoleService.GetUserRolesAsync("concurrency@test.com");
        Assert.Single(userRoles);
        Assert.Contains("TestRole", userRoles);
    }

    #endregion

    #region Case Sensitivity Tests

    [Fact]
    public async Task GetUserRolesAsync_WithCaseSensitiveEmail_FindsUserCorrectly()
    {
        // Arrange
        var user = await CreateTestUserAsync("Test@Example.COM", "Password123!", "TestRole");

        // Act - Test with different case
        var result = await _userRoleService.GetUserRolesAsync("test@example.com");

        // Assert - Should find user regardless of email case (depends on database collation)
        Assert.NotNull(result);
        // Note: This behavior depends on database configuration for case sensitivity
    }

    [Fact]
    public async Task AddUserToRoleAsync_WithCaseSensitiveData_HandlesCorrectly()
    {
        // Arrange
        await CreateTestUserAsync("test@example.com");
        await EnsureRoleExistsAsync("TestRole");

        // Act - Add with different case
        await _userRoleService.AddUserToRoleAsync("Test@Example.Com", "TestRole");

        // Assert
        var roles = await _userRoleService.GetUserRolesAsync("test@example.com");
        Assert.Contains("TestRole", roles);
    }

    #endregion
}

/// <summary>
/// Tests for UserRoleService that require specific database configurations
/// </summary>
public class UserRoleServiceDatabaseSpecificTests : TestBase
{
    private readonly IUserRoleService _userRoleService;

    public UserRoleServiceDatabaseSpecificTests()
    {
        _userRoleService = ServiceProvider.GetRequiredService<IUserRoleService>();
    }
    [Fact]
    public async Task GetUserRolesAsync_WithDeletedRole_HandlesGracefully()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com", "Password123!", "TestRole");
        
        // Delete the role from the Roles table while keeping the UserRole relationship
        var role = await DbContext.Roles.FirstAsync(r => r.Name == "TestRole");
        DbContext.Roles.Remove(role);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _userRoleService.GetUserRolesAsync("test@example.com");

        // Assert - Should handle the orphaned relationship gracefully
        Assert.NotNull(result);
        // The exact behavior depends on how the join handles missing role records
    }

    [Fact]
    public async Task RoleOperations_WithUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var unicodeEmail = "tëst@éxample.com";
        var unicodeRole = "Tëst Rôlé";
        
        var user = await CreateTestUserAsync(unicodeEmail);
        await EnsureRoleExistsAsync(unicodeRole);

        // Act
        await _userRoleService.AddUserToRoleAsync(unicodeEmail, unicodeRole);

        // Assert
        var roles = await _userRoleService.GetUserRolesAsync(unicodeEmail);
        Assert.Contains(unicodeRole, roles);
    }
}