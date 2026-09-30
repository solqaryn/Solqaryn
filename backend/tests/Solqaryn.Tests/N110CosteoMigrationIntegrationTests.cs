using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class N110CosteoMigrationIntegrationTests
{
    private const string MigracionAnterior = "20260817100000_N1_9_TrazabilidadLotesSeries";

    private static DbContextOptions<AppDbContext> CreateOptions(string dbName) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                $"Server=localhost;Port=3306;Database={dbName};User=root;Password=root;",
                new MySqlServerVersion(new Version(8, 4, 3)))
            .Options;

    [Fact]
    public async Task MigrateAsync_BaseVacia_CreaEmpresaActivaYPoliticaPromedioPonderado()
    {
        var dbName = $"test_n110_costeo_bootstrap_{Guid.NewGuid():N}";
        var options = CreateOptions(dbName);

        await using var context = new AppDbContext(options);
        try
        {
            await context.Database.MigrateAsync();

            var empresas = await context.EmpresaConfiguraciones.AsNoTracking().ToListAsync();
            var politicas = await context.Set<PoliticaCosteoInventario>().AsNoTracking().ToListAsync();

            var empresa = Assert.Single(empresas);
            Assert.True(empresa.Activa);
            Assert.Equal("Storefront", empresa.NombreComercial);
            Assert.Equal("HNL", empresa.Moneda);
            Assert.Equal("America/Tegucigalpa", empresa.ZonaHoraria);

            var politica = Assert.Single(politicas);
            Assert.Equal(empresa.Id, politica.EmpresaConfiguracionId);
            Assert.Equal(MetodoCosteoInventario.PromedioPonderado, politica.Metodo);
            Assert.True(politica.EstaVigente);
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task MigrateAsync_BaseExistenteSinEmpresaActiva_FallaCerradoYNoAutorreparaDatos()
    {
        var dbName = $"test_n110_costeo_guard_{Guid.NewGuid():N}";
        var options = CreateOptions(dbName);

        await using var context = new AppDbContext(options);
        try
        {
            var migrator = context.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(MigracionAnterior);

            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO `EmpresaConfiguraciones`
                    (`NombreComercial`,`Eslogan`,`NombreVisibleSistema`,`DescripcionSistema`,`MensajeLogin`,
                     `Copyright`,`MostrarCopyright`,`UsarAnioAutomaticoCopyright`,`EncabezadoActivo`,`PiePaginaActivo`,
                     `Moneda`,`ZonaHoraria`,`FormatoFecha`,`Activa`,`FechaActualizacion`)
                VALUES
                    ('Empresa inactiva preexistente','QA','Empresa inactiva preexistente','QA N1.10.C','QA',
                     'QA',1,1,1,1,'HNL','America/Tegucigalpa','dd/MM/yyyy',0,UTC_TIMESTAMP());
                """);

            var error = await Assert.ThrowsAnyAsync<Exception>(() => migrator.MigrateAsync());

            Assert.Contains("CK_N110C_Guard_Cero", error.ToString(), StringComparison.Ordinal);
            Assert.Equal(1, await context.EmpresaConfiguraciones.CountAsync());
            Assert.Equal(0, await context.EmpresaConfiguraciones.CountAsync(x => x.Activa));
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }
}
