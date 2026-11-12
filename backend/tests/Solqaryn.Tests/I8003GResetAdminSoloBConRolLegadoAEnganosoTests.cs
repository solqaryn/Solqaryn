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
/// I8-003.G: prueba negativa sobre el servicio REAL de administración empresarial.
/// El RolId global legado parece pertenecer a A, pero la única membresía real
/// está en B. Un admin de A jamás puede restablecer esa clave, ni siquiera
/// si la membresía de B está inactiva. EF InMemory, identidades sintéticas.
/// </summary>
public sealed class I8003GResetAdminSoloBConRolLegadoAEnganosoTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AdminA_NoRestableceClaveDeUsuarioSoloB_AunqueRolLegadoDigaA(
        bool membresiaBActiva)
    {
        const int empresaA = 11;
        const int empresaB = 22;
        const int usuarioB = 71;
        const string hashOriginal = "hash-sintetico-b-inmutable";
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("i8003g-" + Guid.NewGuid().ToString("N")).Options);

        db.Roles.AddRange(
            new Rol
            {
                Id = 110, Nombre = "Admin A", NombreNormalizado = "admin_a",
                EmpresaId = empresaA, Ambito = AmbitoAutorizacion.Empresa,
                EsAdministrador = true, EsSistema = false
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
            NombreCompleto = "Cuenta sintetica de B",
            // Control adversarial distinto a I8-003.E:
            // el rol global heredado parece de A; la membresía real es solo B.
            RolId = 111, PasswordHash = hashOriginal, Activo = true
        });
        var membresiaB = new UsuarioEmpresa(usuarioB, empresaB, 220);
        if (!membresiaBActiva) membresiaB.Desactivar();
        db.UsuarioEmpresas.Add(membresiaB);
        await db.SaveChangesAsync();

        var usuarios = new Mock<IUsuarioService>(MockBehavior.Strict);
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        var current = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);

        current.SetupGet(c => c.UsuarioId).Returns(1);
        current.SetupGet(c => c.EstaAutenticado).Returns(true);
        scope.Setup(s => s.ObtenerActualAsync(empresaA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(1, empresaA, 110, "Admin A", true));
        permisos.Setup(p => p.VerificarPermisoAsync(
                empresaA, ModuloSistema.Usuarios, AccionPermiso.Editar))
            .Returns(Task.CompletedTask);

        var sut = new UsuarioAdministracionEmpresaService(
            db, usuarios.Object, scope.Object, permisos.Object,
            current.Object, auditoria.Object);

        var solicitud = new UpdateUsuarioDto
        {
            NombreUsuario = "solo_empresa_b",
            NombreCompleto = "Cuenta sintetica de B",
            RolId = 111,
            NuevaPassword = "Clave-Sintetica-No-Real-2026!"
        };

        var error = await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => sut.ActualizarAsync(usuarioB, solicitud, empresaA));
        Assert.DoesNotContain("solo_empresa_b", error.Message, StringComparison.OrdinalIgnoreCase);

        var posterior = db.Usuarios.IgnoreQueryFilters().Single(u => u.Id == usuarioB);
        Assert.Equal(hashOriginal, posterior.PasswordHash);
        Assert.Equal(111, posterior.RolId);
        Assert.Equal(empresaB, db.UsuarioEmpresas.IgnoreQueryFilters().Single().EmpresaId);
        Assert.Equal(membresiaBActiva,
            db.UsuarioEmpresas.IgnoreQueryFilters().Single().Activa);
        Assert.Empty(db.UsuarioEmpresas.IgnoreQueryFilters()
            .Where(m => m.EmpresaId == empresaA));
        permisos.Verify(p => p.VerificarPermisoAsync(
            empresaA, ModuloSistema.Usuarios, AccionPermiso.Editar), Times.Once);
        permisos.Verify(p => p.VerificarPermisoAsync(
            It.IsAny<int>(), ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena),
            Times.Never);
        permisos.VerifyNoOtherCalls();
        usuarios.VerifyNoOtherCalls();
        auditoria.VerifyNoOtherCalls();
    }
}
