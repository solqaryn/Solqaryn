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
/// P1-004.A: pruebas del limite entre scope autenticado real y delegacion.
/// EF InMemory: no equivalen a JWT HTTP ni al proveedor MySQL real.
/// </summary>
public sealed class P1004APlataformaDelegacionScopeIntegrationTests
{
    [Fact]
    public async Task Grant_global_vivo_funciona_aunque_claims_de_rol_y_admin_no_concedan_autoridad()
    {
        var fixture = await Fixture.CrearAsync();
        await using var db = fixture.Db;

        var scopeActual = await fixture.Scope.ObtenerActualAsync();

        Assert.NotNull(scopeActual);
        Assert.Equal(fixture.Usuario.Id, scopeActual!.UsuarioId);
        Assert.Equal(fixture.Rol.Id, scopeActual.RolId);
        Assert.True(await fixture.Delegacion.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Ver));
    }

    [Fact]
    public async Task Revocar_asignacion_global_deniega_en_la_siguiente_consulta()
    {
        var fixture = await Fixture.CrearAsync();
        await using var db = fixture.Db;

        Assert.True(await fixture.Delegacion.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Ver));

        fixture.Asignacion.Desactivar();
        await db.SaveChangesAsync();

        Assert.Null(await fixture.Scope.ObtenerActualAsync());
        Assert.False(await fixture.Delegacion.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Ver));
    }

    [Fact]
    public async Task Sesion_con_nombre_ajeno_o_usuario_bloqueado_no_hereda_grant()
    {
        var fixture = await Fixture.CrearAsync();
        await using var db = fixture.Db;

        fixture.Current.SetupGet(x => x.NombreUsuario).Returns("otro-usuario");
        Assert.Null(await fixture.Scope.ObtenerActualAsync());
        Assert.False(await fixture.Delegacion.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Ver));

        fixture.Current.SetupGet(x => x.NombreUsuario).Returns(fixture.Usuario.NombreUsuario);
        fixture.Usuario.Bloqueado = true;
        await db.SaveChangesAsync();

        Assert.Null(await fixture.Scope.ObtenerActualAsync());
        Assert.False(await fixture.Delegacion.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Ver));
    }

    [Fact]
    public async Task Dos_roles_administradores_globales_activos_fallan_cerrado()
    {
        var fixture = await Fixture.CrearAsync();
        await using var db = fixture.Db;

        var segundoRol = new Rol
        {
            Nombre = "Administrador alterno",
            NombreNormalizado = "administrador_alterno",
            Ambito = AmbitoAutorizacion.Plataforma,
            EmpresaId = null,
            EsAdministrador = true,
            EsSistema = false,
            Activo = true,
            Eliminado = false
        };
        db.Roles.Add(segundoRol);
        await db.SaveChangesAsync();
        db.Set<UsuarioRolPlataforma>().Add(
            new UsuarioRolPlataforma(fixture.Usuario.Id, segundoRol.Id));
        await db.SaveChangesAsync();

        Assert.Null(await fixture.Scope.ObtenerActualAsync());
        Assert.False(await fixture.Delegacion.TienePermisoDelegadoAsync(
            ModuloSistema.Configuracion, AccionPermiso.Ver));
    }

    private sealed record Fixture(
        AppDbContext Db,
        Usuario Usuario,
        Rol Rol,
        UsuarioRolPlataforma Asignacion,
        Mock<ICurrentUserService> Current,
        PlataformaScopeService Scope,
        PlataformaDelegacionService Delegacion)
    {
        public static async Task<Fixture> CrearAsync()
        {
            var db = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseInMemoryDatabase("p1-004-a-scope-" + Guid.NewGuid().ToString("N"))
                    .Options);

            var rol = new Rol
            {
                Nombre = "Delegado administracion plataforma",
                NombreNormalizado = "delegado_administracion_plataforma",
                Ambito = AmbitoAutorizacion.Plataforma,
                EmpresaId = null,
                EsAdministrador = true,
                EsSistema = false,
                Activo = true,
                Eliminado = false
            };
            db.Roles.Add(rol);
            await db.SaveChangesAsync();

            var usuario = new Usuario
            {
                NombreUsuario = "delegado-sintetico",
                NombreCompleto = "Delegado de pruebas",
                PasswordHash = "no-es-credencial-real",
                RolId = rol.Id,
                Activo = true,
                Bloqueado = false,
                Eliminado = false
            };
            db.Usuarios.Add(usuario);

            var permiso = new Permiso
            {
                Codigo = "PLATAFORMA.CONFIGURACION.VER",
                Nombre = "Ver configuracion sintetica",
                Modulo = ModuloSistema.Configuracion,
                Accion = AccionPermiso.Ver,
                Ambito = AmbitoAutorizacion.Plataforma,
                Activo = true,
                Eliminado = false
            };
            db.Set<Permiso>().Add(permiso);
            await db.SaveChangesAsync();

            var asignacion = new UsuarioRolPlataforma(usuario.Id, rol.Id);
            db.Set<UsuarioRolPlataforma>().Add(asignacion);
            db.Set<RolPermiso>().Add(new RolPermiso
            {
                RolId = rol.Id,
                PermisoId = permiso.Id
            });
            await db.SaveChangesAsync();

            var current = new Mock<ICurrentUserService>();
            current.SetupGet(x => x.EstaAutenticado).Returns(true);
            current.SetupGet(x => x.UsuarioId).Returns(usuario.Id);
            current.SetupGet(x => x.NombreUsuario).Returns(usuario.NombreUsuario);
            current.SetupGet(x => x.RolId).Returns(0);
            current.SetupGet(x => x.EsAdministrador).Returns(false);

            var scope = new PlataformaScopeService(db, current.Object);
            var delegacion = new PlataformaDelegacionService(db, scope);
            return new Fixture(db, usuario, rol, asignacion, current, scope, delegacion);
        }
    }
}
