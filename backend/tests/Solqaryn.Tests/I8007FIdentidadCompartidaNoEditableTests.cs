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
/// I8-007.F: caso sintetico de identidad compartida A/B, incluida membresia
/// inactiva en B. No acredita HTTP/JWT, MySQL real ni edicion productiva.
/// </summary>
public sealed class I8007FIdentidadCompartidaNoEditableTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AdminA_NoEditaUsuarioCompartidoAunqueBInactiva(bool bActiva)
    {
        const int empresaA = 11, empresaB = 22, usuarioId = 74, rolA = 111, rolB = 222;
        const string hashOriginal = "HASH-SINTETICO-I8007F-NO-ES-SECRETO";
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("i8007f-" + Guid.NewGuid().ToString("N")).Options);
        db.Roles.AddRange(
            new Rol { Id = 101, Nombre = "Admin A", NombreNormalizado = "I8007F_ADMIN_A",
                EmpresaId = empresaA, Ambito = AmbitoAutorizacion.Empresa, EsAdministrador = true, Activo = true },
            new Rol { Id = rolA, Nombre = "Usuario A", NombreNormalizado = "I8007F_USER_A",
                EmpresaId = empresaA, Ambito = AmbitoAutorizacion.Empresa, Activo = true },
            new Rol { Id = rolB, Nombre = "Usuario B", NombreNormalizado = "I8007F_USER_B",
                EmpresaId = empresaB, Ambito = AmbitoAutorizacion.Empresa, Activo = true });
        db.Usuarios.Add(new Usuario {
            Id = usuarioId, NombreUsuario = "i8007f-compartida", NombreCompleto = "Identidad sintetica AB",
            PasswordHash = hashOriginal, RolId = rolA, Activo = true });
        var miembroA = new UsuarioEmpresa(usuarioId, empresaA, rolA);
        var miembroB = new UsuarioEmpresa(usuarioId, empresaB, rolB);
        if (!bActiva) miembroB.Desactivar();
        db.UsuarioEmpresas.AddRange(miembroA, miembroB);
        await db.SaveChangesAsync();

        var usuarios = new Mock<IUsuarioService>(MockBehavior.Strict);
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        var actual = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        actual.SetupGet(x => x.UsuarioId).Returns(1);
        actual.SetupGet(x => x.EstaAutenticado).Returns(true);
        scope.Setup(x => x.ObtenerActualAsync(empresaA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(1, empresaA, 101, "Admin A", true));
        permisos.Setup(x => x.VerificarPermisoAsync(
            empresaA, ModuloSistema.Usuarios, AccionPermiso.Editar))
            .Returns(Task.CompletedTask);

        var servicio = new UsuarioAdministracionEmpresaService(
            db, usuarios.Object, scope.Object, permisos.Object, actual.Object, auditoria.Object);
        var solicitud = new UpdateUsuarioDto {
            NombreUsuario = "i8007f-compartida",
            NombreCompleto = "Intento de modificar perfil compartido",
            RolId = rolA,
            NuevaPassword = "I8007F-Credencial-Sintetica-2026" };
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            servicio.ActualizarAsync(usuarioId, solicitud, empresaA));

        var entidad = db.Usuarios.IgnoreQueryFilters().Single(x => x.Id == usuarioId);
        Assert.Equal(hashOriginal, entidad.PasswordHash);
        Assert.Equal("Identidad sintetica AB", entidad.NombreCompleto);
        Assert.Equal(rolA, entidad.RolId);
        var membresias = db.UsuarioEmpresas.IgnoreQueryFilters().ToList();
        Assert.Equal(2, membresias.Count);
        Assert.True(membresias.Single(x => x.EmpresaId == empresaA).Activa);
        Assert.Equal(bActiva, membresias.Single(x => x.EmpresaId == empresaB).Activa);
        permisos.Verify(p => p.VerificarPermisoAsync(
            empresaA, ModuloSistema.Usuarios, AccionPermiso.Editar), Times.Once);
        permisos.Verify(p => p.VerificarPermisoAsync(
            It.IsAny<int>(), ModuloSistema.Usuarios, AccionPermiso.AsignarRol), Times.Never);
        permisos.Verify(p => p.VerificarPermisoAsync(
            It.IsAny<int>(), ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena), Times.Never);
        usuarios.VerifyNoOtherCalls();
        auditoria.VerifyNoOtherCalls();
    }
}
