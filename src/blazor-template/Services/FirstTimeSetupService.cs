using BlazorTemplate.Data;

namespace BlazorTemplate.Services
{
    public interface IFirstTimeSetupService
    {
        void Setup();
    }

    public class FirstTimeSetupService : IFirstTimeSetupService
    {
        private readonly ILogger<FirstTimeSetupService> _logger;
        private readonly IServiceProvider _services;

        public FirstTimeSetupService(ILogger<FirstTimeSetupService> logger, IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        public void Setup()
        {
            ConfigureDatabase();
        }

        private void ConfigureDatabase()
        {
            using var scope = _services.CreateScope();
            var dataSeeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
            dataSeeder.SeedDatabase();
        }
    }
}
