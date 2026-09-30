using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N21SolicitudCompraRbacRegressionTests
{
    [Fact]
    public async Task Administrador_sin_grant_explicito_no_puede_aprobar_solicitud()
    {
        var rolPermisos = new Mock<IRolPermisoRepository>(MockBehavior.Strict);
        rolPermisos
            .Setup(repo => repo.TienePermisoPorRolIdAsync(99, ModuloSistema.Compras, AccionPermiso.Aprobar))
            .ReturnsAsync(false);

        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        scope.Setup(x => x.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(7, 99, "Administrador", EsAdministrador: true));

        var service = CrearServicio(rolPermisos.Object, scope.Object);

        Assert.False(await service.TienePermisoAsync(ModuloSistema.Compras, AccionPermiso.Aprobar));
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.VerificarPermisoAsync(ModuloSistema.Compras, AccionPermiso.Aprobar));

        rolPermisos.Verify(
            repo => repo.TienePermisoPorRolIdAsync(99, ModuloSistema.Compras, AccionPermiso.Aprobar),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Grant_relacional_explicito_concede_permiso_compras()
    {
        var rolPermisos = new Mock<IRolPermisoRepository>(MockBehavior.Strict);
        rolPermisos
            .Setup(repo => repo.TienePermisoPorRolIdAsync(5, ModuloSistema.Compras, AccionPermiso.Confirmar))
            .ReturnsAsync(true);

        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        scope.Setup(x => x.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(11, 5, "Comprador", EsAdministrador: false));

        var service = CrearServicio(rolPermisos.Object, scope.Object);

        Assert.True(await service.TienePermisoAsync(ModuloSistema.Compras, AccionPermiso.Confirmar));
        await service.VerificarPermisoAsync(ModuloSistema.Compras, AccionPermiso.Confirmar);

        rolPermisos.Verify(
            repo => repo.TienePermisoPorRolIdAsync(5, ModuloSistema.Compras, AccionPermiso.Confirmar),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Scope_no_resuelto_falla_cerrado_sin_consultar_grants()
    {
        var rolPermisos = new Mock<IRolPermisoRepository>(MockBehavior.Strict);
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        scope.Setup(x => x.ObtenerActualAsync()).ReturnsAsync((UsuarioScopeActual?)null);

        var service = CrearServicio(rolPermisos.Object, scope.Object);

        Assert.False(await service.TienePermisoAsync(ModuloSistema.Compras, AccionPermiso.Ver));
        rolPermisos.VerifyNoOtherCalls();
    }

    private static PermisoService CrearServicio(
        IRolPermisoRepository rolPermisos,
        IUsuarioScopeService scope) =>
        new(
            rolPermisos,
            Mock.Of<IRolRepository>(),
            Mock.Of<IPermisoRepository>(),
            Mock.Of<IAuditoriaService>(),
            Mock.Of<ICurrentUserService>(),
            scope);
}
