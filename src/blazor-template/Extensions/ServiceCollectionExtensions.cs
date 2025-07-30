using BlazorTemplate.Configuration.Navigation;
using BlazorTemplate.Services;

namespace BlazorTemplate.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNavigationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<NavigationConfiguration>(configuration.GetSection(NavigationConfiguration.SectionName));
            services.AddScoped<INavigationService, NavigationService>();
            return services;
        }
    }
}
