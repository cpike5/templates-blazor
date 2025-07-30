namespace BlazorTemplate.Configuration
{
    public class ConfigurationOptions
    {
        public static readonly string SectionName = "Site";

        public string AdminEmail { get; set; } = "admin@test.com";

        public IEnumerable<string> UserRoles { get; set; } = new List<string>
        {
            "Administrator",
            "User"
        };
    }
}
