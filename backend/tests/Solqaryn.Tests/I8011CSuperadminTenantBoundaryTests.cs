using System;
using System.Threading.Tasks;
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
/// I8-011.C: una autoridad global de Plataforma no implica membresia
/// comercial; un grant explicito de A jamas concede B.
/// Prueba de servicios con EF InMemory, no prueba HTTP/JWT/MySQL real.
/// </summary>
public sealed class I8011CSuperadminTenantBoundaryTests
{
    [Fact]
    public async Task I8011C_SuperadminGlobalNoPuedeSaltarMembresiaEmpresarialAB()
    {
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("i8011c-" + Guid.NewGuid().ToString("N"))
                .Options);

        var empresaA = new Empresa("I8011C Empresa A");
        var empresaB = new Empresa("I8011C Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        var rolGlobal = new Rol
        {
            Nombre = "Superadministrador de plataforma",
            NombreNormalizado = "I8011C_PLATAFORMA",
            Ambito = AmbitoAutorizacion.Plataforma,
            EmpresaId = null,
            EsSistema = true,
            EsAdministrador = true,
            Activo = true
        };
        var rolEmpresaA = new Rol
        {
            Nombre = "Operador de empresa A",
            NombreNormalizado = "I8011C_OPERADOR_A",
            Ambito = AmbitoAutorizacion.Empresa,
            EmpresaId = empresaA.Id,
            EsSistema = false,
            EsAdministrador = false,
            Activo = true
        };
        db.Roles.AddRange(rolGlobal, rolEmpresaA);
        await db.SaveChangesAsync();

        var usuario = new Usuario
        {
            NombreUsuario = "i8011c-superadmin",
            NombreCompleto = "Superadmin fixture sin bypass",
            PasswordHash = "fixture-sin-credenciales-reales",
            RolId = rolGlobal.Id,
            Activo = true
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        db.Set<UsuarioRolPlataforma>().Add(
            new UsuarioRolPlataforma(usuario.Id, rolGlobal.Id));
        await db.SaveChangesAsync();

        var actual = new Mock<ICurrentUserService>(MockBehavior.Strict);
        actual.SetupGet(x => x.EstaAutenticado).Returns(true);
        actual.SetupGet(x => x.UsuarioId).Returns(usuario.Id);
        actual.SetupGet(x => x.NombreUsuario).Returns(usuario.NombreUsuario);
        var plataforma = new PlataformaScopeService(db, actual.Object);
        var comercial = new UsuarioScopeService(db, actual.Object);

        // Positivo global: una sola autoridad de sistema habilita la plataforma.
        Assert.NotNull(await plataforma.ObtenerActualAsync());

        // Negativo comercial: el superadmin no hereda A/B por rol legacy.
        Assert.Null(await comercial.ObtenerActualAsync(empresaA.Id));
        Assert.Null(await comercial.ObtenerActualAsync(empresaB.Id));
        Assert.Null(await comercial.ObtenerActualAsync());
        Assert.Null(await comercial.ObtenerUnicoActualAsync());
        Assert.Empty(await comercial.ListarEmpresasSeleccionablesAsync());

        // Un grant empresarial explicito activa solo A, nunca B.
        db.UsuarioEmpresas.Add(
            new UsuarioEmpresa(usuario.Id, empresaA.Id, rolEmpresaA.Id));
        await db.SaveChangesAsync();

        Assert.NotNull(await comercial.ObtenerActualAsync(empresaA.Id));
        Assert.False((await comercial.ObtenerActualAsync(empresaA.Id))!.EsAdministrador);
        Assert.Null(await comercial.ObtenerActualAsync(empresaB.Id));
        Assert.Equal(empresaA.Id,
            (await comercial.ObtenerUnicoActualAsync())!.EmpresaId);
        Assert.Equal(empresaA.Id,
            Assert.Single(await comercial.ListarEmpresasSeleccionablesAsync()).EmpresaId);
        Assert.NotNull(await plataforma.ObtenerActualAsync());

        // La revocacion por bloqueo aplica tanto a Plataforma como a A.
        usuario.Bloqueado = true;
        await db.SaveChangesAsync();
        Assert.Null(await plataforma.ObtenerActualAsync());
        Assert.Null(await comercial.ObtenerActualAsync(empresaA.Id));
        Assert.Null(await comercial.ObtenerActualAsync(empresaB.Id));
        Assert.Empty(await comercial.ListarEmpresasSeleccionablesAsync());
    }
}
