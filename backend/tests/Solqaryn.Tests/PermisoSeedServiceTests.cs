using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public class PermisoSeedServiceTests
{
    private static AppDbContext CrearContexto() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task SeedDefaults_CreaGrantsExplicitosAdministrador_SinBypassNiFilasDenegadas()
    {
        await using var context = CrearContexto();
        var service = new SeedPermisoService(context);

        await service.SeedDefaultsAsync();

        var administrador = await context.Roles.IgnoreQueryFilters().SingleAsync(r => r.EsAdministrador);
        var vendedor = await context.Roles.IgnoreQueryFilters().SingleAsync(r => r.NombreNormalizado == "VENDEDOR");
        var permisosActivos = await context.Permisos.IgnoreQueryFilters().CountAsync(p => p.Activo && !p.Eliminado && p.Accion != AccionPermiso.ReadAuditCorrelation && p.Accion != AccionPermiso.WriteCaseFileVersion);

        var grantsAdmin = await context.RolPermisos.CountAsync(rp => rp.RolId == administrador.Id);
        var grantsVendedor = await context.RolPermisos.CountAsync(rp => rp.RolId == vendedor.Id);

        Assert.True(permisosActivos > 0);
        Assert.Equal(permisosActivos, grantsAdmin);
        Assert.Equal(0, grantsVendedor);
        Assert.Equal(grantsAdmin, await context.RolPermisos
            .Where(rp => rp.RolId == administrador.Id)
            .Select(rp => new { rp.RolId, rp.PermisoId })
            .Distinct()
            .CountAsync());
    }

    [Fact]
    public async Task Api12_CatalogExistsButStartupNeverGrantsOrRestoresIt()
    {
        await using var context = CrearContexto();
        var service = new SeedPermisoService(context);
        await service.SeedDefaultsAsync();
        var admin = await context.Roles.IgnoreQueryFilters().SingleAsync(r => r.EsAdministrador);
        var api12 = await context.Permisos.IgnoreQueryFilters().SingleAsync(p =>
            p.Modulo == ModuloSistema.Auditoria && p.Accion == AccionPermiso.ReadAuditCorrelation);
        Assert.True(api12.Activo);
        Assert.False(await context.RolPermisos.IgnoreQueryFilters().AnyAsync(r => r.PermisoId == api12.Id));
        // A synthetic explicit grant in the fixture must be preserved, not manufactured.
        var grant = new RolPermiso { RolId = admin.Id, PermisoId = api12.Id };
        context.RolPermisos.Add(grant);
        await context.SaveChangesAsync();
        await service.SeedDefaultsAsync();
        Assert.Equal(1, await context.RolPermisos.IgnoreQueryFilters().CountAsync(r => r.PermisoId == api12.Id));
        api12.Activo = false;
        await context.SaveChangesAsync();
        await service.SeedDefaultsAsync();
        Assert.False(api12.Activo);
        context.RolPermisos.Remove(grant);
        await context.SaveChangesAsync();
        await service.SeedDefaultsAsync();
        Assert.False(await context.RolPermisos.IgnoreQueryFilters().AnyAsync(r => r.PermisoId == api12.Id));
    }

    [Fact]
    public async Task SeedDefaults_EsIdempotente_YRestauraGrantAdministradorEliminado()
    {
        await using var context = CrearContexto();
        var service = new SeedPermisoService(context);
        await service.SeedDefaultsAsync();

        var administrador = await context.Roles.IgnoreQueryFilters().SingleAsync(r => r.EsAdministrador);
        var grant = await context.RolPermisos.FirstAsync(rp => rp.RolId == administrador.Id);
        var permisoId = grant.PermisoId;

        context.RolPermisos.Remove(grant);
        await context.SaveChangesAsync();
        Assert.False(await context.RolPermisos.AnyAsync(rp => rp.RolId == administrador.Id && rp.PermisoId == permisoId));

        await service.SeedDefaultsAsync();
        await service.SeedDefaultsAsync();

        Assert.Equal(1, await context.RolPermisos.CountAsync(rp =>
            rp.RolId == administrador.Id && rp.PermisoId == permisoId));
    }
}
