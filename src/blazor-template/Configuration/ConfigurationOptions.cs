namespace BlazorTemplate.Configuration
{
    public class ConfigurationOptions
    {
        public static readonly string SectionName = "Site";
        public AdministrationOptions Administration { get; set; } = new AdministrationOptions();
        public SetupOptions Setup { get; set; } = new SetupOptions();

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
    public class SetupOptions
    {
        public bool EnableSetupMode { get; set; } = false;
        public bool EnableGuestUser { get; set; } = false;
        public GuestUserAccount GuestUser { get; set; } = new GuestUserAccount();
    }
    public class GuestUserAccount
    {
        public string Email { get; set; } = "guest@test.com";
        public string Password { get; set; } = ">a<cy2*U5MFjedHe";
    }
}
