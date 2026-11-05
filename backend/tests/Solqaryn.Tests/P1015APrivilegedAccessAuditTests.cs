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
/// P1-015.A: auditoria de lecturas y cambios de autoridad global.
/// Reutiliza superficies existentes, sin habilitar nuevas capacidades.
/// EF InMemory prueba contrato aplicativo; no certifica MySQL ni HTTP DEV remoto.
/// </summary>
public sealed class P1015APrivilegedAccessAuditTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        public AppDbContext Db { get; }
        public Mock<ICurrentUserService> Current { get; }
        public Usuario Superadmin { get; private set; } = null!;
        public Usuario Delegado { get; private set; } = null!;
        public Usuario EmpresaAdmin { get; private set; } = null!;
        public Usuario Acting { get; private set; } = null!;
        private PlataformaScopeService Scope { get; }
        private PlataformaDelegacionService Delegation { get; }

        private Fixture()
        {
            Db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("p1015a-audit-" + Guid.NewGuid().ToString("N")).Options);
            Current = new Mock<ICurrentUserService>();
            Current.SetupGet(x => x.EstaAutenticado).Returns(true);
            Current.SetupGet(x => x.UsuarioId).Returns(() => (int?)Acting.Id);
            Current.SetupGet(x => x.NombreUsuario).Returns(() => Acting.NombreUsuario);
            Current.SetupGet(x => x.EsAdministrador).Returns(true);
            Scope = new PlataformaScopeService(Db, Current.Object);
            Delegation = new PlataformaDelegacionService(Db, Current.Object);
        }

        public static async Task<Fixture> CreateAsync()
        {
            var f = new Fixture();
            var plataforma = new Rol
            {
                Nombre = "Global audit", NombreNormalizado = "P1015A_GLOBAL",
                EsSistema = true, EsAdministrador = true, Activo = true,
                Ambito = AmbitoAutorizacion.Plataforma
            };
            var empresa = new Rol
            {
                Nombre = "Empresa audit", NombreNormalizado = "P1015A_EMPRESA",
                EsSistema = true, EsAdministrador = true, Activo = true,
                Ambito = AmbitoAutorizacion.Empresa
            };
            f.Db.Roles.AddRange(plataforma, empresa);
            await f.Db.SaveChangesAsync();
            f.Superadmin = new Usuario
            {
                NombreUsuario = "p1015a-super", NombreCompleto = "Global Audit",
                PasswordHash = "no-credential", RolId = empresa.Id, Activo = true
            };
            f.Delegado = new Usuario
            {
                NombreUsuario = "p1015a-delegado", NombreCompleto = "Delegado Audit",
                PasswordHash = "no-credential", RolId = empresa.Id, Activo = true
            };
            f.EmpresaAdmin = new Usuario
            {
                NombreUsuario = "p1015a-empresa", NombreCompleto = "Empresa Audit",
                PasswordHash = "no-credential", RolId = empresa.Id, Activo = true
            };
            f.Db.Usuarios.AddRange(f.Superadmin, f.Delegado, f.EmpresaAdmin);
            var tenant = new Empresa("P1-015 tenant A");
            f.Db.Set<Empresa>().Add(tenant);
            await f.Db.SaveChangesAsync();
            f.Db.UsuarioRolesPlataforma.Add(new UsuarioRolPlataforma(f.Superadmin.Id, plataforma.Id));
            f.Db.UsuarioEmpresas.Add(new UsuarioEmpresa(f.EmpresaAdmin.Id, tenant.Id, empresa.Id));
            await f.Db.SaveChangesAsync();
            f.Acting = f.Superadmin;
            return f;
        }

        public PlataformaDelegacionesController Controller(Usuario actor, string correlationId)
        {
            Acting = actor;
            var controller = new PlataformaDelegacionesController(Db, Current.Object, Scope, Delegation);
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, actor.Id.ToString()),
                    new Claim(ClaimTypes.Name, actor.NombreUsuario)
                }, "TestJWT"))
            };
            context.TraceIdentifier = correlationId;
            controller.ControllerContext = new ControllerContext { HttpContext = context };
            return controller;
        }

        public async ValueTask DisposeAsync() => await Db.DisposeAsync();
    }

    [Fact]
    public async Task PrivilegedWrite_RecordsActorReasonCorrelationAndPlatformScope()
    {
        await using var f = await Fixture.CreateAsync();
        var controller = f.Controller(f.Superadmin, "p1015a-grant-001");
        Assert.IsType<CreatedResult>(await controller.Conceder(
            new DelegacionPlataformaRequest
            {
                UsuarioId = f.Delegado.Id,
                Motivo = "  Delegacion autorizada por auditoria  "
            }, default));

        var audit = Assert.Single(await f.Db.RegistrosAuditoria.ToArrayAsync());
        Assert.Equal(f.Superadmin.Id, audit.UsuarioId);
        Assert.Equal(f.Superadmin.NombreUsuario, audit.NombreUsuario);
        Assert.Equal("Delegacion autorizada por auditoria", audit.Motivo);
        Assert.Equal("p1015a-grant-001", audit.CorrelationId);
        Assert.Null(audit.EmpresaId); // Plataforma no es tenant empresarial.
        Assert.Equal(AmbitoAutorizacion.Plataforma, audit.AmbitoAutoridad);
        Assert.Equal(OrigenAutorizacion.RolPlataforma, audit.OrigenAutorizacion);
        Assert.Equal(AccionPermiso.AsignarRol, audit.Accion);
        Assert.Equal("Exito", audit.Resultado);
        Assert.Equal(f.Delegado.Id, audit.ReferenciaId);
    }

    [Fact]
    public async Task PrivilegedRead_RecordsVerifiedActorAndDoesNotTrustTenantHeader()
    {
        await using var f = await Fixture.CreateAsync();
        Assert.IsType<CreatedResult>(await f.Controller(f.Superadmin, "grant-read")
            .Conceder(new DelegacionPlataformaRequest
            {
                UsuarioId = f.Delegado.Id,
                Motivo = "Consulta delegada con fundamento"
            }, default));

        var controller = f.Controller(f.Delegado, "p1015a-read-002");
        controller.Request.Headers["X-Empresa-Id"] = "999999";
        Assert.IsType<OkObjectResult>(await controller.Directorio(default));

        var read = Assert.Single((await f.Db.RegistrosAuditoria.ToArrayAsync())
            .Where(x => x.Accion == AccionPermiso.ConsultarHistorial));
        Assert.Equal(f.Delegado.Id, read.UsuarioId);
        Assert.Equal(f.Delegado.NombreUsuario, read.NombreUsuario);
        Assert.Equal("Lectura delegada explícita", read.Motivo);
        Assert.Equal("p1015a-read-002", read.CorrelationId);
        Assert.Null(read.EmpresaId);
        Assert.Equal(AmbitoAutorizacion.Plataforma, read.AmbitoAutoridad);
        Assert.Equal("Exito", read.Resultado);
        Assert.False(await f.Db.UsuarioEmpresas.AnyAsync(x => x.UsuarioId == f.Delegado.Id));
    }

    [Fact]
    public async Task PrivilegedRevocation_RecordsReasonAndDeniedReadAfterRevocation()
    {
        await using var f = await Fixture.CreateAsync();
        Assert.IsType<CreatedResult>(await f.Controller(f.Superadmin, "grant-revoke")
            .Conceder(new DelegacionPlataformaRequest
            {
                UsuarioId = f.Delegado.Id,
                Motivo = "Delegacion temporal para soporte"
            }, default));

        Assert.IsType<OkObjectResult>(await f.Controller(f.Superadmin, "p1015a-revoke-003")
            .Revocar(f.Delegado.Id, new RevocarDelegacionPlataformaRequest
            {
                Motivo = "Revocacion solicitada por responsable"
            }, default));

        var revocation = Assert.Single((await f.Db.RegistrosAuditoria.ToArrayAsync())
            .Where(x => x.Accion == AccionPermiso.Desactivar));
        Assert.Equal(f.Superadmin.Id, revocation.UsuarioId);
        Assert.Equal("Revocacion solicitada por responsable", revocation.Motivo);
        Assert.Equal("p1015a-revoke-003", revocation.CorrelationId);
        Assert.Null(revocation.EmpresaId);
        Assert.Equal("Exito", revocation.Resultado);
        Assert.IsType<ForbidResult>(await f.Controller(f.Delegado, "read-revoked")
            .Directorio(default));
        Assert.Equal(2, await f.Db.RegistrosAuditoria.CountAsync());
    }

    [Fact]
    public async Task SelfGrant_IsDeniedAndAuditedWithoutNewPrivilege()
    {
        await using var f = await Fixture.CreateAsync();
        Assert.IsType<ForbidResult>(await f.Controller(f.Superadmin, "p1015a-self-denied")
            .Conceder(new DelegacionPlataformaRequest
            {
                UsuarioId = f.Superadmin.Id,
                Motivo = "Autodelegacion que debe rechazarse"
            }, default));
        var denial = Assert.Single(await f.Db.RegistrosAuditoria.ToArrayAsync());
        Assert.Equal(f.Superadmin.Id, denial.UsuarioId);
        Assert.Equal("Denegado", denial.Resultado);
        Assert.Equal("p1015a-self-denied", denial.CorrelationId);
        Assert.Equal("Autodelegacion que debe rechazarse", denial.Motivo);
        Assert.Null(denial.EmpresaId);
        Assert.Equal(1, await f.Db.UsuarioRolesPlataforma.CountAsync());
    }

    [Fact]
    public async Task EnterpriseOnlyActor_CannotReadOrChangeGlobalPermissions()
    {
        await using var f = await Fixture.CreateAsync();
        var controller = f.Controller(f.EmpresaAdmin, "p1015a-forbidden");
        controller.Request.Headers["X-Empresa-Id"] = "999999";
        Assert.IsType<ForbidResult>(await controller.Conceder(
            new DelegacionPlataformaRequest
            {
                UsuarioId = f.Delegado.Id,
                Motivo = "Intento sin permiso de plataforma"
            }, default));
        Assert.IsType<ForbidResult>(await controller.Directorio(default));
        Assert.Empty(await f.Db.RegistrosAuditoria.ToArrayAsync());
        Assert.Equal(1, await f.Db.UsuarioRolesPlataforma.CountAsync());
    }

    [Fact]
    public async Task EmptyReason_IsRejectedWithoutMutatingOrAuditingSuccess()
    {
        await using var f = await Fixture.CreateAsync();
        Assert.IsType<BadRequestObjectResult>(await f.Controller(f.Superadmin, "p1015a-no-reason")
            .Conceder(new DelegacionPlataformaRequest
            {
                UsuarioId = f.Delegado.Id,
                Motivo = "  "
            }, default));
        Assert.Empty(await f.Db.RegistrosAuditoria.ToArrayAsync());
        Assert.Equal(1, await f.Db.UsuarioRolesPlataforma.CountAsync());
    }
}
