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
/// I8-003.E: un administrador de A no puede iniciar un reset de contraseña
/// de una identidad cuya única membresía pertenece a B. Prueba aislada EF
/// InMemory; no acredita HTTP, JWT, SMTP ni MySQL real.
/// </summary>
public sealed class I8003EResetAdminSoloTenantBTests
{
    [Fact]
    public async Task AdminA_NoPuedeRestablecerClaveDeUsuarioSoloB_NiModificarIdentidad()
    {
        const int empresaA = 11;
        const int empresaB = 22;
        const int usuarioB = 71;
        const string hashOriginal = "hash-sintetico-B-inmutable";

        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("i8003e-" + Guid.NewGuid().ToString("N")).Options);

        db.Roles.AddRange(
            new Rol
            {
                Id = 110, Nombre = "Admin A", NombreNormalizado = "admin_a",
                EmpresaId = empresaA, Ambito = AmbitoAutorizacion.Empresa,
                EsSistema = false, EsAdministrador = true
            },
            new Rol
            {
                Id = 111, Nombre = "Operador A", NombreNormalizado = "operador_a",
                EmpresaId = empresaA, Ambito = AmbitoAutorizacion.Empresa,
                EsSistema = false
            },
            new Rol
            {
                Id = 220, Nombre = "Operador B", NombreNormalizado = "operador_b",
                EmpresaId = empresaB, Ambito = AmbitoAutorizacion.Empresa,
                EsSistema = false
            });
        db.Usuarios.Add(new Usuario
        {
            Id = usuarioB, NombreUsuario = "solo_empresa_b",
            NombreCompleto = "Usuario de B", PasswordHash = hashOriginal,
            RolId = 220, Activo = true
        });
        db.UsuarioEmpresas.Add(new UsuarioEmpresa(usuarioB, empresaB, 220));
        await db.SaveChangesAsync();

        var users = new Mock<IUsuarioService>(MockBehavior.Strict);
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        var current = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var audit = new Mock<IAuditoriaService>(MockBehavior.Strict);

        current.SetupGet(x => x.UsuarioId).Returns(1);
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        scope.Setup(x => x.ObtenerActualAsync(empresaA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(1, empresaA, 110, "Admin A", true));
        permisos.Setup(x => x.VerificarPermisoAsync(
                empresaA, ModuloSistema.Usuarios, AccionPermiso.Editar))
            .Returns(Task.CompletedTask);

        var sut = new UsuarioAdministracionEmpresaService(
            db, users.Object, scope.Object, permisos.Object, current.Object, audit.Object);

        var solicitud = new UpdateUsuarioDto
        {
            NombreUsuario = "solo_empresa_b",
            NombreCompleto = "Usuario de B",
            RolId = 111,
            NuevaPassword = "Clave-Sintetica-Solo-Prueba-2026!"
        };
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => sut.ActualizarAsync(usuarioB, solicitud, empresaA));

        Assert.Equal(hashOriginal, db.Usuarios.IgnoreQueryFilters()
            .Single(x => x.Id == usuarioB).PasswordHash);
        var membresia = Assert.Single(db.UsuarioEmpresas.IgnoreQueryFilters());
        Assert.Equal(empresaB, membresia.EmpresaId);
        Assert.True(membresia.Activa);
        Assert.Empty(db.UsuarioEmpresas.IgnoreQueryFilters()
            .Where(m => m.EmpresaId == empresaA));

        permisos.Verify(x => x.VerificarPermisoAsync(
            empresaA, ModuloSistema.Usuarios, AccionPermiso.Editar), Times.Once);
        permisos.Verify(x => x.VerificarPermisoAsync(
            It.IsAny<int>(), ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena),
            Times.Never);
        users.VerifyNoOtherCalls();
        audit.VerifyNoOtherCalls();
    }
}
