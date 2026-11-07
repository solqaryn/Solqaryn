using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
/// P2-013.D: una sesion JWT emitida antes de revocar UsuarioEmpresa
/// no concede derechos tenant despues de desactivar la membresia.
/// EF InMemory no certifica validacion HTTP criptografica ni MySQL.
/// </summary>
public sealed class P2013DJwtMembresiaRevocadaTests
{
    [Fact]
    public async Task Jwt_vigente_no_restaura_membresia_revocada_ni_habilita_tenant_B()
    {
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("p2013d-" + Guid.NewGuid().ToString("N"))
                .Options);

        var empresaA = new Empresa("P2013D Empresa A");
        var empresaB = new Empresa("P2013D Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        var rolA = new Rol
        {
            Nombre = "Operador A",
            NombreNormalizado = "P2013D_OPERADOR_A",
            EmpresaId = empresaA.Id,
            Ambito = AmbitoAutorizacion.Empresa,
            Activo = true
        };
        db.Roles.Add(rolA);
        await db.SaveChangesAsync();

        var usuario = new Usuario
        {
            NombreUsuario = "p2013d-usuario-a",
            NombreCompleto = "Operador A",
            PasswordHash = "fixture-sin-secreto",
            RolId = rolA.Id,
            RolEntidad = rolA,
            Activo = true
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        var membresiaA = new UsuarioEmpresa(usuario.Id, empresaA.Id, rolA.Id);
        db.UsuarioEmpresas.Add(membresiaA);
        await db.SaveChangesAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "Test-Only-Secret-With-More-Than-32-Characters-2026",
                ["Jwt:Issuer"] = "Solqaryn.Tests",
                ["Jwt:Audience"] = "Solqaryn.Tests.Frontend",
                ["Jwt:ExpiraMinutos"] = "35"
            })
            .Build();

        var (token, _) = new JwtService(configuration).GenerarToken(usuario);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var idDeSesion = Assert.Single(jwt.Claims.Where(c => c.Type == "id")).Value;
        Assert.Equal(usuario.Id.ToString(), idDeSesion);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "empresaId");

        var identidad = new Mock<ICurrentUserService>(MockBehavior.Strict);
        identidad.SetupGet(s => s.EstaAutenticado).Returns(true);
        identidad.SetupGet(s => s.UsuarioId).Returns(int.Parse(idDeSesion));
        var controller = new TenantContextController(
            new UsuarioScopeService(db, identidad.Object))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        Assert.True(typeof(TenantContextController)
            .IsDefined(typeof(AuthorizeAttribute), inherit: true));

        // A tiene membresia valida, B no: ni la cabecera altera la fuente RBAC.
        controller.HttpContext.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        Assert.IsType<ForbidResult>(
            await controller.Obtener(empresaB.Id, CancellationToken.None));
        controller.HttpContext.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        Assert.IsType<OkObjectResult>(
            await controller.Obtener(empresaA.Id, CancellationToken.None));

        // Se revoca en base de datos, sin reemitir ni modificar la identidad JWT.
        membresiaA.Desactivar();
        await db.SaveChangesAsync();
        Assert.False(membresiaA.Activa);
        Assert.IsType<ForbidResult>(
            await controller.Obtener(empresaA.Id, CancellationToken.None));
        controller.HttpContext.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        Assert.IsType<ForbidResult>(
            await controller.Obtener(empresaB.Id, CancellationToken.None));
    }
}
