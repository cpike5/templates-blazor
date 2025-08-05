using BlazorTemplate.Data;
using BlazorTemplate.DTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlazorTemplate.Services
{
    /// <summary>
    /// Test class to verify UserManagementService functionality
    /// This is for development testing only and should be removed in production
    /// </summary>
    public static class TestUserManagementService
    {
        public static async Task<bool> TestBasicFunctionality(IServiceProvider serviceProvider)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var userManagementService = scope.ServiceProvider.GetRequiredService<IUserManagementService>();
                
                // Test 1: Get user statistics
                var stats = await userManagementService.GetUserStatisticsAsync();
                Console.WriteLine($"User Statistics - Total: {stats.TotalUsers}, Active: {stats.ActiveUsers}, Locked: {stats.LockedUsers}, Pending: {stats.PendingUsers}");
                
                // Test 2: Search for users (empty search to get all users)
                var searchCriteria = new UserSearchCriteria { PageSize = 5 };
                var users = await userManagementService.GetUsersAsync(searchCriteria);
                Console.WriteLine($"Found {users.TotalCount} users, displaying {users.Items.Count} in current page");
                
                foreach (var user in users.Items)
                {
                    Console.WriteLine($"User: {user.Email} - Status: {user.Status} - Roles: {string.Join(", ", user.Roles)}");
                }
                
                // Test 3: Check if service can handle non-existent user
                var nonExistentUser = await userManagementService.GetUserByIdAsync("non-existent-id");
                if (nonExistentUser == null)
                {
                    Console.WriteLine("Successfully handled non-existent user lookup");
                }
                
                Console.WriteLine("UserManagementService basic functionality test completed successfully!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UserManagementService test failed: {ex.Message}");
                return false;
            }
        }
        
        public static async Task<bool> TestUserCreation(IServiceProvider serviceProvider)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var userManagementService = scope.ServiceProvider.GetRequiredService<IUserManagementService>();
                
                // Test creating a user
                var createRequest = new CreateUserRequest
                {
                    Email = $"test.user.{Guid.NewGuid().ToString()[..8]}@test.com",
                    UserName = $"testuser{Guid.NewGuid().ToString()[..8]}",
                    Password = "TestPassword123!",
                    EmailConfirmed = true,
                    Roles = new List<string> { "User" }
                };
                
                var result = await userManagementService.CreateUserAsync(createRequest);
                
                if (result.Succeeded)
                {
                    Console.WriteLine($"Successfully created test user: {createRequest.Email}");
                    
                    // Test finding the created user
                    var createdUser = await userManagementService.GetUserByEmailAsync(createRequest.Email);
                    if (createdUser != null)
                    {
                        Console.WriteLine($"Successfully found created user with ID: {createdUser.Id}");
                        
                        // Clean up - delete the test user
                        var deleteResult = await userManagementService.DeleteUserAsync(createdUser.Id);
                        if (deleteResult.Succeeded)
                        {
                            Console.WriteLine("Successfully cleaned up test user");
                        }
                    }
                    
                    return true;
                }
                else
                {
                    Console.WriteLine($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UserManagementService user creation test failed: {ex.Message}");
                return false;
            }
        }
    }
}