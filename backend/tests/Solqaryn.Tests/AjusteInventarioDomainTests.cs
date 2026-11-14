using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class AjusteInventarioDomainTests
{
    [Fact]
    public void NuevoAjuste_IniciaEnBorradorYSinSnapshots()
    {
        var ajuste = new AjusteInventario();
        var detalle = new AjusteInventarioDetalle
        {
            ProductoId = 1,
            CantidadObjetivo = 8
        };

        Assert.Equal(EstadoAjusteInventario.Borrador, ajuste.Estado);
        Assert.False(detalle.TieneSnapshotConfirmacion);
        Assert.Null(detalle.CantidadAnteriorSnapshot);
        Assert.Null(detalle.DiferenciaSnapshot);
        Assert.Null(detalle.ImpactoCostoSnapshot);
    }

    [Fact]
    public void MaterializarConfirmacion_CapturaCantidadCostoYDiferencia()
    {
        var detalle = new AjusteInventarioDetalle
        {
            ProductoId = 1,
            CantidadObjetivo = 8
        };

        detalle.MaterializarConfirmacion(10, 25m);

        Assert.True(detalle.TieneSnapshotConfirmacion);
        Assert.Equal(10, detalle.CantidadAnteriorSnapshot);
        Assert.Equal(8, detalle.CantidadNuevaSnapshot);
        Assert.Equal(-2, detalle.DiferenciaSnapshot);
        Assert.Equal(25m, detalle.CostoUnitarioSnapshot);
        Assert.Equal(-50m, detalle.ImpactoCostoSnapshot);
    }

    [Fact]
    public void MaterializarConfirmacion_SinDiferenciaReal_FallaCerrado()
    {
        var detalle = new AjusteInventarioDetalle
        {
            ProductoId = 1,
            CantidadObjetivo = 10
        };

        Assert.Throws<InvalidOperationException>(() => detalle.MaterializarConfirmacion(10, 20m));
    }

    [Fact]
    public void MaterializarConfirmacion_ConCantidadObjetivoNegativa_FallaCerrado()
    {
        var detalle = new AjusteInventarioDetalle
        {
            ProductoId = 1,
            CantidadObjetivo = -1
        };

        Assert.Throws<InvalidOperationException>(() => detalle.MaterializarConfirmacion(10, 20m));
    }

    [Fact]
    public void Confirmar_ConDetalleMaterializado_RegistraAuditoriaDeConfirmableEntity()
    {
        var fecha = new DateTime(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc);
        var ajuste = CrearAjusteValido();
        var detalle = new AjusteInventarioDetalle
        {
            ProductoId = 1,
            CantidadObjetivo = 8
        };
        detalle.MaterializarConfirmacion(10, 25m);
        ajuste.Detalles.Add(detalle);

        ajuste.Confirmar(7, "qa", fecha);

        Assert.Equal(EstadoAjusteInventario.Confirmado, ajuste.Estado);
        Assert.Equal(fecha, ajuste.FechaConfirmacion);
        Assert.Equal(7, ajuste.ConfirmadoPorUsuarioId);
        Assert.Equal("qa", ajuste.ConfirmadoPorNombreUsuario);
    }

    [Fact]
    public void Confirmar_SinDetalles_FallaCerrado()
    {
        var ajuste = CrearAjusteValido();

        Assert.Throws<InvalidOperationException>(() =>
            ajuste.Confirmar(1, "qa", DateTime.UtcNow));
    }

    [Fact]
    public void Confirmar_ConDetalleSinSnapshot_FallaCerrado()
    {
        var ajuste = CrearAjusteValido();
        ajuste.Detalles.Add(new AjusteInventarioDetalle
        {
            ProductoId = 1,
            CantidadObjetivo = 8
        });

        Assert.Throws<InvalidOperationException>(() =>
            ajuste.Confirmar(1, "qa", DateTime.UtcNow));
    }

    [Fact]
    public void Confirmar_SinNumeroOMotivo_FallaCerrado()
    {
        var detalle = new AjusteInventarioDetalle
        {
            ProductoId = 1,
            CantidadObjetivo = 8
        };
        detalle.MaterializarConfirmacion(10, 25m);

        var sinNumero = new AjusteInventario { Motivo = "Conteo físico" };
        sinNumero.Detalles.Add(detalle);
        Assert.Throws<InvalidOperationException>(() =>
            sinNumero.Confirmar(1, "qa", DateTime.UtcNow));

        var sinMotivo = new AjusteInventario { NumeroAjuste = "AJ-1" };
        sinMotivo.Detalles.Add(detalle);
        Assert.Throws<InvalidOperationException>(() =>
            sinMotivo.Confirmar(1, "qa", DateTime.UtcNow));
    }

    [Fact]
    public void Anular_Confirmado_RegistraMotivoYAuditoria()
    {
        var ajuste = CrearAjusteValido();
        var detalle = new AjusteInventarioDetalle
        {
            ProductoId = 1,
            CantidadObjetivo = 12
        };
        detalle.MaterializarConfirmacion(10, 30m);
        ajuste.Detalles.Add(detalle);
        ajuste.Confirmar(1, "qa", DateTime.UtcNow);
        var fecha = DateTime.UtcNow.AddMinutes(1);

        ajuste.Anular(2, "admin", "Conteo corregido", fecha);

        Assert.Equal(EstadoAjusteInventario.Anulado, ajuste.Estado);
        Assert.Equal("Conteo corregido", ajuste.MotivoAnulacion);
        Assert.Equal(fecha, ajuste.FechaAnulacion);
        Assert.Equal(2, ajuste.AnuladoPorUsuarioId);
        Assert.Equal("admin", ajuste.AnuladoPorNombreUsuario);
    }

    private static AjusteInventario CrearAjusteValido() => new()
    {
        NumeroAjuste = "AJ-0001",
        Motivo = "Conteo físico"
    };
}
