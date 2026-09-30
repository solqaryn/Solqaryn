using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class PromocionPublicaServiceTests
{
    [Fact]
    public async Task ResolverAsync_PorcentajeVigenteDeProducto_CalculaPrecioYAhorro()
    {
        var ahora = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);
        var repo = new Mock<IDescuentoRepository>();
        repo.Setup(x => x.GetVigentesConRelacionesAsync(ahora)).ReturnsAsync(new List<Descuento>
        {
            new()
            {
                Id = 1, Nombre = "20% web", Activo = true, Tipo = TipoDescuento.Porcentaje,
                Valor = 20, Prioridad = 1, FechaInicio = ahora.AddDays(-1), FechaFin = ahora.AddDays(1),
                Productos = new List<DescuentoProducto> { new() { ProductoId = 10 } }
            }
        });

        var oferta = await new PromocionPublicaService(repo.Object).ResolverAsync(10, 2, 1000m, ahora);

        Assert.NotNull(oferta);
        Assert.Equal(1000m, oferta!.PrecioNormal);
        Assert.Equal(800m, oferta.PrecioOferta);
        Assert.Equal(200m, oferta.Ahorro);
        Assert.Equal(20m, oferta.PorcentajeAhorro);
        Assert.Equal("20% web", oferta.Nombre);
    }

    [Fact]
    public async Task ResolverAsync_DescuentoVencidoAunqueRepositorioLoDevuelva_NoSePublica()
    {
        var ahora = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);
        var repo = new Mock<IDescuentoRepository>();
        repo.Setup(x => x.GetVigentesConRelacionesAsync(ahora)).ReturnsAsync(new List<Descuento>
        {
            new()
            {
                Id = 2, Nombre = "Vencida", Activo = true, Tipo = TipoDescuento.Porcentaje,
                Valor = 50, FechaFin = ahora.AddSeconds(-1)
            }
        });

        var oferta = await new PromocionPublicaService(repo.Object).ResolverAsync(10, null, 1000m, ahora);

        Assert.Null(oferta);
    }

    [Fact]
    public async Task ResolverAsync_ReglasCondicionadas_NoSeConviertenEnPrecioPublico()
    {
        var ahora = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);
        var repo = new Mock<IDescuentoRepository>();
        repo.Setup(x => x.GetVigentesConRelacionesAsync(ahora)).ReturnsAsync(new List<Descuento>
        {
            new() { Id = 3, Nombre = "Codigo", Activo = true, Tipo = TipoDescuento.Porcentaje, Valor = 10, CodigoPromocionalNormalizado = "WEB10" },
            new() { Id = 4, Nombre = "Dos unidades", Activo = true, Tipo = TipoDescuento.Porcentaje, Valor = 10, CantidadMinima = 2 },
            new() { Id = 5, Nombre = "Monto fijo", Activo = true, Tipo = TipoDescuento.MontoFijo, Valor = 100 }
        });

        var oferta = await new PromocionPublicaService(repo.Object).ResolverAsync(10, null, 1000m, ahora);

        Assert.Null(oferta);
    }

    [Fact]
    public async Task ResolverAsync_LimiteTotalAgotado_NoSePublica()
    {
        var ahora = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);
        var repo = new Mock<IDescuentoRepository>();
        repo.Setup(x => x.GetVigentesConRelacionesAsync(ahora)).ReturnsAsync(new List<Descuento>
        {
            new() { Id = 6, Nombre = "Cupo", Activo = true, Tipo = TipoDescuento.Porcentaje, Valor = 15, LimiteTotalUsos = 3 }
        });
        repo.Setup(x => x.ContarUsosAsync(6)).ReturnsAsync(3);

        var oferta = await new PromocionPublicaService(repo.Object).ResolverAsync(10, null, 1000m, ahora);

        Assert.Null(oferta);
    }
}
