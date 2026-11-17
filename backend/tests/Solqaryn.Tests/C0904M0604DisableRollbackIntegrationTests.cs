using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C09-04.M06.04: rollback-only rehearsal on genuine ephemeral MySQL 8.4.
/// A separate schema defect CK_ConfiguracionesWhatsAppEmpresa_Numero_E164
/// prevents claiming that WhatsApp configuration deactivation was tested in DB.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0904M0604DisableRollbackIntegrationTests
{
    private static MySqlConnectionStringBuilder LocalOnly(string input)
    {
        var cs = new MySqlConnectionStringBuilder(input);
        if (cs.Server is not ("127.0.0.1" or "localhost") || cs.Port != 3306)
            throw new InvalidOperationException("Only local ephemeral MySQL 8.4:3306 is permitted.");
        return cs;
    }

    [Theory]
    [InlineData("Server=remote.example;Port=3306;Database=prod;User=root;Password=dummy;")]
    [InlineData("Server=localhost;Port=3307;Database=prod;User=root;Password=dummy;")]
    public void RejectRemoteOrWrongPort(string connection)
        => Assert.Throws<InvalidOperationException>(() => LocalOnly(connection));

    [Fact]
    public async Task RollbackProcessingOfTenantAKeepsBothLegitimateMessages()
    {
        var cs = LocalOnly(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Ephemeral MySQL 8.4 CI required."));
        cs.Database = "v19_c0904m0604_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(cs.ConnectionString, o => o.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;
        try
        {
            int tenantA, tenantB;
            await using (var setup = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
                var a = new Empresa("M0604 Synthetic A");
                var b = new Empresa("M0604 Synthetic B");
                setup.Set<Empresa>().AddRange(a, b);
                await setup.SaveChangesAsync();
                (tenantA, tenantB) = (a.Id, b.Id);
                Assert.NotEqual(tenantA, tenantB);
                setup.Set<MensajeOutbox>().AddRange(
                    MensajeOutbox.Crear(tenantA, "crm.mensaje.preparado",
                        "{\"test\":\"a\"}", "rollback-preserve-a"),
                    MensajeOutbox.Crear(tenantB, "crm.mensaje.preparado",
                        "{\"test\":\"b\"}", "rollback-preserve-b"));
                await setup.SaveChangesAsync();
            }

            await using (var checking = new AppDbContext(options))
            {
                Assert.Equal(2, await checking.Set<MensajeOutbox>().CountAsync());
                var a = await checking.Set<MensajeOutbox>().AsNoTracking()
                    .SingleAsync(x => x.EmpresaId == tenantA && x.ClaveIdempotencia == "rollback-preserve-a");
                var b = await checking.Set<MensajeOutbox>().AsNoTracking()
                    .SingleAsync(x => x.EmpresaId == tenantB && x.ClaveIdempotencia == "rollback-preserve-b");
                Assert.Equal(EstadoMensajeOutbox.Pendiente, a.Estado);
                Assert.Equal(EstadoMensajeOutbox.Pendiente, b.Estado);
                Assert.Equal(0, a.Intentos);
                Assert.Equal(0, b.Intentos);
                Assert.Equal("{\"test\":\"a\"}", a.PayloadJson);
                Assert.Equal("{\"test\":\"b\"}", b.PayloadJson);
                Assert.Equal(0, await checking.Set<MensajeOutbox>()
                    .CountAsync(x => x.EmpresaId == tenantB && x.ClaveIdempotencia == "rollback-preserve-a"));
            }

            // A real SQL transaction on tenant A changes state, then rolls back
            // without deleting or changing any committed business rows.
            await using (var modifying = new AppDbContext(options))
            {
                await using var tx = await modifying.Database.BeginTransactionAsync();
                var a = await modifying.Set<MensajeOutbox>()
                    .SingleAsync(x => x.EmpresaId == tenantA && x.ClaveIdempotencia == "rollback-preserve-a");
                a.MarcarProcesando(DateTime.UtcNow.AddMinutes(5));
                await modifying.SaveChangesAsync();
                Assert.Equal(EstadoMensajeOutbox.Procesando, a.Estado);
                Assert.Equal(1, a.Intentos);
                await tx.RollbackAsync();
            }

            await using (var final = new AppDbContext(options))
            {
                Assert.Equal(2, await final.Set<MensajeOutbox>().CountAsync());
                var a = await final.Set<MensajeOutbox>().AsNoTracking()
                    .SingleAsync(x => x.EmpresaId == tenantA && x.ClaveIdempotencia == "rollback-preserve-a");
                var b = await final.Set<MensajeOutbox>().AsNoTracking()
                    .SingleAsync(x => x.EmpresaId == tenantB && x.ClaveIdempotencia == "rollback-preserve-b");
                Assert.Equal(EstadoMensajeOutbox.Pendiente, a.Estado);
                Assert.Equal(EstadoMensajeOutbox.Pendiente, b.Estado);
                Assert.Equal(0, a.Intentos);
                Assert.Equal(0, b.Intentos);
                Assert.Null(a.ProcesandoDesdeUtc);
                Assert.Null(b.ProcesandoDesdeUtc);
                Assert.Equal("{\"test\":\"a\"}", a.PayloadJson);
                Assert.Equal("{\"test\":\"b\"}", b.PayloadJson);
            }
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
