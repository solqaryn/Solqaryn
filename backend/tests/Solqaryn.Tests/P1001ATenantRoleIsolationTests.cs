using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// Regresion sin credenciales: el repositorio EF y RolService deben ocultar
/// el rol personalizado de B cuando existe un contexto RBAC verificado de A.
/// No sustituye autenticacion HTTP ni readback del MySQL DEV desplegado.
/// </summary>
public sealed class P1001ATenantRoleIsolationTests
{
    [Fact]
    public async Task Tenant_A_no_puede_leer_ni_mutar_rol_personalizado_de_B()
    {
        var http = new DefaultHttpContext();
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("p1001a-role-isolation-" + Guid.NewGuid().ToString("N"))
                .Options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("RBAC TEST A");
        var empresaB = new Empresa("RBAC TEST B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var rolA = new Rol
        {
            Nombre = "ROL PRIVADO A", NombreNormalizado = "rol_privado_a",
            EmpresaId = empresaA.Id, EsSistema = false
        };
        db.Roles.Add(rolA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var rolB = new Rol
        {
            Nombre = "ROL PRIVADO B", NombreNormalizado = "rol_privado_b",
            EmpresaId = empresaB.Id, EsSistema = false
        };
        db.Roles.Add(rolB);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var repository = new RolRepository(db);
        var visiblesA = await repository.GetAllAsync();
        Assert.Contains(visiblesA, r => r.Id == rolA.Id);
        Assert.DoesNotContain(visiblesA, r => r.Id == rolB.Id);
        Assert.NotNull(await repository.GetByIdAsync(rolA.Id));
        Assert.Null(await repository.GetByIdAsync(rolB.Id));

        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var usuario = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var service = new RolService(repository, auditoria.Object, usuario.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.UpdateAsync(rolB.Id, new ActualizarRolDto { Nombre = "ALTERADO" }));
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.ActivarAsync(rolB.Id));
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.DesactivarAsync(rolB.Id));
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.EliminarLogicoAsync(rolB.Id));
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.EliminarPermanenteAsync(rolB.Id));
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.DuplicarAsync(rolB.Id, "COPIA AJENA", empresaA.Id));

        auditoria.VerifyNoOtherCalls();
        usuario.VerifyNoOtherCalls();
        Assert.False(db.ChangeTracker.HasChanges());

        // Control positivo: el rol realmente existe y su tenant B si puede leerlo.
        db.ChangeTracker.Clear();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var visibleB = await repository.GetByIdAsync(rolB.Id);
        Assert.NotNull(visibleB);
        Assert.Equal("ROL PRIVADO B", visibleB!.Nombre);
        Assert.False(visibleB.Eliminado);
    }
}
