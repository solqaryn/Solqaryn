using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-003.D: un administrador de A no puede restablecer la clave de una
/// identidad compartida con B, aunque la membresia B este inactiva.
/// Prueba de servicio EF InMemory, no certifica HTTP/JWT ni MySQL.
/// </summary>
public sealed class I8003DResetAdminIdentidadCompartidaTests
{
    [Fact]
    public async Task ResetPorAdminA_RechazaIdentidadCompartidaConBInactiva_SinMutarPassword()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("i8003d-" + Guid.NewGuid().ToString("N")).Options);

        db.Roles.AddRange(
            new Rol
            {
                Id = 110, Nombre = "Admin A", NombreNormalizado = "admin_a",
                EmpresaId = 11, Ambito = AmbitoAutorizacion.Empresa,
                EsSistema = false, EsAdministrador = true
            },
            new Rol
            {
                Id = 111, Nombre = "Operador A", NombreNormalizado = "operador_a",
                EmpresaId = 11, Ambito = AmbitoAutorizacion.Empresa,
                EsSistema = false
            },
            new Rol
            {
                Id = 220, Nombre = "Operador B", NombreNormalizado = "operador_b",
                EmpresaId = 22, Ambito = AmbitoAutorizacion.Empresa,
                EsSistema = false
            });

        const string hashOriginal = "hash-fixture-no-modificar";
        db.Usuarios.Add(new Usuario
        {
            Id = 71, NombreUsuario = "usuario_compartido",
            NombreCompleto = "Identidad de A y B", PasswordHash = hashOriginal,
            RolId = 111, Activo = true
        });
        var membresiaBInactiva = new UsuarioEmpresa(71, 22, 220);
        membresiaBInactiva.Desactivar();
        db.UsuarioEmpresas.AddRange(
            new UsuarioEmpresa(71, 11, 111), membresiaBInactiva);
        await db.SaveChangesAsync();

        var users = new Mock<IUsuarioService>(MockBehavior.Strict);
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        var current = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var perms = new Mock<IPermisoService>(MockBehavior.Strict);
        var audit = new Mock<IAuditoriaService>(MockBehavior.Strict);

        current.SetupGet(x => x.UsuarioId).Returns(1);
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        scope.Setup(x => x.ObtenerActualAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(1, 11, 110, "Admin A", true));
        perms.Setup(x => x.VerificarPermisoAsync(
                11, ModuloSistema.Usuarios, AccionPermiso.Editar))
            .Returns(Task.CompletedTask);

        var sut = new UsuarioAdministracionEmpresaService(
            db, users.Object, scope.Object, perms.Object, current.Object, audit.Object);
        var solicitud = new UpdateUsuarioDto
        {
            NombreUsuario = "usuario_compartido",
            NombreCompleto = "Identidad de A y B",
            RolId = 111,
            NuevaPassword = "NuevoSecreto-2026!"
        };

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => sut.ActualizarAsync(71, solicitud, 11));

        Assert.Equal(hashOriginal, db.Usuarios.IgnoreQueryFilters()
            .Single(x => x.Id == 71).PasswordHash);
        Assert.True(db.UsuarioEmpresas.IgnoreQueryFilters()
            .Single(x => x.UsuarioId == 71 && x.EmpresaId == 11).Activa);
        Assert.False(db.UsuarioEmpresas.IgnoreQueryFilters()
            .Single(x => x.UsuarioId == 71 && x.EmpresaId == 22).Activa);

        perms.Verify(x => x.VerificarPermisoAsync(
            11, ModuloSistema.Usuarios, AccionPermiso.Editar), Times.Once);
        perms.Verify(x => x.VerificarPermisoAsync(
            11, ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena),
            Times.Never);
        users.VerifyNoOtherCalls();
        audit.VerifyNoOtherCalls();
    }
}
