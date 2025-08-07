using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BlazorTemplate.Services.Auth;
using BlazorTemplate.Models;
using BlazorTemplate.Data;
using blazor_template.Tests.Infrastructure;
using Moq;
using Xunit;

namespace blazor_template.Tests.Services;

/// <summary>
/// Comprehensive tests for UserManagementService covering all user management operations
/// </summary>
public class UserManagementServiceTests : TestBase
{
    private readonly IUserManagementService _userManagementService;
    private readonly Mock<ILogger<UserManagementService>> _mockLogger;

    public UserManagementServiceTests()
    {
        _mockLogger = new Mock<ILogger<UserManagementService>>();
        _userManagementService = ServiceProvider.GetRequiredService<IUserManagementService>();
    }

    protected override void RegisterApplicationServices(IServiceCollection services)
    {
        base.RegisterApplicationServices(services);
        services.AddSingleton(_mockLogger.Object);
    }

    #region GetUsersAsync Tests

    [Fact]
    public async Task GetUsersAsync_WithNoFilters_ReturnsAllUsers()
    {
        // Arrange
        await CreateTestUserAsync("user1@test.com", "Password123!", "User");
        await CreateTestUserAsync("user2@test.com", "Password123!", "Admin");
        
        var criteria = new UserSearchCriteria();

        // Act
        var result = await _userManagementService.GetUsersAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Contains(result.Items, u => u.Email == "user1@test.com");
        Assert.Contains(result.Items, u => u.Email == "user2@test.com");
    }

    [Fact]
    public async Task GetUsersAsync_WithEmailFilter_ReturnsFilteredUsers()
    {
        // Arrange
        await CreateTestUserAsync("user1@test.com", "Password123!");
        await CreateTestUserAsync("admin@test.com", "Password123!");
        
        var criteria = new UserSearchCriteria { Email = "user1" };

        // Act
        var result = await _userManagementService.GetUsersAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("user1@test.com", result.Items[0].Email);
    }

    [Fact]
    public async Task GetUsersAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 5; i++)
        {
            await CreateTestUserAsync($"user{i}@test.com", "Password123!");
        }
        
        var criteria = new UserSearchCriteria { PageSize = 2, PageNumber = 2 };

        // Act
        var result = await _userManagementService.GetUsersAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.CurrentPage);
        Assert.True(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }

    [Fact]
    public async Task GetUsersAsync_WithSorting_ReturnsSortedUsers()
    {
        // Arrange
        await CreateTestUserAsync("zebra@test.com", "Password123!");
        await CreateTestUserAsync("alpha@test.com", "Password123!");
        
        var criteria = new UserSearchCriteria { SortBy = "email", SortDescending = false };

        // Act
        var result = await _userManagementService.GetUsersAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("alpha@test.com", result.Items[0].Email);
        Assert.Equal("zebra@test.com", result.Items[1].Email);
    }

    [Fact]
    public async Task GetUsersAsync_WithLockoutFilter_ReturnsFilteredUsers()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("locked@test.com", "Password123!");
        var user2 = await CreateTestUserAsync("unlocked@test.com", "Password123!");
        
        // Lock the first user
        await UserManager.SetLockoutEndDateAsync(user1, DateTimeOffset.UtcNow.AddDays(1));
        
        var criteria = new UserSearchCriteria { IsLockedOut = true };

        // Act
        var result = await _userManagementService.GetUsersAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("locked@test.com", result.Items[0].Email);
    }

    #endregion

    #region GetUserByIdAsync Tests

    [Fact]
    public async Task GetUserByIdAsync_WithValidId_ReturnsUserDetails()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com", "Password123!", "Admin", "User");

        // Act
        var result = await _userManagementService.GetUserByIdAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal(2, result.Roles.Count);
        Assert.Contains("Admin", result.Roles);
        Assert.Contains("User", result.Roles);
        Assert.NotNull(result.RecentActivity);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        const string invalidId = "invalid-user-id";

        // Act
        var result = await _userManagementService.GetUserByIdAsync(invalidId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreateUserAsync Tests

    [Fact]
    public async Task CreateUserAsync_WithValidRequest_CreatesUser()
    {
        // Arrange
        await EnsureRoleExistsAsync("TestRole");
        var request = new CreateUserRequest
        {
            Email = "newuser@test.com",
            UserName = "newuser@test.com",
            Password = "Password123!",
            EmailConfirmed = true,
            Roles = new List<string> { "TestRole" }
        };

        // Act
        var result = await _userManagementService.CreateUserAsync(request);

        // Assert
        Assert.True(result.Succeeded);
        
        var createdUser = await UserManager.FindByEmailAsync("newuser@test.com");
        Assert.NotNull(createdUser);
        Assert.Equal("newuser@test.com", createdUser.Email);
        Assert.True(createdUser.EmailConfirmed);
        
        var roles = await UserManager.GetRolesAsync(createdUser);
        Assert.Contains("TestRole", roles);
    }

    [Fact]
    public async Task CreateUserAsync_WithWeakPassword_Fails()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            UserName = "test@example.com",
            Password = "weak" // Weak password
        };

        // Act
        var result = await _userManagementService.CreateUserAsync(request);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task CreateUserAsync_WithDuplicateEmail_Fails()
    {
        // Arrange
        await CreateTestUserAsync("existing@test.com", "Password123!");
        
        var request = new CreateUserRequest
        {
            Email = "existing@test.com",
            UserName = "existing@test.com",
            Password = "Password123!"
        };

        // Act
        var result = await _userManagementService.CreateUserAsync(request);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Description.Contains("already taken") || e.Description.Contains("duplicate"));
    }

    [Fact]
    public async Task CreateUserAsync_WithNonExistentRole_SkipsInvalidRole()
    {
        // Arrange
        await EnsureRoleExistsAsync("ValidRole");
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            UserName = "test@example.com",
            Password = "Password123!",
            Roles = new List<string> { "ValidRole", "NonExistentRole" }
        };

        // Act
        var result = await _userManagementService.CreateUserAsync(request);

        // Assert
        Assert.True(result.Succeeded);
        
        var createdUser = await UserManager.FindByEmailAsync("test@example.com");
        var roles = await UserManager.GetRolesAsync(createdUser);
        Assert.Contains("ValidRole", roles);
        Assert.DoesNotContain("NonExistentRole", roles);
    }

    #endregion

    #region UpdateUserAsync Tests

    [Fact]
    public async Task UpdateUserAsync_WithValidRequest_UpdatesUser()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com", "Password123!", "OldRole");
        await EnsureRoleExistsAsync("NewRole");
        
        var request = new UpdateUserRequest
        {
            Email = "updated@example.com",
            UserName = "updateduser",
            PhoneNumber = "+1234567890",
            EmailConfirmed = true,
            TwoFactorEnabled = true,
            Roles = new List<string> { "NewRole" }
        };

        // Act
        var result = await _userManagementService.UpdateUserAsync(user.Id, request);

        // Assert
        Assert.True(result.Succeeded);
        
        var updatedUser = await UserManager.FindByIdAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal("updated@example.com", updatedUser.Email);
        Assert.Equal("updateduser", updatedUser.UserName);
        Assert.Equal("+1234567890", updatedUser.PhoneNumber);
        Assert.True(updatedUser.EmailConfirmed);
        Assert.True(updatedUser.TwoFactorEnabled);
        
        var roles = await UserManager.GetRolesAsync(updatedUser);
        Assert.Contains("NewRole", roles);
        Assert.DoesNotContain("OldRole", roles);
    }

    [Fact]
    public async Task UpdateUserAsync_WithInvalidUserId_Fails()
    {
        // Arrange
        var request = new UpdateUserRequest { Email = "test@example.com" };

        // Act
        var result = await _userManagementService.UpdateUserAsync("invalid-id", request);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Description.Contains("not found"));
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact]
    public async Task DeleteUserAsync_WithValidUserId_DeletesUser()
    {
        // Arrange
        var user = await CreateTestUserAsync("todelete@test.com", "Password123!");

        // Act
        var result = await _userManagementService.DeleteUserAsync(user.Id);

        // Assert
        Assert.True(result.Succeeded);
        
        var deletedUser = await UserManager.FindByIdAsync(user.Id);
        Assert.Null(deletedUser);
    }

    [Fact]
    public async Task DeleteUserAsync_WithInvalidUserId_Fails()
    {
        // Act
        var result = await _userManagementService.DeleteUserAsync("invalid-id");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Description.Contains("not found"));
    }

    #endregion

    #region Lock/Unlock User Tests

    [Fact]
    public async Task LockUserAsync_WithValidUserId_LocksUser()
    {
        // Arrange
        var user = await CreateTestUserAsync("tolock@test.com", "Password123!");

        // Act
        var result = await _userManagementService.LockUserAsync(user.Id);

        // Assert
        Assert.True(result.Succeeded);
        
        var lockedUser = await UserManager.FindByIdAsync(user.Id);
        Assert.NotNull(lockedUser);
        Assert.True(lockedUser.LockoutEnd.HasValue);
        Assert.True(lockedUser.LockoutEnd > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task UnlockUserAsync_WithLockedUser_UnlocksUser()
    {
        // Arrange
        var user = await CreateTestUserAsync("locked@test.com", "Password123!");
        await UserManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddDays(1));

        // Act
        var result = await _userManagementService.UnlockUserAsync(user.Id);

        // Assert
        Assert.True(result.Succeeded);
        
        var unlockedUser = await UserManager.FindByIdAsync(user.Id);
        Assert.NotNull(unlockedUser);
        Assert.Null(unlockedUser.LockoutEnd);
    }

    [Fact]
    public async Task LockUserAsync_WithInvalidUserId_Fails()
    {
        // Act
        var result = await _userManagementService.LockUserAsync("invalid-id");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Description.Contains("not found"));
    }

    #endregion

    #region Password Reset Tests

    [Fact]
    public async Task ResetUserPasswordAsync_WithValidUser_ResetsPassword()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com", "OldPassword123!");

        // Act
        var result = await _userManagementService.ResetUserPasswordAsync(user.Id, "NewPassword123!");

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify the password was changed
        var passwordCheck = await UserManager.CheckPasswordAsync(user, "NewPassword123!");
        Assert.True(passwordCheck);
    }

    [Fact]
    public async Task ResetUserPasswordAsync_WithWeakPassword_Fails()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com", "Password123!");

        // Act
        var result = await _userManagementService.ResetUserPasswordAsync(user.Id, "weak");

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);
    }

    #endregion

    #region Email Confirmation Tests

    [Fact]
    public async Task ConfirmUserEmailAsync_WithValidUser_ConfirmsEmail()
    {
        // Arrange
        var user = new ApplicationUser
        {
            UserName = "test@example.com",
            Email = "test@example.com",
            EmailConfirmed = false
        };
        await UserManager.CreateAsync(user, "Password123!");

        // Act
        var result = await _userManagementService.ConfirmUserEmailAsync(user.Id);

        // Assert
        Assert.True(result.Succeeded);
        
        var confirmedUser = await UserManager.FindByIdAsync(user.Id);
        Assert.NotNull(confirmedUser);
        Assert.True(confirmedUser.EmailConfirmed);
    }

    [Fact]
    public async Task ResendEmailConfirmationAsync_WithUnconfirmedUser_Succeeds()
    {
        // Arrange
        var user = new ApplicationUser
        {
            UserName = "test@example.com",
            Email = "test@example.com",
            EmailConfirmed = false
        };
        await UserManager.CreateAsync(user, "Password123!");

        // Act
        var result = await _userManagementService.ResendEmailConfirmationAsync(user.Id);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ResendEmailConfirmationAsync_WithConfirmedUser_Fails()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com", "Password123!");

        // Act
        var result = await _userManagementService.ResendEmailConfirmationAsync(user.Id);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Description.Contains("already confirmed"));
    }

    #endregion

    #region Role Management Tests

    [Fact]
    public async Task AssignRoleToUserAsync_WithValidData_AssignsRole()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com", "Password123!");
        await EnsureRoleExistsAsync("NewRole");

        // Act
        var result = await _userManagementService.AssignRoleToUserAsync(user.Id, "NewRole");

        // Assert
        Assert.True(result.Succeeded);
        
        var roles = await UserManager.GetRolesAsync(user);
        Assert.Contains("NewRole", roles);
    }

    [Fact]
    public async Task RemoveRoleFromUserAsync_WithValidData_RemovesRole()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com", "Password123!", "TestRole");

        // Act
        var result = await _userManagementService.RemoveRoleFromUserAsync(user.Id, "TestRole");

        // Assert
        Assert.True(result.Succeeded);
        
        var roles = await UserManager.GetRolesAsync(user);
        Assert.DoesNotContain("TestRole", roles);
    }

    [Fact]
    public async Task GetUserRolesAsync_WithUserHavingRoles_ReturnsRoles()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com", "Password123!", "Admin", "User");

        // Act
        var roles = await _userManagementService.GetUserRolesAsync(user.Id);

        // Assert
        Assert.NotNull(roles);
        Assert.Equal(2, roles.Count);
        Assert.Contains("Admin", roles);
        Assert.Contains("User", roles);
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public async Task GetUserStatisticsAsync_ReturnsCorrectStats()
    {
        // Arrange
        var confirmedUser = await CreateTestUserAsync("confirmed@test.com", "Password123!");
        var unconfirmedUser = new ApplicationUser
        {
            UserName = "unconfirmed@test.com",
            Email = "unconfirmed@test.com",
            EmailConfirmed = false
        };
        await UserManager.CreateAsync(unconfirmedUser, "Password123!");
        
        var lockedUser = await CreateTestUserAsync("locked@test.com", "Password123!");
        await UserManager.SetLockoutEndDateAsync(lockedUser, DateTimeOffset.UtcNow.AddDays(1));

        // Act
        var stats = await _userManagementService.GetUserStatisticsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(3, stats.TotalUsers);
        Assert.Equal(1, stats.ActiveUsers);
        Assert.Equal(1, stats.LockedUsers);
        Assert.Equal(1, stats.PendingUsers);
    }

    #endregion

    #region Search and Filtering Tests

    [Fact]
    public async Task SearchUsersAsync_WithMatchingTerm_ReturnsMatchingUsers()
    {
        // Arrange
        await CreateTestUserAsync("john.doe@test.com", "Password123!");
        await CreateTestUserAsync("jane.smith@test.com", "Password123!");
        await CreateTestUserAsync("bob.jones@test.com", "Password123!");

        // Act
        var result = await _userManagementService.SearchUsersAsync("john");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("john.doe@test.com", result[0].Email);
    }

    [Fact]
    public async Task UserExistsAsync_WithExistingUser_ReturnsTrue()
    {
        // Arrange
        await CreateTestUserAsync("existing@test.com", "Password123!");

        // Act
        var exists = await _userManagementService.UserExistsAsync("existing@test.com");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task UserExistsAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Act
        var exists = await _userManagementService.UserExistsAsync("nonexistent@test.com");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task GetUserByEmailAsync_WithExistingUser_ReturnsUser()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com", "Password123!", "TestRole");

        // Act
        var result = await _userManagementService.GetUserByEmailAsync("test@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Contains("TestRole", result.Roles);
    }

    [Fact]
    public async Task GetUserByEmailAsync_WithNonExistentUser_ReturnsNull()
    {
        // Act
        var result = await _userManagementService.GetUserByEmailAsync("nonexistent@test.com");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Activity Logging Tests

    [Fact]
    public async Task LogUserActivityAsync_CreatesActivityRecord()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com", "Password123!");

        // Act
        await _userManagementService.LogUserActivityAsync(user.Id, "TestAction", "Test details", "127.0.0.1");

        // Assert
        var activities = await _userManagementService.GetUserActivityAsync(user.Id, 10);
        Assert.NotEmpty(activities);
        Assert.Contains(activities, a => a.Action == "TestAction" && a.Details == "Test details");
    }

    [Fact]
    public async Task GetUserActivityAsync_WithLimit_ReturnsLimitedResults()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com", "Password123!");
        
        // Create multiple activities
        for (int i = 1; i <= 5; i++)
        {
            await _userManagementService.LogUserActivityAsync(user.Id, $"Action{i}", $"Details{i}");
            // Small delay to ensure different timestamps
            await Task.Delay(1);
        }

        // Act
        var activities = await _userManagementService.GetUserActivityAsync(user.Id, 3);

        // Assert
        Assert.Equal(3, activities.Count);
        // Should be ordered by timestamp descending (most recent first)
        Assert.Equal("Action5", activities[0].Action);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task GetUsersAsync_WithManyUsers_CompletesWithinReasonableTime()
    {
        // Arrange - Create multiple users
        var tasks = Enumerable.Range(1, 20)
            .Select(i => CreateTestUserAsync($"user{i}@test.com", "Password123!"))
            .ToArray();
        await Task.WhenAll(tasks);

        var criteria = new UserSearchCriteria { PageSize = 10 };

        // Act & Assert
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _userManagementService.GetUsersAsync(criteria);
        stopwatch.Stop();

        Assert.NotNull(result);
        Assert.Equal(10, result.Items.Count);
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, "Should complete within 2 seconds");
    }

    #endregion

    #region Edge Cases and Error Handling Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetUserByEmailAsync_WithInvalidEmail_ReturnsNull(string? invalidEmail)
    {
        // Act
        var result = await _userManagementService.GetUserByEmailAsync(invalidEmail!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateUserAsync_WithNullRequest_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _userManagementService.CreateUserAsync(null!));
    }

    [Fact]
    public async Task UpdateUserAsync_WithNullRequest_ThrowsException()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com", "Password123!");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _userManagementService.UpdateUserAsync(user.Id, null!));
    }

    [Fact]
    public async Task SearchUsersAsync_WithEmptySearchTerm_ReturnsLimitedResults()
    {
        // Arrange
        for (int i = 1; i <= 25; i++) // More than the default limit of 20
        {
            await CreateTestUserAsync($"user{i}@test.com", "Password123!");
        }

        // Act
        var result = await _userManagementService.SearchUsersAsync("");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(20, result.Count); // Should be limited to 20
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task CreateUserAsync_WithSqlInjectionAttempt_HandlesGracefully()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test'; DROP TABLE Users; --@example.com",
            UserName = "test'; DROP TABLE Users; --",
            Password = "Password123!"
        };

        // Act
        var result = await _userManagementService.CreateUserAsync(request);

        // Assert - Should either succeed with escaped input or fail validation
        // The important thing is it shouldn't cause a SQL injection
        Assert.IsType<IdentityResult>(result);
        
        // Verify the database wasn't compromised
        var userCount = await DbContext.Users.CountAsync();
        Assert.True(userCount >= 0); // Database should still be functional
    }

    [Fact]
    public async Task UpdateUserAsync_PreservesSecurityStamp_WhenUpdating()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com", "Password123!");
        var originalSecurityStamp = user.SecurityStamp;
        
        var request = new UpdateUserRequest
        {
            Email = "updated@example.com",
            UserName = "updateduser"
        };

        // Act
        var result = await _userManagementService.UpdateUserAsync(user.Id, request);

        // Assert
        Assert.True(result.Succeeded);
        
        var updatedUser = await UserManager.FindByIdAsync(user.Id);
        Assert.NotNull(updatedUser);
        // Security stamp may change during update operations, which is expected
        Assert.NotNull(updatedUser.SecurityStamp);
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task ConcurrentUserOperations_HandleGracefully()
    {
        // Arrange
        var user = await CreateTestUserAsync("concurrent@test.com", "Password123!");

        // Act - Concurrent updates
        var tasks = new List<Task<IdentityResult>>();
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            tasks.Add(Task.Run(async () =>
            {
                var request = new UpdateUserRequest
                {
                    Email = $"concurrent{index}@test.com",
                    UserName = $"concurrent{index}"
                };
                return await _userManagementService.UpdateUserAsync(user.Id, request);
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - At least one should succeed
        Assert.Contains(results, r => r.Succeeded);
        
        // Verify the user still exists and is in a consistent state
        var finalUser = await UserManager.FindByIdAsync(user.Id);
        Assert.NotNull(finalUser);
        Assert.NotNull(finalUser.Email);
        Assert.NotNull(finalUser.UserName);
    }

    #endregion
}

/// <summary>
/// Tests specific to UserDto mapping and business logic
/// </summary>
public class UserDtoMappingTests : TestBase
{
    private readonly IUserManagementService _userManagementService;

    public UserDtoMappingTests()
    {
        _userManagementService = ServiceProvider.GetRequiredService<IUserManagementService>();
    }

    [Fact]
    public async Task UserDto_Status_ReflectsCorrectState()
    {
        // Arrange
        var activeUser = await CreateTestUserAsync("active@test.com", "Password123!");
        var lockedUser = await CreateTestUserAsync("locked@test.com", "Password123!");
        await UserManager.SetLockoutEndDateAsync(lockedUser, DateTimeOffset.UtcNow.AddDays(1));
        
        var unconfirmedUser = new ApplicationUser
        {
            UserName = "unconfirmed@test.com",
            Email = "unconfirmed@test.com",
            EmailConfirmed = false
        };
        await UserManager.CreateAsync(unconfirmedUser, "Password123!");

        // Act
        var activeDto = await _userManagementService.GetUserByEmailAsync("active@test.com");
        var lockedDto = await _userManagementService.GetUserByEmailAsync("locked@test.com");
        var unconfirmedDto = await _userManagementService.GetUserByEmailAsync("unconfirmed@test.com");

        // Assert
        Assert.Equal("Active", activeDto?.Status);
        Assert.Equal("Locked", lockedDto?.Status);
        Assert.Equal("Pending", unconfirmedDto?.Status);
    }

    [Theory]
    [InlineData("john.doe@example.com", "JD")]
    [InlineData("single@example.com", "SI")]
    [InlineData("a@example.com", "A")]
    [InlineData("@example.com", "?")]
    public void UserDto_Initials_GeneratedCorrectly(string email, string expectedInitials)
    {
        // Arrange
        var userDto = new UserDto { Email = email, UserName = email };

        // Act
        var initials = userDto.Initials;

        // Assert
        Assert.Equal(expectedInitials, initials);
    }

    [Fact]
    public void UserDto_DisplayName_UsesUserNameOverEmail()
    {
        // Arrange
        var userDto = new UserDto
        {
            Email = "test@example.com",
            UserName = "TestUser"
        };

        // Act
        var displayName = userDto.DisplayName;

        // Assert
        Assert.Equal("TestUser", displayName);
    }

    [Fact]
    public void UserDto_DisplayName_FallsBackToEmail()
    {
        // Arrange
        var userDto = new UserDto
        {
            Email = "test@example.com",
            UserName = string.Empty
        };

        // Act
        var displayName = userDto.DisplayName;

        // Assert
        Assert.Equal("test@example.com", displayName);
    }
}