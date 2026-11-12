using System.Threading;
using Moq;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-008.H: regresion positiva de permiso por empresa verificada.
/// El servicio real debe usar exclusivamente el rol devuelto por el scope
/// tenant-aware, incluso cuando el usuario sea administrador empresarial.
/// Prueba con mocks: no certifica JWT/HTTP ni membresias/MySQL reales.
/// </summary>
public sealed class I8008HPermisoPositivoTenantScopeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PermisoDeEmpresaA_SeConcedeSoloConScopeDeA_SinBypassHaciaB(
        bool esAdministrador)
    {
        const int empresaA = 11;
        const int empresaB = 22;
        const int rolDeA = 78;
        var scopes = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        scopes.Setup(s => s.ObtenerActualAsync(
                empresaA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(
                90, empresaA, rolDeA, "Operador A", esAdministrador));
        // El actor no posee una membresia verificada para B.
        scopes.Setup(s => s.ObtenerActualAsync(
                empresaB, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioTenantScopeActual?)null);

        var matriz = new Mock<IRolPermisoRepository>(MockBehavior.Strict);
        matriz.Setup(r => r.TienePermisoPorRolIdAsync(
                rolDeA, ModuloSistema.Usuarios, AccionPermiso.Ver))
            .ReturnsAsync(true);

        var servicio = new PermisoService(
            matriz.Object,
            Mock.Of<IRolRepository>(),
            Mock.Of<IPermisoRepository>(),
            Mock.Of<IAuditoriaService>(),
            Mock.Of<ICurrentUserService>(),
            scopes.Object);

        // Positivo: el permiso explicitamente persistido del rol de A se respeta.
        Assert.True(await servicio.TienePermisoAsync(
            empresaA, ModuloSistema.Usuarios, AccionPermiso.Ver));

        // Negativos: ni el rol de A ni EsAdministrador autorizan B o empresa 0.
        Assert.False(await servicio.TienePermisoAsync(
            empresaB, ModuloSistema.Usuarios, AccionPermiso.Ver));
        Assert.False(await servicio.TienePermisoAsync(
            0, ModuloSistema.Usuarios, AccionPermiso.Ver));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            servicio.VerificarPermisoAsync(
                empresaB, ModuloSistema.Usuarios, AccionPermiso.Ver));

        // No consultar permisos relacionales para empresa B: fail-closed
        // antes de llegar al repositorio de grants.
        matriz.Verify(r => r.TienePermisoPorRolIdAsync(
            rolDeA, ModuloSistema.Usuarios, AccionPermiso.Ver),
            Times.Once);
        matriz.VerifyNoOtherCalls();
        scopes.Verify(s => s.ObtenerActualAsync(
            empresaA, It.IsAny<CancellationToken>()), Times.Once);
        scopes.Verify(s => s.ObtenerActualAsync(
            empresaB, It.IsAny<CancellationToken>()), Times.Exactly(2));
        scopes.VerifyNoOtherCalls();
    }
}
