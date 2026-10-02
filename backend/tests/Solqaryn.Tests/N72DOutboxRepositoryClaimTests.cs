using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N72DOutboxRepositoryClaimTests
{
    private static readonly DateTime AhoraUtc = new(2026, 9, 13, 10, 50, 0, DateTimeKind.Utc);

    [Fact]
    public async Task SegundoClaim_No_Obtiene_Mensaje_Y_Outcome_Queda_Guardado_Por_Intento()
    {
        await using var harness = await Harness.CreateAsync();
        var id = await harness.InsertAsync("idem-claim");

        await using var db1 = harness.NewContext();
        await using var db2 = harness.NewContext();
        var repo1 = new MensajeOutboxRepository(db1);
        var repo2 = new MensajeOutboxRepository(db2);

        var primero = await repo1.ClaimDisponiblesAsync(harness.EmpresaId, AhoraUtc, 10);
        var segundo = await repo2.ClaimDisponiblesAsync(harness.EmpresaId, AhoraUtc, 10);

        var claimed = Assert.Single(primero);
        Assert.Empty(segundo);
        Assert.Equal(id, claimed.Id);
        Assert.Equal(1, claimed.Intentos);
        Assert.Equal(EstadoMensajeOutbox.Procesando, claimed.Estado);

        Assert.True(await repo1.MarcarEntregadoAsync(harness.EmpresaId, id, 1, AhoraUtc.AddSeconds(1)));
        Assert.False(await repo2.MarcarEntregadoAsync(harness.EmpresaId, id, 1, AhoraUtc.AddSeconds(2)));

        await using var verify = harness.NewContext();
        var persisted = await verify.Set<MensajeOutbox>().AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(EstadoMensajeOutbox.Entregado, persisted.Estado);
        Assert.Equal(AhoraUtc.AddSeconds(1), persisted.EntregadoEnUtc);
    }

    [Fact]
    public async Task StaleRecovery_Invalida_Outcome_Del_Worker_Viejo_Y_Permite_Reclaim()
    {
        await using var harness = await Harness.CreateAsync();
        var id = await harness.InsertAsync("idem-stale");

        await using var db = harness.NewContext();
        var repo = new MensajeOutboxRepository(db);
        var first = Assert.Single(await repo.ClaimDisponiblesAsync(harness.EmpresaId, AhoraUtc, 10));
        Assert.Equal(1, first.Intentos);

        var recoveryTime = AhoraUtc.AddMinutes(11);
        var recovered = await repo.RecuperarProcesandoStaleAsync(
            harness.EmpresaId,
            AhoraUtc.AddMinutes(10),
            recoveryTime,
            10);

        Assert.Equal(1, recovered);
        Assert.False(await repo.MarcarEntregadoAsync(harness.EmpresaId, id, 1, recoveryTime.AddSeconds(1)));

        var second = Assert.Single(await repo.ClaimDisponiblesAsync(harness.EmpresaId, recoveryTime, 10));
        Assert.Equal(id, second.Id);
        Assert.Equal(2, second.Intentos);
        Assert.Equal(EstadoMensajeOutbox.Procesando, second.Estado);
    }

    [Fact]
    public async Task Claim_Esta_Aislado_Por_Tenant()
    {
        await using var harness = await Harness.CreateAsync();
        await harness.InsertAsync("idem-tenant", harness.OtraEmpresaId);

        await using var db = harness.NewContext();
        var repo = new MensajeOutboxRepository(db);

        Assert.Empty(await repo.ClaimDisponiblesAsync(harness.EmpresaId, AhoraUtc, 10));
        Assert.Single(await repo.ClaimDisponiblesAsync(harness.OtraEmpresaId, AhoraUtc, 10));
    }

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
            var name = $"n72d-{Guid.NewGuid():N}";
            var connectionString = $"Data Source=file:{name}?mode=memory&cache=shared";
            var keeper = new SqliteConnection(connectionString);
            await keeper.OpenAsync();
            var harness = new Harness(keeper, connectionString);
            await using var db = harness.NewContext();
            await db.Database.EnsureCreatedAsync();

            var empresa = new Empresa("N7.2.D Tenant A");
            var otraEmpresa = new Empresa("N7.2.D Tenant B");
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

        public async Task<int> InsertAsync(string idempotencyKey, int? empresaId = null)
        {
            await using var db = NewContext();
            var mensaje = MensajeOutbox.Crear(
                empresaId ?? EmpresaId,
                "inventario.cambiado",
                "{\"safe\":true}",
                idempotencyKey,
                creadoEnUtc: AhoraUtc.AddMinutes(-1));
            db.Add(mensaje);
            await db.SaveChangesAsync();
            return mensaje.Id;
        }

        public async ValueTask DisposeAsync() => await _keeper.DisposeAsync();
    }
}
