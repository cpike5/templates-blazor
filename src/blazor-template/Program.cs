using BlazorTemplate.Components;
using BlazorTemplate.Components.Account;
using BlazorTemplate.Configuration;
using BlazorTemplate.Data;
using BlazorTemplate.Extensions;
using BlazorTemplate.Services;
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
                    options.DefaultAuthenticateScheme = "JWT_OR_COOKIE";
                    options.DefaultChallengeScheme = "JWT_OR_COOKIE";
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

            // Add Authorization services
            builder.Services.AddAuthorizationBuilder()
                .AddPolicy("AdminOnly", policy => policy.RequireRole("Administrator"))
                .AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Administrator"));

            // Add API services
            builder.Services.AddControllers();
            
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
                    c.SwaggerDoc("v1", new() { Title = "Blazor Template API", Version = "v1" });
                    c.AddSecurityDefinition("Bearer", new()
                    {
                        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                        Name = "Authorization",
                        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                        Scheme = "Bearer"
                    });
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

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            // Enable CORS
            if (app.Configuration.GetValue<bool>("Api:EnableCors"))
            {
                app.UseCors("ApiPolicy");
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
