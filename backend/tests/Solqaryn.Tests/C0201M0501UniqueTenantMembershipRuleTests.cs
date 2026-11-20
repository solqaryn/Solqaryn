using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C02-01.M05.01 real unit/regression of the production membership rule.
/// EF InMemory is intentionally NOT certified as MySQL/JWT/HTTP integration.
/// </summary>
public sealed class C0201M0501UniqueTenantMembershipRuleTests
{
    [Fact]
    public async Task LoginCompanyAutoResolutionNeedsExactlyOneFreshMatchingTenantMembership()
    {
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("c0201-m0501-" + Guid.NewGuid().ToString("N"))
                .Options);

        var companyA = new Empresa("C0201 Empresa A");
        var companyB = new Empresa("C0201 Empresa B");
        db.Set<Empresa>().AddRange(companyA, companyB);
        await db.SaveChangesAsync();

        var roleA = new Rol
        {
            Nombre = "C0201 Rol Empresa A",
            NombreNormalizado = "C0201_ROL_A",
            EmpresaId = companyA.Id,
            Ambito = AmbitoAutorizacion.Empresa,
            EsAdministrador = false,
            EsSistema = false,
            Activo = true
        };
        var roleB = new Rol
        {
            Nombre = "C0201 Rol Empresa B",
            NombreNormalizado = "C0201_ROL_B",
            EmpresaId = companyB.Id,
            Ambito = AmbitoAutorizacion.Empresa,
            EsAdministrador = false,
            EsSistema = false,
            Activo = true
        };
        db.Roles.AddRange(roleA, roleB);
        await db.SaveChangesAsync();

        var actor = new Usuario
        {
            NombreUsuario = "c0201-user",
            NombreCompleto = "Unit Rule User",
            PasswordHash = "not-a-real-credential",
            RolId = roleA.Id,
            Activo = true
        };
        db.Usuarios.Add(actor);
        await db.SaveChangesAsync();

        var inA = new UsuarioEmpresa(actor.Id, companyA.Id, roleA.Id);
        var inB = new UsuarioEmpresa(actor.Id, companyB.Id, roleB.Id);
        db.UsuarioEmpresas.AddRange(inA, inB);
        await db.SaveChangesAsync();

        var current = new Mock<ICurrentUserService>(MockBehavior.Strict);
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        current.SetupGet(x => x.UsuarioId).Returns(actor.Id);
        var service = new UsuarioScopeService(db, current.Object);

        // Two legitimate memberships cannot silently select a tenant from
        // client-side preference, remembered storage, previous session or host.
        Assert.Null(await service.ObtenerUnicoActualAsync());
        Assert.Equal(2, (await service.ListarEmpresasSeleccionablesAsync()).Count);

        // Revocation of B now makes A the only current selectable tenant.
        inB.Desactivar();
        await db.SaveChangesAsync();
        var onlyA = await service.ObtenerUnicoActualAsync();
        Assert.NotNull(onlyA);
        Assert.Equal(companyA.Id, onlyA.EmpresaId);
        Assert.Null(await service.ObtenerActualAsync(companyB.Id));
        var available = await service.ListarEmpresasSeleccionablesAsync();
        Assert.Equal(companyA.Id, Assert.Single(available).EmpresaId);

        // Trying to borrow a tenant-B role for tenant-A membership fails closed.
        inA.CambiarRol(roleB.Id);
        await db.SaveChangesAsync();
        Assert.Null(await service.ObtenerUnicoActualAsync());
        Assert.Null(await service.ObtenerActualAsync(companyA.Id));

        inA.CambiarRol(roleA.Id);
        actor.Bloqueado = true;
        await db.SaveChangesAsync();
        Assert.Null(await service.ObtenerUnicoActualAsync());
        Assert.Empty(await service.ListarEmpresasSeleccionablesAsync());

        var anonymous = new Mock<ICurrentUserService>(MockBehavior.Strict);
        anonymous.SetupGet(x => x.EstaAutenticado).Returns(false);
        var unsigned = new UsuarioScopeService(db, anonymous.Object);
        Assert.Null(await unsigned.ObtenerUnicoActualAsync());
    }
}
