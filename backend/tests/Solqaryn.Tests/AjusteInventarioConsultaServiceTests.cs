using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class AjusteInventarioConsultaServiceTests
{
    [Fact]
    public async Task GetPagedAsync_DevuelveMetadatosYMapeaSnapshots()
    {
        var repository = new Mock<IAjusteInventarioRepository>();
        var filtro = new AjusteInventarioFiltroDto
        {
            Page = 2,
            PageSize = 5,
            Estado = EstadoAjusteInventario.Confirmado,
            Search = "AI-000007"
        };
        var ajuste = new AjusteInventario
        {
            Id = 7,
            NumeroAjuste = "AI-000007",
            Motivo = "Conteo físico"
        };
        var detalle = new AjusteInventarioDetalle
        {
            Id = 70,
            AjusteInventarioId = 7,
            ProductoId = 10,
            CantidadObjetivo = 8,
            NombreSnapshot = "Producto prueba"
        };
        detalle.MaterializarConfirmacion(5, 2m);
        ajuste.Detalles.Add(detalle);
        ajuste.Confirmar(99, "tester", DateTime.UtcNow);

        repository
            .Setup(x => x.GetPagedAsync(filtro))
            .ReturnsAsync((new List<AjusteInventario> { ajuste }, 11));

        var service = new AjusteInventarioConsultaService(repository.Object);
        var resultado = await service.GetPagedAsync(filtro);

        Assert.Equal(2, resultado.Page);
        Assert.Equal(5, resultado.PageSize);
        Assert.Equal(11, resultado.TotalCount);
        Assert.Equal(3, resultado.TotalPages);
        var item = Assert.Single(resultado.Items);
        Assert.Equal("Confirmado", item.Estado);
        Assert.Equal(6m, item.ImpactoCostoTotalSnapshot);
        var dtoDetalle = Assert.Single(item.Detalles);
        Assert.Equal(5, dtoDetalle.CantidadAnteriorSnapshot);
        Assert.Equal(8, dtoDetalle.CantidadNuevaSnapshot);
        Assert.Equal(3, dtoDetalle.DiferenciaSnapshot);
    }

    [Fact]
    public async Task GetPagedAsync_RangoDeFechasInvertido_FallaCerrado()
    {
        var repository = new Mock<IAjusteInventarioRepository>();
        var service = new AjusteInventarioConsultaService(repository.Object);
        var filtro = new AjusteInventarioFiltroDto
        {
            Desde = new DateTime(2026, 8, 14),
            Hasta = new DateTime(2026, 8, 13)
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.GetPagedAsync(filtro));

        Assert.Contains("fecha inicial", ex.Message, StringComparison.OrdinalIgnoreCase);
        repository.Verify(x => x.GetPagedAsync(It.IsAny<AjusteInventarioFiltroDto>()), Times.Never);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(null, 0)]
    public async Task GetPagedAsync_IdsInvalidos_FallaCerrado(int? productoId, int? varianteId)
    {
        var repository = new Mock<IAjusteInventarioRepository>();
        var service = new AjusteInventarioConsultaService(repository.Object);
        var filtro = new AjusteInventarioFiltroDto
        {
            ProductoId = productoId,
            ProductoVarianteId = varianteId
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.GetPagedAsync(filtro));
        repository.Verify(x => x.GetPagedAsync(It.IsAny<AjusteInventarioFiltroDto>()), Times.Never);
    }
}
