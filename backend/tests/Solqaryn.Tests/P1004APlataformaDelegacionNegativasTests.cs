using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P1-004.A: negativos adicionales de delegacion global. Solo datos sinteticos
/// en EF InMemory; no prueba HTTP, MySQL real ni autoriza mutaciones de grants.
/// </summary>
public sealed class P1004APlataformaDelegacionNegativasTests
{
    [Fact]
    public async Task Deniega_rol_no_administrador_aun_con_grant_global()
    {
        var fixture = await CrearAsync();
        await using var db = fixture.Db;

        Assert.True(await fixture.Service.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Ver));

        fixture.Rol.EsAdministrador = false;
        await db.SaveChangesAsync();

        Assert.False(await fixture.Service.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Ver));
    }

    [Fact]
    public async Task Deniega_scope_con_usuario_o_rol_id_invalido()
    {
        var fixture = await CrearAsync();
        await using var db = fixture.Db;

        fixture.Scope.Setup(x => x.ObtenerActualAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioPlataformaScopeActual(
                0, fixture.Rol.Id, "delegado", fixture.Rol.Nombre));

        Assert.False(await fixture.Service.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Ver));

        fixture.Scope.Setup(x => x.ObtenerActualAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioPlataformaScopeActual(
                7, 0, "delegado", fixture.Rol.Nombre));

        Assert.False(await fixture.Service.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Ver));
    }

    [Fact]
    public async Task Revocacion_de_grant_surte_efecto_en_siguiente_consulta()
    {
        var fixture = await CrearAsync();
        await using var db = fixture.Db;

        Assert.True(await fixture.Service.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Ver));

        db.Set<RolPermiso>().RemoveRange(await db.Set<RolPermiso>().ToListAsync());
        await db.SaveChangesAsync();

        Assert.False(await fixture.Service.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Ver));
    }

    private sealed record Fixture(
        AppDbContext Db,
        Rol Rol,
        Mock<IPlataformaScopeService> Scope,
        PlataformaDelegacionService Service);

    private static async Task<Fixture> CrearAsync()
    {
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("p1-004-a-neg-" + Guid.NewGuid().ToString("N"))
                .Options);

        var rol = new Rol
        {
            Nombre = "Delegado de pruebas",
            NombreNormalizado = "delegado_pruebas",
            Ambito = AmbitoAutorizacion.Plataforma,
            EmpresaId = null,
            EsAdministrador = true,
            EsSistema = false,
            Activo = true,
            Eliminado = false
        };
        var permiso = new Permiso
        {
            Codigo = "PLATAFORMA.CONFIGURACION.VER",
            Nombre = "Lectura de configuracion de pruebas",
            Modulo = ModuloSistema.Configuracion,
            Accion = AccionPermiso.Ver,
            Ambito = AmbitoAutorizacion.Plataforma,
            Activo = true,
            Eliminado = false
        };

        db.Roles.Add(rol);
        db.Set<Permiso>().Add(permiso);
        await db.SaveChangesAsync();

        db.Set<RolPermiso>().Add(new RolPermiso
        {
            RolId = rol.Id,
            PermisoId = permiso.Id
        });
        await db.SaveChangesAsync();

        var scope = new Mock<IPlataformaScopeService>();
        scope.Setup(x => x.ObtenerActualAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioPlataformaScopeActual(
                7, rol.Id, "delegado", rol.Nombre));

        return new Fixture(
            db, rol, scope,
            new PlataformaDelegacionService(db, scope.Object));
    }
}
