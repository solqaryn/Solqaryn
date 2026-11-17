using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C09-04.M05.03: integracion REAL Oracle EF Core/MySQL 8.4 efimero.
/// Prueba CAS durable de claims/reintentos entre DbContexts y frontera A/B.
/// No conecta SMTP/WhatsApp, no invoca webhooks, no usa Aiven/produccion.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0904M0503MySqlOutboxIntegrationTests
{
    private static MySqlConnectionStringBuilder ExigirServidorEfimero(string raw)
    {
        var parsed = new MySqlConnectionStringBuilder(raw);
        if (parsed.Server is not ("127.0.0.1" or "localhost") || parsed.Port != 3306)
            throw new InvalidOperationException("MySQL local efimero: host y puerto obligatorios.");
        return parsed;
    }

    [Theory]
    [InlineData("Server=example.com;Port=3306;Database=production;User=root;Password=dummy;")]
    [InlineData("Server=localhost;Port=3307;Database=test;User=root;Password=dummy;")]
    public void RechazarConexionRemotaOPuertoNoAislado(string cadena)
    {
        Assert.Throws<InvalidOperationException>(() => ExigirServidorEfimero(cadena));
    }

    [Fact]
    public async Task ClaimsReintentosYRolloverDurables_AisladosPorTenant_SinReplay()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("MySQL efimero 8.4 requerido desde GitHub Actions.");
        ExigirServidorEfimero(raw);

        var conexion = new MySqlConnectionStringBuilder(raw)
        {
            Database = "v19_c0904m0503_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(conexion, o => o.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            int tenantA, tenantB;
            await using (var db = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
                var a = new Empresa("V19 Outbox integracion A");
                var b = new Empresa("V19 Outbox integracion B");
                db.Set<Empresa>().AddRange(a, b);
                await db.SaveChangesAsync();
                (tenantA, tenantB) = (a.Id, b.Id);
                Assert.NotEqual(tenantA, tenantB);
            }

            await using (var db = new AppDbContext(options))
            {
                var repo = new MensajeOutboxRepository(db);
                await repo.AddAsync(MensajeOutbox.Crear(
                    tenantA, "crm.mensaje.preparado", "{\"canal\":\"email\"}", "integracion-clave-a"));
                await repo.AddAsync(MensajeOutbox.Crear(
                    tenantB, "crm.mensaje.preparado", "{\"canal\":\"email\"}", "integracion-clave-b"));
                await db.SaveChangesAsync();
            }

            var ahora = DateTime.UtcNow.AddMinutes(3);
            await using var contextoA = new AppDbContext(options);
            await using var contextoB = new AppDbContext(options);
            var repoA = new MensajeOutboxRepository(contextoA);
            var repoB = new MensajeOutboxRepository(contextoB);

            var intentoA = Assert.Single(await repoA.ClaimDisponiblesAsync(tenantA, ahora, 10));
            var intentoB = Assert.Single(await repoB.ClaimDisponiblesAsync(tenantB, ahora, 10));
            Assert.Equal(1, intentoA.Intentos);
            Assert.Equal(1, intentoB.Intentos);
            Assert.NotEqual(intentoA.Id, intentoB.Id);
            Assert.Empty(await repoB.ClaimDisponiblesAsync(tenantA, ahora, 10));
            Assert.Empty(await repoA.ClaimDisponiblesAsync(tenantB, ahora, 10));

            // Un worker de B no puede confirmar ni fallar un claim de A.
            Assert.False(await repoB.MarcarEntregadoAsync(
                tenantB, intentoA.Id, intentoA.Intentos, ahora.AddMinutes(1)));
            Assert.False(await repoB.RegistrarFalloAsync(
                tenantB, intentoA.Id, intentoA.Intentos, "RETRYABLE",
                ahora.AddMinutes(1), ahora.AddMinutes(5), false));
            // Un numero de intento incorrecto tampoco confirma el claim de A.
            Assert.False(await repoA.MarcarEntregadoAsync(
                tenantA, intentoA.Id, intentoA.Intentos + 1, ahora.AddMinutes(1)));

            Assert.True(await repoA.RegistrarFalloAsync(
                tenantA, intentoA.Id, intentoA.Intentos, "RETRYABLE",
                ahora.AddMinutes(1), ahora.AddMinutes(5), false));
            Assert.Empty(await repoB.ClaimDisponiblesAsync(tenantA, ahora.AddMinutes(4), 10));

            var segundoIntento = Assert.Single(
                await repoB.ClaimDisponiblesAsync(tenantA, ahora.AddMinutes(6), 10));
            Assert.Equal(intentoA.Id, segundoIntento.Id);
            Assert.Equal(intentoA.Intentos + 1, segundoIntento.Intentos);

            // El worker stale no puede marcar entregado ni duplicar el efecto.
            Assert.False(await repoA.MarcarEntregadoAsync(
                tenantA, intentoA.Id, intentoA.Intentos, ahora.AddMinutes(7)));
            Assert.True(await repoB.MarcarEntregadoAsync(
                tenantA, segundoIntento.Id, segundoIntento.Intentos, ahora.AddMinutes(7)));
            Assert.True(await repoA.MarcarEntregadoAsync(
                tenantB, intentoB.Id, intentoB.Intentos, ahora.AddMinutes(7)));
            Assert.Empty(await repoA.ClaimDisponiblesAsync(tenantA, ahora.AddMinutes(8), 10));

            // Un SaveChanges en una transaccion abortada no publica un mensaje.
            await using (var transaccional = new AppDbContext(options))
            {
                await using var tx = await transaccional.Database.BeginTransactionAsync();
                await new MensajeOutboxRepository(transaccional).AddAsync(
                    MensajeOutbox.Crear(tenantA, "crm.mensaje.preparado",
                        "{\"canal\":\"email\"}", "rollback-no-publicar"));
                await transaccional.SaveChangesAsync();
                await tx.RollbackAsync();
            }

            await using (var verificacion = new AppDbContext(options))
            {
                var repositorio = new MensajeOutboxRepository(verificacion);
                var confirmadoA = await repositorio.GetByClaveIdempotenciaAsync(
                    tenantA, "integracion-clave-a");
                var confirmadoB = await repositorio.GetByClaveIdempotenciaAsync(
                    tenantB, "integracion-clave-b");
                Assert.NotNull(confirmadoA);
                Assert.NotNull(confirmadoB);
                Assert.Equal(EstadoMensajeOutbox.Entregado, confirmadoA.Estado);
                Assert.Equal(EstadoMensajeOutbox.Entregado, confirmadoB.Estado);
                Assert.Equal(2, confirmadoA.Intentos);
                Assert.Equal(1, confirmadoB.Intentos);
                Assert.Null(await repositorio.GetByClaveIdempotenciaAsync(
                    tenantB, "integracion-clave-a"));
                Assert.Null(await repositorio.GetByClaveIdempotenciaAsync(
                    tenantA, "integracion-clave-b"));
                Assert.Null(await repositorio.GetByClaveIdempotenciaAsync(
                    tenantA, "rollback-no-publicar"));
            }
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
