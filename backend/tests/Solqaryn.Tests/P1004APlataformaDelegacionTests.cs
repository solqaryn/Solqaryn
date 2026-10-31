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
/// P1-004.A: contrato de delegacion de privilegios plataforma sin secretos,
/// HTTP, proveedores o datos comerciales. No se prueban mutaciones de grants:
/// ninguna API de autoasignacion se expone en este changeset.
/// </summary>
public sealed class P1004APlataformaDelegacionTests
{
    [Fact]
    public async Task Permite_solo_el_grant_global_explicito_de_un_delegado_vivo()
    {
        var fixture = await Fixture.CreateAsync();
        await using var db = fixture.Db;

        Assert.True(await fixture.Service.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Ver));
        Assert.False(await fixture.Service.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Administrar));
        Assert.False(await fixture.Service.TienePermisoDelegadoAsync(
            ModuloSistema.Usuarios, AccionPermiso.Ver));
        Assert.Equal(1, await db.Set<RolPermiso>().CountAsync());
    }

    [Fact]
    public async Task Deniega_sin_scope_vivo_o_con_rol_distinto_al_grant()
    {
        var fixture = await Fixture.CreateAsync();
        await using var db = fixture.Db;
        fixture.Scope.Setup(x => x.ObtenerActualAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioPlataformaScopeActual?)null);
        Assert.False(await fixture.Service.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Ver));

        fixture.Scope.Setup(x => x.ObtenerActualAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioPlataformaScopeActual(7, fixture.Rol.Id + 1, "delegado", "Delegado"));
        Assert.False(await fixture.Service.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Ver));
    }

    [Fact]
    public async Task Deniega_permiso_empresarial_inactivo_o_eliminado()
    {
        var fixture = await Fixture.CreateAsync();
        await using var db = fixture.Db;

        fixture.Permiso.Ambito = AmbitoAutorizacion.Empresa;
        await db.SaveChangesAsync();
        Assert.False(await fixture.Service.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Ver));

        fixture.Permiso.Ambito = AmbitoAutorizacion.Plataforma;
        fixture.Permiso.Activo = false;
        await db.SaveChangesAsync();
        Assert.False(await fixture.Service.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Ver));

        fixture.Permiso.Activo = true;
        fixture.Permiso.Eliminado = true;
        await db.SaveChangesAsync();
        Assert.False(await fixture.Service.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Ver));
    }

    [Fact]
    public async Task Deniega_rol_tenant_sistema_revocado_o_eliminado()
    {
        var fixture = await Fixture.CreateAsync();
        await using var db = fixture.Db;

        fixture.Rol.Ambito = AmbitoAutorizacion.Empresa;
        fixture.Rol.EmpresaId = 22;
        await db.SaveChangesAsync();
        Assert.False(await fixture.Service.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Ver));

        fixture.Rol.Ambito = AmbitoAutorizacion.Plataforma;
        fixture.Rol.EmpresaId = null;
        fixture.Rol.EsSistema = true;
        await db.SaveChangesAsync();
        Assert.False(await fixture.Service.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Ver));

        fixture.Rol.EsSistema = false;
        fixture.Rol.Activo = false;
        await db.SaveChangesAsync();
        Assert.False(await fixture.Service.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Ver));

        fixture.Rol.Activo = true;
        fixture.Rol.Eliminado = true;
        await db.SaveChangesAsync();
        Assert.False(await fixture.Service.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Ver));
    }

    private sealed record Fixture(
        AppDbContext Db,
        Rol Rol,
        Permiso Permiso,
        Mock<IPlataformaScopeService> Scope,
        PlataformaDelegacionService Service)
    {
        public static async Task<Fixture> CreateAsync()
        {
            var db = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseInMemoryDatabase("p1-004-a-" + Guid.NewGuid().ToString("N"))
                    .Options);
            var rol = new Rol
            {
                Nombre = "Delegado plataforma", NombreNormalizado = "delegado_plataforma",
                Ambito = AmbitoAutorizacion.Plataforma,
                EmpresaId = null, EsAdministrador = true, EsSistema = false,
                Activo = true, Eliminado = false
            };
            var permiso = new Permiso
            {
                Codigo = "PLATAFORMA.CONFIGURACION.VER", Nombre = "Leer configuracion global",
                Modulo = ModuloSistema.Configuracion, Accion = AccionPermiso.Ver,
                Ambito = AmbitoAutorizacion.Plataforma,
                Activo = true, Eliminado = false
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

            var usuario = new Usuario
            {
                Id = 7,
                NombreUsuario = "delegado",
                NombreCompleto = "Delegado sintetico",
                PasswordHash = "solo-fixture-no-credencial",
                RolId = rol.Id,
                Activo = true,
                Bloqueado = false,
                Eliminado = false
            };
            db.Usuarios.Add(usuario);
            db.Set<UsuarioRolPlataforma>().Add(
                new UsuarioRolPlataforma(usuario.Id, rol.Id));
            await db.SaveChangesAsync();

            var scope = new Mock<IPlataformaScopeService>();
            scope.Setup(x => x.ObtenerActualAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UsuarioPlataformaScopeActual(7, rol.Id, "delegado", rol.Nombre));

            return new Fixture(
                db, rol, permiso, scope,
                new PlataformaDelegacionService(db, scope.Object));
        }
    }
}
