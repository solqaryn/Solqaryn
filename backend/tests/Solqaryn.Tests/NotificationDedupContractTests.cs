using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public sealed class NotificationDedupContractTests
{
    private static readonly DateTime CreadoUtc = new(2026, 9, 13, 12, 35, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Misma_Clave_No_Puede_Duplicarse_Dentro_Del_Mismo_Tenant()
    {
        await using var harness = await Harness.CreateAsync();
        await harness.InsertAsync(harness.EmpresaId, "notify-42");

        await using var duplicateDb = harness.NewContext();
        duplicateDb.Add(Crear(harness.EmpresaId, "notify-42"));

        await Assert.ThrowsAsync<DbUpdateException>(() => duplicateDb.SaveChangesAsync());
    }

    [Fact]
    public async Task Misma_Clave_Es_Valida_En_Tenants_Diferentes()
    {
        await using var harness = await Harness.CreateAsync();
        await harness.InsertAsync(harness.EmpresaId, "notify-shared");
        await harness.InsertAsync(harness.OtraEmpresaId, "notify-shared");

        await using var db = harness.NewContext();
        var repo = new MensajeOutboxRepository(db);

        Assert.True(await repo.ExisteClaveIdempotenciaAsync(harness.EmpresaId, " notify-shared "));
        Assert.True(await repo.ExisteClaveIdempotenciaAsync(harness.OtraEmpresaId, "notify-shared"));
    }

    [Fact]
    public async Task Lookup_De_Dedup_No_Filtra_Claves_De_Otro_Tenant()
    {
        await using var harness = await Harness.CreateAsync();
        await harness.InsertAsync(harness.OtraEmpresaId, "notify-other-tenant");

        await using var db = harness.NewContext();
        var repo = new MensajeOutboxRepository(db);

        Assert.False(await repo.ExisteClaveIdempotenciaAsync(harness.EmpresaId, "notify-other-tenant"));
        Assert.True(await repo.ExisteClaveIdempotenciaAsync(harness.OtraEmpresaId, "notify-other-tenant"));
    }

    private static MensajeOutbox Crear(int empresaId, string key) => MensajeOutbox.Crear(
        empresaId,
        "notification.dispatch",
        "{\"recipient\":\"allow-listed\"}",
        key,
        creadoEnUtc: CreadoUtc);

    private sealed class Harness : IAsyncDisposable
    {
        private readonly SqliteConnection _keeper;
        private readonly string _connectionString;

        private Harness(SqliteConnection keeper, string connectionString)
        {
            _keeper = keeper;
            _connectionString = connectionString;
        }

        public int EmpresaId { get; private set; }
        public int OtraEmpresaId { get; private set; }

        public static async Task<Harness> CreateAsync()
        {
            var name = $"n72g-dedup-{Guid.NewGuid():N}";
            var connectionString = $"Data Source=file:{name}?mode=memory&cache=shared";
            var keeper = new SqliteConnection(connectionString);
            await keeper.OpenAsync();
            var harness = new Harness(keeper, connectionString);
            await using var db = harness.NewContext();
            await db.Database.EnsureCreatedAsync();

            var empresa = new Empresa("N7.2.G Dedup A");
            var otraEmpresa = new Empresa("N7.2.G Dedup B");
            db.AddRange(empresa, otraEmpresa);
            await db.SaveChangesAsync();
            harness.EmpresaId = empresa.Id;
            harness.OtraEmpresaId = otraEmpresa.Id;
            return harness;
        }

        public AppDbContext NewContext()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.CreateFunction<string?, int>("CHAR_LENGTH", value => value?.Length ?? 0);
            connection.Open();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection, contextOwnsConnection: true)
                .Options;
            return new AppDbContext(options);
        }

        public async Task InsertAsync(int empresaId, string key)
        {
            await using var db = NewContext();
            db.Add(Crear(empresaId, key));
            await db.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync() => await _keeper.DisposeAsync();
    }
}
