using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>
/// Permite que las herramientas de EF Core creen el contexto sin arrancar toda
/// la API ni depender de JWT, SMTP u otros servicios externos. En CI reutiliza
/// la conexión recibida por variables de entorno; el fallback solo se utiliza
/// para construir el modelo y generar migraciones, sin abrir una conexión.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=127.0.0.1;Port=3306;Database=solqaryn_dev_design;User=root;Password=root;SslMode=None;AllowPublicKeyRetrieval=True;";

        var serverVersionText = Environment.GetEnvironmentVariable("Database__ServerVersion") ?? "8.4.3";
        if (!Version.TryParse(serverVersionText, out var serverVersion))
            serverVersion = new Version(8, 4, 3);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(connectionString, new MySqlServerVersion(serverVersion))
            .Options;

        return new AppDbContext(options);
    }
}
