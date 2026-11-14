using System.Security.Claims;
using System.Text.Json;
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
/// P1-015.B: cierre atomico de auditoria privilegiada.
/// Prueba que credenciales y PII del usuario no se copian a la bitacora
/// en las rutas autorizada y denegada de delegacion global.
/// EF InMemory: no certifica MySQL remoto ni logging transversal HTTP.
/// </summary>
public sealed class P1015BPrivilegedAuditPrivacyTests
{
    private const string FakeHash = "P1015B_HASH_SENTINEL_NO_LOG";
    private const string PrivateName = "P1015B_PRIVATE_NAME_NO_LOG";

    private sealed class Fixture : IAsyncDisposable
    {
        public AppDbContext Db { get; }
        public Usuario Admin { get; private set; } = null!;
        public Usuario Target { get; private set; } = null!;
        private readonly Mock<ICurrentUserService> _current = new();
        private readonly PlataformaScopeService _scope;
        private readonly PlataformaDelegacionService _delegation;

        private Fixture()
        {
            Db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("p1015b-privacy-" + Guid.NewGuid().ToString("N")).Options);
            _current.SetupGet(x => x.EstaAutenticado).Returns(true);
            _current.SetupGet(x => x.UsuarioId).Returns(() => (int?)Admin.Id);
            _current.SetupGet(x => x.NombreUsuario).Returns(() => Admin.NombreUsuario);
            _current.SetupGet(x => x.EsAdministrador).Returns(true);
            _scope = new PlataformaScopeService(Db, _current.Object);
            _delegation = new PlataformaDelegacionService(Db, _current.Object);
        }

        public static async Task<Fixture> CreateAsync()
        {
            var f = new Fixture();
            var global = new Rol
            {
                Nombre = "Global audit privacy",
                NombreNormalizado = "P1015B_GLOBAL",
                EsSistema = true,
                EsAdministrador = true,
                Activo = true,
                Ambito = AmbitoAutorizacion.Plataforma
            };
            var enterprise = new Rol
            {
                Nombre = "Empresa audit privacy",
                NombreNormalizado = "P1015B_ENTERPRISE",
                EsSistema = true,
                EsAdministrador = true,
                Activo = true,
                Ambito = AmbitoAutorizacion.Empresa
            };
            f.Db.Roles.AddRange(global, enterprise);
            await f.Db.SaveChangesAsync();
            f.Admin = new Usuario
            {
                NombreUsuario = "p1015b-global", NombreCompleto = PrivateName,
                PasswordHash = FakeHash, RolId = enterprise.Id, Activo = true
            };
            f.Target = new Usuario
            {
                NombreUsuario = "p1015b-target", NombreCompleto = PrivateName + " TARGET",
                PasswordHash = FakeHash, RolId = enterprise.Id, Activo = true
            };
            f.Db.Usuarios.AddRange(f.Admin, f.Target);
            await f.Db.SaveChangesAsync();
            f.Db.UsuarioRolesPlataforma.Add(new UsuarioRolPlataforma(f.Admin.Id, global.Id));
            await f.Db.SaveChangesAsync();
            return f;
        }

        public PlataformaDelegacionesController Controller(string correlationId)
        {
            var controller = new PlataformaDelegacionesController(
                Db, _current.Object, _scope, _delegation);
            var http = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, Admin.Id.ToString()),
                    new Claim(ClaimTypes.Name, Admin.NombreUsuario)
                }, "TestJWT"))
            };
            http.TraceIdentifier = correlationId;
            controller.ControllerContext = new ControllerContext { HttpContext = http };
            return controller;
        }

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private static void AssertMinimalAudit(RegistroAuditoria audit, string correlationId)
    {
        Assert.Equal(correlationId, audit.CorrelationId);
        Assert.Null(audit.EmpresaId);
        Assert.Equal(AmbitoAutorizacion.Plataforma, audit.AmbitoAutoridad);
        Assert.Null(audit.ValoresAnteriores);
        Assert.Null(audit.ValoresNuevos);
        Assert.Null(audit.Error);
        var payload = JsonSerializer.Serialize(audit);
        Assert.DoesNotContain(FakeHash, payload);
        Assert.DoesNotContain(PrivateName, payload);
        Assert.DoesNotContain("PasswordHash", payload);
    }

    [Fact]
    public async Task P1015B_ConcesionValidaNoRegistraHashNiPIIExcesiva()
    {
        await using var f = await Fixture.CreateAsync();
        var controller = f.Controller("p1015b-authorized");
        Assert.IsType<CreatedResult>(await controller.Conceder(
            new DelegacionPlataformaRequest
            {
                UsuarioId = f.Target.Id,
                Motivo = "Revision de delegacion autorizada"
            }, default));

        var audit = Assert.Single(await f.Db.RegistrosAuditoria.ToArrayAsync());
        Assert.Equal("Exito", audit.Resultado);
        Assert.Equal(f.Admin.Id, audit.UsuarioId);
        Assert.Equal(f.Target.Id, audit.ReferenciaId);
        AssertMinimalAudit(audit, "p1015b-authorized");
    }

    [Fact]
    public async Task P1015B_AutodelegacionNegadaTampocoRegistraSecretos()
    {
        await using var f = await Fixture.CreateAsync();
        var controller = f.Controller("p1015b-denied");
        Assert.IsType<ForbidResult>(await controller.Conceder(
            new DelegacionPlataformaRequest
            {
                UsuarioId = f.Admin.Id,
                Motivo = "Intento de autodelegacion prohibido"
            }, default));

        var audit = Assert.Single(await f.Db.RegistrosAuditoria.ToArrayAsync());
        Assert.Equal("Denegado", audit.Resultado);
        Assert.Equal(1, await f.Db.UsuarioRolesPlataforma.CountAsync());
        AssertMinimalAudit(audit, "p1015b-denied");
    }
}
