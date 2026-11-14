using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public sealed class TtlValidationTests
{
    private static readonly DateTime ClaimUtc = new(2026, 9, 13, 12, 35, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Stale_Claim_No_Se_Recupera_Antes_Del_Cutoff_Y_Si_En_El_Limite()
    {
        await using var harness = await Harness.CreateAsync();
        await harness.InsertAsync("ttl-boundary");

        await using var db = harness.NewContext();
        var repo = new MensajeOutboxRepository(db);
        Assert.Single(await repo.ClaimDisponiblesAsync(harness.EmpresaId, ClaimUtc, 10));

        var antesDelCutoff = await repo.RecuperarProcesandoStaleAsync(
            harness.EmpresaId,
            ClaimUtc.AddTicks(-1),
            ClaimUtc.AddMinutes(10),
            10);

        Assert.Equal(0, antesDelCutoff);

        var enElCutoff = await repo.RecuperarProcesandoStaleAsync(
            harness.EmpresaId,
            ClaimUtc,
            ClaimUtc.AddMinutes(10),
            10);

        Assert.Equal(1, enElCutoff);
    }

    [Fact]
    public async Task Stale_Recovery_Esta_Aislado_Por_Tenant()
    {
        await using var harness = await Harness.CreateAsync();
        await harness.InsertAsync("ttl-a", harness.EmpresaId);
        await harness.InsertAsync("ttl-b", harness.OtraEmpresaId);

        await using var db = harness.NewContext();
        var repo = new MensajeOutboxRepository(db);
        Assert.Single(await repo.ClaimDisponiblesAsync(harness.EmpresaId, ClaimUtc, 10));
        Assert.Single(await repo.ClaimDisponiblesAsync(harness.OtraEmpresaId, ClaimUtc, 10));

        var recuperados = await repo.RecuperarProcesandoStaleAsync(
            harness.EmpresaId,
            ClaimUtc,
            ClaimUtc.AddMinutes(10),
            10);

        Assert.Equal(1, recuperados);

        await using var verify = harness.NewContext();
        var other = await verify.Set<MensajeOutbox>()
            .AsNoTracking()
            .SingleAsync(x => x.EmpresaId == harness.OtraEmpresaId);
        Assert.NotNull(other.ProcesandoDesdeUtc);
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
            var name = $"n72g-ttl-{Guid.NewGuid():N}";
            var connectionString = $"Data Source=file:{name}?mode=memory&cache=shared";
            var keeper = new SqliteConnection(connectionString);
            await keeper.OpenAsync();
            var harness = new Harness(keeper, connectionString);
            await using var db = harness.NewContext();
            await db.Database.EnsureCreatedAsync();

            var empresa = new Empresa("N7.2.G TTL A");
            var otraEmpresa = new Empresa("N7.2.G TTL B");
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

        public async Task InsertAsync(string key, int? empresaId = null)
        {
            await using var db = NewContext();
            db.Add(MensajeOutbox.Crear(
                empresaId ?? EmpresaId,
                "inventario.cambiado",
                "{\"safe\":true}",
                key,
                creadoEnUtc: ClaimUtc.AddMinutes(-1)));
            await db.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync() => await _keeper.DisposeAsync();
    }
}
