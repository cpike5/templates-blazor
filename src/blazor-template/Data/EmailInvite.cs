using System.ComponentModel.DataAnnotations;

namespace BlazorTemplate.Data
{
    public class EmailInvite
    {
        public int Id { get; set; }
        
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string InviteToken { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime ExpiresAt { get; set; }
        
        public bool IsUsed { get; set; } = false;
        
        public DateTime? UsedAt { get; set; }
        
        public string? UsedByUserId { get; set; }
        
        public ApplicationUser? UsedByUser { get; set; }
        
        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;
        
        public ApplicationUser? CreatedByUser { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        public DateTime? EmailSentAt { get; set; }
        
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        
        public bool IsValid => !IsUsed && !IsExpired;
    }
}