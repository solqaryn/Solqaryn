using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests.Domain;

public class NotaCreditoProveedorTests
{
    private static NotaCreditoProveedor CrearValida() => new()
    {
        NumeroNotaCredito = "NC-001",
        ProveedorId = 7,
        FacturaProveedorId = 19,
        ProveedorNombreSnapshot = "Proveedor Demo",
        Moneda = "HNL",
        FechaEmisionUtc = new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc),
        Motivo = "Ajuste comercial",
        SubtotalCredito = 100m,
        ImpuestoCredito = 15m
    };

    [Fact]
    public void Registrar_DocumentoValido_CambiaARegistradaYConservaAuditoria()
    {
        var nota = CrearValida();
        var fecha = new DateTime(2026, 8, 21, 12, 0, 0, DateTimeKind.Utc);

        nota.Registrar(5, " Analista ", fecha);

        Assert.Equal(EstadoNotaCreditoProveedor.Registrada, nota.Estado);
        Assert.False(nota.EsEditable);
        Assert.Equal(fecha, nota.FechaRegistroUtc);
        Assert.Equal(5, nota.RegistradaPorUsuarioId);
        Assert.Equal("Analista", nota.RegistradaPorNombreSnapshot);
        Assert.Equal(115m, nota.TotalCredito);
    }

    [Fact]
    public void Registrar_ExigeFacturaProveedorAcreditada()
    {
        var nota = CrearValida();
        nota.FacturaProveedorId = 0;

        var error = Assert.Throws<InvalidOperationException>(() => nota.Registrar(5, "Analista", DateTime.UtcNow));

        Assert.Contains("factura de proveedor", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EstadoNotaCreditoProveedor.Borrador, nota.Estado);
    }

    [Fact]
    public void Registrar_PermiteDevolucionNoInformada()
    {
        var nota = CrearValida();
        nota.DevolucionProveedorId = null;

        nota.Registrar(5, "Analista", DateTime.UtcNow);

        Assert.Equal(EstadoNotaCreditoProveedor.Registrada, nota.Estado);
    }

    [Fact]
    public void Registrar_RechazaDevolucionInvalidaCuandoSeInforma()
    {
        var nota = CrearValida();
        nota.DevolucionProveedorId = 0;

        Assert.Throws<InvalidOperationException>(() => nota.Registrar(5, "Analista", DateTime.UtcNow));
    }

    [Fact]
    public void Registrar_RechazaCreditoSinValorFinanciero()
    {
        var nota = CrearValida();
        nota.SubtotalCredito = 0m;
        nota.ImpuestoCredito = 0m;

        var error = Assert.Throws<InvalidOperationException>(() => nota.Registrar(5, "Analista", DateTime.UtcNow));

        Assert.Contains("mayor que cero", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Anular_SoloPermiteDocumentoRegistrado()
    {
        var borrador = CrearValida();
        Assert.Throws<InvalidOperationException>(() => borrador.Anular(5, "Corrección", DateTime.UtcNow));

        var nota = CrearValida();
        nota.Registrar(5, "Analista", DateTime.UtcNow);
        var fecha = DateTime.UtcNow.AddMinutes(1);

        nota.Anular(9, " Documento sustituido ", fecha);

        Assert.Equal(EstadoNotaCreditoProveedor.Anulada, nota.Estado);
        Assert.Equal(fecha, nota.FechaAnulacionUtc);
        Assert.Equal(9, nota.AnuladaPorUsuarioId);
        Assert.Equal("Documento sustituido", nota.MotivoAnulacion);
    }
}
