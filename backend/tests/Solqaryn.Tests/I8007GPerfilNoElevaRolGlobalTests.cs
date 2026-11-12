using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
/// I8-007.G: negative contract for commercial profile editing. The tenant
/// administrator must not assign a platform role or mutate security flags.
/// Synthetic EF InMemory and strict mocks, not an HTTP/JWT/MySQL E2E test.
/// </summary>
public sealed class I8007GPerfilNoElevaRolGlobalTests
{
    private const int EmpresaA = 11;
    private const int Actor = 1;
    private const int Usuario = 75;
    private const int RolLocal = 112;
    private const int RolPlataforma = 902;
    private const string HashOriginal = "I8007G-HASH-SINTETICO-NO-SECRETO";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EdicionComercial_NoAsignaRolGlobalNiRestableceClave(bool incluyePassword)
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("i8007g-" + Guid.NewGuid().ToString("N")).Options);
        db.Roles.AddRange(
            new Rol { Id = 101, Nombre = "Admin A", NombreNormalizado = "I8007G_ADMIN",
                EmpresaId = EmpresaA, Ambito = AmbitoAutorizacion.Empresa,
                EsAdministrador = true, Activo = true },
            new Rol { Id = RolLocal, Nombre = "Usuario A", NombreNormalizado = "I8007G_LOCAL",
                EmpresaId = EmpresaA, Ambito = AmbitoAutorizacion.Empresa, Activo = true },
            new Rol { Id = RolPlataforma, Nombre = "Plataforma",
                NombreNormalizado = "I8007G_PLATAFORMA", Ambito = AmbitoAutorizacion.Plataforma,
                EmpresaId = null, EsAdministrador = true, EsSistema = true, Activo = true });
        db.Usuarios.Add(new Usuario {
            Id = Usuario, NombreUsuario = "i8007g-empresa-a",
            NombreCompleto = "Perfil original", PasswordHash = HashOriginal,
            RolId = RolLocal, Activo = true, Bloqueado = false });
        db.UsuarioEmpresas.Add(new UsuarioEmpresa(Usuario, EmpresaA, RolLocal));
        await db.SaveChangesAsync();

        var usuarios = new Mock<IUsuarioService>(MockBehavior.Strict);
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        var actual = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        actual.SetupGet(x => x.UsuarioId).Returns(Actor);
        actual.SetupGet(x => x.EstaAutenticado).Returns(true);
        scope.Setup(x => x.ObtenerActualAsync(EmpresaA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(Actor, EmpresaA, 101, "Admin A", true));
        permisos.Setup(x => x.VerificarPermisoAsync(
            EmpresaA, ModuloSistema.Usuarios, AccionPermiso.Editar))
            .Returns(Task.CompletedTask);

        var servicio = new UsuarioAdministracionEmpresaService(
            db, usuarios.Object, scope.Object, permisos.Object, actual.Object, auditoria.Object);
        var dto = new UpdateUsuarioDto {
            NombreUsuario = "i8007g-empresa-a", NombreCompleto = "Escalada intentada",
            RolId = RolPlataforma,
            NuevaPassword = incluyePassword ? "CLAVE-FICTICIA-NO-USAR-2026" : null };

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            servicio.ActualizarAsync(Usuario, dto, EmpresaA));

        var user = db.Usuarios.IgnoreQueryFilters().Single(x => x.Id == Usuario);
        Assert.Equal("Perfil original", user.NombreCompleto);
        Assert.Equal(RolLocal, user.RolId);
        Assert.Equal(HashOriginal, user.PasswordHash);
        Assert.True(user.Activo);
        Assert.False(user.Bloqueado);
        Assert.False(user.Eliminado);
        var membership = Assert.Single(db.UsuarioEmpresas.IgnoreQueryFilters());
        Assert.Equal(EmpresaA, membership.EmpresaId);
        Assert.Equal(RolLocal, membership.RolId);
        Assert.True(membership.Activa);
        permisos.Verify(x => x.VerificarPermisoAsync(
            EmpresaA, ModuloSistema.Usuarios, AccionPermiso.Editar), Times.Once);
        permisos.Verify(x => x.VerificarPermisoAsync(
            It.IsAny<int>(), ModuloSistema.Usuarios, AccionPermiso.AsignarRol), Times.Never);
        permisos.Verify(x => x.VerificarPermisoAsync(
            It.IsAny<int>(), ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena), Times.Never);
        usuarios.VerifyNoOtherCalls();
        auditoria.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("Activo")]
    [InlineData("Bloqueado")]
    [InlineData("Eliminado")]
    [InlineData("EsAdministrador")]
    [InlineData("EmpresaId")]
    public void DtoPerfil_NoExponeCamposDeSeguridadParaMassAssignment(string propiedad)
    {
        Assert.Null(typeof(UpdateUsuarioDto).GetProperty(propiedad));
    }
}
