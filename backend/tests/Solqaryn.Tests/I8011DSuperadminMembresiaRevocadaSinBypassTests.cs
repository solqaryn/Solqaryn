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
/// I8-011.D: una autoridad global de Plataforma no sirve como rol
/// empresarial ni rescata una membresia revocada. EF InMemory, no HTTP/JWT/MySQL.
/// </summary>
public sealed class I8011DSuperadminMembresiaRevocadaSinBypassTests
{
    [Fact]
    public async Task I8011D_RolPlataformaAsignadoAEmpresaNoConcedeAccesoNiRescataRevocacion()
    {
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("i8011d-" + Guid.NewGuid().ToString("N"))
                .Options);

        var empresaA = new Empresa("I8011D empresa A");
        var empresaB = new Empresa("I8011D empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        var rolPlataforma = new Rol
        {
            Nombre = "Superadmin global",
            NombreNormalizado = "I8011D_PLATAFORMA",
            Ambito = AmbitoAutorizacion.Plataforma,
            EmpresaId = null,
            EsSistema = true,
            EsAdministrador = true,
            Activo = true
        };
        var rolA = new Rol
        {
            Nombre = "Operador A",
            NombreNormalizado = "I8011D_OPERADOR_A",
            Ambito = AmbitoAutorizacion.Empresa,
            EmpresaId = empresaA.Id,
            EsSistema = false,
            EsAdministrador = false,
            Activo = true
        };
        db.Roles.AddRange(rolPlataforma, rolA);
        await db.SaveChangesAsync();

        var usuario = new Usuario
        {
            NombreUsuario = "i8011d-global",
            NombreCompleto = "Fixture autoridad global",
            PasswordHash = "fixture-no-es-una-credencial",
            RolId = rolPlataforma.Id,
            Activo = true
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        db.Set<UsuarioRolPlataforma>().Add(
            new UsuarioRolPlataforma(usuario.Id, rolPlataforma.Id));

        var membresiaA = new UsuarioEmpresa(usuario.Id, empresaA.Id, rolA.Id);
        // Fixture adversarial: una relacion de empresa intenta reutilizar
        // un rol global que no pertenece al ambito Empresa.
        var membresiaBFalsa = new UsuarioEmpresa(usuario.Id, empresaB.Id, rolPlataforma.Id);
        db.UsuarioEmpresas.AddRange(membresiaA, membresiaBFalsa);
        await db.SaveChangesAsync();

        var current = new Mock<ICurrentUserService>(MockBehavior.Strict);
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        current.SetupGet(x => x.UsuarioId).Returns(usuario.Id);
        current.SetupGet(x => x.NombreUsuario).Returns(usuario.NombreUsuario);

        var plataforma = new PlataformaScopeService(db, current.Object);
        var comercial = new UsuarioScopeService(db, current.Object);

        // Plataforma valida; solo el grant empresarial legitimo A es usable.
        Assert.NotNull(await plataforma.ObtenerActualAsync());
        var scopeA = await comercial.ObtenerActualAsync(empresaA.Id);
        Assert.NotNull(scopeA);
        Assert.False(scopeA.EsAdministrador);
        Assert.Null(await comercial.ObtenerActualAsync(empresaB.Id));
        Assert.Equal(empresaA.Id, (await comercial.ObtenerUnicoActualAsync())!.EmpresaId);
        Assert.Equal(empresaA.Id,
            Assert.Single(await comercial.ListarEmpresasSeleccionablesAsync()).EmpresaId);

        // Revocacion de A: B continua invalida a pesar del rol global.
        membresiaA.Desactivar();
        await db.SaveChangesAsync();

        Assert.NotNull(await plataforma.ObtenerActualAsync());
        Assert.Null(await comercial.ObtenerActualAsync(empresaA.Id));
        Assert.Null(await comercial.ObtenerActualAsync(empresaB.Id));
        Assert.Null(await comercial.ObtenerUnicoActualAsync());
        Assert.Null(await comercial.ObtenerActualAsync());
        Assert.Empty(await comercial.ListarEmpresasSeleccionablesAsync());
    }
}
