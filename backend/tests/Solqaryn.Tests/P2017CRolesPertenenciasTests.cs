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
/// P2-017.C: regresion del scope de roles y membresias A/B.
/// EF InMemory: no acredita HTTP/JWT real ni MySQL desplegado.
/// </summary>
public sealed class P2017CRolesPertenenciasTests
{
    [Fact]
    public async Task B_rol_eliminado_o_membresia_inactiva_no_revoca_A_ni_autoriza_B()
    {
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("p2017c-" + Guid.NewGuid().ToString("N"))
                .Options);

        var empresaA = new Empresa("P2017C Empresa A");
        var empresaB = new Empresa("P2017C Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        var rolA = new Rol
        {
            Nombre = "Operador A", NombreNormalizado = "P2017C_OPERADOR_A",
            EmpresaId = empresaA.Id, Ambito = AmbitoAutorizacion.Empresa,
            Activo = true
        };
        var rolB = new Rol
        {
            Nombre = "Administrador B", NombreNormalizado = "P2017C_ADMIN_B",
            EmpresaId = empresaB.Id, Ambito = AmbitoAutorizacion.Empresa,
            EsAdministrador = true, Activo = true
        };
        db.Roles.AddRange(rolA, rolB);
        await db.SaveChangesAsync();

        // RolId legacy apunta a B: no debe elevar ni sustituir el rol de A.
        var usuario = new Usuario
        {
            NombreUsuario = "p2017c-operador", NombreCompleto = "Operador A y B",
            PasswordHash = "fixture-sin-secreto", RolId = rolB.Id, Activo = true
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        var membresiaA = new UsuarioEmpresa(usuario.Id, empresaA.Id, rolA.Id);
        var membresiaB = new UsuarioEmpresa(usuario.Id, empresaB.Id, rolB.Id);
        db.UsuarioEmpresas.AddRange(membresiaA, membresiaB);
        await db.SaveChangesAsync();

        var identidad = new Mock<ICurrentUserService>(MockBehavior.Strict);
        identidad.SetupGet(x => x.EstaAutenticado).Returns(true);
        identidad.SetupGet(x => x.UsuarioId).Returns(usuario.Id);
        var scope = new UsuarioScopeService(db, identidad.Object);

        Assert.Equal(rolA.Id, (await scope.ObtenerActualAsync(empresaA.Id))!.RolId);
        Assert.True((await scope.ObtenerActualAsync(empresaB.Id))!.EsAdministrador);

        // Una baja logica de rol B invalida la membresia aunque siga activa.
        rolB.Eliminado = true;
        await db.SaveChangesAsync();
        Assert.Null(await scope.ObtenerActualAsync(empresaB.Id));
        Assert.Equal(rolA.Id, (await scope.ObtenerActualAsync(empresaA.Id))!.RolId);
        var seleccionables = await scope.ListarEmpresasSeleccionablesAsync();
        Assert.Equal(empresaA.Id, Assert.Single(seleccionables).EmpresaId);
        Assert.Equal(rolB.Id, usuario.RolId);

        // Restaurar rol B no puede resucitar una membresia revocada.
        rolB.Eliminado = false;
        membresiaB.Desactivar();
        await db.SaveChangesAsync();
        Assert.Null(await scope.ObtenerActualAsync(empresaB.Id));
        Assert.Equal(empresaA.Id, (await scope.ObtenerUnicoActualAsync())!.EmpresaId);
        Assert.Equal(empresaA.Id,
            Assert.Single(await scope.ListarEmpresasSeleccionablesAsync()).EmpresaId);
        Assert.True(membresiaA.Activa);
    }
}
