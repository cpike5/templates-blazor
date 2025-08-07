using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BlazorTemplate.Services.Invites;
using BlazorTemplate.Configuration;
using BlazorTemplate.Data;
using blazor_template.Tests.Infrastructure;
using Moq;
using Xunit;

namespace blazor_template.Tests.Services;

/// <summary>
/// Comprehensive tests for InviteService covering invite code and email invite management
/// </summary>
public class InviteServiceTests : TestBase
{
    private readonly IInviteService _inviteService;
    private readonly Mock<ILogger<InviteService>> _mockLogger;

    public InviteServiceTests()
    {
        _mockLogger = new Mock<ILogger<InviteService>>();
        _inviteService = ServiceProvider.GetRequiredService<IInviteService>();
    }

    protected override void RegisterApplicationServices(IServiceCollection services)
    {
        base.RegisterApplicationServices(services);
        services.AddSingleton(_mockLogger.Object);
    }

    #region Invite Code Generation Tests

    [Fact]
    public async Task GenerateInviteCodeAsync_WithValidInput_CreatesInviteCode()
    {
        // Arrange
        var createdByUserId = "user123";
        var notes = "Test invite code";

        // Act
        var result = await _inviteService.GenerateInviteCodeAsync(createdByUserId, notes);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Code);
        Assert.Equal(8, result.Code.Length); // Should be 8 characters
        Assert.Equal(createdByUserId, result.CreatedByUserId);
        Assert.Equal(notes, result.Notes);
        Assert.False(result.IsUsed);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);

        // Verify it's saved in database
        var savedInvite = await DbContext.InviteCodes.FindAsync(result.Id);
        Assert.NotNull(savedInvite);
        Assert.Equal(result.Code, savedInvite.Code);
    }

    [Fact]
    public async Task GenerateInviteCodeAsync_WithCustomExpiration_UsesCustomExpiration()
    {
        // Arrange
        var createdByUserId = "user123";
        var customExpirationHours = 5;

        // Act
        var result = await _inviteService.GenerateInviteCodeAsync(createdByUserId, expirationHours: customExpirationHours);

        // Assert
        Assert.NotNull(result);
        var expectedExpiration = result.CreatedAt.AddHours(customExpirationHours);
        Assert.True(Math.Abs((result.ExpiresAt - expectedExpiration).TotalMinutes) < 1);
    }

    [Fact]
    public async Task GenerateInviteCodeAsync_MultipleCodes_GeneratesUniqueCodes()
    {
        // Arrange
        var createdByUserId = "user123";
        var codes = new HashSet<string>();

        // Act - Generate multiple codes
        for (int i = 0; i < 10; i++)
        {
            var invite = await _inviteService.GenerateInviteCodeAsync(createdByUserId);
            codes.Add(invite.Code);
        }

        // Assert - All codes should be unique
        Assert.Equal(10, codes.Count);
    }

    [Fact]
    public async Task GenerateInviteCodeAsync_CodesUseSecureCharacterSet()
    {
        // Arrange
        var createdByUserId = "user123";

        // Act
        var result = await _inviteService.GenerateInviteCodeAsync(createdByUserId);

        // Assert - Should not contain easily confused characters (0, O, I, 1, etc.)
        Assert.DoesNotContain('0', result.Code);
        Assert.DoesNotContain('O', result.Code);
        Assert.DoesNotContain('I', result.Code);
        Assert.DoesNotContain('1', result.Code);
        Assert.DoesNotContain('L', result.Code);
        
        // Should only contain safe characters
        Assert.All(result.Code, c => 
        {
            Assert.True(char.IsLetterOrDigit(c));
            Assert.True(char.IsUpper(c) || char.IsDigit(c));
        });
    }

    #endregion

    #region Email Invite Generation Tests

    [Fact]
    public async Task GenerateEmailInviteAsync_WithValidInput_CreatesEmailInvite()
    {
        // Arrange
        var email = "test@example.com";
        var createdByUserId = "user123";
        var notes = "Test email invite";

        // Act
        var result = await _inviteService.GenerateEmailInviteAsync(email, createdByUserId, notes);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email.ToLowerInvariant(), result.Email);
        Assert.NotEmpty(result.InviteToken);
        Assert.True(result.InviteToken.Length > 50); // Should be long secure token
        Assert.Equal(createdByUserId, result.CreatedByUserId);
        Assert.Equal(notes, result.Notes);
        Assert.False(result.IsUsed);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);

        // Verify token is URL-safe
        Assert.DoesNotContain('+', result.InviteToken);
        Assert.DoesNotContain('/', result.InviteToken);
        Assert.DoesNotContain('=', result.InviteToken);
    }

    [Fact]
    public async Task GenerateEmailInviteAsync_WithMixedCaseEmail_NormalizesToLowerCase()
    {
        // Arrange
        var email = "Test@EXAMPLE.COM";
        var createdByUserId = "user123";

        // Act
        var result = await _inviteService.GenerateEmailInviteAsync(email, createdByUserId);

        // Assert
        Assert.Equal("test@example.com", result.Email);
    }

    [Fact]
    public async Task GenerateEmailInviteAsync_WithCustomExpiration_UsesCustomExpiration()
    {
        // Arrange
        var email = "test@example.com";
        var createdByUserId = "user123";
        var customExpirationHours = 48;

        // Act
        var result = await _inviteService.GenerateEmailInviteAsync(email, createdByUserId, expirationHours: customExpirationHours);

        // Assert
        var expectedExpiration = result.CreatedAt.AddHours(customExpirationHours);
        Assert.True(Math.Abs((result.ExpiresAt - expectedExpiration).TotalMinutes) < 1);
    }

    [Fact]
    public async Task GenerateEmailInviteAsync_MultipleInvites_GeneratesUniqueTokens()
    {
        // Arrange
        var createdByUserId = "user123";
        var tokens = new HashSet<string>();

        // Act - Generate multiple invites
        for (int i = 0; i < 10; i++)
        {
            var invite = await _inviteService.GenerateEmailInviteAsync($"test{i}@example.com", createdByUserId);
            tokens.Add(invite.InviteToken);
        }

        // Assert - All tokens should be unique
        Assert.Equal(10, tokens.Count);
    }

    #endregion

    #region Invite Validation Tests

    [Fact]
    public async Task ValidateInviteCodeAsync_WithValidCode_ReturnsInviteCode()
    {
        // Arrange
        var invite = await _inviteService.GenerateInviteCodeAsync("user123");

        // Act
        var result = await _inviteService.ValidateInviteCodeAsync(invite.Code);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(invite.Id, result.Id);
        Assert.Equal(invite.Code, result.Code);
    }

    [Fact]
    public async Task ValidateInviteCodeAsync_WithInvalidCode_ReturnsNull()
    {
        // Act
        var result = await _inviteService.ValidateInviteCodeAsync("INVALID1");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateInviteCodeAsync_WithExpiredCode_ReturnsNull()
    {
        // Arrange
        var invite = await _inviteService.GenerateInviteCodeAsync("user123", expirationHours: -1); // Expired

        // Act
        var result = await _inviteService.ValidateInviteCodeAsync(invite.Code);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateInviteCodeAsync_WithUsedCode_ReturnsNull()
    {
        // Arrange
        var invite = await _inviteService.GenerateInviteCodeAsync("user123");
        await _inviteService.UseInviteCodeAsync(invite.Code, "user456");

        // Act
        var result = await _inviteService.ValidateInviteCodeAsync(invite.Code);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateEmailInviteAsync_WithValidToken_ReturnsEmailInvite()
    {
        // Arrange
        var invite = await _inviteService.GenerateEmailInviteAsync("test@example.com", "user123");

        // Act
        var result = await _inviteService.ValidateEmailInviteAsync(invite.InviteToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(invite.Id, result.Id);
        Assert.Equal(invite.InviteToken, result.InviteToken);
    }

    [Fact]
    public async Task ValidateEmailInviteAsync_WithInvalidToken_ReturnsNull()
    {
        // Act
        var result = await _inviteService.ValidateEmailInviteAsync("invalid-token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateEmailInviteAsync_WithExpiredToken_ReturnsNull()
    {
        // Arrange
        var invite = await _inviteService.GenerateEmailInviteAsync("test@example.com", "user123", expirationHours: -1);

        // Act
        var result = await _inviteService.ValidateEmailInviteAsync(invite.InviteToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateEmailInviteAsync_WithUsedToken_ReturnsNull()
    {
        // Arrange
        var invite = await _inviteService.GenerateEmailInviteAsync("test@example.com", "user123");
        await _inviteService.UseEmailInviteAsync(invite.InviteToken, "user456");

        // Act
        var result = await _inviteService.ValidateEmailInviteAsync(invite.InviteToken);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Invite Usage Tests

    [Fact]
    public async Task UseInviteCodeAsync_WithValidCode_MarksAsUsed()
    {
        // Arrange
        var invite = await _inviteService.GenerateInviteCodeAsync("user123");
        var usedByUserId = "user456";

        // Act
        var result = await _inviteService.UseInviteCodeAsync(invite.Code, usedByUserId);

        // Assert
        Assert.True(result);

        // Verify in database
        var updatedInvite = await DbContext.InviteCodes.FindAsync(invite.Id);
        Assert.NotNull(updatedInvite);
        Assert.True(updatedInvite.IsUsed);
        Assert.Equal(usedByUserId, updatedInvite.UsedByUserId);
        Assert.True(updatedInvite.UsedAt.HasValue);
        Assert.True(updatedInvite.UsedAt.Value <= DateTime.UtcNow);
    }

    [Fact]
    public async Task UseInviteCodeAsync_WithInvalidCode_ReturnsFalse()
    {
        // Act
        var result = await _inviteService.UseInviteCodeAsync("INVALID1", "user456");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UseInviteCodeAsync_WithAlreadyUsedCode_ReturnsFalse()
    {
        // Arrange
        var invite = await _inviteService.GenerateInviteCodeAsync("user123");
        await _inviteService.UseInviteCodeAsync(invite.Code, "user456"); // First use

        // Act
        var result = await _inviteService.UseInviteCodeAsync(invite.Code, "user789"); // Second use

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UseEmailInviteAsync_WithValidToken_MarksAsUsed()
    {
        // Arrange
        var invite = await _inviteService.GenerateEmailInviteAsync("test@example.com", "user123");
        var usedByUserId = "user456";

        // Act
        var result = await _inviteService.UseEmailInviteAsync(invite.InviteToken, usedByUserId);

        // Assert
        Assert.True(result);

        // Verify in database
        var updatedInvite = await DbContext.EmailInvites.FindAsync(invite.Id);
        Assert.NotNull(updatedInvite);
        Assert.True(updatedInvite.IsUsed);
        Assert.Equal(usedByUserId, updatedInvite.UsedByUserId);
        Assert.True(updatedInvite.UsedAt.HasValue);
        Assert.True(updatedInvite.UsedAt.Value <= DateTime.UtcNow);
    }

    [Fact]
    public async Task UseEmailInviteAsync_WithInvalidToken_ReturnsFalse()
    {
        // Act
        var result = await _inviteService.UseEmailInviteAsync("invalid-token", "user456");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Active Invites Retrieval Tests

    [Fact]
    public async Task GetActiveInviteCodesAsync_ReturnsOnlyActiveCodesForUser()
    {
        // Arrange
        var userId1 = "user123";
        var userId2 = "user456";

        // Create active invites for user1
        var activeInvite1 = await _inviteService.GenerateInviteCodeAsync(userId1, "Active 1");
        var activeInvite2 = await _inviteService.GenerateInviteCodeAsync(userId1, "Active 2");

        // Create used invite for user1
        var usedInvite = await _inviteService.GenerateInviteCodeAsync(userId1, "Used");
        await _inviteService.UseInviteCodeAsync(usedInvite.Code, "someuser");

        // Create expired invite for user1
        await _inviteService.GenerateInviteCodeAsync(userId1, "Expired", expirationHours: -1);

        // Create invite for different user
        await _inviteService.GenerateInviteCodeAsync(userId2, "Other user");

        // Act
        var result = await _inviteService.GetActiveInviteCodesAsync(userId1);

        // Assert
        var activeInvites = result.ToList();
        Assert.Equal(2, activeInvites.Count);
        Assert.Contains(activeInvites, i => i.Id == activeInvite1.Id);
        Assert.Contains(activeInvites, i => i.Id == activeInvite2.Id);
        
        // Verify ordering (most recent first)
        Assert.True(activeInvites[0].CreatedAt >= activeInvites[1].CreatedAt);
    }

    [Fact]
    public async Task GetActiveEmailInvitesAsync_ReturnsOnlyActiveInvitesForUser()
    {
        // Arrange
        var userId1 = "user123";
        var userId2 = "user456";

        // Create active invites for user1
        var activeInvite1 = await _inviteService.GenerateEmailInviteAsync("test1@example.com", userId1);
        var activeInvite2 = await _inviteService.GenerateEmailInviteAsync("test2@example.com", userId1);

        // Create used invite for user1
        var usedInvite = await _inviteService.GenerateEmailInviteAsync("used@example.com", userId1);
        await _inviteService.UseEmailInviteAsync(usedInvite.InviteToken, "someuser");

        // Create expired invite for user1
        await _inviteService.GenerateEmailInviteAsync("expired@example.com", userId1, expirationHours: -1);

        // Create invite for different user
        await _inviteService.GenerateEmailInviteAsync("other@example.com", userId2);

        // Act
        var result = await _inviteService.GetActiveEmailInvitesAsync(userId1);

        // Assert
        var activeInvites = result.ToList();
        Assert.Equal(2, activeInvites.Count);
        Assert.Contains(activeInvites, i => i.Id == activeInvite1.Id);
        Assert.Contains(activeInvites, i => i.Id == activeInvite2.Id);
    }

    #endregion

    #region Quota Management Tests

    [Fact]
    public async Task CanCreateMoreCodesAsync_WithinQuota_ReturnsTrue()
    {
        // Arrange
        var userId = "user123";
        
        // Create some active codes (but less than max)
        await _inviteService.GenerateInviteCodeAsync(userId);
        await _inviteService.GenerateInviteCodeAsync(userId);

        // Act
        var result = await _inviteService.CanCreateMoreCodesAsync(userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanCreateMoreCodesAsync_AtQuotaLimit_ReturnsFalse()
    {
        // Arrange
        var userId = "user123";
        var maxCodes = Configuration.GetValue<int>("Site:Administration:DefaultRoles:0") > 0 ? 10 : 10; // Use default from config

        // Create maximum number of active codes
        for (int i = 0; i < maxCodes; i++)
        {
            await _inviteService.GenerateInviteCodeAsync(userId);
        }

        // Act
        var result = await _inviteService.CanCreateMoreCodesAsync(userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanCreateMoreCodesAsync_WithUsedCodes_DoesNotCountUsedCodes()
    {
        // Arrange
        var userId = "user123";

        // Create and use some codes
        for (int i = 0; i < 5; i++)
        {
            var invite = await _inviteService.GenerateInviteCodeAsync(userId);
            await _inviteService.UseInviteCodeAsync(invite.Code, "someuser");
        }

        // Act
        var result = await _inviteService.CanCreateMoreCodesAsync(userId);

        // Assert
        Assert.True(result); // Used codes shouldn't count towards quota
    }

    #endregion

    #region Cleanup Tests

    [Fact]
    public async Task CleanupExpiredInvitesAsync_RemovesExpiredInvites()
    {
        // Arrange
        var userId = "user123";

        // Create active invites
        var activeCodeInvite = await _inviteService.GenerateInviteCodeAsync(userId, "Active");
        var activeEmailInvite = await _inviteService.GenerateEmailInviteAsync("active@example.com", userId);

        // Create expired invites
        var expiredCodeInvite = await _inviteService.GenerateInviteCodeAsync(userId, "Expired", expirationHours: -1);
        var expiredEmailInvite = await _inviteService.GenerateEmailInviteAsync("expired@example.com", userId, expirationHours: -1);

        // Create used invites (should not be cleaned up as they have historical value)
        var usedCodeInvite = await _inviteService.GenerateInviteCodeAsync(userId, "Used");
        await _inviteService.UseInviteCodeAsync(usedCodeInvite.Code, "someuser");

        // Act
        await _inviteService.CleanupExpiredInvitesAsync();

        // Assert
        var remainingCodes = await DbContext.InviteCodes.Where(ic => ic.CreatedByUserId == userId).ToListAsync();
        var remainingEmails = await DbContext.EmailInvites.Where(ei => ei.CreatedByUserId == userId).ToListAsync();

        Assert.Equal(2, remainingCodes.Count); // Active + Used
        Assert.Single(remainingEmails); // Active only

        Assert.Contains(remainingCodes, c => c.Id == activeCodeInvite.Id);
        Assert.Contains(remainingCodes, c => c.Id == usedCodeInvite.Id);
        Assert.Contains(remainingEmails, e => e.Id == activeEmailInvite.Id);
    }

    [Fact]
    public async Task CleanupExpiredInvitesAsync_WithNoExpiredInvites_DoesNothing()
    {
        // Arrange
        var userId = "user123";
        await _inviteService.GenerateInviteCodeAsync(userId);
        await _inviteService.GenerateEmailInviteAsync("test@example.com", userId);

        var initialCodeCount = await DbContext.InviteCodes.CountAsync();
        var initialEmailCount = await DbContext.EmailInvites.CountAsync();

        // Act
        await _inviteService.CleanupExpiredInvitesAsync();

        // Assert
        var finalCodeCount = await DbContext.InviteCodes.CountAsync();
        var finalEmailCount = await DbContext.EmailInvites.CountAsync();

        Assert.Equal(initialCodeCount, finalCodeCount);
        Assert.Equal(initialEmailCount, finalEmailCount);
    }

    #endregion

    #region Security and Edge Case Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ValidateInviteCodeAsync_WithNullOrEmptyCode_ReturnsNull(string? code)
    {
        // Act
        var result = await _inviteService.ValidateInviteCodeAsync(code!);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ValidateEmailInviteAsync_WithNullOrEmptyToken_ReturnsNull(string? token)
    {
        // Act
        var result = await _inviteService.ValidateEmailInviteAsync(token!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateInviteCodeAsync_WithVeryLongNotes_HandlesGracefully()
    {
        // Arrange
        var userId = "user123";
        var longNotes = new string('A', 1000); // Very long notes

        // Act & Assert - Should not throw
        var result = await _inviteService.GenerateInviteCodeAsync(userId, longNotes);
        Assert.NotNull(result);
        Assert.Equal(longNotes, result.Notes);
    }

    [Fact]
    public async Task GenerateEmailInviteAsync_WithVeryLongEmail_HandlesGracefully()
    {
        // Arrange
        var userId = "user123";
        var longEmail = new string('a', 200) + "@example.com"; // Very long email

        // Act & Assert - Should not throw
        var result = await _inviteService.GenerateEmailInviteAsync(longEmail, userId);
        Assert.NotNull(result);
        Assert.Equal(longEmail.ToLowerInvariant(), result.Email);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task GenerateInviteCodeAsync_MultipleParallel_HandlesGracefully()
    {
        // Arrange
        var userId = "user123";
        var tasks = new List<Task<InviteCode>>();

        // Act - Generate multiple codes in parallel
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_inviteService.GenerateInviteCodeAsync(userId, $"Parallel {i}"));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, results.Length);
        var codes = results.Select(r => r.Code).ToHashSet();
        Assert.Equal(10, codes.Count); // All codes should be unique
    }

    [Fact]
    public async Task CleanupExpiredInvitesAsync_WithManyExpiredInvites_CompletesWithinReasonableTime()
    {
        // Arrange - Create many expired invites
        var userId = "user123";
        var tasks = new List<Task>();

        for (int i = 0; i < 100; i++)
        {
            tasks.Add(_inviteService.GenerateInviteCodeAsync(userId, $"Expired {i}", expirationHours: -1));
            tasks.Add(_inviteService.GenerateEmailInviteAsync($"expired{i}@example.com", userId, expirationHours: -1));
        }

        await Task.WhenAll(tasks);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await _inviteService.CleanupExpiredInvitesAsync();
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 5000); // Should complete within 5 seconds
        
        // Verify cleanup worked
        var remainingExpired = await DbContext.InviteCodes.CountAsync(ic => ic.ExpiresAt < DateTime.UtcNow && !ic.IsUsed);
        Assert.Equal(0, remainingExpired);
    }

    #endregion
}

/// <summary>
/// Tests for EmailInviteService functionality
/// </summary>
public class EmailInviteServiceTests : TestBase
{
    private readonly IEmailInviteService _emailInviteService;
    private readonly Mock<ILogger<EmailInviteService>> _mockLogger;

    public EmailInviteServiceTests()
    {
        _mockLogger = new Mock<ILogger<EmailInviteService>>();
        _emailInviteService = new EmailInviteService(_mockLogger.Object);
    }

    #region Invite Link Generation Tests

    [Fact]
    public async Task GenerateInviteLinkAsync_WithValidInput_GeneratesCorrectLink()
    {
        // Arrange
        var emailInvite = new EmailInvite
        {
            Email = "test@example.com",
            InviteToken = "test-token-123"
        };
        var baseUrl = "https://example.com";

        // Act
        var result = await _emailInviteService.GenerateInviteLinkAsync(emailInvite, baseUrl);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("https://example.com/Account/Register", result);
        Assert.Contains("inviteToken=test-token-123", result);
        Assert.Contains("email=test%40example.com", result); // URL encoded
    }

    [Fact]
    public async Task GenerateInviteLinkAsync_WithTrailingSlashInBaseUrl_HandlesCorrectly()
    {
        // Arrange
        var emailInvite = new EmailInvite
        {
            Email = "test@example.com",
            InviteToken = "test-token"
        };
        var baseUrl = "https://example.com/"; // Trailing slash

        // Act
        var result = await _emailInviteService.GenerateInviteLinkAsync(emailInvite, baseUrl);

        // Assert
        Assert.StartsWith("https://example.com/Account/Register", result);
        Assert.DoesNotContain("//Account", result); // Should not have double slash
    }

    [Fact]
    public async Task GenerateInviteLinkAsync_WithSpecialCharactersInEmail_EncodesCorrectly()
    {
        // Arrange
        var emailInvite = new EmailInvite
        {
            Email = "test+tag@example.com",
            InviteToken = "token123"
        };
        var baseUrl = "https://example.com";

        // Act
        var result = await _emailInviteService.GenerateInviteLinkAsync(emailInvite, baseUrl);

        // Assert
        Assert.Contains("email=test%2Btag%40example.com", result);
    }

    #endregion

    #region Email Sending Tests (Stub Implementation)

    [Fact]
    public async Task SendInviteEmailAsync_WithValidInput_ReturnsSuccess()
    {
        // Arrange
        var emailInvite = new EmailInvite
        {
            Email = "test@example.com",
            InviteToken = "test-token"
        };
        var inviteLink = "https://example.com/Account/Register?inviteToken=test-token&email=test%40example.com";

        // Act
        var result = await _emailInviteService.SendInviteEmailAsync(emailInvite, inviteLink);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SendInviteEmailAsync_CompletesWithinReasonableTime()
    {
        // Arrange
        var emailInvite = new EmailInvite
        {
            Email = "test@example.com",
            InviteToken = "test-token"
        };
        var inviteLink = "https://example.com/invite";

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _emailInviteService.SendInviteEmailAsync(emailInvite, inviteLink);
        stopwatch.Stop();

        // Assert
        Assert.True(result);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000); // Should complete quickly (it's a stub)
    }

    #endregion

    #region Bulk Email Tests

    [Fact]
    public async Task SendBulkInviteEmailsAsync_WithValidInvites_ReturnsSuccess()
    {
        // Arrange
        var emailInvites = new List<EmailInvite>
        {
            new() { Email = "test1@example.com", InviteToken = "token1" },
            new() { Email = "test2@example.com", InviteToken = "token2" },
            new() { Email = "test3@example.com", InviteToken = "token3" }
        };
        var baseUrl = "https://example.com";

        // Act
        var result = await _emailInviteService.SendBulkInviteEmailsAsync(emailInvites, baseUrl);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SendBulkInviteEmailsAsync_WithEmptyList_ReturnsTrue()
    {
        // Arrange
        var emailInvites = new List<EmailInvite>();
        var baseUrl = "https://example.com";

        // Act
        var result = await _emailInviteService.SendBulkInviteEmailsAsync(emailInvites, baseUrl);

        // Assert
        Assert.True(result); // Empty list should be considered successful
    }

    [Fact]
    public async Task SendBulkInviteEmailsAsync_WithManyInvites_CompletesWithinReasonableTime()
    {
        // Arrange
        var emailInvites = Enumerable.Range(1, 50)
            .Select(i => new EmailInvite 
            { 
                Email = $"test{i}@example.com", 
                InviteToken = $"token{i}" 
            })
            .ToList();
        var baseUrl = "https://example.com";

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _emailInviteService.SendBulkInviteEmailsAsync(emailInvites, baseUrl);
        stopwatch.Stop();

        // Assert
        Assert.True(result);
        Assert.True(stopwatch.ElapsedMilliseconds < 10000); // Should complete within 10 seconds
    }

    #endregion

    #region Edge Cases and Security Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GenerateInviteLinkAsync_WithInvalidBaseUrl_HandlesGracefully(string? baseUrl)
    {
        // Arrange
        var emailInvite = new EmailInvite
        {
            Email = "test@example.com",
            InviteToken = "test-token"
        };

        // Act & Assert - Should handle gracefully or throw meaningful exception
        if (string.IsNullOrEmpty(baseUrl))
        {
            // This might throw or return a malformed URL - either is acceptable
            try
            {
                var result = await _emailInviteService.GenerateInviteLinkAsync(emailInvite, baseUrl!);
                // If it doesn't throw, the result should at least contain the token
                Assert.Contains("test-token", result);
            }
            catch (Exception ex)
            {
                // Throwing an exception is also acceptable for invalid input
                Assert.IsType<ArgumentException>(ex);
            }
        }
    }

    [Fact]
    public async Task GenerateInviteLinkAsync_WithMaliciousInput_SanitizesCorrectly()
    {
        // Arrange
        var emailInvite = new EmailInvite
        {
            Email = "test@example.com",
            InviteToken = "token<script>alert('xss')</script>"
        };
        var baseUrl = "https://example.com";

        // Act
        var result = await _emailInviteService.GenerateInviteLinkAsync(emailInvite, baseUrl);

        // Assert
        // The token should be URL encoded, making it safe
        Assert.Contains("%3Cscript%3E", result); // < should be encoded
        Assert.DoesNotContain("<script>", result); // Should not contain raw script tags
    }

    #endregion
}