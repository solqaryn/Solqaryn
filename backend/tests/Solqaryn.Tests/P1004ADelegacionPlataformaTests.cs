using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P1-004.A: tests de autorización, aislamiento, delegación, revocación y
/// auditoría sobre servicios/controllers reales. EF InMemory NO acredita
/// transacciones MySQL ni HTTP/JWT real: pertenecen a P1-004.B.
/// </summary>
public sealed class P1004ADelegacionPlataformaTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        public AppDbContext Db { get; }
        public Usuario Superadmin { get; private set; } = null!;
        public Usuario Delegado { get; private set; } = null!;
        public Usuario EmpresaAdmin { get; private set; } = null!;
        public Usuario Acting { get; set; } = null!;
        public Mock<ICurrentUserService> Current { get; }
        public PlataformaScopeService Scope { get; }
        public PlataformaDelegacionService Delegation { get; }

        private Fixture()
        {
            Db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("p1004a-" + Guid.NewGuid().ToString("N")).Options);
            Current = new Mock<ICurrentUserService>();
            Current.SetupGet(x => x.EstaAutenticado).Returns(true);
            Current.SetupGet(x => x.UsuarioId).Returns(() => (int?)Acting.Id);
            Current.SetupGet(x => x.NombreUsuario).Returns(() => Acting.NombreUsuario);
            // El JWT puede declarar admin; la BD sigue siendo la fuente viva.
            Current.SetupGet(x => x.EsAdministrador).Returns(true);
            Scope = new PlataformaScopeService(Db, Current.Object);
            Delegation = new PlataformaDelegacionService(Db, Current.Object);
        }

        public static async Task<Fixture> CreateAsync()
        {
            var f = new Fixture();
            var superRole = new Rol
            {
                Nombre = "Superadmin global", NombreNormalizado = "P1004_SUPER",
                EsAdministrador = true, EsSistema = true,
                Ambito = AmbitoAutorizacion.Plataforma, Activo = true
            };
            var enterpriseRole = new Rol
            {
                Nombre = "Administrador empresa", NombreNormalizado = "P1004_EMPRESA",
                EsAdministrador = true, EsSistema = true,
                Ambito = AmbitoAutorizacion.Empresa, Activo = true
            };
            f.Db.Roles.AddRange(superRole, enterpriseRole);
            await f.Db.SaveChangesAsync();

            f.Superadmin = new Usuario
            {
                NombreUsuario = "p1004-super", NombreCompleto = "Superadmin test",
                PasswordHash = "fixture-no-utilizable", RolId = enterpriseRole.Id,
                Activo = true
            };
            f.Delegado = new Usuario
            {
                NombreUsuario = "p1004-delegado", NombreCompleto = "Delegado test",
                PasswordHash = "fixture-no-utilizable", RolId = enterpriseRole.Id,
                Activo = true
            };
            f.EmpresaAdmin = new Usuario
            {
                NombreUsuario = "p1004-empresa", NombreCompleto = "Admin empresa test",
                PasswordHash = "fixture-no-utilizable", RolId = enterpriseRole.Id,
                Activo = true
            };
            f.Db.Usuarios.AddRange(f.Superadmin, f.Delegado, f.EmpresaAdmin);
            var empresaA = new Empresa("P1004 empresa A");
            var empresaB = new Empresa("P1004 empresa B");
            f.Db.Set<Empresa>().AddRange(empresaA, empresaB);
            await f.Db.SaveChangesAsync();
            f.Db.UsuarioRolesPlataforma.Add(new UsuarioRolPlataforma(f.Superadmin.Id, superRole.Id));
            f.Db.UsuarioEmpresas.Add(new UsuarioEmpresa(f.EmpresaAdmin.Id, empresaA.Id, enterpriseRole.Id));
            await f.Db.SaveChangesAsync();
            f.Acting = f.Superadmin;
            return f;
        }

        public PlataformaDelegacionesController Controller()
        {
            var controller = new PlataformaDelegacionesController(Db, Current.Object, Scope, Delegation);
            var http = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, Acting.Id.ToString()),
                    new Claim(ClaimTypes.Name, Acting.NombreUsuario)
                }, "test"))
            };
            http.TraceIdentifier = "p1004a-" + Guid.NewGuid().ToString("N");
            controller.ControllerContext = new ControllerContext { HttpContext = http };
            return controller;
        }

        public async ValueTask DisposeAsync() => await Db.DisposeAsync();
    }

    private static DelegacionPlataformaRequest Grant(int userId) =>
        new() { UsuarioId = userId, Motivo = "P1-004A delegación controlada" };

    [Fact]
    public async Task Superadmin_otorga_solo_lectura_y_delegado_consulta_directorio_auditado()
    {
        await using var f = await Fixture.CreateAsync();
        var created = await f.Controller().Conceder(Grant(f.Delegado.Id), default);
        Assert.IsType<CreatedResult>(created);
        Assert.Equal(1, await f.Db.UsuarioRolesPlataforma.CountAsync(
            x => x.UsuarioId == f.Delegado.Id && x.Activa));
        var role = await f.Db.Roles.SingleAsync(
            r => r.NombreNormalizado == PoliticaDelegacionPlataforma.RolNombreNormalizado);
        Assert.False(role.EsSistema);
        Assert.True(role.EsAdministrador);
        Assert.Equal(AmbitoAutorizacion.Plataforma, role.Ambito);
        Assert.Equal(1, await f.Db.RolPermisos.CountAsync(x => x.RolId == role.Id));
        Assert.Single(await f.Db.Permisos.Where(p =>
            p.Codigo == PoliticaDelegacionPlataforma.CodigoPermiso).ToArrayAsync());

        f.Acting = f.Delegado;
        Assert.Null(await f.Scope.ObtenerActualAsync()); // Nunca superadmin.
        Assert.NotNull(await f.Delegation.ObtenerDelegadoActualAsync());
        Assert.IsType<OkObjectResult>(await f.Controller().Directorio(default));
        Assert.Equal(2, await f.Db.Set<Empresa>().IgnoreQueryFilters().CountAsync());
        Assert.Equal(2, await f.Db.RegistrosAuditoria.CountAsync(x =>
            x.AmbitoAutoridad == AmbitoAutorizacion.Plataforma &&
            x.Resultado == "Exito"));
        Assert.All(await f.Db.RegistrosAuditoria.ToListAsync(), a => Assert.Null(a.EmpresaId));
        Assert.Empty(await f.Db.UsuarioEmpresas.Where(m => m.UsuarioId == f.Delegado.Id).ToListAsync());
    }

    [Fact]
    public async Task Revocacion_corta_inmediatamente_la_autorizacion_sin_eliminar_el_grant_de_otros()
    {
        await using var f = await Fixture.CreateAsync();
        Assert.IsType<CreatedResult>(await f.Controller().Conceder(Grant(f.Delegado.Id), default));
        f.Acting = f.Delegado;
        Assert.NotNull(await f.Delegation.ObtenerDelegadoActualAsync());

        f.Acting = f.Superadmin;
        var response = await f.Controller().Revocar(f.Delegado.Id,
            new RevocarDelegacionPlataformaRequest { Motivo = "Revocación operativa controlada" },
            default);
        Assert.IsType<OkObjectResult>(response);
        f.Acting = f.Delegado;
        Assert.Null(await f.Delegation.ObtenerDelegadoActualAsync());
        Assert.IsType<ForbidResult>(await f.Controller().Directorio(default));
        Assert.True(await f.Db.RegistrosAuditoria.AnyAsync(x =>
            x.Accion == AccionPermiso.Desactivar && x.Resultado == "Exito"));
        Assert.Equal(1, await f.Db.RolPermisos.CountAsync());
    }

    [Fact]
    public async Task Delegado_no_puede_autootorgar_privilegios_ni_gestionar_otras_cuentas()
    {
        await using var f = await Fixture.CreateAsync();
        Assert.IsType<CreatedResult>(await f.Controller().Conceder(Grant(f.Delegado.Id), default));
        var previous = await f.Db.UsuarioRolesPlataforma.CountAsync();
        f.Acting = f.Delegado;
        Assert.IsType<ForbidResult>(await f.Controller().Conceder(Grant(f.Delegado.Id), default));
        Assert.IsType<ForbidResult>(await f.Controller().Conceder(Grant(f.EmpresaAdmin.Id), default));
        Assert.IsType<ForbidResult>(await f.Controller().Revocar(f.Superadmin.Id,
            new RevocarDelegacionPlataformaRequest { Motivo = "Intento sin autoridad superadmin" },
            default));
        Assert.Equal(previous, await f.Db.UsuarioRolesPlataforma.CountAsync());
    }

    [Fact]
    public async Task Superadmin_no_puede_crearse_una_delegacion_para_si_mismo()
    {
        await using var f = await Fixture.CreateAsync();
        Assert.IsType<ForbidResult>(await f.Controller().Conceder(Grant(f.Superadmin.Id), default));
        Assert.Equal(1, await f.Db.UsuarioRolesPlataforma.CountAsync());
        Assert.True(await f.Db.RegistrosAuditoria.AnyAsync(x =>
            x.Resultado == "Denegado" && x.UsuarioId == f.Superadmin.Id));
    }

    [Fact]
    public async Task Empresa_admin_con_claim_falso_y_empresa_activa_no_obtiene_autoridad_global()
    {
        await using var f = await Fixture.CreateAsync();
        f.Acting = f.EmpresaAdmin;
        f.Controller().HttpContext.Request.Headers["X-Empresa-Id"] = "1";
        Assert.Null(await f.Scope.ObtenerActualAsync());
        Assert.Null(await f.Delegation.ObtenerDelegadoActualAsync());
        Assert.IsType<ForbidResult>(await f.Controller().Conceder(Grant(f.Delegado.Id), default));
        Assert.IsType<ForbidResult>(await f.Controller().Directorio(default));
    }

    [Fact]
    public async Task Revocacion_de_permiso_rol_usuario_o_asignacion_es_fail_closed()
    {
        await using var f = await Fixture.CreateAsync();
        Assert.IsType<CreatedResult>(await f.Controller().Conceder(Grant(f.Delegado.Id), default));
        f.Acting = f.Delegado;
        var permiso = await f.Db.Permisos.SingleAsync(
            x => x.Codigo == PoliticaDelegacionPlataforma.CodigoPermiso);
        permiso.Activo = false;
        await f.Db.SaveChangesAsync();
        Assert.Null(await f.Delegation.ObtenerDelegadoActualAsync());

        permiso.Activo = true;
        await f.Db.SaveChangesAsync();
        f.Delegado.Bloqueado = true;
        await f.Db.SaveChangesAsync();
        Assert.Null(await f.Delegation.ObtenerDelegadoActualAsync());

        f.Delegado.Bloqueado = false;
        var role = await f.Db.Roles.SingleAsync(
            x => x.NombreNormalizado == PoliticaDelegacionPlataforma.RolNombreNormalizado);
        role.Eliminado = true;
        await f.Db.SaveChangesAsync();
        Assert.Null(await f.Delegation.ObtenerDelegadoActualAsync());

        role.Eliminado = false;
        await f.Db.SaveChangesAsync();
        var grant = await f.Db.RolPermisos.SingleAsync();
        f.Db.RolPermisos.Remove(grant);
        await f.Db.SaveChangesAsync();
        Assert.Null(await f.Delegation.ObtenerDelegadoActualAsync());
    }

    [Fact]
    public async Task Dos_asignaciones_globales_o_identidad_cambiada_no_eligen_rol_arbitrariamente()
    {
        await using var f = await Fixture.CreateAsync();
        Assert.IsType<CreatedResult>(await f.Controller().Conceder(Grant(f.Delegado.Id), default));
        f.Acting = f.Delegado;
        Assert.NotNull(await f.Delegation.ObtenerDelegadoActualAsync());
        var other = new Rol
        {
            Nombre = "Otro rol global", NombreNormalizado = "P1004_GLOBAL_OTRO",
            Ambito = AmbitoAutorizacion.Plataforma,
            EsSistema = false, EsAdministrador = true, Activo = true
        };
        f.Db.Roles.Add(other);
        await f.Db.SaveChangesAsync();
        f.Db.UsuarioRolesPlataforma.Add(new UsuarioRolPlataforma(f.Delegado.Id, other.Id));
        await f.Db.SaveChangesAsync();
        Assert.Null(await f.Delegation.ObtenerDelegadoActualAsync());

        var extra = await f.Db.UsuarioRolesPlataforma.SingleAsync(
            x => x.UsuarioId == f.Delegado.Id && x.RolId == other.Id);
        extra.Desactivar();
        await f.Db.SaveChangesAsync();
        f.Current.SetupGet(x => x.NombreUsuario).Returns("identidad-jwt-falsificada");
        Assert.Null(await f.Delegation.ObtenerDelegadoActualAsync());
    }

    [Fact]
    public async Task No_renueva_privilegios_ajenos_ni_resucita_rol_o_permiso_inactivo()
    {
        await using var f = await Fixture.CreateAsync();
        Assert.IsType<CreatedResult>(await f.Controller().Conceder(Grant(f.Delegado.Id), default));
        Assert.IsType<ConflictObjectResult>(await f.Controller().Conceder(Grant(f.Delegado.Id), default));
        var permission = await f.Db.Permisos.SingleAsync(x =>
            x.Codigo == PoliticaDelegacionPlataforma.CodigoPermiso);
        permission.Activo = false;
        await f.Db.SaveChangesAsync();
        var another = new Usuario
        {
            NombreUsuario = "otro-p1004", NombreCompleto = "Otro operador test",
            PasswordHash = "fixture-no-utilizable", RolId = f.EmpresaAdmin.RolId,
            Activo = true
        };
        f.Db.Usuarios.Add(another);
        await f.Db.SaveChangesAsync();
        Assert.IsType<ConflictObjectResult>(await f.Controller().Conceder(Grant(another.Id), default));
        Assert.False(permission.Activo);
        Assert.False(await f.Db.UsuarioRolesPlataforma.AnyAsync(x => x.UsuarioId == another.Id));
    }
}
