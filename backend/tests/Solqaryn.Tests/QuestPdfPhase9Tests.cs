using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Solqaryn.Application.DTOs;
using Solqaryn.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using UglyToad.PdfPig;
using Xunit;

namespace Solqaryn.Tests;

public sealed class QuestPdfPhase9Tests
{
    private readonly QuestPdfFacturaPerfilesService _service;

    public QuestPdfPhase9Tests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        _service = new QuestPdfFacturaPerfilesService(
            configuration,
            new Mock<ILogger<QuestPdfFacturaPerfilesService>>().Object);
    }

    [Fact]
    public async Task Perfiles_Caracteres_Paginacion_Y_Termicos_Son_Estructuralmente_Validos()
    {
        var unicode = CrearFactura("unicode", 5, unicode: true);
        foreach (var formato in Enum.GetValues<FacturaFormatoPdf>())
        {
            var pdf = await _service.GenerarPdfAsync(unicode, formato);
            var manifest = InspeccionarPdf(pdf, FacturaFormatoPdfCatalogo.ObtenerCodigo(formato), "unicode", 0, 0);
            Assert.True(manifest.Bytes > 3_000);
            Assert.Contains("FAC-F9-UNICODE", manifest.TextoNormalizado, StringComparison.Ordinal);
            Assert.Contains("José Núñez", manifest.TextoNormalizado, StringComparison.Ordinal);
            Assert.Contains("Peña & Compañía", manifest.TextoNormalizado, StringComparison.Ordinal);
            Assert.Contains("Crédito", manifest.TextoNormalizado, StringComparison.Ordinal);
            Assert.Contains("Información", manifest.TextoNormalizado, StringComparison.Ordinal);
            Assert.Contains("Dirección", manifest.TextoNormalizado, StringComparison.Ordinal);
            Assert.Contains("¿Gracias por su compra?", manifest.TextoNormalizado, StringComparison.Ordinal);
            Assert.Contains("¡Vuelva pronto!", manifest.TextoNormalizado, StringComparison.Ordinal);
            Assert.Contains("©", manifest.TextoNormalizado, StringComparison.Ordinal);
            Assert.Contains("×", manifest.TextoNormalizado, StringComparison.Ordinal);
            Assert.DoesNotContain("□", manifest.TextoNormalizado, StringComparison.Ordinal);
        }

        foreach (var formato in new[] { FacturaFormatoPdf.A4, FacturaFormatoPdf.Carta, FacturaFormatoPdf.Legal, FacturaFormatoPdf.Oficio, FacturaFormatoPdf.A5 })
        {
            var pdf = await _service.GenerarPdfAsync(CrearFactura("larga", 72), formato);
            using var doc = PdfDocument.Open(pdf);
            Assert.True(doc.NumberOfPages >= 2, $"{formato} debe forzar paginación con 72 líneas.");
            var texto = NormalizarTexto(string.Join(" ", doc.GetPages().Select(x => x.Text)));
            Assert.Contains("Producto largo 01", texto, StringComparison.Ordinal);
            Assert.Contains("Producto largo 72", texto, StringComparison.Ordinal);
            Assert.Contains("Página 1 de", texto, StringComparison.Ordinal);
        }

        foreach (var formato in new[] { FacturaFormatoPdf.Pos58, FacturaFormatoPdf.Pos80 })
        {
            var corta = await _service.GenerarPdfAsync(CrearFactura("corta", 3), formato);
            var larga = await _service.GenerarPdfAsync(CrearFactura("larga", 30), formato);
            var corto = InspeccionarPdf(corta, FacturaFormatoPdfCatalogo.ObtenerCodigo(formato), "corta", 0, 0);
            var largo = InspeccionarPdf(larga, FacturaFormatoPdfCatalogo.ObtenerCodigo(formato), "larga", 0, 0);
            Assert.True(largo.Paginas[0].Alto > corto.Paginas[0].Alto + 100);
            var anchoEsperado = formato == FacturaFormatoPdf.Pos58 ? 164.41 : 226.77;
            Assert.InRange(corto.Paginas[0].Ancho, anchoEsperado - 2.5, anchoEsperado + 2.5);
            Assert.InRange(largo.Paginas[0].Ancho, anchoEsperado - 2.5, anchoEsperado + 2.5);
        }
    }

    [Fact]
    public async Task Generacion_Concurrente_No_Mezcla_Facturas()
    {
        var tareas = Enumerable.Range(1, 12).Select(async indice =>
        {
            var factura = CrearFactura($"concurrente-{indice:00}", 8);
            factura.NumeroFactura = $"FAC-F9-C{indice:00}";
            factura.ClienteNombre = $"Cliente concurrente {indice:00}";
            var pdf = await _service.GenerarPdfAsync(factura, indice % 2 == 0 ? FacturaFormatoPdf.A4 : FacturaFormatoPdf.Pos80);
            var manifest = InspeccionarPdf(pdf, "concurrente", indice.ToString(CultureInfo.InvariantCulture), 0, 0);
            Assert.Contains(factura.NumeroFactura, manifest.TextoNormalizado, StringComparison.Ordinal);
            Assert.Contains(factura.ClienteNombre, manifest.TextoNormalizado, StringComparison.Ordinal);
            return manifest.TextSha256;
        });

        var hashes = await Task.WhenAll(tareas);
        Assert.Equal(12, hashes.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task Captura_Artefactos_Y_Manifest_Cuando_CI_Lo_Solicita()
    {
        var destino = Environment.GetEnvironmentVariable("SOLQARYN_PHASE9_PDF_ARTIFACT_DIR");
        if (string.IsNullOrWhiteSpace(destino))
            return;

        Directory.CreateDirectory(destino);
        var manifests = new List<PdfManifest>();
        foreach (var formato in Enum.GetValues<FacturaFormatoPdf>())
        {
            foreach (var caso in new[] { "corta", "larga", "unicode" })
            {
                var factura = caso switch
                {
                    "corta" => CrearFactura(caso, 3),
                    "larga" => CrearFactura(caso, formato is FacturaFormatoPdf.Pos58 or FacturaFormatoPdf.Pos80 ? 30 : 72),
                    _ => CrearFactura(caso, 5, unicode: true)
                };

                var stopwatch = Stopwatch.StartNew();
                var memoriaAntes = GC.GetTotalMemory(false);
                var pdf = await _service.GenerarPdfAsync(factura, formato);
                var memoriaDespues = GC.GetTotalMemory(false);
                stopwatch.Stop();

                var codigo = FacturaFormatoPdfCatalogo.ObtenerCodigo(formato);
                var nombre = $"factura-{codigo}-{caso}.pdf";
                await File.WriteAllBytesAsync(Path.Combine(destino, nombre), pdf);
                manifests.Add(InspeccionarPdf(
                    pdf,
                    codigo,
                    caso,
                    stopwatch.Elapsed.TotalMilliseconds,
                    memoriaDespues - memoriaAntes));
            }
        }

        var reporte = GenerarReportePdf();
        await File.WriteAllBytesAsync(Path.Combine(destino, "reporte-administrativo.pdf"), reporte.Contenido);
        manifests.Add(InspeccionarPdf(reporte.Contenido, "reporte", "administrativo", 0, 0));

        var envelope = new Phase9Envelope
        {
            QuestPdfInformationalVersion = typeof(QuestPDF.Settings).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown",
            License = QuestPDF.Settings.License.ToString(),
            Settings = CapturarSettings(),
            PdfUaConfiguredInProduct = false,
            QrProductivoObservado = false,
            EmbeddedAttachmentsObservados = false,
            Manifests = manifests
        };

        var json = JsonSerializer.Serialize(envelope, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(destino, "manifest.json"), json, new UTF8Encoding(false));
        Console.WriteLine("PHASE9_MANIFEST_BEGIN");
        Console.WriteLine(json);
        Console.WriteLine("PHASE9_MANIFEST_END");
    }

    private static ArchivoDescargableDto GenerarReportePdf()
    {
        var metodo = typeof(ReporteAdministrativoService).GetMethod(
            "CrearPdf",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(metodo);

        string[] encabezados = ["Nombre", "Rol", "Estado", "Descripción"];
        IReadOnlyList<string[]> filas =
        [
            ["José Núñez", "Administrador", "Habilitado", "Información – crédito – © – ×"],
            ["Peña & Compañía", "Operador", "Habilitado", "Dirección: Tegucigalpa — Honduras"]
        ];
        var resultado = metodo!.Invoke(null, [encabezados, filas, "phase9-reporte"]);
        return Assert.IsType<ArchivoDescargableDto>(resultado);
    }

    private static Dictionary<string, string> CapturarSettings()
    {
        var nombres = new[] { "UseSystemFonts", "ThrowOnMissingFontFamilies", "ThrowOnMissingTextGlyphs" };
        var resultado = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var nombre in nombres)
        {
            var propiedad = typeof(QuestPDF.Settings).GetProperty(nombre, BindingFlags.Public | BindingFlags.Static);
            resultado[nombre] = propiedad is null ? "NOT_AVAILABLE" : Convert.ToString(propiedad.GetValue(null), CultureInfo.InvariantCulture) ?? "null";
        }
        return resultado;
    }

    private static PdfManifest InspeccionarPdf(byte[] pdf, string formato, string caso, double elapsedMs, long memoryDelta)
    {
        Assert.True(pdf.Length > 1_000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));

        using var document = PdfDocument.Open(pdf);
        var paginas = document.GetPages()
            .Select(page => new PdfPageManifest
            {
                Numero = page.Number,
                Ancho = Math.Round(Convert.ToDouble(page.Width, CultureInfo.InvariantCulture), 2),
                Alto = Math.Round(Convert.ToDouble(page.Height, CultureInfo.InvariantCulture), 2)
            })
            .ToList();
        var texto = NormalizarTexto(string.Join(" ", document.GetPages().Select(page => page.Text)));
        var latin1 = Encoding.Latin1.GetString(pdf);
        var fonts = Regex.Matches(latin1, @"/BaseFont\s*/([^\s/<>]+)", RegexOptions.CultureInvariant)
            .Select(x => x.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        return new PdfManifest
        {
            Formato = formato,
            Caso = caso,
            Bytes = pdf.Length,
            PageCount = document.NumberOfPages,
            Paginas = paginas,
            TextSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texto))).ToLowerInvariant(),
            TextoNormalizado = texto,
            Fonts = fonts,
            StructTreeRootObserved = latin1.Contains("/StructTreeRoot", StringComparison.Ordinal),
            MarkInfoObserved = latin1.Contains("/MarkInfo", StringComparison.Ordinal),
            PdfUaMarkerObserved = latin1.Contains("PDF/UA", StringComparison.OrdinalIgnoreCase),
            ElapsedMs = Math.Round(elapsedMs, 2),
            ManagedBytesDeltaApprox = memoryDelta
        };
    }

    private static string NormalizarTexto(string texto) =>
        Regex.Replace(texto, @"\s+", " ", RegexOptions.CultureInvariant).Trim();

    private static FacturaDto CrearFactura(string caso, int lineas, bool unicode = false)
    {
        var factura = new FacturaDto
        {
            Id = 9900,
            VentaId = 9800,
            NumeroVentaOrigen = "VEN-F9-0001",
            NumeroFactura = unicode ? "FAC-F9-UNICODE" : $"FAC-F9-{caso.ToUpperInvariant()}",
            FechaEmision = new DateTime(2026, 10, 7, 15, 0, 0, DateTimeKind.Utc),
            Estado = "Emitida",
            EmpresaNombre = unicode ? "Peña & Compañía" : "SOLQARYN Empresa Demo",
            EmpresaRTN = "08019000000000",
            EmpresaTelefono = "+504 9999-9999",
            EmpresaCorreo = "facturacion@example.invalid",
            EmpresaDireccion = unicode ? "Dirección Central – Tegucigalpa — Honduras" : "Tegucigalpa, Honduras",
            EmpresaEslogan = unicode ? "Información, crédito y tecnología" : "Tecnología empresarial",
            EmpresaTextoFactura = unicode ? "¿Gracias por su compra? ¡Vuelva pronto!" : "Gracias por su compra.",
            EmpresaTextoLegal = unicode ? "Información legal © SOLQARYN – uso interno." : "Documento interno.",
            EmpresaCopyright = unicode ? "© SOLQARYN" : "Todos los derechos reservados.",
            ClienteNombre = unicode ? "José Núñez" : "Cliente Fase 9",
            ClienteIdentidadORTN = "0801199012345",
            ClienteTelefono = "33425030",
            ClienteCorreo = "cliente@example.invalid",
            ClienteDireccion = unicode ? "Dirección: Colonia Centro – casa 10." : "Colonia Centro",
            VendedorNombreUsuario = "phase9_admin",
            GeneradaPorNombreUsuario = "phase9_admin",
            ImporteBruto = lineas * 125m,
            Subtotal = lineas * 100m,
            Descuento = 25m,
            Impuesto = lineas * 15m,
            ImpuestoIncluido = 0m,
            ImpuestoAdicional = lineas * 15m,
            Total = lineas * 115m - 25m,
            MetodoPago = unicode ? "Crédito" : "Efectivo",
            EstadoPago = "Pagado",
            Observaciones = unicode ? "Información especial: ñ Ñ ¿ ¡ © × % – — '." : $"Caso {caso} con {lineas} líneas.",
            Detalles = Enumerable.Range(1, lineas)
                .Select(i => new FacturaDetalleDto
                {
                    ProductoNombre = unicode && i == 1 ? "Información técnica – Cable × 2" : $"Producto largo {i:00}",
                    ProductoMarca = unicode ? "Peña" : "SOLQARYN",
                    ProductoModelo = $"M-{i:00}",
                    Cantidad = i % 3 + 1,
                    PrecioUnitario = 125m,
                    Subtotal = 125m,
                    VarianteColor = i % 2 == 0 ? "Azul" : "Negro",
                    VarianteSku = $"F9-{i:000}"
                })
                .ToList(),
            DescuentosAplicados =
            [
                new FacturaDescuentoAplicadoDto { DescuentoId = 1, Nombre = "Promoción Fase 9", Codigo = "F9-25", Monto = 25m }
            ],
            ImpuestosAplicados =
            [
                new FacturaImpuestoAplicadoDto { ImpuestoId = 1, Nombre = "ISV", Tasa = 15m, Monto = lineas * 15m, IncluidoEnPrecio = false }
            ]
        };
        return factura;
    }

    private sealed class Phase9Envelope
    {
        public string QuestPdfInformationalVersion { get; set; } = string.Empty;
        public string License { get; set; } = string.Empty;
        public Dictionary<string, string> Settings { get; set; } = [];
        public bool PdfUaConfiguredInProduct { get; set; }
        public bool QrProductivoObservado { get; set; }
        public bool EmbeddedAttachmentsObservados { get; set; }
        public List<PdfManifest> Manifests { get; set; } = [];
    }

    private sealed class PdfManifest
    {
        public string Formato { get; set; } = string.Empty;
        public string Caso { get; set; } = string.Empty;
        public int Bytes { get; set; }
        public int PageCount { get; set; }
        public List<PdfPageManifest> Paginas { get; set; } = [];
        public string TextSha256 { get; set; } = string.Empty;
        public string TextoNormalizado { get; set; } = string.Empty;
        public List<string> Fonts { get; set; } = [];
        public bool StructTreeRootObserved { get; set; }
        public bool MarkInfoObserved { get; set; }
        public bool PdfUaMarkerObserved { get; set; }
        public double ElapsedMs { get; set; }
        public long ManagedBytesDeltaApprox { get; set; }
    }

    private sealed class PdfPageManifest
    {
        public int Numero { get; set; }
        public double Ancho { get; set; }
        public double Alto { get; set; }
    }
}
