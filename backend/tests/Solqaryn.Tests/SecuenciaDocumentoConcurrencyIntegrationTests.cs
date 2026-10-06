using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class SecuenciaDocumentoConcurrencyIntegrationTests
{
    private static string ConnectionString(string database) =>
        $"Server=localhost;Port=3306;Database={database};User=root;Password=root;";

    private static DbContextOptions<AppDbContext> Options(string database) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(ConnectionString(database), mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

    [Fact]
    public async Task ReservasConcurrentes_ProducenNumeracionUnicaMonotonicaYAuditoriaAtomica()
    {
        var database = $"test_sequence_concurrency_{Guid.NewGuid():N}";
        var options = Options(database);
        var empresaId = 0;
        var secuenciaId = 0;

        try
        {
            await using (var setup = new AppDbContext(options))
            {
                await setup.Database.MigrateAsync();
                var empresa = new Empresa("Empresa concurrencia de secuencias CI");
                setup.Set<Empresa>().Add(empresa);
                await setup.SaveChangesAsync();
                empresaId = empresa.Id;

                var secuencia = new SecuenciaDocumento(empresaId, null, "FACTURA", "CI-", 4, 100);
                setup.SecuenciasDocumento.Add(secuencia);
                await setup.SaveChangesAsync();
                secuenciaId = secuencia.Id;
            }

            var reservations = await Task.WhenAll(Enumerable.Range(0, 10).Select(async _ =>
            {
                await using var context = new AppDbContext(options);
                var scope = new Mock<IUsuarioScopeService>();
                scope.Setup(x => x.ObtenerActualAsync(empresaId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new UsuarioTenantScopeActual(1, empresaId, 1, "Administrador", true));
                var service = new SecuenciaDocumentoService(context, scope.Object);
                return await service.ReservarSiguienteAsync(
                    new ReservarSecuenciaDocumentoRequest(empresaId, null, "FACTURA"));
            }));

            var values = reservations.Select(x => x.Valor).OrderBy(x => x).ToArray();
            Assert.Equal(Enumerable.Range(101, 10).Select(x => (long)x), values);
            Assert.Equal(10, reservations.Select(x => x.Numero).Distinct().Count());

            await using var verify = new AppDbContext(options);
            var persisted = await verify.SecuenciasDocumento.AsNoTracking().SingleAsync(x => x.Id == secuenciaId);
            var audits = await verify.RegistrosAuditoria.AsNoTracking()
                .CountAsync(x => x.Entidad == nameof(SecuenciaDocumento) && x.ReferenciaId == secuenciaId);
            Assert.Equal(110, persisted.UltimoValor);
            Assert.Equal(10, audits);
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
