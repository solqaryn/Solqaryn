using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class N19TrazabilidadInventarioDomainTests
{
    [Fact]
    public void Variante_sin_configuracion_permanece_sin_trazabilidad()
    {
        var variante = new ProductoVariante();
        Assert.False(variante.RequiereTrazabilidad);
    }

    [Fact]
    public void Vencimiento_requiere_lote_y_alerta_no_negativa()
    {
        var variante = new ProductoVariante();

        Assert.Throws<InvalidOperationException>(() => variante.ConfigurarTrazabilidad(false, false, true, 15));
        Assert.Throws<ArgumentOutOfRangeException>(() => variante.ConfigurarTrazabilidad(true, false, true, -1));

        variante.ConfigurarTrazabilidad(true, false, true, 15);
        Assert.True(variante.ControlaLote);
        Assert.True(variante.ControlaFechaVencimiento);
        Assert.Equal(15, variante.DiasAlertaVencimiento);
    }

    [Fact]
    public void Configuracion_invalida_de_trazabilidad_no_muta_politica_vigente()
    {
        var variante = new ProductoVariante();
        variante.ConfigurarTrazabilidad(true, true, true, 30);

        Assert.Throws<InvalidOperationException>(() => variante.ConfigurarTrazabilidad(false, true, true, 5));

        Assert.True(variante.ControlaLote);
        Assert.True(variante.ControlaNumeroSerie);
        Assert.True(variante.ControlaFechaVencimiento);
        Assert.Equal(30, variante.DiasAlertaVencimiento);
        Assert.True(variante.RequiereTrazabilidad);
    }

    [Fact]
    public void Dias_alerta_solo_aplica_con_vencimiento_y_fallo_no_muta_configuracion()
    {
        var variante = new ProductoVariante();
        variante.ConfigurarTrazabilidad(true, true, true, 3650);

        Assert.Throws<InvalidOperationException>(() => variante.ConfigurarTrazabilidad(true, true, false, 10));

        Assert.True(variante.ControlaLote);
        Assert.True(variante.ControlaNumeroSerie);
        Assert.True(variante.ControlaFechaVencimiento);
        Assert.Equal(3650, variante.DiasAlertaVencimiento);
    }

    [Fact]
    public void Lote_normaliza_codigo_y_rechaza_vencimiento_anterior_a_fabricacion()
    {
        var lote = new LoteInventario { ProductoVarianteId = 11 };

        Assert.Throws<InvalidOperationException>(() => lote.ConfigurarIdentidad(
            "L-1",
            new DateTime(2026, 8, 10),
            new DateTime(2026, 8, 9),
            true));

        lote.ConfigurarIdentidad(" lote-001 ", new DateTime(2026, 8, 10), new DateTime(2027, 8, 10), true);
        Assert.Equal("LOTE-001", lote.Codigo);
        Assert.False(lote.EstaVencido(new DateTime(2026, 8, 17)));
        Assert.True(lote.VenceDentroDe(new DateTime(2027, 8, 1), 10));
    }

    [Fact]
    public void Lote_exige_vencimiento_solo_cuando_la_politica_lo_requiere()
    {
        var lote = new LoteInventario { ProductoVarianteId = 11 };

        Assert.Throws<InvalidOperationException>(() => lote.ConfigurarIdentidad("L-1", null, null, true));

        lote.ConfigurarIdentidad("L-1", null, null, false);
        Assert.Null(lote.FechaVencimiento);
    }

    [Fact]
    public void Lote_vigencia_respeta_limites_de_fecha_y_alerta_cero()
    {
        var hoy = new DateTime(2026, 8, 17, 23, 59, 59, DateTimeKind.Utc);
        var lote = new LoteInventario { ProductoVarianteId = 11 };
        lote.ConfigurarIdentidad("L-HOY", null, hoy, true);

        Assert.False(lote.EstaVencido(hoy));
        Assert.True(lote.VenceDentroDe(hoy, 0));
        Assert.True(lote.EstaVencido(hoy.AddDays(1)));
        Assert.False(lote.VenceDentroDe(hoy.AddDays(1), 30));
    }

    [Fact]
    public void Lote_reconfiguracion_invalida_no_deja_identidad_parcialmente_mutada()
    {
        var lote = new LoteInventario { ProductoVarianteId = 11 };
        var fabricacionOriginal = new DateTime(2026, 8, 1);
        var vencimientoOriginal = new DateTime(2026, 9, 1);
        lote.ConfigurarIdentidad("LOTE-ORIGINAL", fabricacionOriginal, vencimientoOriginal, true);

        Assert.Throws<InvalidOperationException>(() => lote.ConfigurarIdentidad(
            "LOTE-NUEVO",
            new DateTime(2026, 10, 1),
            new DateTime(2026, 9, 30),
            true));

        Assert.Equal("LOTE-ORIGINAL", lote.Codigo);
        Assert.Equal(fabricacionOriginal.Date, lote.FechaFabricacion);
        Assert.Equal(vencimientoOriginal.Date, lote.FechaVencimiento);
    }

    [Fact]
    public void Lote_codigo_invalido_no_borra_identidad_previa()
    {
        var lote = new LoteInventario { ProductoVarianteId = 11 };
        var vencimientoOriginal = new DateTime(2027, 1, 31);
        lote.ConfigurarIdentidad("LOTE-SEGURO", null, vencimientoOriginal, true);

        Assert.Throws<ArgumentException>(() => lote.ConfigurarIdentidad("   ", null, new DateTime(2028, 1, 31), true));

        Assert.Equal("LOTE-SEGURO", lote.Codigo);
        Assert.Equal(vencimientoOriginal.Date, lote.FechaVencimiento);
    }

    [Fact]
    public void Lote_alerta_negativa_falla_sin_mutar_vigencia()
    {
        var lote = new LoteInventario { ProductoVarianteId = 11 };
        var vencimiento = new DateTime(2026, 9, 1);
        lote.ConfigurarIdentidad("L-ALERTA", null, vencimiento, true);

        Assert.Throws<ArgumentOutOfRangeException>(() => lote.VenceDentroDe(new DateTime(2026, 8, 17), -1));

        Assert.Equal("L-ALERTA", lote.Codigo);
        Assert.Equal(vencimiento.Date, lote.FechaVencimiento);
        Assert.True(lote.Activo);
    }

    [Fact]
    public void Serie_normaliza_identidad_y_lifecycle_es_fail_closed()
    {
        var serie = new SerieInventario { ProductoVarianteId = 11 };
        serie.ConfigurarIdentidad(" sn-001 ");

        Assert.Equal("SN-001", serie.NumeroSerie);
        Assert.Equal(EstadoSerieInventario.Disponible, serie.Estado);

        serie.Reservar();
        Assert.Equal(EstadoSerieInventario.Reservada, serie.Estado);
        Assert.Throws<InvalidOperationException>(() => serie.Reservar());

        serie.MarcarEnTransito();
        Assert.Equal(EstadoSerieInventario.EnTransito, serie.Estado);
        serie.RecibirDeTransito();
        Assert.Equal(EstadoSerieInventario.Disponible, serie.Estado);
        serie.Vender();
        Assert.Equal(EstadoSerieInventario.Vendida, serie.Estado);
        Assert.Throws<InvalidOperationException>(() => serie.DarDeBaja());
        Assert.Equal(EstadoSerieInventario.Vendida, serie.Estado);
    }

    [Fact]
    public void Serie_transicion_invalida_no_muta_estado()
    {
        var serie = new SerieInventario { ProductoVarianteId = 11 };
        serie.ConfigurarIdentidad("SN-TRANSITO");
        serie.MarcarEnTransito();

        Assert.Throws<InvalidOperationException>(() => serie.Vender());
        Assert.Equal(EstadoSerieInventario.EnTransito, serie.Estado);

        serie.RecibirDeTransito();
        serie.Reservar();
        serie.DarDeBaja();

        Assert.Throws<InvalidOperationException>(() => serie.LiberarReserva());
        Assert.Equal(EstadoSerieInventario.Baja, serie.Estado);
    }

    [Fact]
    public void Serie_identidad_invalida_no_borra_numero_previo()
    {
        var serie = new SerieInventario { ProductoVarianteId = 11 };
        serie.ConfigurarIdentidad("SN-SEGURA");

        Assert.Throws<ArgumentException>(() => serie.ConfigurarIdentidad("  "));

        Assert.Equal("SN-SEGURA", serie.NumeroSerie);
        Assert.Equal(EstadoSerieInventario.Disponible, serie.Estado);
    }

    [Fact]
    public void Serie_lote_no_persistido_falla_sin_reemplazar_vinculo_existente()
    {
        var serie = new SerieInventario { ProductoVarianteId = 11 };
        var loteValido = new LoteInventario { Id = 8, ProductoVarianteId = 11 };
        serie.VincularLote(loteValido);

        var loteNoPersistido = new LoteInventario { ProductoVarianteId = 11 };
        Assert.Throws<InvalidOperationException>(() => serie.VincularLote(loteNoPersistido));

        Assert.Equal(8, serie.LoteInventarioId);
        Assert.Same(loteValido, serie.LoteInventario);
    }

    [Fact]
    public void Serie_no_puede_vincularse_a_lote_de_otra_variante()
    {
        var serie = new SerieInventario { ProductoVarianteId = 11 };
        var loteAjeno = new LoteInventario { Id = 7, ProductoVarianteId = 12 };

        Assert.Throws<InvalidOperationException>(() => serie.VincularLote(loteAjeno));
        Assert.Null(serie.LoteInventarioId);

        var loteValido = new LoteInventario { Id = 8, ProductoVarianteId = 11 };
        serie.VincularLote(loteValido);
        Assert.Equal(8, serie.LoteInventarioId);
    }
}
