using BlazorTemplate.Data;
using System.Diagnostics;

namespace BlazorTemplate.Services.Setup
{
    /// <summary>
    /// Service interface for handling first-time application setup operations
    /// </summary>
    public interface IFirstTimeSetupService
    {
        /// <summary>
        /// Performs first-time setup operations including database initialization
        /// </summary>
        void Setup();
    }

    /// <summary>
    /// Service implementation for handling first-time application setup operations
    /// </summary>
    public class FirstTimeSetupService : IFirstTimeSetupService
    {
        private readonly ILogger<FirstTimeSetupService> _logger;
        private readonly IServiceProvider _services;

        public FirstTimeSetupService(ILogger<FirstTimeSetupService> logger, IServiceProvider services)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            
            _logger.LogDebug("FirstTimeSetupService initialized");
        }

        /// <summary>
        /// Performs comprehensive first-time setup operations for the application
        /// </summary>
        /// <exception cref='InvalidOperationException'>Thrown when setup operations fail</exception>
        public void Setup()
        {
            var stopwatch = Stopwatch.StartNew();
            
            _logger.LogInformation("Starting first-time application setup");
            
            try
            {
                ConfigureDatabase();
                
                stopwatch.Stop();
                _logger.LogInformation("First-time application setup completed successfully in {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "First-time application setup failed after {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException("First-time setup failed. See inner exception for details.", ex);
            }
        }

        /// <summary>
        /// Configures and initializes the database with initial data
        /// </summary>
        /// <exception cref='InvalidOperationException'>Thrown when database configuration fails</exception>
        private void ConfigureDatabase()
        {
            var stopwatch = Stopwatch.StartNew();
            
            _logger.LogInformation("Starting database configuration and seeding");
            
            try
            {
                using var scope = _services.CreateScope();
                _logger.LogDebug("Created service scope for database operations");
                
                var dataSeeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
                _logger.LogDebug("Retrieved DataSeeder service from dependency injection container");
                
                _logger.LogInformation("Beginning database seeding operations");
                dataSeeder.SeedDatabase();
                
                stopwatch.Stop();
                _logger.LogInformation("Database configuration and seeding completed successfully in {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Database configuration failed after {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException("Database configuration and seeding failed. See inner exception for details.", ex);
            }
        }
    }
}
