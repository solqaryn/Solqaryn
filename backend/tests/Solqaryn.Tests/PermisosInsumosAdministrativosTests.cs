using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public class PermisosInsumosAdministrativosTests
{
    private static AppDbContext CrearContexto() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task SeedDefaultsAsync_AgregaPermisosNuevosAlAdministrador_SinOtorgarlosAlVendedor()
    {
        await using var context = CrearContexto();
        var service = new SeedPermisoService(context);
        await service.SeedDefaultsAsync();

        var administrador = await context.Roles.IgnoreQueryFilters().SingleAsync(r => r.EsAdministrador);
        var vendedor = await context.Roles.IgnoreQueryFilters().SingleAsync(r => r.NombreNormalizado == "VENDEDOR");

        var adminInsumos = await context.RolPermisos
            .Include(rp => rp.Permiso)
            .Where(rp => rp.RolId == administrador.Id &&
                         rp.Permiso.Modulo == ModuloSistema.InsumosAdministrativos)
            .ToListAsync();

        Assert.NotEmpty(adminInsumos);
        Assert.Contains(adminInsumos, rp => rp.Permiso.Accion == AccionPermiso.RegistrarConsumo);
        Assert.Contains(adminInsumos, rp => rp.Permiso.Accion == AccionPermiso.AjustarStock);
        Assert.False(await context.RolPermisos
            .Include(rp => rp.Permiso)
            .AnyAsync(rp => rp.RolId == vendedor.Id &&
                            rp.Permiso.Modulo == ModuloSistema.InsumosAdministrativos));
    }

    [Fact]
    public async Task SeedDefaultsAsync_RestauraGrantAdministradorAusente_SinDuplicarlo()
    {
        await using var context = CrearContexto();
        var service = new SeedPermisoService(context);
        await service.SeedDefaultsAsync();

        var administrador = await context.Roles.IgnoreQueryFilters().SingleAsync(r => r.EsAdministrador);
        var permiso = await context.Permisos.IgnoreQueryFilters().SingleAsync(p =>
            p.Modulo == ModuloSistema.Ventas && p.Accion == AccionPermiso.ExonerarEnvio);
        var grant = await context.RolPermisos.SingleAsync(rp =>
            rp.RolId == administrador.Id && rp.PermisoId == permiso.Id);

        context.RolPermisos.Remove(grant);
        await context.SaveChangesAsync();
        await service.SeedDefaultsAsync();
        await service.SeedDefaultsAsync();

        Assert.Equal(1, await context.RolPermisos.CountAsync(rp =>
            rp.RolId == administrador.Id && rp.PermisoId == permiso.Id));
    }
}
