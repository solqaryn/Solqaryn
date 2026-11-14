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
/// P1-004.B regression gates against real controller/service code with
/// synthetic EF InMemory data. These NEVER replace the separate live
/// HTTP/JWT/MySQL DEV run required for functional certification.
/// </summary>
public sealed class P1004BDelegacionCertNegativesTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        public AppDbContext Db { get; } = new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p1004b-"+Guid.NewGuid().ToString("N")).Options);
        public Mock<ICurrentUserService> Current { get; } = new();
        public Usuario Super { get; private set; } = null!;
        public Usuario Delegate { get; private set; } = null!;
        public Usuario EnterpriseOnly { get; private set; } = null!;
        public Usuario Acting { get; set; } = null!;
        public PlataformaScopeService SuperScope { get; }
        public PlataformaDelegacionService Delegation { get; }

        private Fixture()
        {
            Current.SetupGet(x => x.EstaAutenticado).Returns(true);
            Current.SetupGet(x => x.UsuarioId).Returns(() => (int?)Acting.Id);
            Current.SetupGet(x => x.NombreUsuario).Returns(() => Acting.NombreUsuario);
            Current.SetupGet(x => x.EsAdministrador).Returns(true);
            SuperScope = new PlataformaScopeService(Db, Current.Object);
            Delegation = new PlataformaDelegacionService(Db, Current.Object);
        }

        public static async Task<Fixture> Create()
        {
            var f = new Fixture();
            var platformRole = new Rol
            {
                Nombre = "Sistema P1004B", NombreNormalizado = "P1004B_PLATFORM",
                EsAdministrador = true, EsSistema = true,
                Ambito = AmbitoAutorizacion.Plataforma, Activo = true
            };
            var enterpriseRole = new Rol
            {
                Nombre = "Empresa P1004B", NombreNormalizado = "P1004B_ENTERPRISE",
                EsAdministrador = true, EsSistema = true,
                Ambito = AmbitoAutorizacion.Empresa, Activo = true
            };
            f.Db.Roles.AddRange(platformRole, enterpriseRole);
            await f.Db.SaveChangesAsync();

            static Usuario NewUser(string name, int rol) => new()
            {
                NombreUsuario = name, NombreCompleto = name,
                PasswordHash = "fixture-no-credential", RolId = rol, Activo = true
            };
            f.Super = NewUser("p1004b-super",enterpriseRole.Id);
            f.Delegate = NewUser("p1004b-delegate",enterpriseRole.Id);
            f.EnterpriseOnly = NewUser("p1004b-business",enterpriseRole.Id);
            f.Db.Usuarios.AddRange(f.Super,f.Delegate,f.EnterpriseOnly);
            var a = new Empresa("P1004B A");
            var b = new Empresa("P1004B B");
            f.Db.Set<Empresa>().AddRange(a,b);
            await f.Db.SaveChangesAsync();
            f.Db.UsuarioRolesPlataforma.Add(new UsuarioRolPlataforma(f.Super.Id,platformRole.Id));
            f.Db.UsuarioEmpresas.Add(new UsuarioEmpresa(f.Delegate.Id,a.Id,enterpriseRole.Id));
            f.Db.UsuarioEmpresas.Add(new UsuarioEmpresa(f.EnterpriseOnly.Id,b.Id,enterpriseRole.Id));
            await f.Db.SaveChangesAsync();
            f.Acting = f.Super;
            return f;
        }

        public PlataformaDelegacionesController Controller()
        {
            var http = new DefaultHttpContext();
            http.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] {new Claim(ClaimTypes.NameIdentifier,Acting.Id.ToString()),
                       new Claim(ClaimTypes.Name,Acting.NombreUsuario)}, "JwtBearer"));
            http.TraceIdentifier = "p1004b-"+Guid.NewGuid().ToString("N");
            return new PlataformaDelegacionesController(Db,Current.Object,SuperScope,Delegation)
            {
                ControllerContext = new ControllerContext {HttpContext=http}
            };
        }

        public async Task Grant()
        {
            Acting = Super;
            var response = await Controller().Conceder(new DelegacionPlataformaRequest
            {
                UsuarioId=Delegate.Id, Motivo="P1004B test-controlled delegation"
            },default);
            Assert.IsType<CreatedResult>(response);
        }
        public async Task Revoke()
        {
            Acting = Super;
            var response = await Controller().Revocar(Delegate.Id,
                new RevocarDelegacionPlataformaRequest
                {
                    Motivo="P1004B test-controlled revocation"
                },default);
            Assert.IsType<OkObjectResult>(response);
        }
        public async ValueTask DisposeAsync() => await Db.DisposeAsync();
    }

    [Fact]
    public async Task Ningun_usuario_sin_grant_puede_leer_directorio()
    {
        await using var f = await Fixture.Create();
        f.Acting = f.EnterpriseOnly;
        Assert.IsType<ForbidResult>(await f.Controller().Directorio(default));
        f.Acting = f.Delegate;
        Assert.IsType<ForbidResult>(await f.Controller().Directorio(default));
        Assert.Null(await f.Delegation.ObtenerDelegadoActualAsync());
    }

    [Fact]
    public async Task Delegado_con_claim_admin_no_puede_autootorgarse_o_gestionar_terceros()
    {
        await using var f = await Fixture.Create();
        await f.Grant();
        var before = await f.Db.UsuarioRolesPlataforma.CountAsync();
        f.Acting = f.Delegate;
        Assert.Null(await f.SuperScope.ObtenerActualAsync());
        Assert.IsType<OkObjectResult>(await f.Controller().Directorio(default));
        Assert.IsType<ForbidResult>(await f.Controller().Conceder(new DelegacionPlataformaRequest
        {
            UsuarioId=f.Delegate.Id, Motivo="P1004B reject self promotion"
        },default));
        Assert.IsType<ForbidResult>(await f.Controller().Conceder(new DelegacionPlataformaRequest
        {
            UsuarioId=f.EnterpriseOnly.Id, Motivo="P1004B reject global assign"
        },default));
        Assert.IsType<ForbidResult>(await f.Controller().Revocar(f.EnterpriseOnly.Id,
            new RevocarDelegacionPlataformaRequest
            {
                Motivo="P1004B reject delegated revocation"
            },default));
        Assert.Equal(before,await f.Db.UsuarioRolesPlataforma.CountAsync());
        Assert.Null(await f.SuperScope.ObtenerActualAsync());
    }

    [Fact]
    public async Task Revocar_autorizacion_impide_reuso_del_contexto_sin_nuevo_JWT()
    {
        await using var f = await Fixture.Create();
        var memberships=await f.Db.UsuarioEmpresas.CountAsync();
        await f.Grant();
        f.Acting = f.Delegate;
        Assert.IsType<OkObjectResult>(await f.Controller().Directorio(default));
        await f.Revoke();
        f.Acting = f.Delegate;
        Assert.IsType<ForbidResult>(await f.Controller().Directorio(default));
        Assert.Null(await f.Delegation.ObtenerDelegadoActualAsync());
        Assert.Equal(memberships,await f.Db.UsuarioEmpresas.CountAsync());
        var entries=await f.Db.RegistrosAuditoria.ToListAsync();
        Assert.Contains(entries,x=>x.Accion==AccionPermiso.AsignarRol&&x.Resultado=="Exito");
        Assert.Contains(entries,x=>x.Accion==AccionPermiso.ConsultarHistorial&&x.Resultado=="Exito");
        Assert.Contains(entries,x=>x.Accion==AccionPermiso.Desactivar&&x.Resultado=="Exito");
        Assert.All(entries,x=>Assert.Null(x.EmpresaId));
    }

    [Fact]
    public async Task Superadmin_no_puede_autoconcederse_rol_delegado()
    {
        await using var f = await Fixture.Create();
        Assert.IsType<ForbidResult>(await f.Controller().Conceder(new DelegacionPlataformaRequest
        {
            UsuarioId=f.Super.Id,Motivo="P1004B negative super self-grant"
        },default));
        Assert.Equal(1,await f.Db.UsuarioRolesPlataforma.CountAsync());
        Assert.Contains(await f.Db.RegistrosAuditoria.ToListAsync(),
            x=>x.Resultado=="Denegado"&&x.UsuarioId==f.Super.Id);
    }

    [Fact]
    public async Task Permiso_o_rol_revocado_no_puede_usarse_ni_reactivarse_silenciosamente()
    {
        await using var f=await Fixture.Create();
        await f.Grant();
        f.Acting=f.Delegate;
        var permission=await f.Db.Permisos.SingleAsync(x=>
            x.Codigo==PoliticaDelegacionPlataforma.CodigoPermiso);
        permission.Activo=false;
        await f.Db.SaveChangesAsync();
        Assert.IsType<ForbidResult>(await f.Controller().Directorio(default));
        f.Acting=f.Super;
        Assert.IsType<ConflictObjectResult>(await f.Controller().Conceder(
            new DelegacionPlataformaRequest
            {
                UsuarioId=f.EnterpriseOnly.Id,Motivo="P1004B reject inactive permission"
            },default));
        Assert.False(permission.Activo);
        Assert.Equal(1,await f.Db.UsuarioRolesPlataforma.CountAsync(
            x=>x.UsuarioId==f.Delegate.Id&&x.Activa));
        Assert.False(await f.Db.UsuarioRolesPlataforma.AnyAsync(
            x=>x.UsuarioId==f.EnterpriseOnly.Id));
    }
}
