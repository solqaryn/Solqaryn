using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class N19SerieInventarioLifecycleRegressionTests
{
    [Fact]
    public void Serie_reservada_puede_venderse_y_no_liberarse_despues()
    {
        var serie = new SerieInventario { ProductoVarianteId = 11 };
        serie.ConfigurarIdentidad("SN-VENTA-RESERVADA");
        serie.Reservar();

        serie.Vender();

        Assert.Equal(EstadoSerieInventario.Vendida, serie.Estado);
        Assert.Throws<InvalidOperationException>(() => serie.LiberarReserva());
        Assert.Equal(EstadoSerieInventario.Vendida, serie.Estado);
    }

    [Fact]
    public void Serie_en_transito_rechaza_venta_y_conserva_estado()
    {
        var serie = new SerieInventario { ProductoVarianteId = 11 };
        serie.ConfigurarIdentidad("SN-TRANSITO-VENTA");
        serie.MarcarEnTransito();

        Assert.Throws<InvalidOperationException>(() => serie.Vender());

        Assert.Equal(EstadoSerieInventario.EnTransito, serie.Estado);
    }

    [Fact]
    public void Dar_de_baja_repetido_es_fail_closed_y_no_muta_estado_terminal()
    {
        var serie = new SerieInventario { ProductoVarianteId = 11 };
        serie.ConfigurarIdentidad("SN-BAJA-IDEMPOTENTE");
        serie.DarDeBaja();

        Assert.Equal(EstadoSerieInventario.Baja, serie.Estado);
        Assert.Throws<InvalidOperationException>(() => serie.DarDeBaja());
        Assert.Equal(EstadoSerieInventario.Baja, serie.Estado);
    }

    [Fact]
    public void Vinculo_de_lote_invalido_no_reemplaza_lote_valido_preexistente()
    {
        var serie = new SerieInventario { ProductoVarianteId = 11 };
        serie.ConfigurarIdentidad("SN-LOTE-ATOMICIDAD");
        var loteValido = new LoteInventario { Id = 8, ProductoVarianteId = 11 };
        loteValido.ConfigurarIdentidad("L-11", null, null, false);
        serie.VincularLote(loteValido);

        var loteAjeno = new LoteInventario { Id = 9, ProductoVarianteId = 12 };
        loteAjeno.ConfigurarIdentidad("L-12", null, null, false);

        Assert.Throws<InvalidOperationException>(() => serie.VincularLote(loteAjeno));
        Assert.Equal(8, serie.LoteInventarioId);
        Assert.Same(loteValido, serie.LoteInventario);
    }

    [Fact]
    public void Identidad_invalida_no_reemplaza_numero_serie_valido_preexistente()
    {
        var serie = new SerieInventario { ProductoVarianteId = 11 };
        serie.ConfigurarIdentidad("SN-VALIDA");

        Assert.Throws<InvalidOperationException>(() => serie.ConfigurarIdentidad(new string('X', 121)));

        Assert.Equal("SN-VALIDA", serie.NumeroSerie);
    }
}
