using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

public sealed class CommercialTenantSqlLockScopeTests
{
    [Fact]
    public async Task SqlLock_DosEmpresas_NoAceptaSinRbacNiCapacidadPublica()
    {
        var http = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = http };
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-direct-sql-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options, tenantHttpContextAccessor: accessor);
        var a = new Empresa("Tenant A");
        var b = new Empresa("Tenant B");
        b.Desactivar();
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => CommercialTenantSqlLockScope.RequireAsync(db));

        http.Request.Path = "/tienda/productos";
        http.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => CommercialTenantSqlLockScope.RequireAsync(db));

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        Assert.Equal(a.Id, await CommercialTenantSqlLockScope.RequireAsync(db));

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = b.Id;
        Assert.Equal(b.Id, await CommercialTenantSqlLockScope.RequireAsync(db));

        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => CommercialTenantSqlLockScope.RequireAsync(db));
    }

    [Fact]
    public async Task SqlLock_SoloUnaEmpresa_PermiteContratoHistoricoSinMarca()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-direct-sql-single-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options);
        db.Set<Empresa>().Add(new Empresa("Única"));
        await db.SaveChangesAsync();

        Assert.Null(await CommercialTenantSqlLockScope.RequireAsync(db));
    }
}
