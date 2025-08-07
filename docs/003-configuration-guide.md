# Configuration Guide

## Overview

The Blazor Template uses a centralized configuration system with hierarchical settings from `appsettings.json`. This guide covers all configuration options, their purposes, and recommended values for different environments.

## Configuration Structure

### Site Settings

#### Administration
```json
{
  "Site": {
    "Administration": {
      "AdminEmail": "admin@yourcompany.com"
    }
  }
}
```

**AdminEmail**: The email address that automatically receives Administrator role upon registration. This should be set to the primary administrator's email address.

#### Setup Configuration
```json
{
  "Site": {
    "Setup": {
      "EnableSetupMode": true,
      "EnableGuestUser": false,
      "GuestUser": {
        "Email": "guest@test.com"
      },
      "InviteOnly": {
        "EnableInviteOnly": true,
        "EnableEmailInvites": false,
        "EnableInviteCodes": true,
        "DefaultCodeExpirationHours": 24,
        "DefaultEmailInviteExpirationHours": 72,
        "MaxActiveCodesPerAdmin": 50
      }
    },
    "Title": "Your Application Name"
  }
}
```

**Setup Mode Settings:**
- `EnableSetupMode`: Enable for initial setup, disable in production
- `EnableGuestUser`: Create a demo guest user with auto-generated password
- `GuestUser.Email`: Email address for the guest user account

**Invite System Settings:**
- `EnableInviteOnly`: Require valid invites for user registration
- `EnableEmailInvites`: Allow email-based invitations
- `EnableInviteCodes`: Allow short code invitations
- `DefaultCodeExpirationHours`: Default expiration time for new invite codes
- `DefaultEmailInviteExpirationHours`: Default expiration for email invites
- `MaxActiveCodesPerAdmin`: Maximum active codes per administrator

### JWT Configuration

```json
{
  "Jwt": {
    "Key": "ThisIsAVerySecureKeyThatIsAtLeast256BitsLongForHMACSHA256SigningAndShouldBeStoredSecurely",
    "Issuer": "YourApplicationName",
    "Audience": "YourApplicationNameApi",
    "AccessTokenExpirationMinutes": 30,
    "RefreshTokenExpirationDays": 7
  }
}
```

**JWT Settings:**
- `Key`: HMAC-SHA256 signing key (minimum 256 bits/32 characters)
- `Issuer`: JWT issuer claim (typically your application name)
- `Audience`: JWT audience claim (typically your API identifier)
- `AccessTokenExpirationMinutes`: How long access tokens remain valid
- `RefreshTokenExpirationDays`: How long refresh tokens remain valid

**Security Notes:**
- The signing key should be cryptographically secure and unique per environment
- Store production keys in secure configuration (Azure Key Vault, etc.)
- Never commit production keys to source control

### API Configuration

```json
{
  "Api": {
    "EnableSwagger": true,
    "EnableCors": true,
    "AllowedOrigins": [
      "https://localhost:5001",
      "http://localhost:5000",
      "https://yourdomain.com"
    ],
    "RateLimiting": {
      "EnableRateLimiting": false,
      "GeneralRateLimit": "100:1m",
      "AuthRateLimit": "5:1m"
    }
  }
}
```

**API Settings:**
- `EnableSwagger`: Enable Swagger/OpenAPI documentation (disable in production)
- `EnableCors`: Enable Cross-Origin Resource Sharing
- `AllowedOrigins`: Array of allowed origins for CORS requests

**Rate Limiting Settings:**
- `EnableRateLimiting`: Enable request rate limiting
- `GeneralRateLimit`: General API rate limit (format: "requests:timespan")
- `AuthRateLimit`: Authentication endpoint rate limit (stricter)

**Time Span Formats:**
- `s` - seconds
- `m` - minutes  
- `h` - hours
- `d` - days

### Navigation Configuration

```json
{
  "Navigation": {
    "Items": [
      {
        "Id": "home",
        "Title": "Home",
        "Href": "/",
        "Icon": "fas fa-home",
        "RequiredRoles": [],
        "Order": 10
      },
      {
        "Id": "admin-section",
        "Title": "Administration",
        "Icon": "fas fa-cog",
        "RequiredRoles": ["Administrator"],
        "Order": 900,
        "Children": [
          {
            "Id": "users",
            "Title": "Users",
            "Href": "/Admin/Users",
            "Icon": "fas fa-users",
            "RequiredRoles": ["Administrator"],
            "Order": 10
          },
          {
            "Id": "invites",
            "Title": "Invite Management", 
            "Href": "/Admin/Invites",
            "Icon": "fas fa-envelope-open-text",
            "RequiredRoles": ["Administrator"],
            "Order": 20
          }
        ]
      },
      {
        "Id": "external-links",
        "Title": "Resources",
        "Icon": "fas fa-external-link-alt",
        "RequiredRoles": [],
        "Order": 950,
        "Children": [
          {
            "Id": "github",
            "Title": "GitHub Repository",
            "Href": "https://github.com/your-org/your-repo",
            "Icon": "fab fa-github",
            "RequiredRoles": [],
            "Order": 10,
            "IsExternal": true
          }
        ]
      }
    ]
  }
}
```

**Navigation Item Properties:**
- `Id`: Unique identifier for the navigation item
- `Title`: Display text for the menu item
- `Href`: URL path (optional for parent groups)
- `Icon`: Font Awesome icon class
- `RequiredRoles`: Array of roles required to see this item (empty = visible to all)
- `Order`: Sort order for menu items (lower numbers appear first)
- `Children`: Sub-menu items for grouped navigation
- `IsVisible`: Whether the item should be displayed (default: true)
- `IsExternal`: Whether the link opens in a new tab/window (default: false)

### Database Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=your_app_name;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

**Connection String Components:**
- `Server`: Database server location
- `Database`: Database name
- `Trusted_Connection`: Use Windows authentication (set to False for SQL auth)
- `TrustServerCertificate`: Accept self-signed certificates (development only)
- `MultipleActiveResultSets`: Allow multiple result sets (required for EF Core)

### Logging Configuration

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    }
  }
}
```

**Logging Levels:**
- `Verbose`: Most detailed logging
- `Debug`: Debug information
- `Information`: General application flow
- `Warning`: Warnings and recoverable errors
- `Error`: Errors and exceptions
- `Fatal`: Critical errors that cause application termination

### Theme Configuration

```json
{
  "ColorScheme": "trust-blue"
}
```

**Available Themes:**
- `default` - Professional Neutral
- `executive-purple` - Executive Purple
- `sunset-rose` - Sunset Rose  
- `trust-blue` - Trust Blue
- `growth-green` - Growth Green
- `innovation-orange` - Innovation Orange
- `midnight-teal` - Midnight Teal
- `heritage-burgundy` - Heritage Burgundy
- `platinum-gray` - Platinum Gray
- `deep-navy` - Deep Navy

## Environment-Specific Configuration

### Development (`appsettings.Development.json`)

```json
{
  "Site": {
    "Setup": {
      "EnableSetupMode": true,
      "EnableGuestUser": true
    }
  },
  "Api": {
    "EnableSwagger": true,
    "EnableCors": true,
    "RateLimiting": {
      "EnableRateLimiting": false
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  }
}
```

**Development Settings:**
- Enable setup mode for easy testing
- Enable guest user for demo purposes
- Enable Swagger for API documentation
- Disable rate limiting for development ease
- More verbose logging

### Staging (`appsettings.Staging.json`)

```json
{
  "Site": {
    "Setup": {
      "EnableSetupMode": false,
      "EnableGuestUser": false
    }
  },
  "Api": {
    "EnableSwagger": true,
    "RateLimiting": {
      "EnableRateLimiting": true
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    }
  }
}
```

**Staging Settings:**
- Disable setup mode (production-like)
- Disable guest user
- Enable Swagger for testing
- Enable rate limiting
- Information-level logging

### Production (`appsettings.Production.json`)

```json
{
  "Site": {
    "Setup": {
      "EnableSetupMode": false,
      "EnableGuestUser": false
    }
  },
  "Api": {
    "EnableSwagger": false,
    "RateLimiting": {
      "EnableRateLimiting": true,
      "GeneralRateLimit": "1000:1h",
      "AuthRateLimit": "10:1m"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Warning"
    }
  }
}
```

**Production Settings:**
- Disable setup mode
- Disable guest user
- Disable Swagger (security)
- Enable stricter rate limiting
- Warning-level logging only

## Security Configuration

### Secure Key Management

**Development:**
```json
{
  "Jwt": {
    "Key": "development-key-at-least-256-bits-long-for-testing-only-never-use-in-production"
  }
}
```

**Production (Azure Key Vault):**
```csharp
// In Program.cs
if (builder.Environment.IsProduction())
{
    var keyVaultUrl = builder.Configuration["KeyVaultUrl"];
    var credential = new DefaultAzureCredential();
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), credential);
}
```

```json
{
  "Jwt": {
    "Key": "@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/JwtSigningKey/)"
  }
}
```

### Connection String Security

**Development:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=blazor_template_dev;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

**Production (Azure SQL):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/DatabaseConnectionString/)"
  }
}
```

## Configuration Validation

### Startup Validation

The application validates critical configuration settings at startup:

```csharp
// In Program.cs
builder.Services.Configure<ConfigurationOptions>(builder.Configuration);
builder.Services.AddSingleton<IValidateOptions<ConfigurationOptions>, ConfigurationOptionsValidator>();
```

**Validated Settings:**
- JWT signing key length and format
- Required email addresses for admin setup
- Database connection string format
- Navigation structure integrity

### Runtime Validation

Settings are validated during application initialization:

```csharp
public class ConfigurationOptionsValidator : IValidateOptions<ConfigurationOptions>
{
    public ValidateOptionsResult Validate(string name, ConfigurationOptions options)
    {
        if (string.IsNullOrEmpty(options.Jwt.Key) || options.Jwt.Key.Length < 32)
        {
            return ValidateOptionsResult.Fail("JWT key must be at least 32 characters");
        }
        
        // Additional validation logic...
        
        return ValidateOptionsResult.Success;
    }
}
```

## Troubleshooting Configuration

### Common Issues

#### JWT Authentication Failures
1. **Verify Key Length**: Must be at least 256 bits (32 characters)
2. **Check Issuer/Audience**: Must match between generation and validation
3. **Validate Expiration**: Ensure reasonable expiration times
4. **Review Clock Skew**: Consider time synchronization between servers

#### Database Connection Issues
1. **Connection String Format**: Verify proper SQL Server format
2. **Network Connectivity**: Ensure database server is accessible
3. **Authentication**: Check SQL Server authentication method
4. **Database Existence**: Ensure target database exists

#### Navigation Not Appearing
1. **Role Configuration**: Verify user has required roles
2. **JSON Syntax**: Ensure valid JSON in navigation configuration
3. **Order Values**: Check ordering and hierarchy
4. **Service Registration**: Confirm NavigationService is registered

#### Invite System Not Working
1. **Feature Flags**: Verify EnableInviteOnly and related flags
2. **Email Configuration**: Check email service setup if using email invites
3. **Expiration Times**: Validate expiration hour settings
4. **Database Tables**: Ensure invite tables exist (run migrations)

### Configuration Testing

Test configuration settings with unit tests:

```csharp
[Test]
public void Configuration_ShouldHaveValidJwtSettings()
{
    // Arrange
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build();
    
    var options = new ConfigurationOptions();
    configuration.Bind(options);
    
    // Act & Assert
    Assert.That(options.Jwt.Key, Is.Not.Null.And.Length.GreaterThan(31));
    Assert.That(options.Jwt.Issuer, Is.Not.Null.And.Not.Empty);
    Assert.That(options.Jwt.AccessTokenExpirationMinutes, Is.GreaterThan(0));
}
```

## Best Practices

### Configuration Management
1. **Environment Separation**: Use different settings per environment
2. **Secret Management**: Never store secrets in source control
3. **Validation**: Validate configuration at startup
4. **Documentation**: Document all configuration options
5. **Defaults**: Provide sensible defaults where possible

### Security Considerations
1. **Key Rotation**: Regularly rotate JWT signing keys
2. **Access Control**: Limit configuration file access
3. **Audit Trail**: Log configuration changes
4. **Encryption**: Encrypt sensitive configuration data
5. **Backup**: Secure backup of configuration files

### Performance Optimization
1. **Caching**: Cache frequently accessed configuration
2. **Lazy Loading**: Load configuration only when needed
3. **Startup Time**: Minimize configuration processing at startup
4. **Memory Usage**: Avoid storing large configuration objects
5. **Hot Reload**: Support configuration changes without restart where appropriate