using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Infrastructure.Persistence;

var modeRaw = (Environment.GetEnvironmentVariable("SOLQARYN_DATABASE_BOOTSTRAP_MODE") ?? "adopt")
    .Trim()
    .ToLowerInvariant();

var mode = modeRaw switch
{
    "fresh" => OracleDatabaseBootstrapMode.Fresh,
    "adopt" => OracleDatabaseBootstrapMode.Adopt,
    _ => throw new InvalidOperationException(
        "SOLQARYN_DATABASE_BOOTSTRAP_MODE must be 'fresh' or 'adopt'.")
};

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "ConnectionStrings__DefaultConnection must be supplied through the environment.");

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseMySQL(connectionString, mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
    .Options;

await using var db = new AppDbContext(options);
var result = await OracleDatabaseBootstrapper.ApplyAsync(db, mode);

Console.WriteLine(
    $"SOLQARYN_ORACLE_BOOTSTRAP=PASS mode={result.EffectiveMode.ToString().ToLowerInvariant()} tables={result.ApplicationTables} pending={result.PendingMigrations}");
