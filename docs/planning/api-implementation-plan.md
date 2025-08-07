# API Layer Implementation Plan
*Blazor Template REST API Integration*

## Executive Summary

This document outlines the implementation plan for adding a comprehensive REST API layer to the existing Blazor Server template. The API will provide programmatic access to user management, authentication, role management, and administrative functions while maintaining security and consistency with the existing Blazor application.

## Current Architecture Analysis

### Existing Services Ready for API Integration
- **UserManagementService**: Complete CRUD operations for users
- **InviteService**: Invite code and email invite management
- **NavigationService**: Role-based navigation configuration
- **AdminRoleService**: Role management operations
- **FirstTimeSetupService**: System initialization

### Available DTOs
- `UserDto`, `UserDetailDto`, `UserStatsDto`
- `UserActivityDto`
- Request models: `UserSearchCriteria`, `CreateUserRequest`, `UpdateUserRequest`
- `PagedResult<T>` for pagination

### Current Authentication
- ASP.NET Core Identity with cookie authentication
- Role-based authorization
- Google OAuth integration

## API Architecture Design

### 1. API Versioning Strategy
```
/api/v1/users
/api/v1/roles  
/api/v1/admin
/api/v1/auth
```

### 2. Authentication Strategy

#### JWT Token Implementation
- **Access Tokens**: Short-lived (15-30 minutes) for API access
- **Refresh Tokens**: Long-lived (7-30 days) stored securely
- **Dual Authentication**: Support both cookie (Blazor) and JWT (API)

#### Token Endpoint Structure
```
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/revoke
GET  /api/v1/auth/profile
```

### 3. API Endpoint Structure

#### User Management API
```http
GET    /api/v1/users                    # List users (paginated)
GET    /api/v1/users/{id}              # Get user details
POST   /api/v1/users                   # Create user
PUT    /api/v1/users/{id}              # Update user
DELETE /api/v1/users/{id}              # Delete user
POST   /api/v1/users/{id}/lock         # Lock user account
POST   /api/v1/users/{id}/unlock       # Unlock user account
POST   /api/v1/users/{id}/reset-password # Reset user password
GET    /api/v1/users/{id}/activity     # Get user activity log
```

#### Role Management API
```http
GET    /api/v1/roles                   # List all roles
GET    /api/v1/roles/{id}              # Get role details
POST   /api/v1/roles                   # Create role
PUT    /api/v1/roles/{id}              # Update role
DELETE /api/v1/roles/{id}              # Delete role
POST   /api/v1/users/{id}/roles        # Assign role to user
DELETE /api/v1/users/{id}/roles/{roleId} # Remove role from user
```

#### Invitation API
```http
GET    /api/v1/invites                 # List invitations
POST   /api/v1/invites/codes           # Create invite codes
POST   /api/v1/invites/emails          # Send email invitations
PUT    /api/v1/invites/{id}            # Update invitation
DELETE /api/v1/invites/{id}            # Cancel invitation
GET    /api/v1/invites/{token}/validate # Validate invite token
```

#### Admin Dashboard API
```http
GET    /api/v1/admin/stats             # System statistics
GET    /api/v1/admin/health            # System health metrics
GET    /api/v1/admin/activities        # Recent system activities
GET    /api/v1/admin/settings          # System configuration
PUT    /api/v1/admin/settings          # Update system configuration
```

#### Navigation API
```http
GET    /api/v1/navigation              # Get navigation for current user
GET    /api/v1/navigation/all          # Get all navigation items (admin)
```

## Implementation Phases

### Phase 1: Foundation (Week 1-2)
**Goal**: Establish JWT authentication and basic API infrastructure

#### Tasks:
1. **Add NuGet Packages**
   ```xml
   <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.18" />
   <PackageReference Include="Swashbuckle.AspNetCore" Version="6.8.0" />
   <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.18" />
   ```

2. **Create API-Specific DTOs**
   - `LoginRequest`, `LoginResponse`
   - `RefreshTokenRequest`, `TokenResponse`
   - `ApiResponse<T>` wrapper
   - `ApiError` for standardized error responses

3. **JWT Service Implementation**
   ```csharp
   public interface IJwtTokenService
   {
       Task<TokenResponse> GenerateTokenAsync(ApplicationUser user);
       Task<TokenResponse> RefreshTokenAsync(string refreshToken);
       Task RevokeTokenAsync(string refreshToken);
       ClaimsPrincipal? GetPrincipalFromToken(string token);
   }
   ```

4. **Configure JWT Authentication**
   - Update `Program.cs` to support dual authentication
   - Add JWT middleware configuration
   - Create refresh token storage mechanism

5. **Basic API Infrastructure**
   - Create `ApiControllerBase` with common functionality
   - Implement `ApiResponse<T>` standardization
   - Add global exception handling middleware

### Phase 2: Core APIs (Week 3-4)
**Goal**: Implement user management and authentication endpoints

#### Tasks:
1. **Authentication Controller** (`AuthController`)
   - Login endpoint with JWT generation
   - Token refresh mechanism
   - Profile information endpoint
   - Token revocation

2. **Users Controller** (`UsersController`)
   - CRUD operations for users
   - User search with pagination
   - User statistics endpoint
   - Activity log retrieval

3. **Input Validation & Model Binding**
   - Add FluentValidation or use Data Annotations
   - Custom model validation attributes
   - Standardized error responses

4. **API Documentation**
   - Swagger/OpenAPI configuration
   - XML comments for documentation
   - Example request/response models

### Phase 3: Administrative APIs (Week 5-6)
**Goal**: Implement role management and administrative functions

#### Tasks:
1. **Roles Controller** (`RolesController`)
   - Role CRUD operations
   - Role assignment/removal
   - Role-based user listings

2. **Admin Controller** (`AdminController`)
   - System statistics and health metrics
   - User management bulk operations
   - System configuration endpoints

3. **Invitations Controller** (`InvitationsController`)
   - Invite code generation and management
   - Email invitation system
   - Invitation validation

4. **Navigation Controller** (`NavigationController`)
   - Dynamic navigation based on user roles
   - Navigation configuration management

### Phase 4: Security & Performance (Week 7-8)
**Goal**: Implement advanced security features and optimize performance

#### Tasks:
1. **Advanced Security Features**
   - Rate limiting middleware
   - API key authentication (optional)
   - CORS configuration for specific clients
   - Request/response logging

2. **Caching Implementation**
   - Memory cache for frequently accessed data
   - Distributed cache preparation (Redis ready)
   - Cache invalidation strategies

3. **Performance Optimization**
   - Response compression
   - Pagination optimization
   - Query optimization review
   - Response time monitoring

4. **API Versioning**
   - URL path versioning implementation
   - Backward compatibility strategy
   - Migration path documentation

### Phase 5: Testing & Documentation (Week 9-10)
**Goal**: Comprehensive testing and documentation

#### Tasks:
1. **Unit Testing**
   - Controller unit tests
   - Service layer tests
   - JWT token handling tests
   - Validation tests

2. **Integration Testing**
   - End-to-end API testing
   - Authentication flow testing
   - Database integration tests

3. **API Documentation**
   - Complete Swagger documentation
   - Postman collection creation
   - API usage examples
   - Client SDK considerations

4. **Deployment Configuration**
   - Environment-specific configurations
   - Security hardening checklist
   - Monitoring and logging setup

## Security Considerations

### Authentication Security
- **Token Security**: Short-lived access tokens, secure refresh token storage
- **Password Security**: Maintain existing ASP.NET Identity security features  
- **Brute Force Protection**: Rate limiting on authentication endpoints
- **Token Rotation**: Automatic refresh token rotation

### Authorization Security
- **Role-Based Access**: Consistent with existing Blazor implementation
- **Resource-Level Security**: Ensure users can only access their own resources
- **Admin Endpoints**: Additional security for administrative functions
- **Audit Trail**: Log all API access for security monitoring

### Data Security
- **Input Validation**: Comprehensive validation on all endpoints
- **SQL Injection Protection**: Maintain Entity Framework best practices
- **XSS Prevention**: Proper data encoding in responses
- **CSRF Protection**: Token-based API naturally resistant to CSRF

### Network Security
- **HTTPS Only**: Enforce HTTPS for all API endpoints
- **CORS Policy**: Restrictive CORS configuration
- **Content Security**: Proper content-type validation
- **Error Handling**: Secure error messages (no sensitive data exposure)

## Technical Implementation Details

### JWT Configuration Example
```csharp
// Program.cs JWT Configuration
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "JWT_OR_COOKIE";
    options.DefaultChallengeScheme = "JWT_OR_COOKIE";
})
.AddCookie(IdentityConstants.ApplicationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
})
.AddPolicyScheme("JWT_OR_COOKIE", "JWT_OR_COOKIE", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        string authorization = context.Request.Headers.Authorization;
        if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
            return JwtBearerDefaults.AuthenticationScheme;
        return IdentityConstants.ApplicationScheme;
    };
});
```

### API Response Standardization
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class PagedApiResponse<T> : ApiResponse<IEnumerable<T>>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
```

### Controller Base Class
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<ApiResponse<T>> Success<T>(T data, string message = "Success")
    {
        return Ok(new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        });
    }

    protected ActionResult<ApiResponse<object>> Error(string message, int statusCode = 400)
    {
        return StatusCode(statusCode, new ApiResponse<object>
        {
            Success = false,
            Message = message
        });
    }
}
```

## Database Considerations

### Additional Tables Needed
```sql
-- Refresh Tokens Table
CREATE TABLE RefreshTokens (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Token NVARCHAR(MAX) NOT NULL,
    UserId NVARCHAR(450) NOT NULL,
    ExpiryDate DATETIME2 NOT NULL,
    IsRevoked BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id)
);

-- API Usage Logs (Optional)
CREATE TABLE ApiUsageLogs (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(450),
    Endpoint NVARCHAR(500) NOT NULL,
    Method NVARCHAR(10) NOT NULL,
    StatusCode INT NOT NULL,
    ResponseTime INT NOT NULL,
    IpAddress NVARCHAR(50),
    UserAgent NVARCHAR(MAX),
    Timestamp DATETIME2 DEFAULT GETUTCDATE()
);
```

## Configuration Requirements

### appsettings.json Additions
```json
{
  "Jwt": {
    "Key": "[Generate a secure key]",
    "Issuer": "BlazorTemplate",
    "Audience": "BlazorTemplateApi",
    "AccessTokenExpirationMinutes": 30,
    "RefreshTokenExpirationDays": 7
  },
  "Api": {
    "EnableSwagger": true,
    "EnableCors": true,
    "AllowedOrigins": ["https://yourdomain.com"],
    "RateLimiting": {
      "EnableRateLimiting": true,
      "GeneralRateLimit": "100:1m",
      "AuthRateLimit": "5:1m"
    }
  }
}
```

## Success Metrics

### Phase 1-2 Success Criteria
- [ ] JWT authentication working with existing cookie authentication
- [ ] Basic CRUD operations for users via API
- [ ] Swagger documentation accessible
- [ ] Unit tests for authentication flow

### Phase 3-4 Success Criteria
- [ ] Complete API coverage for existing Blazor functionality
- [ ] Rate limiting and security measures implemented
- [ ] Performance benchmarks meet requirements
- [ ] Integration tests passing

### Final Success Criteria
- [ ] Full API documentation complete
- [ ] Security audit passed
- [ ] Performance requirements met (sub-200ms response times)
- [ ] Ready for production deployment

## Risk Mitigation

### Technical Risks
- **Breaking Changes**: Maintain backward compatibility with existing Blazor app
- **Security Vulnerabilities**: Regular security reviews and dependency updates
- **Performance Impact**: Load testing and optimization
- **Authentication Complexity**: Thorough testing of dual authentication

### Mitigation Strategies
- **Incremental Development**: Phase-based implementation with testing
- **Feature Flags**: Enable/disable API features during development
- **Monitoring**: Comprehensive logging and monitoring from day one
- **Documentation**: Maintain detailed documentation throughout development

## Conclusion

This API implementation plan provides a comprehensive roadmap for adding REST API capabilities to the existing Blazor template. The phased approach ensures minimal disruption to existing functionality while adding powerful programmatic access to all system features.

The implementation leverages existing services and DTOs, maintaining consistency with the current architecture while adding new capabilities for external integrations, mobile applications, and third-party services.

**Estimated Timeline**: 10 weeks
**Required Skills**: ASP.NET Core API, JWT authentication, Entity Framework, API security
**Testing Strategy**: Unit tests, integration tests, security testing
**Documentation**: Swagger/OpenAPI, usage examples, client SDKs