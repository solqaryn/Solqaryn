using Moq;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-008.A: cierre del limite entre autoridad Plataforma legacy y membresia
/// tenant verificada. Complementa los recibos I8-008.C/D/E; no valida HTTP,
/// JWT, middleware, base de datos ni integracion end-to-end.
/// </summary>
public sealed class I8008ACierreAmbitoPlataformaContextoTenantTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RolGlobalLegacy_SinContextoTenant_NoConcedeNiEnumeraPermisos(
        bool esAdministrador)
    {
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        scope.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(25, 900, "Plataforma", esAdministrador));
        scope.Setup(s => s.ObtenerActualAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioTenantScopeActual?)null);

        var matriz = new Mock<IRolPermisoRepository>(MockBehavior.Strict);
        var servicio = CrearServicio(matriz.Object, scope.Object);

        Assert.False(await servicio.TienePermisoAsync(
            11, ModuloSistema.Usuarios, AccionPermiso.Ver));
        var efectivos = await servicio.GetMisPermisosAsync(11);
        Assert.Empty(efectivos.Permisos);
        Assert.False(efectivos.EsAdministrador);

        scope.Verify(s => s.ObtenerActualAsync(), Times.Never);
        scope.Verify(s => s.ObtenerActualAsync(
            11, It.IsAny<CancellationToken>()), Times.Exactly(2));
        matriz.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SelectorTenant_SoloUsaRolDeMembresiaAutorizada()
    {
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        scope.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(25, 900, "Plataforma", true));
        scope.Setup(s => s.ObtenerActualAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(
                25, 11, 78, "Operador empresa 11", false));
        scope.Setup(s => s.ObtenerActualAsync(22, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioTenantScopeActual?)null);
        var matriz = new Mock<IRolPermisoRepository>(MockBehavior.Strict);
        matriz.Setup(m => m.TienePermisoPorRolIdAsync(
                78, ModuloSistema.Usuarios, AccionPermiso.Ver))
            .ReturnsAsync(true);

        var servicio = CrearServicio(matriz.Object, scope.Object);
        Assert.True(await servicio.TienePermisoAsync(
            11, ModuloSistema.Usuarios, AccionPermiso.Ver));
        Assert.False(await servicio.TienePermisoAsync(
            22, ModuloSistema.Usuarios, AccionPermiso.Ver));
        Assert.False(await servicio.TienePermisoAsync(
            0, ModuloSistema.Usuarios, AccionPermiso.Ver));

        matriz.Verify(m => m.TienePermisoPorRolIdAsync(
            78, ModuloSistema.Usuarios, AccionPermiso.Ver), Times.Once);
        matriz.VerifyNoOtherCalls();
        scope.Verify(s => s.ObtenerActualAsync(), Times.Never);
        scope.Verify(s => s.ObtenerActualAsync(
            22, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static PermisoService CrearServicio(
        IRolPermisoRepository matriz, IUsuarioScopeService scope) =>
        new(
            matriz,
            Mock.Of<IRolRepository>(),
            Mock.Of<IPermisoRepository>(),
            Mock.Of<IAuditoriaService>(),
            Mock.Of<ICurrentUserService>(),
            scope);
}
