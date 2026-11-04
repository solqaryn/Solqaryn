using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P1-007.A: el cambio de empresa solicita nuevamente autoridad al backend;
/// una selección previa jamás es un grant reusable para otro tenant.
/// </summary>
public sealed class P1007ATenantSwitchAuthorizationTests
{
    [Fact]
    public async Task CambiarDeAABSinMembresiaRechazaB_AunqueAAntesFueValida()
    {
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        var contextoA = new UsuarioTenantScopeActual(17, 11, 3, "Operador", false);
        scope.Setup(x => x.ObtenerActualAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contextoA);
        scope.Setup(x => x.ObtenerActualAsync(22, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioTenantScopeActual?)null);
        var controller = new TenantContextController(scope.Object);

        var respuestaA = Assert.IsType<OkObjectResult>(
            await controller.Obtener(11, CancellationToken.None));
        var dataA = Assert.IsType<ApiResponse<UsuarioTenantScopeActual>>(respuestaA.Value);
        Assert.Equal(11, dataA.Data!.EmpresaId);

        Assert.IsType<ForbidResult>(
            await controller.Obtener(22, CancellationToken.None));

        scope.Verify(x => x.ObtenerActualAsync(11, It.IsAny<CancellationToken>()), Times.Once);
        scope.Verify(x => x.ObtenerActualAsync(22, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevocacionDelTenantAEntreDosSolicitudesImpideReusarAutorizacion()
    {
        var activa = true;
        var contextoA = new UsuarioTenantScopeActual(17, 11, 3, "Operador", false);
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        scope.Setup(x => x.ObtenerActualAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => activa ? contextoA : null);
        var controller = new TenantContextController(scope.Object);

        Assert.IsType<OkObjectResult>(
            await controller.Obtener(11, CancellationToken.None));
        activa = false;
        Assert.IsType<ForbidResult>(
            await controller.Obtener(11, CancellationToken.None));

        scope.Verify(x => x.ObtenerActualAsync(11, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task EmpresaNoValidaRechazadaAntesDeConsultarMembresias(int empresaId)
    {
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        var controller = new TenantContextController(scope.Object);

        Assert.IsType<BadRequestObjectResult>(
            await controller.Obtener(empresaId, CancellationToken.None));

        scope.Verify(x => x.ObtenerActualAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
