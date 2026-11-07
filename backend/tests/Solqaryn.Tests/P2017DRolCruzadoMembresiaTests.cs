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
/// P2-017.D: el rol de B no puede autorizar la membresia de A.
/// Regresion EF InMemory; no certifica JWT/HTTP ni MySQL real.
/// </summary>
public sealed class P2017DRolCruzadoMembresiaTests
{
    [Fact]
    public async Task Rol_admin_B_en_membresia_A_no_autoriza_A_y_corregir_A_no_revoca_B()
    {
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("p2017d-" + Guid.NewGuid().ToString("N"))
                .Options);

        var empresaA = new Empresa("P2017D Empresa A");
        var empresaB = new Empresa("P2017D Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        var rolA = new Rol
        {
            Nombre = "Operador A", NombreNormalizado = "P2017D_OPERADOR_A",
            EmpresaId = empresaA.Id, Ambito = AmbitoAutorizacion.Empresa,
            Activo = true
        };
        var rolB = new Rol
        {
            Nombre = "Administrador B", NombreNormalizado = "P2017D_ADMIN_B",
            EmpresaId = empresaB.Id, Ambito = AmbitoAutorizacion.Empresa,
            EsAdministrador = true, Activo = true
        };
        db.Roles.AddRange(rolA, rolB);
        await db.SaveChangesAsync();

        // RolId legacy B nunca sustituye la autorizacion de la membresia A.
        var usuario = new Usuario
        {
            NombreUsuario = "p2017d-operador",
            NombreCompleto = "Operador de prueba A/B",
            PasswordHash = "fixture-sin-secreto",
            RolId = rolB.Id,
            Activo = true
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        // Membresia A manipulada para apuntar al rol administrativo de B.
        var membresiaA = new UsuarioEmpresa(usuario.Id, empresaA.Id, rolB.Id);
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

        // Al restaurar el rol propio de A ambas membresias son validas.
        membresiaA.CambiarRol(rolA.Id);
        await db.SaveChangesAsync();
        var alcanceA = await scope.ObtenerActualAsync(empresaA.Id);
        Assert.Equal(rolA.Id, alcanceA!.RolId);
        Assert.False(alcanceA.EsAdministrador);
        Assert.True((await scope.ObtenerActualAsync(empresaB.Id))!.EsAdministrador);
        Assert.Null(await scope.ObtenerUnicoActualAsync());

        // Revocar B no revoca A ni permite reusar su rol B.
        membresiaB.Desactivar();
        await db.SaveChangesAsync();
        Assert.Null(await scope.ObtenerActualAsync(empresaB.Id));
        Assert.Equal(empresaA.Id, (await scope.ObtenerUnicoActualAsync())!.EmpresaId);
        Assert.Equal(empresaA.Id,
            Assert.Single(await scope.ListarEmpresasSeleccionablesAsync()).EmpresaId);
        Assert.Equal(rolB.Id, usuario.RolId);
    }
}
