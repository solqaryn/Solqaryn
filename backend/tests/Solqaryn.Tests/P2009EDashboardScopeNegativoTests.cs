using System.Threading.Tasks;
using Moq;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-009.E: rechazar reportes y KPI cuando no hay un usuario con alcance
/// autenticado resoluble. Es una prueba unitaria de servicio, no HTTP/JWT,
/// RBAC de endpoint, filtros MySQL ni aislamiento de los datos de un tenant.
/// </summary>
public sealed class P2009EDashboardScopeNegativoTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Sin_alcance_de_usuario_ni_resumen_ni_reporte_consultan_repositorios(
        bool reporteVariantes)
    {
        var productos = new Mock<IProductoRepository>(MockBehavior.Strict);
        var variantes = new Mock<IProductoVarianteRepository>(MockBehavior.Strict);
        var compras = new Mock<ICompraRepository>(MockBehavior.Strict);
        var ventas = new Mock<IVentaRepository>(MockBehavior.Strict);
        var revisiones = new Mock<IRevisionFinancieraRepository>(MockBehavior.Strict);
        var movimientos = new Mock<IMovimientoFinancieroRepository>(MockBehavior.Strict);
        var alcance = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        alcance.Setup(x => x.ObtenerActualAsync())
            .ReturnsAsync((UsuarioScopeActual?)null);

        var servicio = new DashboardService(
            productos.Object,
            variantes.Object,
            compras.Object,
            ventas.Object,
            revisiones.Object,
            movimientos.Object,
            alcance.Object);

        if (reporteVariantes)
        {
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => servicio.GetInventarioVariantesAsync());
        }
        else
        {
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => servicio.GetResumenAsync());
        }

        alcance.Verify(x => x.ObtenerActualAsync(), Times.Once);
        alcance.VerifyNoOtherCalls();
        productos.VerifyNoOtherCalls();
        variantes.VerifyNoOtherCalls();
        compras.VerifyNoOtherCalls();
        ventas.VerifyNoOtherCalls();
        revisiones.VerifyNoOtherCalls();
        movimientos.VerifyNoOtherCalls();
    }
}
