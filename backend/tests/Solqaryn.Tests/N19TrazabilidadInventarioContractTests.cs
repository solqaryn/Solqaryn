using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class N19TrazabilidadInventarioContractTests
{
    [Fact]
    public void Configuracion_expone_controles_independientes_y_alerta_nullable()
    {
        var dto = new ConfiguracionTrazabilidadVarianteDto
        {
            ProductoVarianteId = 11,
            ControlaLote = true,
            ControlaNumeroSerie = true,
            ControlaFechaVencimiento = true,
            DiasAlertaVencimiento = 30
        };

        Assert.Equal(11, dto.ProductoVarianteId);
        Assert.True(dto.ControlaLote);
        Assert.True(dto.ControlaNumeroSerie);
        Assert.True(dto.ControlaFechaVencimiento);
        Assert.Equal(30, dto.DiasAlertaVencimiento);
    }

    [Fact]
    public void Lote_y_serie_conservan_identidad_de_variante_y_relacion_opcional()
    {
        var lote = new LoteInventarioDto
        {
            Id = 7,
            ProductoVarianteId = 11,
            Codigo = "LOTE-001",
            FechaVencimiento = new DateTime(2027, 8, 17),
            Activo = true
        };
        var serie = new SerieInventarioDto
        {
            Id = 17,
            ProductoVarianteId = 11,
            LoteInventarioId = lote.Id,
            NumeroSerie = "SN-001",
            Estado = EstadoSerieInventario.Disponible
        };

        Assert.Equal(lote.ProductoVarianteId, serie.ProductoVarianteId);
        Assert.Equal(lote.Id, serie.LoteInventarioId);
        Assert.Equal(EstadoSerieInventario.Disponible, serie.Estado);
    }

    [Fact]
    public void Contratos_permiten_variante_sin_lote_serie_ni_vencimiento()
    {
        var request = new ConfigurarTrazabilidadVarianteRequest();

        Assert.False(request.ControlaLote);
        Assert.False(request.ControlaNumeroSerie);
        Assert.False(request.ControlaFechaVencimiento);
        Assert.Null(request.DiasAlertaVencimiento);
    }
}
