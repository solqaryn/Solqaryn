using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Infrastructure.Persistence;

var mode = (Environment.GetEnvironmentVariable("SOLQARYN_DATABASE_BOOTSTRAP_MODE") ?? "adopt").Trim().ToLowerInvariant();
if (mode is not ("fresh" or "adopt"))
    throw new InvalidOperationException("SOLQARYN_DATABASE_BOOTSTRAP_MODE must be 'fresh' or 'adopt'.");

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings__DefaultConnection must be supplied through the environment.");

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseMySQL(connectionString, mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
    .Options;

await using var db = new AppDbContext(options);
await db.Database.OpenConnectionAsync();

static async Task<long> ScalarLongAsync(AppDbContext db, string sql)
{
    await using var command = db.Database.GetDbConnection().CreateCommand();
    command.CommandText = sql;
    return Convert.ToInt64(await command.ExecuteScalarAsync());
}

var applicationTablesBefore = await ScalarLongAsync(
    db,
    """
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_TYPE = 'BASE TABLE'
      AND TABLE_NAME <> '__EFMigrationsHistory';
    """);

if (mode == "fresh")
{
    if (applicationTablesBefore != 0)
        throw new InvalidOperationException($"Fresh bootstrap refused because {applicationTablesBefore} application tables already exist.");

    await db.Database.CloseConnectionAsync();
    var created = await db.Database.EnsureCreatedAsync();
    if (!created)
        throw new InvalidOperationException("Fresh bootstrap expected to create an empty database schema.");
    await db.Database.OpenConnectionAsync();
}
else
{
    if (applicationTablesBefore < 100)
        throw new InvalidOperationException($"Adoption refused because only {applicationTablesBefore} application tables exist.");
}

var beforeAdoption = await ScalarLongAsync(
    db,
    """
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_TYPE = 'BASE TABLE'
      AND TABLE_NAME <> '__EFMigrationsHistory';
    """);

await db.Database.CloseConnectionAsync();
await db.Database.MigrateAsync();
await db.Database.OpenConnectionAsync();

var afterAdoption = await ScalarLongAsync(
    db,
    """
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_TYPE = 'BASE TABLE'
      AND TABLE_NAME <> '__EFMigrationsHistory';
    """);

if (afterAdoption != beforeAdoption)
    throw new InvalidOperationException($"Oracle baseline adoption changed physical table count: before={beforeAdoption}, after={afterAdoption}.");

var marker = await ScalarLongAsync(
    db,
    """
    SELECT COUNT(*)
    FROM __EFMigrationsHistory
    WHERE MigrationId = '20261006111818_OracleBaseline';
    """);

if (marker != 1)
    throw new InvalidOperationException($"Oracle baseline marker mismatch: {marker}.");

var pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();
if (pending.Length != 0)
    throw new InvalidOperationException($"Pending Oracle migrations remain: {string.Join(",", pending)}.");

Console.WriteLine($"SOLQARYN_ORACLE_BOOTSTRAP=PASS mode={mode} tables={afterAdoption} pending=0");
