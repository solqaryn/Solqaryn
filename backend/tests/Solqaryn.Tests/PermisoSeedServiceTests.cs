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
        var permisosActivos = await context.Permisos.IgnoreQueryFilters().CountAsync(p => p.Activo && !p.Eliminado);

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
