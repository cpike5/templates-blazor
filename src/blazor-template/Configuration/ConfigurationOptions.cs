namespace BlazorTemplate.Configuration
{
    public class ConfigurationOptions
    {
        public static readonly string SectionName = "Site";
        public AdministrationOptions Administration { get; set; } = new AdministrationOptions();

    }
    public class AdministrationOptions
    {
        public string AdminEmail { get; set; } = "admin@test.com";

        public string AdministratorRole { get; set; } = "Administrator";
        public IEnumerable<string> UserRoles { get; set; } = new List<string>
        {
            "Administrator",
            "User"
        };
    }
}
