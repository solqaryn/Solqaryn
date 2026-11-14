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

public sealed class UsuarioLegacyScopeTwoTenantsFailClosedTests
{
    [Fact]
    public async Task GlobalLegacyRole_NoAutorizaDosEmpresas_RbacUsaMembresiaVigente()
    {
        var http = new DefaultHttpContext();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns(http);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-scope-legacy-ab-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options, tenantHttpContextAccessor: accessor.Object);

        var a = new Empresa("Empresa A");
        var b = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        var rolA = new Rol
        {
            Nombre = "OPERADOR A", NombreNormalizado = "OPERADOR_A",
            EmpresaId = a.Id, Activo = true, EsAdministrador = false
        };
        var rolB = new Rol
        {
            Nombre = "ADMIN B", NombreNormalizado = "ADMIN_B",
            EmpresaId = b.Id, Activo = true, EsAdministrador = true
        };
        db.Roles.AddRange(rolA, rolB);
        await db.SaveChangesAsync();

        var usuario = new Usuario
        {
            NombreUsuario = "p4-mismo-usuario", NombreCompleto = "Usuario ambas empresas",
            PasswordHash = "hash-prueba", RolId = rolB.Id, Activo = true
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        db.UsuarioEmpresas.AddRange(
            new UsuarioEmpresa(usuario.Id, a.Id, rolA.Id),
            new UsuarioEmpresa(usuario.Id, b.Id, rolB.Id));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        current.SetupGet(x => x.UsuarioId).Returns(usuario.Id);
        var service = new UsuarioScopeService(db, current.Object);

        // El rol legacy parece ser ADMIN B, pero sin marca no puede dar permisos
        // empresariales ni divulgar una de las dos empresas por defecto.
        Assert.Null(await service.ObtenerActualAsync());

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        var scopeA = await service.ObtenerActualAsync();
        Assert.NotNull(scopeA);
        Assert.Equal(rolA.Id, scopeA.RolId);
        Assert.False(scopeA.EsAdministrador);

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = b.Id;
        var scopeB = await service.ObtenerActualAsync();
        Assert.NotNull(scopeB);
        Assert.Equal(rolB.Id, scopeB.RolId);
        Assert.True(scopeB.EsAdministrador);

        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        http.Request.Path = "/tienda/productos";
        http.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] = b.Id;
        Assert.Null(await service.ObtenerActualAsync());
    }
}
