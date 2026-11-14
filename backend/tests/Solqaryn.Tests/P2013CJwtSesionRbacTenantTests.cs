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
/// P2-013.C: la identidad de una sesion JWT no concede el contexto
/// comercial de otra empresa por una cabecera X-Empresa-Id falsificada.
/// Prueba dirigida con EF InMemory; no certifica middleware JWT ni MySQL.
/// </summary>
public sealed class P2013CJwtSesionRbacTenantTests
{
    [Fact]
    public async Task Jwt_de_usuario_A_no_habilita_tenant_B_por_cabecera_no_confiable()
    {
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("p2013c-" + Guid.NewGuid().ToString("N"))
                .Options);

        var empresaA = new Empresa("P2013C Empresa A");
        var empresaB = new Empresa("P2013C Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        var rolA = new Rol
        {
            Nombre = "Operador A",
            NombreNormalizado = "P2013C_OPERADOR_A",
            EmpresaId = empresaA.Id,
            Ambito = AmbitoAutorizacion.Empresa,
            Activo = true
        };
        db.Roles.Add(rolA);
        await db.SaveChangesAsync();

        var usuario = new Usuario
        {
            NombreUsuario = "p2013c-usuario-a",
            NombreCompleto = "Operador A",
            PasswordHash = "fixture-sin-secreto",
            RolId = rolA.Id,
            RolEntidad = rolA,
            Activo = true
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        db.UsuarioEmpresas.Add(new UsuarioEmpresa(usuario.Id, empresaA.Id, rolA.Id));
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
        var idDeSesion = Assert.Single(jwt.Claims.Where(claim => claim.Type == "id")).Value;
        Assert.Equal(usuario.Id.ToString(), idDeSesion);
        Assert.DoesNotContain(jwt.Claims, claim => claim.Type == "empresaId");

        var identidad = new Mock<ICurrentUserService>(MockBehavior.Strict);
        identidad.SetupGet(service => service.EstaAutenticado).Returns(true);
        identidad.SetupGet(service => service.UsuarioId).Returns(int.Parse(idDeSesion));
        var controller = new TenantContextController(
            new UsuarioScopeService(db, identidad.Object))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.HttpContext.Request.Headers["X-Empresa-Id"] =
            empresaB.Id.ToString();

        Assert.True(typeof(TenantContextController)
            .IsDefined(typeof(AuthorizeAttribute), inherit: true));
        Assert.IsType<ForbidResult>(
            await controller.Obtener(empresaB.Id, CancellationToken.None));
        Assert.IsType<OkObjectResult>(
            await controller.Obtener(empresaA.Id, CancellationToken.None));
    }
}
