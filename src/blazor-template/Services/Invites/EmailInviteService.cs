using BlazorTemplate.Data;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace BlazorTemplate.Services.Invites
{
    public interface IEmailInviteService
    {
        Task<string> GenerateInviteLinkAsync(EmailInvite emailInvite, string baseUrl);
        Task<bool> SendInviteEmailAsync(EmailInvite emailInvite, string inviteLink);
        Task<bool> SendBulkInviteEmailsAsync(IEnumerable<EmailInvite> emailInvites, string baseUrl);
    }

    public class EmailInviteService : IEmailInviteService
    {
        private readonly ILogger<EmailInviteService> _logger;
        // TODO: Add email service dependency when available
        // private readonly IEmailSender _emailSender;

        public EmailInviteService(ILogger<EmailInviteService> logger)
        {
            _logger = logger;
        }

        public async Task<string> GenerateInviteLinkAsync(EmailInvite emailInvite, string baseUrl)
        {
            var inviteUrl = $"{baseUrl.TrimEnd('/')}/Account/Register";
            var parameters = new Dictionary<string, object?>
            {
                ["inviteToken"] = emailInvite.InviteToken,
                ["email"] = emailInvite.Email
            };

            // TODO: Use NavigationManager.GetUriWithQueryParameters when available in service context
            var queryString = string.Join("&", parameters.Select(p => 
                $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value?.ToString() ?? "")}"));
            
            var fullInviteLink = $"{inviteUrl}?{queryString}";
            
            _logger.LogInformation("Generated invite link for {Email}: {Link}", emailInvite.Email, fullInviteLink);
            
            return await Task.FromResult(fullInviteLink);
        }

        public async Task<bool> SendInviteEmailAsync(EmailInvite emailInvite, string inviteLink)
        {
            // TODO: Implement actual email sending when email server is configured
            
            _logger.LogInformation("STUB: Would send email invite to {Email} with link {Link}", 
                emailInvite.Email, inviteLink);
            
            // For now, just log what the email would contain
            var emailBody = GenerateEmailBody(emailInvite.Email, inviteLink);
            _logger.LogInformation("STUB: Email body would be:\n{EmailBody}", emailBody);
            
            // Simulate email sending delay
            await Task.Delay(100);
            
            // Return true to indicate success (in real implementation, return actual send result)
            return true;
        }

        public async Task<bool> SendBulkInviteEmailsAsync(IEnumerable<EmailInvite> emailInvites, string baseUrl)
        {
            var successCount = 0;
            var totalCount = emailInvites.Count();
            
            _logger.LogInformation("STUB: Starting bulk email send for {Count} invites", totalCount);
            
            foreach (var emailInvite in emailInvites)
            {
                try
                {
                    var inviteLink = await GenerateInviteLinkAsync(emailInvite, baseUrl);
                    var success = await SendInviteEmailAsync(emailInvite, inviteLink);
                    
                    if (success)
                    {
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "STUB: Failed to send invite email to {Email}", emailInvite.Email);
                }
                
                // Add small delay between emails to avoid overwhelming email service
                await Task.Delay(50);
            }
            
            _logger.LogInformation("STUB: Bulk email send completed. {Success}/{Total} emails sent", 
                successCount, totalCount);
            
            return successCount == totalCount;
        }

        private string GenerateEmailBody(string email, string inviteLink)
        {
            // TODO: Use proper email templates when available
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>You're Invited!</title>
</head>
<body>
    <h2>You're Invited to Join Our Platform</h2>
    
    <p>Hello,</p>
    
    <p>You've been invited to create an account on our platform. Click the link below to get started:</p>
    
    <p><a href=""{inviteLink}"" style=""background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px; display: inline-block;"">Accept Invitation</a></p>
    
    <p>Or copy and paste this link in your browser:</p>
    <p><code>{inviteLink}</code></p>
    
    <p>This invitation will expire in 72 hours.</p>
    
    <p>If you didn't expect this invitation, you can safely ignore this email.</p>
    
    <p>Best regards,<br>The Team</p>
</body>
</html>";
        }

        // TODO: Add methods for email template management when needed
        // TODO: Add methods for tracking email delivery status
        // TODO: Add methods for resending failed invitations
        // TODO: Add integration with actual email service (SendGrid, AWS SES, etc.)
    }
}