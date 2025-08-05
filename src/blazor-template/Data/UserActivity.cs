using System.ComponentModel.DataAnnotations;

namespace BlazorTemplate.Data
{
    public class UserActivity
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string Action { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string Details { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [MaxLength(45)]
        public string? IpAddress { get; set; }
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }

        // Navigation property
        public virtual ApplicationUser? User { get; set; }
    }
}