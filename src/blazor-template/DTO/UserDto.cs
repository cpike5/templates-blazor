namespace BlazorTemplate.DTO
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public List<string> Roles { get; set; } = new();
        public string? PhoneNumber { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd > DateTimeOffset.Now;
        public string Status => IsLockedOut ? "Locked" : (EmailConfirmed ? "Active" : "Pending");
        public string DisplayName => !string.IsNullOrEmpty(UserName) ? UserName : Email;
        public string Initials => GetInitials();

        private string GetInitials()
        {
            var name = DisplayName;
            if (string.IsNullOrEmpty(name)) return "U";
            
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            
            return name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.Substring(0, 1).ToUpper();
        }
    }

    public class UserDetailDto : UserDto
    {
        public int AccessFailedCount { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public string? SecurityStamp { get; set; }
        public string? ConcurrencyStamp { get; set; }
        public List<UserActivityDto> RecentActivity { get; set; } = new();
        public Dictionary<string, string> Claims { get; set; } = new();
    }

    public class UserStatsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int LockedUsers { get; set; }
        public int PendingUsers { get; set; }
        public int NewUsersToday { get; set; }
        public int NewUsersThisWeek { get; set; }
        public int NewUsersThisMonth { get; set; }
    }

    public class UserActivityDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}