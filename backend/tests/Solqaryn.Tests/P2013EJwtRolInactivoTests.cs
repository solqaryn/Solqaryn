using System.IdentityModel.Tokens.Jwt;
using System.Linq;
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
/// P2-013.E: el JWT ya emitido no reactiva un rol deshabilitado ni a un usuario
/// bloqueado; el tenant B permanece denegado pese a X-Empresa-Id.
/// La prueba usa EF InMemory, no middleware HTTP, firma JWT ni MySQL real.
/// </summary>
public sealed class P2013EJwtRolInactivoTests
{
    [Fact]
    public async Task Jwt_previo_no_autoriza_rol_inactivo_o_usuario_bloqueado_ni_tenant_B()
    {
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("p2013e-" + Guid.NewGuid().ToString("N"))
                .Options);

        var empresaA = new Empresa("P2013E Empresa A");
        var empresaB = new Empresa("P2013E Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        var rolA = new Rol
        {
            Nombre = "Operador A",
            NombreNormalizado = "P2013E_OPERADOR_A",
            EmpresaId = empresaA.Id,
            Ambito = AmbitoAutorizacion.Empresa,
            Activo = true
        };
        db.Roles.Add(rolA);
        await db.SaveChangesAsync();

        var usuario = new Usuario
        {
            NombreUsuario = "p2013e-usuario-a",
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

        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "Test-Only-Secret-With-More-Than-32-Characters-2026",
                ["Jwt:Issuer"] = "Solqaryn.Tests",
                ["Jwt:Audience"] = "Solqaryn.Tests.Frontend",
                ["Jwt:ExpiraMinutos"] = "35"
            }).Build();
        var (token, _) = new JwtService(config).GenerarToken(usuario);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var identityId = int.Parse(Assert.Single(jwt.Claims.Where(c => c.Type == "id")).Value);
        Assert.Equal(usuario.Id, identityId);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "empresaId");

        var identity = new Mock<ICurrentUserService>(MockBehavior.Strict);
        identity.SetupGet(x => x.EstaAutenticado).Returns(true);
        identity.SetupGet(x => x.UsuarioId).Returns(identityId);
        var controller = new TenantContextController(
            new UsuarioScopeService(db, identity.Object))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        // El token original da acceso a A, nunca a B aunque el cliente fuerce la cabecera.
        controller.HttpContext.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        Assert.IsType<ForbidResult>(await controller.Obtener(empresaB.Id, CancellationToken.None));
        controller.HttpContext.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        Assert.IsType<OkObjectResult>(await controller.Obtener(empresaA.Id, CancellationToken.None));

        // El mismo JWT y membresia permanecen; el rol deja de tener autoridad.
        rolA.Activo = false;
        await db.SaveChangesAsync();
        Assert.IsType<ForbidResult>(await controller.Obtener(empresaA.Id, CancellationToken.None));
        Assert.Empty(await new UsuarioScopeService(db, identity.Object)
            .ListarEmpresasSeleccionablesAsync());

        // La reactivacion del rol no debe evitar el bloqueo del usuario.
        rolA.Activo = true;
        usuario.Bloqueado = true;
        await db.SaveChangesAsync();
        Assert.IsType<ForbidResult>(await controller.Obtener(empresaA.Id, CancellationToken.None));
        Assert.IsType<ForbidResult>(await controller.Obtener(empresaB.Id, CancellationToken.None));

        // Control positivo: quitar el bloqueo restaura solo la membresia A vigente.
        usuario.Bloqueado = false;
        await db.SaveChangesAsync();
        Assert.IsType<OkObjectResult>(await controller.Obtener(empresaA.Id, CancellationToken.None));
        Assert.IsType<ForbidResult>(await controller.Obtener(empresaB.Id, CancellationToken.None));
    }
}
