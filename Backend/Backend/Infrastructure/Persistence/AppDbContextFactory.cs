using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Backend.Infrastructure.Persistence
{
    /// <summary>
    /// Design-time factory for EF Core migration generation tools.
    /// Allows 'dotnet ef migrations' commands to scaffold migrations without requiring a live database connection.
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseMySQL("Server=localhost;Database=resiliencex;Uid=root;Pwd=Aditya@2005;", mysqlOptions =>
            {
                mysqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            });

            return new AppDbContext(optionsBuilder.Options, null);
        }
    }
}
