namespace BlazorTemplate.Configuration
{
    public class ConfigurationOptions
    {
        public static readonly string SectionName = "Site";
        public AdministrationOptions Administration { get; set; } = new AdministrationOptions();
        public SetupOptions Setup { get; set; } = new SetupOptions();
        public SettingsOptions Settings { get; set; } = new SettingsOptions();
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
        public InviteOnlyOptions InviteOnly { get; set; } = new InviteOnlyOptions();
    }
    public class GuestUserAccount
    {
        public string Email { get; set; } = "guest@test.com";
        public string Password { get; private set; } = string.Empty;

        public GuestUserAccount()
        {
            Password = GenerateSecurePassword();
        }

        private static string GenerateSecurePassword()
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            
            // Generate a password that meets ASP.NET Identity requirements:
            // - At least 6 characters (we'll use 16 for better security)
            // - Contains uppercase letter, lowercase letter, digit, and special character
            
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*";
            const string allChars = uppercase + lowercase + digits + special;
            
            var password = new char[16];
            
            // Ensure at least one character from each required category
            password[0] = uppercase[GetRandomIndex(rng, uppercase.Length)];
            password[1] = lowercase[GetRandomIndex(rng, lowercase.Length)];
            password[2] = digits[GetRandomIndex(rng, digits.Length)];
            password[3] = special[GetRandomIndex(rng, special.Length)];
            
            // Fill remaining positions with random characters from all categories
            for (int i = 4; i < password.Length; i++)
            {
                password[i] = allChars[GetRandomIndex(rng, allChars.Length)];
            }
            
            // Shuffle the password to avoid predictable patterns
            for (int i = password.Length - 1; i > 0; i--)
            {
                int j = GetRandomIndex(rng, i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }
            
            return new string(password);
        }
        
        private static int GetRandomIndex(System.Security.Cryptography.RandomNumberGenerator rng, int maxValue)
        {
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            return (int)(BitConverter.ToUInt32(bytes, 0) % maxValue);
        }
    }

    public class InviteOnlyOptions
    {
        public bool EnableInviteOnly { get; set; } = false;
        public bool EnableEmailInvites { get; set; } = false;
        public bool EnableInviteCodes { get; set; } = true;
        public int DefaultCodeExpirationHours { get; set; } = 24;
        public int DefaultEmailInviteExpirationHours { get; set; } = 72;
        public int MaxActiveCodesPerAdmin { get; set; } = 50;
    }

    public class SettingsOptions
    {
        public bool EnableBackups { get; set; } = true;
        public string EncryptionKey { get; set; } = "DefaultKeyForDevelopmentOnly!";
        public CacheSettingsOptions CacheSettings { get; set; } = new CacheSettingsOptions();
    }

    public class CacheSettingsOptions
    {
        public bool Enabled { get; set; } = true;
        public int ExpirationMinutes { get; set; } = 30;
    }
}
