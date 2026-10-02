using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Solqaryn.Application.DTOs;
using Solqaryn.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class QuestPdfFacturaPerfilesServiceTests
{
    private readonly QuestPdfFacturaPerfilesService _service;

    public QuestPdfFacturaPerfilesServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        _service = new QuestPdfFacturaPerfilesService(
            configuration,
            new Mock<ILogger<QuestPdfFacturaPerfilesService>>().Object);
    }

    [Theory]
    [InlineData(FacturaFormatoPdf.A4, 595.28, 841.89)]
    [InlineData(FacturaFormatoPdf.Carta, 612.00, 792.00)]
    [InlineData(FacturaFormatoPdf.Legal, 612.00, 1008.00)]
    [InlineData(FacturaFormatoPdf.Oficio, 612.00, 936.00)]
    [InlineData(FacturaFormatoPdf.A5, 419.53, 595.28)]
    public async Task GenerarPdfAsync_Respeta_Dimensiones_De_Papel(
        FacturaFormatoPdf formato,
        double anchoEsperado,
        double altoEsperado)
    {
        var pdf = await _service.GenerarPdfAsync(CrearFactura(), formato);

        AssertPdfValido(pdf);
        var (ancho, alto) = LeerMediaBox(pdf);
        Assert.InRange(ancho, anchoEsperado - 2.5, anchoEsperado + 2.5);
        Assert.InRange(alto, altoEsperado - 2.5, altoEsperado + 2.5);
    }

    [Theory]
    [InlineData(FacturaFormatoPdf.Pos58, 164.41)]
    [InlineData(FacturaFormatoPdf.Pos80, 226.77)]
    public async Task GenerarPdfAsync_Respeta_Ancho_Termico_Y_No_Impone_A4_O_297Mm(
        FacturaFormatoPdf formato,
        double anchoEsperado)
    {
        var pdf = await _service.GenerarPdfAsync(CrearFactura(), formato);

        AssertPdfValido(pdf);
        var (ancho, alto) = LeerMediaBox(pdf);
        Assert.InRange(ancho, anchoEsperado - 2.5, anchoEsperado + 2.5);
        Assert.True(alto > ancho, $"El rollo continuo debe crecer verticalmente. MediaBox: {ancho} × {alto}.");
        Assert.True(alto < 800, $"El ticket corto no debe convertirse en una hoja A4/297 mm. MediaBox: {ancho} × {alto}.");
        Assert.NotInRange(alto, 839, 845);
    }

    [Theory]
    [InlineData(FacturaFormatoPdf.Pos58)]
    [InlineData(FacturaFormatoPdf.Pos80)]
    public async Task GenerarPdfAsync_Altura_Termica_Crece_Con_El_Contenido(FacturaFormatoPdf formato)
    {
        var corta = CrearFactura();
        corta.Detalles = [corta.Detalles[0]];
        corta.DescuentosAplicados = [];
        corta.ImpuestosAplicados = [];
        corta.Observaciones = null;

        var larga = CrearFactura();
        larga.Detalles = Enumerable.Range(1, 18)
            .Select(indice => new FacturaDetalleDto
            {
                ProductoNombre = $"Producto térmico número {indice} con descripción extensa para validar crecimiento",
                ProductoMarca = "Storefront",
                ProductoModelo = $"POS-{indice:00}",
                Cantidad = indice % 3 + 1,
                PrecioUnitario = 125.50m,
                Subtotal = 251m
            })
            .ToList();

        var pdfCorto = await _service.GenerarPdfAsync(corta, formato);
        var pdfLargo = await _service.GenerarPdfAsync(larga, formato);
        var (_, altoCorto) = LeerMediaBox(pdfCorto);
        var (_, altoLargo) = LeerMediaBox(pdfLargo);

        Assert.True(
            altoLargo > altoCorto + 100,
            $"La altura continua debe responder al contenido. Corto={altoCorto}; largo={altoLargo}.");
    }

    [Theory]
    [InlineData("Acme", "AC")]
    [InlineData("Mi Empresa", "ME")]
    [InlineData("SOLQARYN", "SO")]
    [InlineData("", "SQ")]
    public void Monograma_Fallback_Se_Deriva_De_La_Empresa_Y_No_De_Una_Marca_Global(
        string nombreEmpresa,
        string esperado)
    {
        var metodo = typeof(QuestPdfFacturaPerfilesService).GetMethod(
            "ConstruirMonogramaEmpresa",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(metodo);
        var actual = Assert.IsType<string>(metodo!.Invoke(null, [nombreEmpresa]));
        Assert.Equal(esperado, actual);
    }

    [Fact]
    public async Task GenerarPdfAsync_Sin_Formato_Conserva_A4_Como_Oficial()
    {
        var pdf = await _service.GenerarPdfAsync(CrearFactura());

        var (ancho, alto) = LeerMediaBox(pdf);
        Assert.InRange(ancho, 592.5, 598.0);
        Assert.InRange(alto, 839.0, 844.5);
    }

    [Theory]
    [InlineData("letter", FacturaFormatoPdf.Carta)]
    [InlineData("58 mm", FacturaFormatoPdf.Pos58)]
    [InlineData("ticket-80", FacturaFormatoPdf.Pos80)]
    [InlineData("OFICIO", FacturaFormatoPdf.Oficio)]
    public void Catalogo_Acepta_Alias_Controlados(string valor, FacturaFormatoPdf esperado)
    {
        var valido = FacturaFormatoPdfCatalogo.TryParse(valor, out var formato);

        Assert.True(valido);
        Assert.Equal(esperado, formato);
    }

    [Fact]
    public void Catalogo_Rechaza_Formato_Desconocido()
    {
        Assert.False(FacturaFormatoPdfCatalogo.TryParse("papel-inexistente", out _));
        Assert.Equal(7, FacturaFormatoPdfCatalogo.ObtenerTodos().Count);
    }

    private static void AssertPdfValido(byte[] pdf)
    {
        Assert.True(pdf.Length > 3_000, $"PDF demasiado pequeño: {pdf.Length} bytes.");
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
    }

    private static (double Ancho, double Alto) LeerMediaBox(byte[] pdf)
    {
        var contenido = Encoding.Latin1.GetString(pdf);
        var coincidencia = Regex.Match(
            contenido,
            @"/MediaBox\s*\[\s*0(?:\.0+)?\s+0(?:\.0+)?\s+([0-9]+(?:\.[0-9]+)?)\s+([0-9]+(?:\.[0-9]+)?)\s*\]",
            RegexOptions.CultureInvariant);

        Assert.True(coincidencia.Success, "El PDF no expuso un MediaBox verificable.");
        return (
            double.Parse(coincidencia.Groups[1].Value, CultureInfo.InvariantCulture),
            double.Parse(coincidencia.Groups[2].Value, CultureInfo.InvariantCulture));
    }

    private static FacturaDto CrearFactura() => new()
    {
        Id = 900,
        VentaId = 800,
        NumeroVentaOrigen = "VEN-F6-0001",
        NumeroFactura = "FAC-F6-0001",
        FechaEmision = new DateTime(2026, 7, 27, 12, 30, 0, DateTimeKind.Utc),
        Estado = "Emitida",
        EmpresaNombre = "Storefront dev",
        EmpresaRTN = "08019000000000",
        EmpresaTelefono = "+504 9999-9999",
        EmpresaCorreo = "dev@solqaryn.invalid",
        EmpresaDireccion = "Dirección de prueba extensa para validar ajuste de texto y perfiles de impresión.",
        EmpresaEslogan = "Tecnología y accesorios",
        EmpresaTextoFactura = "Gracias por su compra.",
        EmpresaTextoLegal = "Conserve este documento para cualquier consulta.",
        EmpresaCopyright = "© Storefront dev",
        ClienteNombre = "Cliente de certificación con nombre deliberadamente extenso",
        ClienteIdentidadORTN = "0801199012345",
        ClienteTelefono = "33425030",
        ClienteCorreo = "cliente@example.invalid",
        ClienteDireccion = "Barrio de prueba, calle principal, edificio número 25.",
        VendedorNombreUsuario = "e2e_admin",
        GeneradaPorNombreUsuario = "e2e_admin",
        ImporteBruto = 1_150m,
        Subtotal = 1_050m,
        Descuento = 100m,
        Impuesto = 157.50m,
        ImpuestoIncluido = 0m,
        ImpuestoAdicional = 157.50m,
        Total = 1_207.50m,
        MetodoPago = "Efectivo",
        EstadoPago = "Pagado",
        Observaciones = "Documento generado para validar Carta, Legal, Oficio, A4, A5, POS 58 y POS 80.",
        Detalles =
        [
            new() { ProductoNombre = "Cargador rápido USB-C", ProductoMarca = "Marca extensa", ProductoModelo = "Modelo 65W Pro", Cantidad = 2, PrecioUnitario = 250m, Subtotal = 500m },
            new() { ProductoNombre = "Cable reforzado de alta velocidad", ProductoMarca = "Storefront", ProductoModelo = "USB-C a USB-C 2m", Cantidad = 3, PrecioUnitario = 150m, Subtotal = 450m },
            new() { ProductoNombre = "Adaptador compacto", ProductoMarca = "Storefront", ProductoModelo = "HDMI 4K", Cantidad = 1, PrecioUnitario = 200m, Subtotal = 200m }
        ],
        DescuentosAplicados =
        [
            new() { DescuentoId = 1, Nombre = "Promoción Fase 6", Codigo = "F6-100", Monto = 100m }
        ],
        ImpuestosAplicados =
        [
            new() { ImpuestoId = 1, Nombre = "ISV", Tasa = 15m, Monto = 157.50m, IncluidoEnPrecio = false }
        ]
    };
}
