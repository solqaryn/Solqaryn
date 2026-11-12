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
/// I8-003.F: fixture negativa preparada para el reset empresarial.
/// Una membresia existente pero INACTIVA no permite cambiar la clave.
/// EF InMemory y mocks; no representa SMTP, enlace de un uso, HTTP/JWT ni MySQL.
/// </summary>
public sealed class I8003FResetAdminMembresiaInactivaTests
{
    [Fact]
    public async Task AdminA_NoReseteaClaveDeUsuarioConMembresiaAInactiva()
    {
        const int empresaA = 11;
        const int usuarioObjetivo = 71;
        const string hashSinCambios = "hash-sintetico-inmutable-tenant-a";

        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("i8003f-" + Guid.NewGuid().ToString("N")).Options);

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
            });
        db.Usuarios.Add(new Usuario
        {
            Id = usuarioObjetivo, NombreUsuario = "objetivo_inactivo_a",
            NombreCompleto = "Usuario sintetico A", PasswordHash = hashSinCambios,
            RolId = 111, Activo = true
        });
        var membresia = new UsuarioEmpresa(usuarioObjetivo, empresaA, 111);
        membresia.Desactivar();
        db.UsuarioEmpresas.Add(membresia);
        await db.SaveChangesAsync();

        var users = new Mock<IUsuarioService>(MockBehavior.Strict);
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        var actual = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var audit = new Mock<IAuditoriaService>(MockBehavior.Strict);

        actual.SetupGet(x => x.UsuarioId).Returns(1);
        actual.SetupGet(x => x.EstaAutenticado).Returns(true);
        scope.Setup(x => x.ObtenerActualAsync(empresaA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(1, empresaA, 110, "Admin A", true));
        permisos.Setup(x => x.VerificarPermisoAsync(
                empresaA, ModuloSistema.Usuarios, AccionPermiso.Editar))
            .Returns(Task.CompletedTask);

        var sut = new UsuarioAdministracionEmpresaService(
            db, users.Object, scope.Object, permisos.Object, actual.Object, audit.Object);
        var solicitud = new UpdateUsuarioDto
        {
            NombreUsuario = "objetivo_inactivo_a",
            NombreCompleto = "Usuario sintetico A",
            RolId = 111,
            NuevaPassword = "Clave-Sintetica-Solo-Prueba-2026!"
        };

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => sut.ActualizarAsync(usuarioObjetivo, solicitud, empresaA));

        Assert.Equal(hashSinCambios,
            db.Usuarios.IgnoreQueryFilters().Single(x => x.Id == usuarioObjetivo).PasswordHash);
        Assert.False(db.UsuarioEmpresas.IgnoreQueryFilters()
            .Single(x => x.UsuarioId == usuarioObjetivo && x.EmpresaId == empresaA).Activa);
        users.VerifyNoOtherCalls();
        audit.VerifyNoOtherCalls();
        permisos.Verify(x => x.VerificarPermisoAsync(
            empresaA, ModuloSistema.Usuarios, AccionPermiso.Editar), Times.Once);
        permisos.Verify(x => x.VerificarPermisoAsync(
            It.IsAny<int>(), ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena),
            Times.Never);
        permisos.VerifyNoOtherCalls();
    }
}
