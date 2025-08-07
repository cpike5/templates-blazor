namespace BlazorTemplate.Models.Dashboard
{
    /// <summary>
    /// Represents a recent activity item for display in the dashboard activity feed.
    /// </summary>
    public class RecentActivityItem
    {
        /// <summary>
        /// User ID associated with the activity.
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Username of the user who performed the activity.
        /// </summary>
        public string UserName { get; set; } = string.Empty;
        
        /// <summary>
        /// Email address of the user who performed the activity.
        /// </summary>
        public string UserEmail { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of the action performed.
        /// </summary>
        public string Action { get; set; } = string.Empty;
        
        /// <summary>
        /// Additional details about the activity.
        /// </summary>
        public string Details { get; set; } = string.Empty;
        
        /// <summary>
        /// Timestamp when the activity occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// IP address from which the activity originated.
        /// </summary>
        public string? IpAddress { get; set; }
        
        // Computed properties for display
        /// <summary>
        /// Display name prioritizing username over email.
        /// </summary>
        public string DisplayName => !string.IsNullOrEmpty(UserName) ? UserName : UserEmail;
        
        /// <summary>
        /// Avatar initials derived from the display name.
        /// </summary>
        public string InitialsAvatar => GetInitials(DisplayName);
        
        /// <summary>
        /// Human-readable time elapsed since the activity.
        /// </summary>
        public string TimeAgo => GetTimeAgo(Timestamp);
        
        /// <summary>
        /// Generates initials from a display name for avatar display.
        /// </summary>
        /// <param name="name">The display name to extract initials from.</param>
        /// <returns>Two-character initials string.</returns>
        private static string GetInitials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "??";
            
            var parts = name.Split(new char[] { ' ', '@', '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            
            return name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.ToUpper();
        }
        
        /// <summary>
        /// Converts a timestamp to a human-readable "time ago" format.
        /// </summary>
        /// <param name="timestamp">The timestamp to convert.</param>
        /// <returns>Human-readable time elapsed string.</returns>
        private static string GetTimeAgo(DateTime timestamp)
        {
            var timeSpan = DateTime.UtcNow - timestamp;
            
            if (timeSpan.TotalMinutes < 1) return "Just now";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} min ago";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours != 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 30) return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays != 1 ? "s" : "")} ago";
            
            return timestamp.ToString("MMM dd, yyyy");
        }
    }
}