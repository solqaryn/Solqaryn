using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public class SeedPermisoServiceTests
{
    private static AppDbContext CrearContexto(string nombre)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(nombre)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task SeedDefaultsAsync_Crea_Roles_Sistema_Y_Grants_Administrador_Explicitos()
    {
        await using var context = CrearContexto(Guid.NewGuid().ToString());
        var service = new SeedPermisoService(context);

        await service.SeedDefaultsAsync();

        var roles = await context.Roles.IgnoreQueryFilters().ToListAsync();
        var administrador = Assert.Single(roles.Where(r => r.NombreNormalizado == "ADMINISTRADOR"));
        var vendedor = Assert.Single(roles.Where(r => r.NombreNormalizado == "VENDEDOR"));
        var permisosActivos = await context.Permisos.IgnoreQueryFilters()
            .CountAsync(p => p.Activo && !p.Eliminado);
        var grantsAdministrador = await context.RolPermisos
            .CountAsync(p => p.RolId == administrador.Id);

        Assert.True(administrador.EsSistema);
        Assert.True(administrador.EsAdministrador);
        Assert.True(vendedor.EsSistema);
        Assert.False(vendedor.EsAdministrador);
        Assert.True(permisosActivos > 0);
        Assert.Equal(permisosActivos, grantsAdministrador);
    }

    [Fact]
    public async Task SeedDefaultsAsync_Segundo_Arranque_No_Reinyecta_Grant_Vendedor_Eliminado()
    {
        await using var context = CrearContexto(Guid.NewGuid().ToString());
        var service = new SeedPermisoService(context);
        await service.SeedDefaultsAsync();

        var vendedor = await context.Roles
            .IgnoreQueryFilters()
            .SingleAsync(r => r.NombreNormalizado == "VENDEDOR");
        var permisoDashboard = await context.Permisos
            .IgnoreQueryFilters()
            .SingleAsync(p => p.Modulo == ModuloSistema.Dashboard && p.Accion == AccionPermiso.Ver);

        // N0.4 deja DefaultVendedor vacío: los grants de vendedor son administrados.
        // Simulamos un grant explícito asignado y luego retirado por administración.
        context.RolPermisos.Add(new RolPermiso
        {
            RolId = vendedor.Id,
            PermisoId = permisoDashboard.Id
        });
        await context.SaveChangesAsync();

        var grant = await context.RolPermisos
            .SingleAsync(p => p.RolId == vendedor.Id && p.PermisoId == permisoDashboard.Id);
        context.RolPermisos.Remove(grant);
        await context.SaveChangesAsync();

        await service.SeedDefaultsAsync();

        Assert.False(await context.RolPermisos
            .AnyAsync(p => p.RolId == vendedor.Id && p.PermisoId == permisoDashboard.Id));
    }

    [Fact]
    public async Task SeedDefaultsAsync_Es_Idempotente_Y_No_Duplica_Grants_Administrador()
    {
        await using var context = CrearContexto(Guid.NewGuid().ToString());
        var service = new SeedPermisoService(context);

        await service.SeedDefaultsAsync();
        await service.SeedDefaultsAsync();

        var administrador = await context.Roles
            .IgnoreQueryFilters()
            .SingleAsync(r => r.NombreNormalizado == "ADMINISTRADOR");
        var grants = await context.RolPermisos
            .Where(p => p.RolId == administrador.Id)
            .Select(p => new { p.RolId, p.PermisoId })
            .ToListAsync();

        Assert.NotEmpty(grants);
        Assert.Equal(grants.Count, grants.Distinct().Count());
    }

    [Fact]
    public async Task SeedDefaultsAsync_Mantiene_Grants_Administrativos_Completos_En_Rearranque()
    {
        await using var context = CrearContexto(Guid.NewGuid().ToString());
        var service = new SeedPermisoService(context);
        await service.SeedDefaultsAsync();

        var administrador = await context.Roles
            .IgnoreQueryFilters()
            .SingleAsync(r => r.NombreNormalizado == "ADMINISTRADOR");
        var permiso = await context.Permisos
            .IgnoreQueryFilters()
            .FirstAsync(p => p.Activo && !p.Eliminado);
        var grant = await context.RolPermisos
            .SingleAsync(p => p.RolId == administrador.Id && p.PermisoId == permiso.Id);
        context.RolPermisos.Remove(grant);
        await context.SaveChangesAsync();

        await service.SeedDefaultsAsync();

        Assert.True(await context.RolPermisos
            .AnyAsync(p => p.RolId == administrador.Id && p.PermisoId == permiso.Id));
    }

    [Fact]
    public async Task SeedDefaultsAsync_Rearranque_ConMatrizValida_NoCambiaEstadoSemanticoAdministrador()
    {
        await using var context = CrearContexto(Guid.NewGuid().ToString());
        var service = new SeedPermisoService(context);
        await service.SeedDefaultsAsync();

        var administrador = await context.Roles
            .IgnoreQueryFilters()
            .SingleAsync(r => r.NombreNormalizado == "ADMINISTRADOR");
        var antes = await context.RolPermisos
            .Where(p => p.RolId == administrador.Id)
            .OrderBy(p => p.PermisoId)
            .Select(p => p.PermisoId)
            .ToListAsync();

        await service.SeedDefaultsAsync();

        var despues = await context.RolPermisos
            .Where(p => p.RolId == administrador.Id)
            .OrderBy(p => p.PermisoId)
            .Select(p => p.PermisoId)
            .ToListAsync();

        Assert.NotEmpty(antes);
        Assert.Equal(antes, despues);
    }
}
