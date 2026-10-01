using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class N31CotizacionDomainTests
{
    private static readonly DateTime FechaUtc = new(2026, 8, 23, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Lifecycle_Permite_Borrador_Enviada_Aceptada_Convertida()
    {
        var cotizacion = CrearValida();

        cotizacion.Enviar(10, FechaUtc);
        Assert.Equal(EstadoCotizacion.Enviada, cotizacion.Estado);
        Assert.Equal(FechaUtc, cotizacion.FechaEnvioUtc);

        cotizacion.Aceptar(11, FechaUtc.AddMinutes(1));
        Assert.Equal(EstadoCotizacion.Aceptada, cotizacion.Estado);

        cotizacion.Convertir(12, FechaUtc.AddMinutes(2));
        Assert.Equal(EstadoCotizacion.Convertida, cotizacion.Estado);
    }

    [Fact]
    public void Lifecycle_Permite_Enviada_Rechazada()
    {
        var cotizacion = CrearValida();
        cotizacion.Enviar(10, FechaUtc);

        cotizacion.Rechazar(11, "Cliente no acepta condiciones", FechaUtc.AddMinutes(1));

        Assert.Equal(EstadoCotizacion.Rechazada, cotizacion.Estado);
        Assert.Equal("Cliente no acepta condiciones", cotizacion.MotivoRechazo);
    }

    [Fact]
    public void Lifecycle_Rechaza_Transiciones_Invalidas()
    {
        var borrador = CrearValida();
        Assert.Throws<InvalidOperationException>(() => borrador.Aceptar(1, FechaUtc));
        Assert.Throws<InvalidOperationException>(() => borrador.Rechazar(1, "x", FechaUtc));
        Assert.Throws<InvalidOperationException>(() => borrador.Convertir(1, FechaUtc));

        var enviada = CrearValida();
        enviada.Enviar(1, FechaUtc);
        Assert.Throws<InvalidOperationException>(() => enviada.Enviar(1, FechaUtc));
        Assert.Throws<InvalidOperationException>(() => enviada.Convertir(1, FechaUtc));

        enviada.Rechazar(2, "No", FechaUtc.AddMinutes(1));
        Assert.Throws<InvalidOperationException>(() => enviada.Aceptar(2, FechaUtc.AddMinutes(2)));
        Assert.Throws<InvalidOperationException>(() => enviada.Convertir(2, FechaUtc.AddMinutes(2)));
    }

    [Fact]
    public void Detalle_Exige_Cantidad_Positiva_Y_Precio_No_Negativo()
    {
        var detalle = new CotizacionDetalle { ProductoId = 1 };

        Assert.Throws<ArgumentOutOfRangeException>(() => detalle.EstablecerValores(0, 10m));
        Assert.Throws<ArgumentOutOfRangeException>(() => detalle.EstablecerValores(1, -0.01m));

        detalle.EstablecerValores(2, 15m);
        Assert.Equal(30m, detalle.Total);
    }

    [Fact]
    public void Enviar_Exige_Cliente_Snapshot_Y_Detalle_Valido()
    {
        var sinCliente = CrearValida();
        sinCliente.ClienteId = 0;
        Assert.Throws<InvalidOperationException>(() => sinCliente.Enviar(1, FechaUtc));

        var sinSnapshot = CrearValida();
        sinSnapshot.ClienteNombreSnapshot = " ";
        Assert.Throws<InvalidOperationException>(() => sinSnapshot.Enviar(1, FechaUtc));

        var sinDetalle = CrearValida();
        sinDetalle.Detalles.Clear();
        Assert.Throws<InvalidOperationException>(() => sinDetalle.Enviar(1, FechaUtc));
    }

    [Fact]
    public void Transiciones_Exigen_Fecha_Utc_Y_Usuario_Valido()
    {
        var cotizacion = CrearValida();

        Assert.Throws<ArgumentOutOfRangeException>(() => cotizacion.Enviar(0, FechaUtc));
        Assert.Throws<ArgumentException>(() => cotizacion.Enviar(1, DateTime.SpecifyKind(FechaUtc, DateTimeKind.Local)));
    }

    [Fact]
    public void Transiciones_Con_Fecha_NoUtc_No_Mutan_Estado_Ni_Metadata()
    {
        var fechaLocal = DateTime.SpecifyKind(FechaUtc, DateTimeKind.Local);

        var borrador = CrearValida();
        Assert.Throws<ArgumentException>(() => borrador.Enviar(1, fechaLocal));
        Assert.Equal(EstadoCotizacion.Borrador, borrador.Estado);
        Assert.Null(borrador.FechaEnvioUtc);
        Assert.Null(borrador.EnviadaPorUsuarioId);

        var enviadaParaAceptar = CrearValida();
        enviadaParaAceptar.Enviar(1, FechaUtc);
        Assert.Throws<ArgumentException>(() => enviadaParaAceptar.Aceptar(2, fechaLocal));
        Assert.Equal(EstadoCotizacion.Enviada, enviadaParaAceptar.Estado);
        Assert.Null(enviadaParaAceptar.FechaAceptacionUtc);
        Assert.Null(enviadaParaAceptar.AceptadaPorUsuarioId);

        var enviadaParaRechazar = CrearValida();
        enviadaParaRechazar.Enviar(1, FechaUtc);
        Assert.Throws<ArgumentException>(() => enviadaParaRechazar.Rechazar(2, "No acepta", fechaLocal));
        Assert.Equal(EstadoCotizacion.Enviada, enviadaParaRechazar.Estado);
        Assert.Null(enviadaParaRechazar.FechaRechazoUtc);
        Assert.Null(enviadaParaRechazar.RechazadaPorUsuarioId);
        Assert.Null(enviadaParaRechazar.MotivoRechazo);

        var aceptada = CrearValida();
        aceptada.Enviar(1, FechaUtc);
        aceptada.Aceptar(2, FechaUtc.AddMinutes(1));
        Assert.Throws<ArgumentException>(() => aceptada.Convertir(3, fechaLocal));
        Assert.Equal(EstadoCotizacion.Aceptada, aceptada.Estado);
        Assert.Null(aceptada.FechaConversionUtc);
        Assert.Null(aceptada.ConvertidaPorUsuarioId);
    }

    [Fact]
    public void DuplicarComoBorrador_Crea_Nuevo_Agregado_Sin_Mutar_Original()
    {
        var original = CrearValida();
        original.Enviar(1, FechaUtc);
        original.Aceptar(2, FechaUtc.AddMinutes(1));

        var copia = original.DuplicarComoBorrador();

        Assert.Equal(EstadoCotizacion.Aceptada, original.Estado);
        Assert.Equal(EstadoCotizacion.Borrador, copia.Estado);
        Assert.Equal(0, copia.Id);
        Assert.Null(copia.FechaEnvioUtc);
        Assert.Null(copia.FechaAceptacionUtc);
        Assert.Single(copia.Detalles);
        Assert.Equal(0, copia.Detalles.Single().Id);
        Assert.Equal(original.Detalles.Single().ProductoId, copia.Detalles.Single().ProductoId);
        Assert.Equal(original.Detalles.Single().Cantidad, copia.Detalles.Single().Cantidad);
        Assert.Equal(original.Detalles.Single().PrecioUnitario, copia.Detalles.Single().PrecioUnitario);
    }

    private static Cotizacion CrearValida()
    {
        var detalle = new CotizacionDetalle
        {
            ProductoId = 100,
            ProductoVarianteId = 200,
            ProductoSkuSnapshot = "SKU-100",
            ProductoNombreSnapshot = "Producto demo"
        };
        detalle.EstablecerValores(2, 50m);

        var cotizacion = new Cotizacion
        {
            ClienteId = 10,
            ClienteNombreSnapshot = "Cliente demo"
        };
        cotizacion.Detalles.Add(detalle);
        return cotizacion;
    }
}
