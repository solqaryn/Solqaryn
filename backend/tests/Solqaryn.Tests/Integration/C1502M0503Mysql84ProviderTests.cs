using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using MySqlConnector;
using Xunit;

namespace Solqaryn.Tests.Integration;

/// <summary>
/// C15-02.M05.03: real Oracle EF Core / MySQL 8.4 integration exclusively
/// in disposable GitHub Actions. Does not certify live JWT or tenancy.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C1502M0503Mysql84ProviderTests
{
    private const string DatabaseName = "solqaryn_c1502_m0503";

    private sealed class EvidenceRow
    {
        public string Id { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string ProviderKey { get; set; } = string.Empty;
        public string EvidenceStatus { get; set; } = string.Empty;
    }

    private sealed class EvidenceContext : DbContext
    {
        private readonly string _tenantId;

        public EvidenceContext(DbContextOptions<EvidenceContext> options, string tenantId)
            : base(options)
        {
            _tenantId = !string.IsNullOrWhiteSpace(tenantId)
                ? tenantId
                : throw new ArgumentException("TEST_TENANT_REQUIRED", nameof(tenantId));
        }

        public DbSet<EvidenceRow> Evidence => Set<EvidenceRow>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EvidenceRow>(entity =>
            {
                entity.ToTable("V22C1502M0503Evidence");
                entity.HasKey(row => row.Id);
                entity.Property(row => row.Id).HasMaxLength(36).IsRequired();
                entity.Property(row => row.TenantId).HasMaxLength(32).IsRequired();
                entity.Property(row => row.ProviderKey).HasMaxLength(80).IsRequired();
                entity.Property(row => row.EvidenceStatus).HasMaxLength(32).IsRequired();
                entity.HasQueryFilter(row => row.TenantId == _tenantId);
            });
        }
    }

    private static string DisposableConnection()
    {
        var raw = Environment.GetEnvironmentVariable("C1502_M0503_MYSQL_TEST")
            ?? throw new InvalidOperationException("MYSQL84_EPHEMERAL_TEST_CONNECTION_REQUIRED");
        var parsed = new MySqlConnectionStringBuilder(raw);
        if (parsed.Server != "127.0.0.1" ||
            parsed.Port != 3306 ||
            parsed.Database != DatabaseName ||
            parsed.UserID != "root" ||
            parsed.SslMode != MySqlSslMode.Disabled)
            throw new InvalidOperationException("MYSQL84_TEST_ONLY_LOOPBACK_DATABASE_REQUIRED");
        return raw;
    }

    private static EvidenceContext OpenContext(string tenant)
    {
        var options = new DbContextOptionsBuilder<EvidenceContext>()
            .UseMySQL(DisposableConnection()).Options;
        return new EvidenceContext(options, tenant);
    }

    private static async Task EnsureTestTableAsync()
    {
        await using var connection = new MySqlConnection(DisposableConnection());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "CREATE TABLE IF NOT EXISTS V22C1502M0503Evidence (" +
            "Id varchar(36) NOT NULL PRIMARY KEY," +
            "TenantId varchar(32) NOT NULL," +
            "ProviderKey varchar(80) NOT NULL," +
            "EvidenceStatus varchar(32) NOT NULL" +
            ") ENGINE=InnoDB";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DeleteTestEvidenceAsync(params string[] ids)
    {
        await using var connection = new MySqlConnection(DisposableConnection());
        await connection.OpenAsync();
        foreach (var id in ids)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM V22C1502M0503Evidence WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public async Task OracleProviderConnectsOnlyToEphemeralMysql84()
    {
        await using var connection = new MySqlConnection(DisposableConnection());
        await connection.OpenAsync();
        await using var versionCommand = connection.CreateCommand();
        versionCommand.CommandText = "SELECT VERSION()";
        var version = Convert.ToString(await versionCommand.ExecuteScalarAsync());
        Assert.NotNull(version);
        Assert.StartsWith("8.4.", version);
        await using var context = OpenContext("tenant-A");
        Assert.True(await context.Database.CanConnectAsync());
    }

    [Fact]
    public async Task OracleEfPersistsAAndBWithoutCrossTenantReadOrIdor()
    {
        await EnsureTestTableAsync();
        var idA = Guid.NewGuid().ToString("D");
        var idB = Guid.NewGuid().ToString("D");
        try
        {
            await using (var a = OpenContext("tenant-A"))
            {
                a.Evidence.Add(new EvidenceRow { Id = idA, TenantId = "tenant-A",
                    ProviderKey = "mysql-84", EvidenceStatus = "UNVERIFIED" });
                await a.SaveChangesAsync();
            }
            await using (var b = OpenContext("tenant-B"))
            {
                b.Evidence.Add(new EvidenceRow { Id = idB, TenantId = "tenant-B",
                    ProviderKey = "mysql-84", EvidenceStatus = "UNVERIFIED" });
                await b.SaveChangesAsync();
            }
            await using (var a = OpenContext("tenant-A"))
            {
                Assert.Equal(idA, (await a.Evidence.AsNoTracking().SingleAsync()).Id);
                Assert.Null(await a.Evidence.AsNoTracking()
                    .SingleOrDefaultAsync(row => row.Id == idB));
            }
            await using (var b = OpenContext("tenant-B"))
            {
                Assert.Equal(idB, (await b.Evidence.AsNoTracking().SingleAsync()).Id);
                Assert.Null(await b.Evidence.AsNoTracking()
                    .SingleOrDefaultAsync(row => row.Id == idA));
            }
        }
        finally { await DeleteTestEvidenceAsync(idA, idB); }
    }

    [Fact]
    public async Task OracleEfTransactionRollbackLeavesNoPartialEvidence()
    {
        await EnsureTestTableAsync();
        var id = Guid.NewGuid().ToString("D");
        try
        {
            await using (var a = OpenContext("tenant-A"))
            await using (var transaction = await a.Database.BeginTransactionAsync())
            {
                a.Evidence.Add(new EvidenceRow { Id = id, TenantId = "tenant-A",
                    ProviderKey = "mysql-84", EvidenceStatus = "PENDING" });
                await a.SaveChangesAsync();
                Assert.True(await a.Evidence.AnyAsync(row => row.Id == id));
                await transaction.RollbackAsync();
            }
            await using var verifier = OpenContext("tenant-A");
            Assert.False(await verifier.Evidence.AsNoTracking()
                .AnyAsync(row => row.Id == id));
        }
        finally { await DeleteTestEvidenceAsync(id); }
    }

    [Fact]
    public async Task HostileTenantIsParameterizedAndNeverSeesOtherTenant()
    {
        await EnsureTestTableAsync();
        var id = Guid.NewGuid().ToString("D");
        try
        {
            await using (var a = OpenContext("tenant-A"))
            {
                a.Evidence.Add(new EvidenceRow { Id = id, TenantId = "tenant-A",
                    ProviderKey = "mysql-84", EvidenceStatus = "UNVERIFIED" });
                await a.SaveChangesAsync();
            }
            await using var hostile = OpenContext("tenant-A' OR '1'='1");
            Assert.False(await hostile.Evidence.AsNoTracking()
                .AnyAsync(row => row.Id == id));
            await using var b = OpenContext("tenant-B");
            Assert.False(await b.Evidence.AsNoTracking()
                .AnyAsync(row => row.Id == id));
        }
        finally { await DeleteTestEvidenceAsync(id); }
    }

    [Fact]
    public async Task PreCancelledEfWriteDoesNotPersistRecord()
    {
        await EnsureTestTableAsync();
        var id = Guid.NewGuid().ToString("D");
        try
        {
            await using (var a = OpenContext("tenant-A"))
            {
                a.Evidence.Add(new EvidenceRow { Id = id, TenantId = "tenant-A",
                    ProviderKey = "mysql-84", EvidenceStatus = "PENDING" });
                using var cancellation = new CancellationTokenSource();
                cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => a.SaveChangesAsync(cancellation.Token));
            }
            await using var verifier = OpenContext("tenant-A");
            Assert.False(await verifier.Evidence.AsNoTracking()
                .AnyAsync(row => row.Id == id));
        }
        finally { await DeleteTestEvidenceAsync(id); }
    }

    [Fact]
    public void MissingTenantIsRejectedBeforeDatabaseAccess()
    {
        Assert.Throws<ArgumentException>(() => OpenContext(""));
        Assert.Throws<ArgumentException>(() => OpenContext(" "));
    }
}
