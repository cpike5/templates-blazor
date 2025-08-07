using BlazorTemplate.Configuration;
using BlazorTemplate.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace BlazorTemplate.Services.Invites
{
    public interface IInviteService
    {
        Task<InviteCode> GenerateInviteCodeAsync(string createdByUserId, string? notes = null, int? expirationHours = null);
        Task<EmailInvite> GenerateEmailInviteAsync(string email, string createdByUserId, string? notes = null, int? expirationHours = null);
        Task<InviteCode?> ValidateInviteCodeAsync(string code);
        Task<EmailInvite?> ValidateEmailInviteAsync(string token);
        Task<bool> UseInviteCodeAsync(string code, string usedByUserId);
        Task<bool> UseEmailInviteAsync(string token, string usedByUserId);
        Task<IEnumerable<InviteCode>> GetActiveInviteCodesAsync(string createdByUserId);
        Task<IEnumerable<EmailInvite>> GetActiveEmailInvitesAsync(string createdByUserId);
        Task<bool> CanCreateMoreCodesAsync(string userId);
        Task CleanupExpiredInvitesAsync();
    }

    public class InviteService : IInviteService
    {
        private readonly ApplicationDbContext _context;
        private readonly ConfigurationOptions _config;
        private readonly ILogger<InviteService> _logger;

        public InviteService(ApplicationDbContext context, IOptions<ConfigurationOptions> config, ILogger<InviteService> logger)
        {
            _context = context;
            _config = config.Value;
            _logger = logger;
        }

        public async Task<InviteCode> GenerateInviteCodeAsync(string createdByUserId, string? notes = null, int? expirationHours = null)
        {
            var expirationTime = expirationHours ?? _config.Setup.InviteOnly.DefaultCodeExpirationHours;
            var code = GenerateSecureCode();
            
            var inviteCode = new InviteCode
            {
                Code = code,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(expirationTime),
                CreatedByUserId = createdByUserId,
                Notes = notes
            };

            _context.InviteCodes.Add(inviteCode);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Generated invite code {Code} by user {UserId}, expires at {ExpiresAt}", 
                code, createdByUserId, inviteCode.ExpiresAt);

            return inviteCode;
        }

        public async Task<EmailInvite> GenerateEmailInviteAsync(string email, string createdByUserId, string? notes = null, int? expirationHours = null)
        {
            var expirationTime = expirationHours ?? _config.Setup.InviteOnly.DefaultEmailInviteExpirationHours;
            var token = GenerateSecureToken();
            
            var emailInvite = new EmailInvite
            {
                Email = email.ToLowerInvariant(),
                InviteToken = token,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(expirationTime),
                CreatedByUserId = createdByUserId,
                Notes = notes
            };

            _context.EmailInvites.Add(emailInvite);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Generated email invite for {Email} by user {UserId}, expires at {ExpiresAt}", 
                email, createdByUserId, emailInvite.ExpiresAt);

            return emailInvite;
        }

        public async Task<InviteCode?> ValidateInviteCodeAsync(string code)
        {
            var inviteCode = await _context.InviteCodes
                .FirstOrDefaultAsync(ic => ic.Code == code && !ic.IsUsed && ic.ExpiresAt > DateTime.UtcNow);

            if (inviteCode == null)
            {
                _logger.LogWarning("Invalid or expired invite code attempted: {Code}", code);
                return null;
            }

            _logger.LogInformation("Valid invite code validated: {Code}", code);
            return inviteCode;
        }

        public async Task<EmailInvite?> ValidateEmailInviteAsync(string token)
        {
            var emailInvite = await _context.EmailInvites
                .FirstOrDefaultAsync(ei => ei.InviteToken == token && !ei.IsUsed && ei.ExpiresAt > DateTime.UtcNow);

            if (emailInvite == null)
            {
                _logger.LogWarning("Invalid or expired email invite token attempted: {Token}", token);
                return null;
            }

            _logger.LogInformation("Valid email invite validated for: {Email}", emailInvite.Email);
            return emailInvite;
        }

        public async Task<bool> UseInviteCodeAsync(string code, string usedByUserId)
        {
            var inviteCode = await ValidateInviteCodeAsync(code);
            if (inviteCode == null)
                return false;

            inviteCode.IsUsed = true;
            inviteCode.UsedAt = DateTime.UtcNow;
            inviteCode.UsedByUserId = usedByUserId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Invite code {Code} used by user {UserId}", code, usedByUserId);
            return true;
        }

        public async Task<bool> UseEmailInviteAsync(string token, string usedByUserId)
        {
            var emailInvite = await ValidateEmailInviteAsync(token);
            if (emailInvite == null)
                return false;

            emailInvite.IsUsed = true;
            emailInvite.UsedAt = DateTime.UtcNow;
            emailInvite.UsedByUserId = usedByUserId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Email invite {Token} used by user {UserId} for email {Email}", 
                token, usedByUserId, emailInvite.Email);
            return true;
        }

        public async Task<IEnumerable<InviteCode>> GetActiveInviteCodesAsync(string createdByUserId)
        {
            return await _context.InviteCodes
                .Where(ic => ic.CreatedByUserId == createdByUserId && !ic.IsUsed && ic.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(ic => ic.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<EmailInvite>> GetActiveEmailInvitesAsync(string createdByUserId)
        {
            return await _context.EmailInvites
                .Where(ei => ei.CreatedByUserId == createdByUserId && !ei.IsUsed && ei.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(ei => ei.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> CanCreateMoreCodesAsync(string userId)
        {
            var activeCount = await _context.InviteCodes
                .CountAsync(ic => ic.CreatedByUserId == userId && !ic.IsUsed && ic.ExpiresAt > DateTime.UtcNow);
            
            return activeCount < _config.Setup.InviteOnly.MaxActiveCodesPerAdmin;
        }

        public async Task CleanupExpiredInvitesAsync()
        {
            var expiredCodes = await _context.InviteCodes
                .Where(ic => ic.ExpiresAt < DateTime.UtcNow && !ic.IsUsed)
                .ToListAsync();

            var expiredEmails = await _context.EmailInvites
                .Where(ei => ei.ExpiresAt < DateTime.UtcNow && !ei.IsUsed)
                .ToListAsync();

            if (expiredCodes.Any() || expiredEmails.Any())
            {
                _context.InviteCodes.RemoveRange(expiredCodes);
                _context.EmailInvites.RemoveRange(expiredEmails);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {CodeCount} expired invite codes and {EmailCount} expired email invites", 
                    expiredCodes.Count, expiredEmails.Count);
            }
        }

        private static string GenerateSecureCode()
        {
            // Generate a 8-character alphanumeric code (uppercase for readability)
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Exclude similar looking characters
            using var rng = RandomNumberGenerator.Create();
            var code = new char[8];
            
            for (int i = 0; i < code.Length; i++)
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                code[i] = chars[(int)(BitConverter.ToUInt32(bytes, 0) % chars.Length)];
            }
            
            return new string(code);
        }

        private static string GenerateSecureToken()
        {
            // Generate a 64-character URL-safe token for email invites
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[48]; // 48 bytes = 64 base64url characters
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
    }
}