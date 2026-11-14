using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class N17ConteoInventarioDomainTests
{
    [Fact]
    public void Lifecycle_Completo_CierraDiferenciasSinConvertirConteoEnAutoridadStock()
    {
        var conteo = CrearConteo();
        var detalle = Assert.Single(conteo.Detalles);
        var inicio = DateTime.UtcNow;

        conteo.Iniciar(11, inicio);
        detalle.RegistrarConteo(8, 12, inicio.AddMinutes(5));
        conteo.Cerrar(13, inicio.AddMinutes(10));
        conteo.Aprobar(14, inicio.AddMinutes(15));

        Assert.Equal(EstadoConteoInventario.Aprobado, conteo.Estado);
        Assert.Equal(-2, detalle.Diferencia);
        Assert.Equal(10, detalle.StockEsperadoSnapshot);
        Assert.Equal(8, detalle.CantidadContada);
        Assert.Equal(-2, conteo.DiferenciaNeta);
    }

    [Fact]
    public void Iniciar_ConClaveFisicaDuplicada_FallaCerrado()
    {
        var conteo = CrearConteo();
        conteo.Detalles.Add(CrearDetalle(stockEsperado: 4));

        Assert.Throws<InvalidOperationException>(() => conteo.Iniciar(1, DateTime.UtcNow));

        Assert.Equal(EstadoConteoInventario.Borrador, conteo.Estado);
        Assert.Null(conteo.FechaInicio);
        Assert.Null(conteo.IniciadoPorUsuarioId);
    }

    [Fact]
    public void Iniciar_SinSnapshotFisicoMaterializado_FallaCerrado()
    {
        var conteo = CrearConteo();
        conteo.Detalles = new List<ConteoInventarioDetalle>
        {
            new()
            {
                ProductoVarianteId = 20,
                AlmacenId = 10,
                UbicacionAlmacenId = 30
            }
        };

        Assert.Throws<InvalidOperationException>(() => conteo.Iniciar(1, DateTime.UtcNow));

        Assert.Equal(EstadoConteoInventario.Borrador, conteo.Estado);
        Assert.Null(conteo.FechaInicio);
        Assert.False(Assert.Single(conteo.Detalles).SnapshotMaterializado);
    }

    [Fact]
    public void Cerrar_ConLineaPendiente_FallaCerradoYSinMutarAuditoria()
    {
        var conteo = CrearConteo();
        conteo.Iniciar(1, DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => conteo.Cerrar(2, DateTime.UtcNow));

        Assert.Equal(EstadoConteoInventario.EnProceso, conteo.Estado);
        Assert.Null(conteo.FechaCierre);
        Assert.Null(conteo.CerradoPorUsuarioId);
        Assert.Null(Assert.Single(conteo.Detalles).Diferencia);
    }

    [Fact]
    public void ConteoCiego_DebeDeclararseExplicitamenteAntesDeIniciar()
    {
        var conteo = CrearConteo();
        conteo.Tipo = TipoConteoInventario.Ciego;
        conteo.EsCiego = false;

        Assert.Throws<InvalidOperationException>(() => conteo.Iniciar(1, DateTime.UtcNow));

        Assert.Equal(EstadoConteoInventario.Borrador, conteo.Estado);
    }

    [Fact]
    public void PorUbicacion_ExigeUbicacionYLineasDelMismoScope()
    {
        var conteo = CrearConteo();
        conteo.Tipo = TipoConteoInventario.PorUbicacion;
        conteo.UbicacionAlmacenId = 50;

        var detalle = Assert.Single(conteo.Detalles);
        detalle.UbicacionAlmacenId = 51;

        Assert.Throws<InvalidOperationException>(() => conteo.Iniciar(1, DateTime.UtcNow));
        Assert.Equal(EstadoConteoInventario.Borrador, conteo.Estado);
    }

    [Fact]
    public void RegistrarConteo_ValorInvalido_NoMutaCapturaAnterior()
    {
        var detalle = CrearDetalle(stockEsperado: 10);
        var fecha = DateTime.UtcNow;
        detalle.RegistrarConteo(9, 4, fecha);

        Assert.Throws<ArgumentOutOfRangeException>(() => detalle.RegistrarConteo(-1, 5, fecha.AddMinutes(1)));

        Assert.Equal(9, detalle.CantidadContada);
        Assert.Equal(4, detalle.ContadoPorUsuarioId);
        Assert.Equal(fecha, detalle.FechaConteo);
    }

    [Fact]
    public void VincularAjuste_SinDiferenciaReal_FallaCerrado()
    {
        var detalle = CrearDetalle(stockEsperado: 5);
        detalle.RegistrarConteo(5, 3, DateTime.UtcNow);
        detalle.CerrarDiferencia();

        Assert.Throws<InvalidOperationException>(() => detalle.VincularAjuste(100));
        Assert.Null(detalle.AjusteInventarioId);
    }

    private static ConteoInventario CrearConteo()
    {
        return new ConteoInventario
        {
            Numero = "CNT-0001",
            Tipo = TipoConteoInventario.General,
            AlmacenId = 10,
            Detalles = new List<ConteoInventarioDetalle>
            {
                CrearDetalle(stockEsperado: 10)
            }
        };
    }

    private static ConteoInventarioDetalle CrearDetalle(int stockEsperado)
    {
        var detalle = new ConteoInventarioDetalle
        {
            ProductoVarianteId = 20,
            AlmacenId = 10,
            UbicacionAlmacenId = 30
        };
        detalle.MaterializarSnapshot(stockEsperado);
        return detalle;
    }
}
