using BlazorTemplate.Authorization;
using BlazorTemplate.Components;
using BlazorTemplate.Components.Account;
using BlazorTemplate.Configuration;
using BlazorTemplate.Data;
using BlazorTemplate.Extensions;
using BlazorTemplate.Middleware;
using BlazorTemplate.Services;
using BlazorTemplate.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

namespace BlazorTemplate
{
    public class Program
    {
        private static ILogger<Program> _logger;
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            bool firstTimeSetup = Convert.ToBoolean(builder.Configuration["Site:Setup:EnableSetupMode"]);

            // Configure logging
            builder.Services.AddSerilog(logger =>
            {
                logger.ReadFrom.Configuration(builder.Configuration)
                    .Enrich.FromLogContext()
                    .WriteTo.Console();
            });

            // Configure Site Options
            builder.Services.Configure<ConfigurationOptions>(builder.Configuration.GetSection(ConfigurationOptions.SectionName));
            builder.Services.AddNavigationServices(builder.Configuration);
            builder.Services.AddAdminServices();

            // Register JWT service
            builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();


            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<IdentityUserAccessor>();
            builder.Services.AddScoped<IdentityRedirectManager>();
            builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

            // Configure JWT
            var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
            var key = Encoding.ASCII.GetBytes(jwtKey);

            var authBuilder = builder.Services.AddAuthentication(options =>
                {
                    options.DefaultScheme = IdentityConstants.ApplicationScheme;
                    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
                });

            authBuilder.AddIdentityCookies();
            
            authBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
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
                        string? authorization = context.Request.Headers.Authorization;
                        if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
                            return JwtBearerDefaults.AuthenticationScheme;
                        return IdentityConstants.ApplicationScheme;
                    };
                });

            if (!string.IsNullOrEmpty(builder.Configuration["Authentication:Google:ClientId"]) && !string.IsNullOrEmpty(builder.Configuration["Authentication:Google:ClientSecret"]))
            {
                authBuilder.AddGoogle(a =>
                {
                    a.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                    a.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
                });
            }

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddRoles<IdentityRole>()
                .AddRoleManager<RoleManager<IdentityRole>>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();

            builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

            // Add Authorization services with custom handler
            builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, ManageOwnAccountHandler>();
            
            builder.Services.AddAuthorization(options =>
            {
                // Configure API authorization policies
                ApiPolicies.ConfigurePolicies(options);
            });

            // Add API services
            builder.Services.AddControllers();

            // Add API Versioning
            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultVersion = new BlazorTemplate.Middleware.ApiVersion(1, 0);
                options.SupportedVersions = new List<BlazorTemplate.Middleware.ApiVersion>
                {
                    new BlazorTemplate.Middleware.ApiVersion(1, 0),
                    new BlazorTemplate.Middleware.ApiVersion(1, 1)
                };
                options.AssumeDefaultVersionWhenUnspecified = true;
            });

            // Add Rate Limiting
            if (builder.Configuration.GetValue<bool>("Api:RateLimiting:EnableRateLimiting"))
            {
                builder.Services.AddRateLimiting(options =>
                {
                    options.EnableRateLimiting = true;
                    
                    var generalLimit = builder.Configuration["Api:RateLimiting:GeneralRateLimit"];
                    var authLimit = builder.Configuration["Api:RateLimiting:AuthRateLimit"];

                    if (!string.IsNullOrEmpty(generalLimit))
                    {
                        options.AuthenticatedUserLimit = RateLimitRule.Parse(generalLimit, "General");
                    }

                    if (!string.IsNullOrEmpty(authLimit))
                    {
                        options.AuthEndpointLimit = RateLimitRule.Parse(authLimit, "Auth");
                    }
                });
            }
            
            // Add CORS
            if (builder.Configuration.GetValue<bool>("Api:EnableCors"))
            {
                var allowedOrigins = builder.Configuration.GetSection("Api:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("ApiPolicy", policy =>
                    {
                        policy.WithOrigins(allowedOrigins)
                              .AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials();
                    });
                });
            }

            // Add Swagger
            if (builder.Configuration.GetValue<bool>("Api:EnableSwagger"))
            {
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen(c =>
                {
                    // API Documentation
                    c.SwaggerDoc("v1", new()
                    {
                        Title = "Blazor Template API",
                        Version = "v1",
                        Description = "A comprehensive API for user management and authentication in the Blazor template application.",
                        Contact = new()
                        {
                            Name = "API Support",
                            Email = "api-support@blazortemplate.com"
                        },
                        License = new()
                        {
                            Name = "MIT License",
                            Url = new Uri("https://opensource.org/licenses/MIT")
                        }
                    });

                    // Include XML comments for better documentation
                    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
                    if (File.Exists(xmlPath))
                    {
                        c.IncludeXmlComments(xmlPath);
                    }

                    // Security scheme for JWT
                    c.AddSecurityDefinition("Bearer", new()
                    {
                        Name = "Authorization",
                        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                        Description = "JWT Authorization header using the Bearer scheme. Enter your token below."
                    });

                    // Global security requirement
                    c.AddSecurityRequirement(new()
                    {
                        {
                            new()
                            {
                                Reference = new()
                                {
                                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });

                    // Custom operation filters
                    c.EnableAnnotations();
                    c.UseInlineDefinitionsForEnums();
                    
                    // Custom schema IDs
                    c.CustomSchemaIds(type => type.FullName);

                    // Response examples and filters
                    c.SchemaFilter<SwaggerExampleSchemaFilter>();
                    c.OperationFilter<SwaggerDefaultResponsesOperationFilter>();
                    c.OperationFilter<SwaggerAuthorizationOperationFilter>();
                });
            }

            if (firstTimeSetup)
            {
                builder.Services.AddFirstTimeSetupServices(builder.Configuration);
            }            

            var app = builder.Build();

            using var scope = app.Services.CreateScope();

            // Get a reference to the DB Context
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (firstTimeSetup)
            {
                var setupService = scope.ServiceProvider.GetRequiredService<IFirstTimeSetupService>();
                setupService.Setup();
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
                
                // Enable Swagger in development
                if (app.Configuration.GetValue<bool>("Api:EnableSwagger"))
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Blazor Template API v1");
                        c.RoutePrefix = "api/docs";
                    });
                }
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Add API error handling for all environments
            app.UseApiErrorHandling();

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            // Enable CORS
            if (app.Configuration.GetValue<bool>("Api:EnableCors"))
            {
                app.UseCors("ApiPolicy");
            }

            // Enable API Versioning
            app.UseApiVersioning();

            // Enable Rate Limiting (before authentication)
            if (app.Configuration.GetValue<bool>("Api:RateLimiting:EnableRateLimiting"))
            {
                app.UseRateLimiting();
            }
            
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            // Add additional endpoints required by the Identity /Account Razor components.
            app.MapAdditionalIdentityEndpoints();

            // Map API controllers
            app.MapControllers();

            app.Run();
        }


    }
}
