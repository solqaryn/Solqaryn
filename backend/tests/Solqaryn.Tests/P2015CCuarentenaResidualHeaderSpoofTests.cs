using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-015.C: delimita la cuarentena residual de autoridad privada al origen
/// de la marca de tenant verificada. La cabecera del cliente no la sustituye.
/// Prueba sintética de servicio con EF InMemory; no certifica HTTP ni MySQL.
/// </summary>
public sealed class P2015CCuarentenaResidualHeaderSpoofTests
{
    [Fact]
    public async Task DosEmpresas_HeaderFalsificadoNoHabilitaRolGlobalLegacySinMarcaVerificada()
    {
        var http = new DefaultHttpContext();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns(http);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2015c-quarantine-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options, tenantHttpContextAccessor: accessor.Object);

        var a = new Empresa("P2015C Empresa A");
        var b = new Empresa("P2015C Empresa B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        var operadorA = new Rol
        {
            Nombre = "OPERADOR A",
            NombreNormalizado = "OPERADOR_A",
            EmpresaId = a.Id,
            Activo = true,
            EsAdministrador = false
        };
        var adminB = new Rol
        {
            Nombre = "ADMIN B",
            NombreNormalizado = "ADMIN_B",
            EmpresaId = b.Id,
            Activo = true,
            EsAdministrador = true
        };
        db.Roles.AddRange(operadorA, adminB);
        await db.SaveChangesAsync();

        var usuario = new Usuario
        {
            NombreUsuario = "p2015c-usuario",
            NombreCompleto = "Usuario sintetico A y B",
            PasswordHash = "hash-sintetico",
            RolId = adminB.Id,
            Activo = true
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        db.UsuarioEmpresas.AddRange(
            new UsuarioEmpresa(usuario.Id, a.Id, operadorA.Id),
            new UsuarioEmpresa(usuario.Id, b.Id, adminB.Id));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        current.SetupGet(x => x.UsuarioId).Returns(usuario.Id);
        var scope = new UsuarioScopeService(db, current.Object);

        // El navegador puede fabricar ambas cabeceras, pero no la marca RBAC.
        http.Request.Headers["X-Empresa-Id"] = b.Id.ToString();
        Assert.Null(await scope.ObtenerActualAsync());
        http.Request.Headers["X-Empresa-Id"] = a.Id.ToString();
        Assert.Null(await scope.ObtenerActualAsync());

        // Control positivo: una marca emitida por el backend selecciona el rol
        // empresarial vigente, nunca el rol global legacy de la otra empresa.
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        var autorizadoA = await scope.ObtenerActualAsync();
        Assert.NotNull(autorizadoA);
        Assert.Equal(operadorA.Id, autorizadoA.RolId);
        Assert.False(autorizadoA.EsAdministrador);

        // La sobrecarga tenant-aware expone EmpresaId; el resultado legacy
        // no lo contiene. Ambos deben resolver el mismo rol empresarial A.
        var autorizadoTenantA = await scope.ObtenerActualAsync(a.Id);
        Assert.NotNull(autorizadoTenantA);
        Assert.Equal(a.Id, autorizadoTenantA.EmpresaId);
        Assert.Equal(autorizadoA.RolId, autorizadoTenantA.RolId);

        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        Assert.Null(await scope.ObtenerActualAsync());
    }
}
