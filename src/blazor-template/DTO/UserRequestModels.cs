using System.ComponentModel.DataAnnotations;

namespace BlazorTemplate.DTO
{
    public class UserSearchCriteria
    {
        [EmailAddress]
        [StringLength(256)]
        public string? Email { get; set; }

        [StringLength(256)]
        public string? UserName { get; set; }

        [StringLength(50)]
        public string? Role { get; set; }

        public bool? EmailConfirmed { get; set; }
        public bool? IsLockedOut { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }

        [StringLength(50)]
        public string SortBy { get; set; } = "Email";

        public bool SortDescending { get; set; } = false;

        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 10;
    }

    public class CreateUserRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        public bool EmailConfirmed { get; set; } = false;
        public bool LockoutEnabled { get; set; } = true;
        public List<string> Roles { get; set; } = new();
    }

    public class UpdateUserRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        public bool EmailConfirmed { get; set; }
        public bool LockoutEnabled { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}