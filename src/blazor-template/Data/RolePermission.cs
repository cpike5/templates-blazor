using System.ComponentModel.DataAnnotations;

namespace BlazorTemplate.Data
{
    /// <summary>
    /// Entity representing the association between roles and permissions
    /// </summary>
    public class RolePermission
    {
        /// <summary>
        /// Unique identifier for the role-permission association
        /// </summary>
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The role ID this permission is associated with
        /// </summary>
        [Required]
        [MaxLength(450)]
        public string RoleId { get; set; } = string.Empty;

        /// <summary>
        /// The permission name (e.g., "users.view", "roles.manage")
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string PermissionName { get; set; } = string.Empty;

        /// <summary>
        /// Whether this permission is granted (true) or denied (false)
        /// </summary>
        public bool IsGranted { get; set; } = true;

        /// <summary>
        /// When this permission was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User who created this permission assignment
        /// </summary>
        [MaxLength(450)]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Navigation property to the user who created this assignment
        /// </summary>
        public ApplicationUser? CreatedByUser { get; set; }
    }
}