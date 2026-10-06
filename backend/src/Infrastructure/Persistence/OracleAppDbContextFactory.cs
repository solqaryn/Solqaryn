using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MySql.EntityFrameworkCore.Extensions;

namespace Solqaryn.Infrastructure.Persistence;

public sealed class OracleAppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Design-time MySQL connection must be supplied through the environment.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connectionString, mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        return new AppDbContext(options);
    }
}
