using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.ValueObjects;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N110CosteoDominioTests
{
    [Fact]
    public void PromedioPonderado_con_asignacion_valida_conserva_cantidad_y_costo()
    {
        var asignacion = AsignacionCostoInventario.Crear(4, 125.50m);
        var resultado = ResultadoCosteoInventario.Crear(
            MetodoCosteoInventario.PromedioPonderado, 4, new[] { asignacion });

        Assert.Equal(4, resultado.Cantidad);
        Assert.Equal(502.00m, resultado.CostoTotal);
        Assert.Equal(125.50m, resultado.CostoUnitarioPromedio);
    }

    [Fact]
    public void Resultado_rechaza_cantidad_asignada_distinta_de_la_valorada()
    {
        var asignacion = AsignacionCostoInventario.Crear(3, 10m);

        Assert.Throws<ArgumentException>(() => ResultadoCosteoInventario.Crear(
            MetodoCosteoInventario.PromedioPonderado, 4, new[] { asignacion }));
    }

    [Fact]
    public void Fifo_requiere_capa_durable_en_cada_asignacion()
    {
        var sinCapa = AsignacionCostoInventario.Crear(2, 30m);

        Assert.Throws<ArgumentException>(() => ResultadoCosteoInventario.Crear(
            MetodoCosteoInventario.FIFO, 2, new[] { sinCapa }));
    }

    [Fact]
    public void Fifo_admite_consumo_multicapa_y_calcula_costo_total()
    {
        var asignaciones = new[]
        {
            AsignacionCostoInventario.Crear(2, 10m, 101),
            AsignacionCostoInventario.Crear(3, 12m, 102)
        };

        var resultado = ResultadoCosteoInventario.Crear(
            MetodoCosteoInventario.FIFO, 5, asignaciones);

        Assert.Equal(56m, resultado.CostoTotal);
        Assert.Equal(11.2m, resultado.CostoUnitarioPromedio);
    }

    [Fact]
    public void Politica_es_version_temporal_y_no_se_puede_cerrar_dos_veces()
    {
        var inicio = new DateTime(2026, 8, 17, 20, 0, 0, DateTimeKind.Utc);
        var politica = PoliticaCosteoInventario.Crear(
            1, MetodoCosteoInventario.PromedioPonderado, inicio, "Cutover N1.10");

        politica.Cerrar(inicio.AddHours(1));

        Assert.False(politica.EstaVigente);
        Assert.Equal(inicio.AddHours(1), politica.VigenteHastaUtc);
        Assert.Throws<InvalidOperationException>(() => politica.Cerrar(inicio.AddHours(2)));
    }

    [Fact]
    public void CapaFifo_consume_y_restaura_sin_superar_saldo_original()
    {
        var fecha = new DateTime(2026, 8, 17, 20, 0, 0, DateTimeKind.Utc);
        var capa = CapaCostoInventario.CrearDesdeMovimiento(
            10, 2, 4, 99, 8, 25m, fecha, "n110-fifo-99");

        capa.Consumir(3);
        Assert.Equal(5, capa.CantidadRestante);
        Assert.Equal(125m, capa.ValorRestante);

        capa.Restaurar(2);
        Assert.Equal(7, capa.CantidadRestante);
        Assert.Throws<InvalidOperationException>(() => capa.Restaurar(2));
    }

    [Fact]
    public void CapaApertura_no_inventa_movimiento_historico()
    {
        var fecha = new DateTime(2026, 8, 17, 20, 0, 0, DateTimeKind.Utc);
        var capa = CapaCostoInventario.CrearApertura(
            10, 2, null, 5, 31.25m, fecha, "n110-cutover-10", "Stock preexistente certificado");

        Assert.True(capa.EsApertura);
        Assert.Null(capa.MovimientoInventarioOrigenId);
        Assert.Null(capa.CapaCostoOrigenId);
        Assert.Equal("Stock preexistente certificado", capa.MotivoApertura);
    }

    [Fact]
    public void CapaTransferida_conserva_linaje_de_capa_origen()
    {
        var fecha = new DateTime(2026, 8, 17, 20, 0, 0, DateTimeKind.Utc);
        var capa = CapaCostoInventario.CrearDesdeMovimiento(
            10, 3, 7, 120, 2, 45m, fecha, "n110-transfer-120", capaCostoOrigenId: 101);

        Assert.False(capa.EsApertura);
        Assert.Equal(120, capa.MovimientoInventarioOrigenId);
        Assert.Equal(101, capa.CapaCostoOrigenId);
    }

    [Fact]
    public void CostoEstandar_calcula_variacion_sin_perder_costo_real()
    {
        var fecha = new DateTime(2026, 8, 17, 20, 0, 0, DateTimeKind.Utc);
        var estandar = CostoEstandarInventario.Crear(10, 100m, fecha, "Costo estándar inicial");

        var variacion = estandar.CalcularVariacion(110m, 3);

        Assert.Equal(30m, variacion);
        Assert.True(estandar.EstaVigente);
    }

    [Fact]
    public void AsignacionPersistibleFifo_exige_capa_y_conserva_costo_historico()
    {
        var asignacion = AsignacionCostoMovimientoInventario.Crear(
            movimientoInventarioId: 200,
            productoVarianteId: 10,
            metodo: MetodoCosteoInventario.FIFO,
            cantidad: 2,
            costoUnitario: 45m,
            correlationId: "n110-salida-200",
            capaCostoInventarioId: 101);

        Assert.Equal(90m, asignacion.CostoTotal);
        Assert.Equal(101, asignacion.CapaCostoInventarioId);
        Assert.Throws<ArgumentException>(() => AsignacionCostoMovimientoInventario.Crear(
            201, 10, MetodoCosteoInventario.FIFO, 1, 45m, "n110-salida-201"));
    }

    [Fact]
    public void VariacionEstandar_preserva_signo_real_menos_estandar()
    {
        var sobre = VariacionCostoEstandarInventario.Crear(
            300, 10, 20, 4, 115m, 100m, "n110-var-300");
        var bajo = VariacionCostoEstandarInventario.Crear(
            301, 10, 20, 2, 90m, 100m, "n110-var-301");

        Assert.Equal(60m, sobre.VariacionTotal);
        Assert.Equal(-20m, bajo.VariacionTotal);
    }
}
