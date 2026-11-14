using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.API;

public sealed class OracleAppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=127.0.0.1;Port=3306;Database=solqaryn_phase7_design;User=root;SslMode=Disabled;AllowPublicKeyRetrieval=True;";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connectionString, mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        return new AppDbContext(options);
    }
}
