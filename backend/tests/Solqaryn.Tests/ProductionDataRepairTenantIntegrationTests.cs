using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class ProductionDataRepairTenantIntegrationTests
{
    private static DbContextOptions<AppDbContext> CrearOpciones(string baseDatos) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                CrearCadena(baseDatos),
                new MySqlServerVersion(new Version(8, 4, 0)))
            .Options;

    private static string CrearCadena(string baseDatos)
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("La cadena MySQL de integración no está configurada.");
        var builder = new MySqlConnectionStringBuilder(raw)
        {
            Database = baseDatos
        };
        return builder.ConnectionString;
    }

    [Fact]
    public async Task Repair_MaterializaTenantDesdeConfiguracionLegacyUnica_YEsIdempotente()
    {
        var nombreBase = $"test_prod_repair_tenant_{Guid.NewGuid():N}";
        var opciones = CrearOpciones(nombreBase);

        try
        {
            await using var db = new AppDbContext(opciones);
            await db.Database.MigrateAsync();

            var legacy = await db.EmpresaConfiguraciones
                .SingleOrDefaultAsync(x => x.Activa);

            if (legacy is null)
            {
                legacy = new EmpresaConfiguracion
                {
                    NombreComercial = "Storefront Repair",
                    NombreVisibleSistema = "Storefront Repair",
                    RTN = "08011999123456",
                    Direccion = "Tegucigalpa",
                    Moneda = "HNL",
                    ZonaHoraria = "America/Tegucigalpa",
                    Activa = true
                };
                db.EmpresaConfiguraciones.Add(legacy);
            }
            else
            {
                legacy.NombreComercial = "Storefront Repair";
                legacy.NombreVisibleSistema = "Storefront Repair";
                legacy.RTN = "08011999123456";
                legacy.Direccion = "Tegucigalpa";
                legacy.Moneda = "HNL";
                legacy.ZonaHoraria = "America/Tegucigalpa";
            }

            var suffix = Guid.NewGuid().ToString("N");
            var rol = new Rol
            {
                Nombre = $"Repair Admin {suffix}",
                NombreNormalizado = $"REPAIR_ADMIN_{suffix}".ToUpperInvariant(),
                EsAdministrador = true,
                EsSistema = false,
                Activo = true
            };
            db.Roles.Add(rol);
            await db.SaveChangesAsync();

            var usuario = new Usuario
            {
                NombreUsuario = $"repair_{suffix}",
                NombreCompleto = "Repair Integration",
                PasswordHash = "test-only-hash-placeholder",
                RolId = rol.Id,
                Activo = true,
                Bloqueado = false,
                Eliminado = false
            };
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();

            Assert.Equal(0, await db.Set<Empresa>().CountAsync());
            Assert.Equal(0, await db.UsuarioEmpresas.CountAsync());

            var candidatosEsperados = await db.Usuarios
                .AsNoTracking()
                .Where(u =>
                    !u.Eliminado &&
                    u.Activo &&
                    !u.Bloqueado &&
                    u.RolId > 0 &&
                    u.RolEntidad.Activo &&
                    !u.RolEntidad.Eliminado &&
                    !db.UsuarioEmpresas.Any(m => m.UsuarioId == u.Id))
                .CountAsync();
            Assert.True(candidatosEsperados >= 1);

            var repair = new ProductionDataRepairService(db);
            var primera = await repair.RepairAsync();

            var empresa = await db.Set<Empresa>().AsNoTracking().SingleAsync();
            Assert.Equal("Storefront Repair", empresa.Nombre);
            Assert.Equal("08011999123456", empresa.Rtn);
            Assert.Equal("Tegucigalpa", empresa.Direccion);

            var config = await db.Set<ConfigEmpresa>().AsNoTracking().SingleAsync();
            Assert.Equal(empresa.Id, config.EmpresaId);
            Assert.Equal("HNL", config.Moneda);
            Assert.Equal("America/Tegucigalpa", config.ZonaHoraria);

            var membresia = await db.UsuarioEmpresas
                .AsNoTracking()
                .SingleAsync(x => x.UsuarioId == usuario.Id && x.EmpresaId == empresa.Id);
            Assert.Equal(usuario.Id, membresia.UsuarioId);
            Assert.Equal(empresa.Id, membresia.EmpresaId);
            Assert.Equal(rol.Id, membresia.RolId);
            Assert.True(membresia.Activa);

            Assert.Equal(1, primera.EmpresasTotales);
            Assert.Equal(1, primera.EmpresaConfiguracionesActivas);
            Assert.Equal(empresa.Id, primera.EmpresaMaterializadaId);
            Assert.Equal(candidatosEsperados, primera.MembresiasLegacyCreadas);

            var segunda = await repair.RepairAsync();

            Assert.Equal(1, await db.Set<Empresa>().CountAsync());
            Assert.Equal(1, await db.Set<ConfigEmpresa>().CountAsync());
            Assert.Equal(
                candidatosEsperados,
                await db.UsuarioEmpresas.CountAsync(x => x.EmpresaId == empresa.Id));
            Assert.Equal(1, segunda.EmpresasTotales);
            Assert.Equal(1, segunda.EmpresaConfiguracionesActivas);
            Assert.Null(segunda.EmpresaMaterializadaId);
            Assert.Equal(0, segunda.MembresiasLegacyCreadas);
        }
        finally
        {
            await using var limpieza = new AppDbContext(opciones);
            await limpieza.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task Repair_SinConfiguracionLegacyActiva_NoInventaEmpresa()
    {
        var nombreBase = $"test_prod_repair_fail_closed_{Guid.NewGuid():N}";
        var opciones = CrearOpciones(nombreBase);

        try
        {
            await using var db = new AppDbContext(opciones);
            await db.Database.MigrateAsync();

            var activas = await db.EmpresaConfiguraciones
                .Where(x => x.Activa)
                .ToListAsync();
            foreach (var config in activas)
                config.Activa = false;
            if (activas.Count > 0)
                await db.SaveChangesAsync();

            var resultado = await new ProductionDataRepairService(db).RepairAsync();

            Assert.Equal(0, await db.Set<Empresa>().CountAsync());
            Assert.Equal(0, resultado.EmpresasTotales);
            Assert.Equal(0, resultado.EmpresaConfiguracionesActivas);
            Assert.Null(resultado.EmpresaMaterializadaId);
            Assert.Equal(0, resultado.MembresiasLegacyCreadas);
        }
        finally
        {
            await using var limpieza = new AppDbContext(opciones);
            await limpieza.Database.EnsureDeletedAsync();
        }
    }
}
