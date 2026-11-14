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
/// P1-007.B: el contexto solicitado para B se resuelve desde la membresía
/// vigente del actor y nunca se sustituye por el contexto previo de A.
/// EF InMemory aísla esta regresión; no representa MySQL DEV en vivo.
/// </summary>
public sealed class P1007BTenantSwitchIsolationTests
{
    [Fact]
    public async Task P1007B_CambioDeASinMembresiaEnB_RechazaBYSinRevocarA()
    {
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("p1007b-" + Guid.NewGuid().ToString("N"))
                .Options);

        var empresaA = new Empresa("P1007B Empresa A");
        var empresaB = new Empresa("P1007B Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        var rolA = new Rol
        {
            Nombre = "P1007B Administrador A",
            NombreNormalizado = "P1007B_ADMIN_A",
            EmpresaId = empresaA.Id,
            Ambito = AmbitoAutorizacion.Empresa,
            EsAdministrador = true,
            EsSistema = false,
            Activo = true
        };
        db.Roles.Add(rolA);
        await db.SaveChangesAsync();

        var usuario = new Usuario
        {
            NombreUsuario = "p1007b-usuario",
            NombreCompleto = "Usuario de prueba P1-007.B",
            PasswordHash = "fixture-sin-clave-real",
            RolId = rolA.Id,
            Activo = true
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        db.Set<UsuarioEmpresa>().Add(new UsuarioEmpresa(usuario.Id, empresaA.Id, rolA.Id));
        await db.SaveChangesAsync();

        var identidad = new Mock<ICurrentUserService>(MockBehavior.Strict);
        identidad.SetupGet(x => x.EstaAutenticado).Returns(true);
        identidad.SetupGet(x => x.UsuarioId).Returns(usuario.Id);
        var servicio = new UsuarioScopeService(db, identidad.Object);

        var contextoA = await servicio.ObtenerActualAsync(empresaA.Id);
        var contextoB = await servicio.ObtenerActualAsync(empresaB.Id);

        Assert.NotNull(contextoA);
        Assert.Equal(empresaA.Id, contextoA.EmpresaId);
        Assert.Equal(usuario.Id, contextoA.UsuarioId);
        Assert.Equal(rolA.Id, contextoA.RolId);
        Assert.True(contextoA.EsAdministrador);

        // La ausencia de membresía para B deniega B; nunca convierte el rol
        // administrativo de A en autoridad de B ni elimina la membresía de A.
        Assert.Null(contextoB);
        Assert.Equal(empresaA.Id, (await servicio.ObtenerActualAsync(empresaA.Id))?.EmpresaId);
        Assert.Equal(empresaA.Id, (await servicio.ObtenerUnicoActualAsync())?.EmpresaId);
    }
}
