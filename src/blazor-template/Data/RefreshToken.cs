using System.ComponentModel.DataAnnotations;

namespace BlazorTemplate.Data
{
    public class RefreshToken
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public string Token { get; set; } = string.Empty;
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public DateTime ExpiryDate { get; set; }
        
        public bool IsRevoked { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? RevokedAt { get; set; }
        
        public string? RevokedByIp { get; set; }
        
        public string? ReplacedByToken { get; set; }
        
        // Navigation property
        public virtual ApplicationUser User { get; set; } = null!;
        
        // Helper properties
        public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiryDate;
        public bool IsExpired => DateTime.UtcNow >= ExpiryDate;
    }
}