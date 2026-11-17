using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C13-04.M05.03: verifica el contrato real del servicio de sesion global
/// con Oracle EF y MySQL 8.4 efimero. La identidad HTTP es una fixture:
/// no certifica validacion criptografica JWT ni middleware HTTP real.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C1304M0503MySqlPlataformaSesionIntegrationTests
{
    [Fact]
    public async Task SesionGlobal_ReleeMySql_DeniegaEmpresaAB_YRevocacionSinCache()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("MySQL 8.4 efimero obligatorio.");
        var endpoint = new MySqlConnectionStringBuilder(raw);
        if (endpoint.Server is not ("localhost" or "127.0.0.1"))
            throw new InvalidOperationException("Se prohíbe MySQL remoto.");

        var connection = new MySqlConnectionStringBuilder(raw)
        {
            Database = "v19_c1304_m0503_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connection, x => x.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            int globalId;
            int empresaId;
            int tenantAId;
            int tenantBId;
            const string globalName = "c1304-platform-owner";
            const string empresaName = "c1304-tenant-admin";

            await using (var setup = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);

                var tenantA = new Empresa("C1304 Integracion A");
                var tenantB = new Empresa("C1304 Integracion B");
                setup.Set<Empresa>().AddRange(tenantA, tenantB);
                await setup.SaveChangesAsync();
                tenantAId = tenantA.Id;
                tenantBId = tenantB.Id;

                var rolGlobal = new Rol
                {
                    Nombre = "C1304 autoridad global",
                    NombreNormalizado = "C1304_GLOBAL",
                    Ambito = AmbitoAutorizacion.Plataforma,
                    EmpresaId = null,
                    EsSistema = true,
                    EsAdministrador = true,
                    Activo = true
                };
                var rolEmpresa = new Rol
                {
                    Nombre = "C1304 administrador empresarial A",
                    NombreNormalizado = "C1304_EMPRESA_A",
                    Ambito = AmbitoAutorizacion.Empresa,
                    EmpresaId = tenantAId,
                    EsSistema = false,
                    EsAdministrador = true,
                    Activo = true
                };
                setup.Roles.AddRange(rolGlobal, rolEmpresa);
                await setup.SaveChangesAsync();

                var propietario = new Usuario
                {
                    NombreUsuario = globalName,
                    NombreCompleto = "Cuenta global de prueba",
                    PasswordHash = "hash-sintetico-no-autenticable",
                    RolId = rolEmpresa.Id,
                    Activo = true
                };
                var empresarial = new Usuario
                {
                    NombreUsuario = empresaName,
                    NombreCompleto = "Cuenta de empresa de prueba",
                    PasswordHash = "hash-sintetico-no-autenticable",
                    RolId = rolEmpresa.Id,
                    Activo = true
                };
                setup.Usuarios.AddRange(propietario, empresarial);
                await setup.SaveChangesAsync();
                globalId = propietario.Id;
                empresaId = empresarial.Id;

                setup.Set<UsuarioRolPlataforma>().Add(
                    new UsuarioRolPlataforma(globalId, rolGlobal.Id));
                setup.Set<UsuarioEmpresa>().Add(
                    new UsuarioEmpresa(empresaId, tenantAId, rolEmpresa.Id));
                await setup.SaveChangesAsync();

                // La cuenta de plataforma no requiere ninguna membresia comercial.
                Assert.Empty(await setup.UsuarioEmpresas.IgnoreQueryFilters()
                    .Where(x => x.UsuarioId == globalId).ToListAsync());
            }

            await using (var globalDb = new AppDbContext(options))
            {
                var controller = Controller(globalDb, globalId, globalName, tenantBId);
                var response = await controller.ObtenerActual(CancellationToken.None);
                var ok = Assert.IsType<OkObjectResult>(response);
                Assert.NotNull(ok.Value);
                Assert.Equal("private, no-store, max-age=0",
                    controller.Response.Headers.CacheControl.ToString());
                var audit = Assert.Single(await globalDb.Set<RegistroAuditoria>().ToListAsync());
                Assert.Equal("Exito", audit.Resultado);
                Assert.Null(audit.EmpresaId);
                Assert.Equal(globalId, audit.UsuarioId);
            }

            // Ni el administrador comercial ni los headers forjados de A o B
            // elevan la autoridad global del endpoint.
            foreach (var companyId in new[] { tenantAId, tenantBId })
            {
                await using var enterpriseDb = new AppDbContext(options);
                var denied = Controller(enterpriseDb, empresaId, empresaName, companyId);
                Assert.IsType<ForbidResult>(await denied.ObtenerActual(CancellationToken.None));
                var audits = await enterpriseDb.Set<RegistroAuditoria>().ToListAsync();
                Assert.Equal("Rechazado", audits[^1].Resultado);
                Assert.Null(audits[^1].EmpresaId);
            }

            // La revocacion debe persistir en una conexion distinta y producir
            // rechazo al volver a consultar; un JWT de identidad previo no da rol.
            await using (var revokeDb = new AppDbContext(options))
            {
                var assignment = await revokeDb.Set<UsuarioRolPlataforma>()
                    .IgnoreQueryFilters()
                    .SingleAsync(x => x.UsuarioId == globalId);
                assignment.Desactivar();
                await revokeDb.SaveChangesAsync();
            }

            await using (var revokedDb = new AppDbContext(options))
            {
                var revoked = Controller(revokedDb, globalId, globalName, tenantAId);
                Assert.IsType<ForbidResult>(await revoked.ObtenerActual(CancellationToken.None));
                var audits = await revokedDb.Set<RegistroAuditoria>().ToListAsync();
                Assert.Equal(4, audits.Count);
                Assert.Equal("Rechazado", audits[^1].Resultado);
                Assert.All(audits, x => Assert.Null(x.EmpresaId));
            }

            await using (var guestDb = new AppDbContext(options))
            {
                var unauthenticated = Controller(
                    guestDb, globalId, globalName, tenantAId, authenticated: false);
                Assert.IsType<ForbidResult>(
                    await unauthenticated.ObtenerActual(CancellationToken.None));
                Assert.Equal(4, await guestDb.Set<RegistroAuditoria>().CountAsync());
            }
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static PlataformaSesionController Controller(
        AppDbContext db, int userId, string name, int companyId, bool authenticated = true)
    {
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.UsuarioId).Returns(userId);
        current.SetupGet(x => x.NombreUsuario).Returns(name);
        current.SetupGet(x => x.EstaAutenticado).Returns(authenticated);

        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(authenticated
                ? new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, name)
                }, "identidad-sintetica-no-jwt")
                : new ClaimsIdentity()),
            TraceIdentifier = "c1304-m0503-mysql-session"
        };
        http.Request.Headers["X-Empresa-Id"] = companyId.ToString();

        return new PlataformaSesionController(
            new PlataformaScopeService(db, current.Object), current.Object, db)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }
}
