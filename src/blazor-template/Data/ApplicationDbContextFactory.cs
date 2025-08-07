using Microsoft.EntityFrameworkCore;

namespace BlazorTemplate.Data
{
    /// <summary>
    /// Factory for creating ApplicationDbContext instances with independent configuration.
    /// This avoids DI conflicts between scoped DbContext and singleton factory services.
    /// </summary>
    public class ApplicationDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly string _connectionString;

        public ApplicationDbContextFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public ApplicationDbContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(_connectionString);
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}