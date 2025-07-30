namespace BlazorTemplate.Configuration
{
    public class ConfigurationOptions
    {
        public static readonly string SectionName = "Site";

        public string AdminEmail { get; set; } = "admin@test.com";

    }
}
