using Xunit;
using BlazorTemplate.Data;
using BlazorTemplate.Configuration;

namespace BlazorTemplate.Tests;

public class BasicTests
{
    [Fact]
    public void ApplicationUser_CanBeCreated()
    {
        // Arrange & Act
        var user = new ApplicationUser
        {
            UserName = "test@example.com",
            Email = "test@example.com"
        };

        // Assert
        Assert.NotNull(user);
        Assert.Equal("test@example.com", user.UserName);
        Assert.Equal("test@example.com", user.Email);
    }

    [Fact]
    public void ConfigurationOptions_CanBeCreated()
    {
        // Arrange & Act
        var config = new ConfigurationOptions();

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.Administration);
        Assert.NotNull(config.Setup);
    }

    [Fact]
    public void UserActivity_HasRequiredProperties()
    {
        // Arrange & Act
        var activity = new UserActivity
        {
            UserId = "test-user",
            Action = "Test Action"
        };

        // Assert
        Assert.NotNull(activity.Id);
        Assert.Equal("test-user", activity.UserId);
        Assert.Equal("Test Action", activity.Action);
        Assert.True(activity.Timestamp > DateTime.MinValue);
    }

    [Fact]
    public void InviteCode_HasCorrectDefaults()
    {
        // Arrange & Act
        var invite = new InviteCode
        {
            Code = "TEST123",
            CreatedByUserId = "user1"
        };

        // Assert
        Assert.Equal("TEST123", invite.Code);
        Assert.False(invite.IsUsed);
        Assert.Null(invite.UsedAt);
    }
}