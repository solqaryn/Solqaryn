using System;
using System.Linq;
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
/// P2-017.E: una membresia empresarial nunca hereda un rol Plataforma,
/// ni convierte un rol sin propietario en plantilla sin EsSistema.
/// Prueba aislada EF InMemory; no certifica HTTP/JWT/MySQL.
/// </summary>
public sealed class P2017EPlantillaRolPlataformaTests
{
    [Fact]
    public async Task Rol_plataforma_o_plantilla_no_sistema_no_autoriza_A_y_B_permanece_aislada()
    {
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("p2017e-" + Guid.NewGuid().ToString("N"))
                .Options);

        var empresaA = new Empresa("P2017E Empresa A");
        var empresaB = new Empresa("P2017E Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        var rolPlataforma = new Rol
        {
            Nombre = "Plataforma", NombreNormalizado = "P2017E_PLATAFORMA",
            Ambito = AmbitoAutorizacion.Plataforma, EmpresaId = null,
            EsSistema = true, EsAdministrador = true, Activo = true
        };
        var plantillaEmpresa = new Rol
        {
            Nombre = "Operador plantilla", NombreNormalizado = "P2017E_PLANTILLA_EMPRESA",
            Ambito = AmbitoAutorizacion.Empresa, EmpresaId = null,
            EsSistema = true, EsAdministrador = false, Activo = true
        };
        var rolB = new Rol
        {
            Nombre = "Admin B", NombreNormalizado = "P2017E_ADMIN_B",
            Ambito = AmbitoAutorizacion.Empresa, EmpresaId = empresaB.Id,
            EsAdministrador = true, Activo = true
        };
        db.Roles.AddRange(rolPlataforma, plantillaEmpresa, rolB);
        await db.SaveChangesAsync();

        // Incluso si el rol legacy del JWT es Plataforma, no da acceso A/B.
        var usuario = new Usuario
        {
            NombreUsuario = "p2017e-usuario",
            NombreCompleto = "Operador roles A/B",
            PasswordHash = "fixture-sin-secreto",
            RolId = rolPlataforma.Id,
            Activo = true
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        var membresiaA = new UsuarioEmpresa(usuario.Id, empresaA.Id, rolPlataforma.Id);
        var membresiaB = new UsuarioEmpresa(usuario.Id, empresaB.Id, rolB.Id);
        db.UsuarioEmpresas.AddRange(membresiaA, membresiaB);
        await db.SaveChangesAsync();

        var identidad = new Mock<ICurrentUserService>(MockBehavior.Strict);
        identidad.SetupGet(x => x.EstaAutenticado).Returns(true);
        identidad.SetupGet(x => x.UsuarioId).Returns(usuario.Id);
        var scope = new UsuarioScopeService(db, identidad.Object);

        Assert.Null(await scope.ObtenerActualAsync(empresaA.Id));
        Assert.True((await scope.ObtenerActualAsync(empresaB.Id))!.EsAdministrador);
        Assert.Equal(empresaB.Id,
            Assert.Single(await scope.ListarEmpresasSeleccionablesAsync()).EmpresaId);
        Assert.Null(await scope.ObtenerActualAsync()); // Legacy sin tenant: 2+ empresas.

        // La plantilla empresarial autentica autoriza A sin privilegios de B.
        membresiaA.CambiarRol(plantillaEmpresa.Id);
        await db.SaveChangesAsync();
        var autorizadoA = await scope.ObtenerActualAsync(empresaA.Id);
        Assert.NotNull(autorizadoA);
        Assert.Equal(plantillaEmpresa.Id, autorizadoA!.RolId);
        Assert.False(autorizadoA.EsAdministrador);
        Assert.True((await scope.ObtenerActualAsync(empresaB.Id))!.EsAdministrador);
        var seleccionables = await scope.ListarEmpresasSeleccionablesAsync();
        Assert.Equal(2, seleccionables.Count);
        Assert.Contains(seleccionables, x => x.EmpresaId == empresaA.Id);
        Assert.Contains(seleccionables, x => x.EmpresaId == empresaB.Id);
        Assert.Null(await scope.ObtenerUnicoActualAsync());

        // Un rol empresarial sin empresa NO es una plantilla sin EsSistema.
        plantillaEmpresa.EsSistema = false;
        await db.SaveChangesAsync();
        Assert.Null(await scope.ObtenerActualAsync(empresaA.Id));
        Assert.Equal(empresaB.Id, (await scope.ObtenerUnicoActualAsync())!.EmpresaId);

        // Marcar como sistema no permite elevar un rol de Plataforma.
        plantillaEmpresa.EsSistema = true;
        plantillaEmpresa.Ambito = AmbitoAutorizacion.Plataforma;
        await db.SaveChangesAsync();
        Assert.Null(await scope.ObtenerActualAsync(empresaA.Id));
        Assert.True((await scope.ObtenerActualAsync(empresaB.Id))!.EsAdministrador);

        // Restaurar el ambito correcto y bloquear usuario: revocacion inmediata.
        plantillaEmpresa.Ambito = AmbitoAutorizacion.Empresa;
        await db.SaveChangesAsync();
        Assert.NotNull(await scope.ObtenerActualAsync(empresaA.Id));
        usuario.Bloqueado = true;
        await db.SaveChangesAsync();
        Assert.Null(await scope.ObtenerActualAsync(empresaA.Id));
        Assert.Null(await scope.ObtenerActualAsync(empresaB.Id));
        Assert.Empty(await scope.ListarEmpresasSeleccionablesAsync());
        usuario.Bloqueado = false;
        await db.SaveChangesAsync();
        Assert.False((await scope.ObtenerActualAsync(empresaA.Id))!.EsAdministrador);
        Assert.True((await scope.ObtenerActualAsync(empresaB.Id))!.EsAdministrador);
        Assert.Equal(rolPlataforma.Id, usuario.RolId);
    }
}
