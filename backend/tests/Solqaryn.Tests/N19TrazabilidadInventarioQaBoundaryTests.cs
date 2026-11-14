using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N19TrazabilidadInventarioQaBoundaryTests
{
    [Fact]
    public void Lote_sin_vencimiento_nunca_se_reporta_vencido_ni_en_alerta()
    {
        var lote = new LoteInventario { ProductoVarianteId = 11 };
        lote.ConfigurarIdentidad("L-SIN-VENC", null, null, false);
        var hoy = new DateTime(2026, 8, 17);

        Assert.False(lote.EstaVencido(hoy));
        Assert.False(lote.VenceDentroDe(hoy, 0));
        Assert.False(lote.VenceDentroDe(hoy, 3650));
    }

    [Fact]
    public void Lote_permite_fabricacion_y_vencimiento_el_mismo_dia()
    {
        var fecha = new DateTime(2026, 8, 17, 18, 30, 0, DateTimeKind.Utc);
        var lote = new LoteInventario { ProductoVarianteId = 11 };

        lote.ConfigurarIdentidad("L-MISMO-DIA", fecha, fecha, true);

        Assert.Equal(fecha.Date, lote.FechaFabricacion);
        Assert.Equal(fecha.Date, lote.FechaVencimiento);
        Assert.False(lote.EstaVencido(fecha));
        Assert.True(lote.VenceDentroDe(fecha, 0));
    }

    [Fact]
    public void Serie_vendida_rechaza_recepcion_y_preserva_estado_terminal()
    {
        var serie = new SerieInventario { ProductoVarianteId = 11 };
        serie.ConfigurarIdentidad("SN-VENDIDA");
        serie.Vender();

        Assert.Throws<InvalidOperationException>(() => serie.RecibirDeTransito());

        Assert.Equal(EstadoSerieInventario.Vendida, serie.Estado);
    }

    [Fact]
    public void Serie_reservada_puede_venderse_sin_liberacion_intermedia()
    {
        var serie = new SerieInventario { ProductoVarianteId = 11 };
        serie.ConfigurarIdentidad("SN-RESERVADA");
        serie.Reservar();

        serie.Vender();

        Assert.Equal(EstadoSerieInventario.Vendida, serie.Estado);
        Assert.Throws<InvalidOperationException>(() => serie.LiberarReserva());
        Assert.Equal(EstadoSerieInventario.Vendida, serie.Estado);
    }

    [Fact]
    public void Desactivar_trazabilidad_limpia_alerta_de_vencimiento()
    {
        var variante = new ProductoVariante();
        variante.ConfigurarTrazabilidad(true, true, true, 30);

        variante.ConfigurarTrazabilidad(false, false, false);

        Assert.False(variante.RequiereTrazabilidad);
        Assert.False(variante.ControlaLote);
        Assert.False(variante.ControlaNumeroSerie);
        Assert.False(variante.ControlaFechaVencimiento);
        Assert.Null(variante.DiasAlertaVencimiento);
    }
}
