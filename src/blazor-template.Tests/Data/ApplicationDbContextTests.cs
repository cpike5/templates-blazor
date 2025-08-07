using Xunit;
using Microsoft.EntityFrameworkCore;
using BlazorTemplate.Data;

namespace BlazorTemplate.Tests.Data;

public class ApplicationDbContextTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public ApplicationDbContextTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public void CanCreateContext()
    {
        // Assert
        Assert.NotNull(_context);
        Assert.True(_context.Database.CanConnect());
    }

    [Fact]
    public async Task CanAddUserActivity()
    {
        // Arrange
        var userActivity = new UserActivity
        {
            UserId = "test-user",
            Action = "Login",
            Details = "User logged in successfully",
            IpAddress = "192.168.1.1"
        };

        // Act
        _context.UserActivities.Add(userActivity);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.UserActivities.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal("Login", saved.Action);
        Assert.Equal("test-user", saved.UserId);
    }

    [Fact]
    public async Task CanAddApplicationSetting()
    {
        // Arrange
        var setting = new ApplicationSetting
        {
            Key = "TestSetting",
            Value = "TestValue",
            Category = "Test"
        };

        // Act
        _context.ApplicationSettings.Add(setting);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.ApplicationSettings.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal("TestSetting", saved.Key);
        Assert.Equal("TestValue", saved.Value);
    }

    [Fact] 
    public async Task CanAddInviteCode()
    {
        // Arrange
        var inviteCode = new InviteCode
        {
            Code = "TESTCODE",
            CreatedByUserId = "test-user",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Act
        _context.InviteCodes.Add(inviteCode);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.InviteCodes.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal("TESTCODE", saved.Code);
        Assert.False(saved.IsUsed);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}